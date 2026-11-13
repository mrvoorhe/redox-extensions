using System;

using Decal.Adapter;

using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.Listeners.Monitors;

/// <summary>
/// Counts rewards received from "X gives you ..." chat lines while /re track is on.  Started and reported together
/// with <see cref="BankingTracker"/>.
/// </summary>
public class RewardTracker : IDisposable
{
    private readonly RewardTally _tally = new RewardTally();
    private bool _trackingEnabled;
    private DateTime _startTime;

    public RewardTracker()
    {
        REPlugin.Instance.Events.Decal.ChatBoxMessage += Decal_ChatBoxMessage;
    }

    public bool IsTracking => this._trackingEnabled;

    public void Dispose()
    {
        REPlugin.Instance.Events.Decal.ChatBoxMessage -= Decal_ChatBoxMessage;
    }

    /// <summary>
    /// Starts (or resets) counting.
    /// </summary>
    public void StartTracking()
    {
        this._tally.Reset();
        this._startTime = DateTime.Now;
        this._trackingEnabled = true;
    }

    public void Report(Action<string> writer)
    {
        if (!this._trackingEnabled)
        {
            writer?.Invoke("[Rewards] Tracking not enabled.  Use /re track first.");
            return;
        }

        this._tally.WriteReport(DateTime.Now - this._startTime, writer);
    }

    private void Decal_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
    {
        if (!this._trackingEnabled)
        {
            return;
        }

        if (!ChatParsingUtilities.TryParseGivesYou(e.Text, out _, out var count, out var itemName))
        {
            return;
        }

        if (RewardCatalog.TryMatch(itemName, out var kind))
        {
            this._tally.Add(kind, count);
        }
    }
}
