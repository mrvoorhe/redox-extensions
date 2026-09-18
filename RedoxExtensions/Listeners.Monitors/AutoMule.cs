using System;
using System.Collections.Generic;
using System.Linq;

using Decal.Adapter.Wrappers;
using NiceIO;

using RedoxExtensions.Continuations;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Data;
using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Listeners.Monitors
{
    /// <summary>
    /// Watches for a configured "mule" character to come within range and, while in peace mode,
    /// automatically gives them every inventory item whose name contains a configured substring.
    ///
    /// Config lives in an "AutoMule" directory next to the plugin DLL.  One "&lt;MuleName&gt;.txt" file
    /// per mule; each line is a case-insensitive substring (see <see cref="RedoxLib.AutoMule.MuleConfig"/>).
    ///
    /// Everything here runs on the game thread (RenderFrame, EndGiveItem, and the delayed
    /// continuations all dispatch to the game thread), so no locking is needed - a single
    /// <see cref="_sessionActive"/> flag guards against starting a second give session.
    /// </summary>
    public class AutoMule : IDisposable
    {
        private const double DefaultRangeMeters = 3.0;
        private const double MinRangeMeters = 0.1;
        private const int ScanIntervalMilliseconds = 500;
        private const int GiveDelayMilliseconds = 100;

        private bool _enabled;
        private double _range = DefaultRangeMeters;
        private int _lastScanTick;

        private List<RedoxLib.AutoMule.MuleConfig> _configs = new List<RedoxLib.AutoMule.MuleConfig>();

        private bool _sessionActive;
        private string _sessionMuleName;
        private int _sessionTargetId;
        private List<int> _sessionItemIds;
        private int _sessionIndex;
        private int _sessionGivenCount;

        public AutoMule()
        {
            REPlugin.Instance.Events.Decal.RenderFrame += OnRenderFrame;
            REPlugin.Instance.Events.RE.EndGiveItem += OnEndGiveItem;
        }

        public bool Enabled
        {
            get { return this._enabled; }
        }

        public double Range
        {
            get { return this._range; }
            set { this._range = value < MinRangeMeters ? MinRangeMeters : value; }
        }

        public void Enable()
        {
            LoadConfigs();
            this._enabled = true;
            if (CurrentThreadContext.OnGameThread)
            {
                REPlugin.Instance.Chat.WriteLine("[RE] AutoMule on (range {0}m)", this._range);
            }
        }

        public void Disable()
        {
            AbortSession();
            this._enabled = false;
            if (CurrentThreadContext.OnGameThread)
            {
                REPlugin.Instance.Chat.WriteLine("[RE] AutoMule off");
            }
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.Decal.RenderFrame -= OnRenderFrame;
            REPlugin.Instance.Events.RE.EndGiveItem -= OnEndGiveItem;
        }

        private static NPath ConfigDirectory
        {
            get { return new Uri(typeof(REPlugin).Assembly.CodeBase).LocalPath.ToNPath().Parent.Combine("AutoMule"); }
        }

        private void LoadConfigs()
        {
            this._configs = new List<RedoxLib.AutoMule.MuleConfig>();

            var dir = ConfigDirectory;
            if (!dir.DirectoryExists())
            {
                REPlugin.Instance.Chat.WriteLine("[RE] AutoMule: config directory not found : {0}", dir);
                return;
            }

            foreach (var file in dir.Files("*.txt"))
            {
                this._configs.Add(RedoxLib.AutoMule.MuleConfig.Parse(file.FileNameWithoutExtension, file.ReadAllLines()));
            }

            REPlugin.Instance.Chat.WriteLine("[RE] AutoMule: loaded {0} mule config(s)", this._configs.Count);
        }

        private void OnRenderFrame(object sender, EventArgs e)
        {
            if (!this._enabled || this._sessionActive)
            {
                return;
            }

            var now = Environment.TickCount;
            if (now - this._lastScanTick < ScanIntervalMilliseconds)
            {
                return;
            }
            this._lastScanTick = now;

            if (!InPeace())
            {
                return;
            }

            foreach (var cfg in this._configs)
            {
                var target = Mag.Shared.Util.GetClosestObject(cfg.MuleName, false);
                if (target == null || target.ObjectClass != ObjectClass.Player)
                {
                    continue;
                }

                double distance;
                try
                {
                    distance = WorldUtilities.GetDistanceFromSelf(target);
                }
                catch
                {
                    continue;
                }

                if (distance > this._range)
                {
                    continue;
                }

                var itemIds = REPlugin.Instance.WorldFilter.GetInventory()
                    .Where(w => !w.IsEquippedByMe() && cfg.Matches(w.Name))
                    .Select(w => w.Id)
                    .ToList();

                if (itemIds.Count == 0)
                {
                    continue;
                }

                StartSession(cfg.MuleName, target.Id, itemIds);
                return;
            }
        }

        private void StartSession(string muleName, int targetId, List<int> itemIds)
        {
            this._sessionActive = true;
            this._sessionMuleName = muleName;
            this._sessionTargetId = targetId;
            this._sessionItemIds = itemIds;
            this._sessionIndex = 0;
            this._sessionGivenCount = 0;

            REPlugin.Instance.Chat.WriteLine("[RE] AutoMule: giving {0} item(s) to {1}", itemIds.Count, muleName);
            GiveNext();
        }

        private void GiveNext()
        {
            if (!this._sessionActive)
            {
                return;
            }

            if (!this._enabled)
            {
                ClearSession();
                return;
            }

            if (!InPeace() || !TargetWithinRange(this._sessionTargetId))
            {
                AbortSession();
                return;
            }

            if (this._sessionIndex >= this._sessionItemIds.Count)
            {
                EndSession();
                return;
            }

            REPlugin.Instance.Actions.GiveItem(this._sessionItemIds[this._sessionIndex], this._sessionTargetId);
        }

        private void OnEndGiveItem(object sender, EndGiveItemEventArgs e)
        {
            if (!this._sessionActive)
            {
                return;
            }

            // Only react to gives to our current mule, so a manual give elsewhere doesn't advance us.
            if (!string.Equals(e.TargetName, this._sessionMuleName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (e.Outcome == GiveItemOutcome.Successful)
            {
                this._sessionGivenCount++;
                this._sessionIndex++;
                DelayedContinuation.ContinueAfterDelayOnGameThread(s => GiveNext(), GiveDelayMilliseconds, null);
            }
            else
            {
                REPlugin.Instance.Debug.WriteLine("[AutoMule] Give failed ({0}); stopping session", e.Outcome);
                AbortSession();
            }
        }

        private bool InPeace()
        {
            return REPlugin.Instance.Actions.CombatMode == CombatState.Peace;
        }

        private bool TargetWithinRange(int targetId)
        {
            var wo = targetId.ToWorldObject();
            if (wo == null)
            {
                return false;
            }

            try
            {
                return WorldUtilities.GetDistanceFromSelf(wo) <= this._range;
            }
            catch
            {
                return false;
            }
        }

        private void EndSession()
        {
            var muleName = this._sessionMuleName;
            var given = this._sessionGivenCount;
            ClearSession();
            REPlugin.Instance.Chat.WriteLine("[RE] AutoMule: gave {0} item(s) to {1}", given, muleName);
        }

        private void AbortSession()
        {
            if (!this._sessionActive)
            {
                return;
            }

            ClearSession();
            REPlugin.Instance.Chat.WriteLine("[RE] AutoMule interrupted");
        }

        private void ClearSession()
        {
            this._sessionActive = false;
            this._sessionMuleName = null;
            this._sessionTargetId = 0;
            this._sessionItemIds = null;
            this._sessionIndex = 0;
            this._sessionGivenCount = 0;
        }
    }
}
