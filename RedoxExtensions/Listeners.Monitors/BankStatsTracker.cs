using System;
using System.Collections.Generic;
using System.Globalization;

using Decal.Adapter;

using RedoxExtensions.Actions;
using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.Listeners.Monitors
{
    /// <summary>
    /// Tracks bank balances over time so the user can gauge the income of a farming spot.
    ///
    /// Enabling tracking records a baseline snapshot of the bank (via a hidden /b query) and the
    /// current time.  A later report re-queries the bank and shows, for each changed balance, the
    /// current value, the total gained, and the rate of increase per hour.
    /// </summary>
    public class BankStatsTracker : IDisposable
    {
        // The /b response arrives as several chatbox lines with no terminator, so we collect for a
        // short window after issuing the query and then finalize.
        private const int CaptureWindowMilliseconds = 1000;

        private bool _trackingEnabled;
        private DateTime _startTime;
        private List<KeyValuePair<string, long>> _baseline;

        private bool _capturing;
        private List<KeyValuePair<string, long>> _captureBuffer;
        private Action<IList<KeyValuePair<string, long>>> _onCaptureComplete;

        public BankStatsTracker()
        {
            REPlugin.Instance.Events.Decal.ChatBoxMessage += Decal_ChatBoxMessage;
        }

        public bool IsTracking
        {
            get { return this._trackingEnabled; }
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.Decal.ChatBoxMessage -= Decal_ChatBoxMessage;
        }

        /// <summary>
        /// Starts (or resets) tracking.  Records a fresh baseline and the current time.
        /// </summary>
        public void StartTracking(Action<string> writer)
        {
            BeginCapture(balances =>
            {
                this._baseline = new List<KeyValuePair<string, long>>(balances);
                this._startTime = DateTime.Now;
                this._trackingEnabled = true;

                if (writer != null)
                {
                    writer("Bank tracking started.");
                }
            });
        }

        /// <summary>
        /// Re-queries the bank and reports the current balances and their rate of increase.
        /// </summary>
        public void Report(Action<string> writer)
        {
            if (!this._trackingEnabled)
            {
                if (writer != null)
                {
                    writer("Bank tracking not enabled.  Use /re track first.");
                }
                return;
            }

            BeginCapture(balances => WriteReport(balances, writer));
        }

        private void BeginCapture(Action<IList<KeyValuePair<string, long>>> onComplete)
        {
            if (this._capturing)
            {
                REPlugin.Instance.Debug.WriteLine("[BankStats] Ignoring request, a bank query is already in progress");
                return;
            }

            this._capturing = true;
            this._captureBuffer = new List<KeyValuePair<string, long>>();
            this._onCaptureComplete = onComplete;

            ACUtilities.ProcessNativeCommand("/b");

            REPlugin.Instance.Dispatch.LegacyGameThread.QueueDelayedAction(FinishCapture, CaptureWindowMilliseconds);
        }

        private void FinishCapture()
        {
            if (!this._capturing)
            {
                return;
            }

            this._capturing = false;

            var buffer = this._captureBuffer;
            var onComplete = this._onCaptureComplete;
            this._captureBuffer = null;
            this._onCaptureComplete = null;

            if (onComplete != null)
            {
                onComplete(buffer);
            }
        }

        private void Decal_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            if (!this._capturing)
            {
                return;
            }

            if (e.Text == null || e.Text.IndexOf(ChatParsingUtilities.BankMessagePrefix.Trim(), StringComparison.Ordinal) < 0)
            {
                return;
            }

            // Hide the response to our own query from the game chat window.
            e.Eat = true;

            string name;
            long amount;
            if (ChatParsingUtilities.TryParseBankBalanceLine(e.Text, out name, out amount))
            {
                this._captureBuffer.Add(new KeyValuePair<string, long>(name, amount));
            }
        }

        private void WriteReport(IList<KeyValuePair<string, long>> current, Action<string> writer)
        {
            if (writer == null)
            {
                return;
            }

            var elapsed = DateTime.Now - this._startTime;
            var hours = elapsed.TotalHours;

            var baselineByName = new Dictionary<string, long>();
            if (this._baseline != null)
            {
                foreach (var kvp in this._baseline)
                {
                    baselineByName[kvp.Key] = kvp.Value;
                }
            }

            writer(string.Format("[BankStats] Tracked {0}", FormatDuration(elapsed)));

            bool anyChanges = false;
            foreach (var kvp in current)
            {
                long baseAmount;
                baselineByName.TryGetValue(kvp.Key, out baseAmount);

                long delta = kvp.Value - baseAmount;
                if (delta == 0)
                {
                    continue;
                }

                anyChanges = true;
                double rate = hours > 0 ? delta / hours : 0;

                writer(string.Format(CultureInfo.InvariantCulture,
                    "{0}: {1:N0} ({2:+#,##0;-#,##0;0} total, {3:+#,##0;-#,##0;0}/hr)",
                    kvp.Key, kvp.Value, delta, rate));
            }

            if (!anyChanges)
            {
                writer("[BankStats] No changes since tracking started.");
            }
        }

        private static string FormatDuration(TimeSpan elapsed)
        {
            int totalMinutes = (int)elapsed.TotalMinutes;
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            if (hours <= 0)
            {
                return string.Format("{0}m", minutes);
            }

            return string.Format("{0}h {1}m", hours, minutes);
        }
    }
}
