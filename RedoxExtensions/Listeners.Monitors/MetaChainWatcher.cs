using System;
using System.Globalization;
using System.IO;
using System.Threading;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Actions;
using RedoxExtensions.Continuations;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.MetaChainWatch;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Listeners.Monitors;

/// <summary>
/// Times each VT meta from when it loads and, when one runs past its limit, sends it to its chain state, then runs
/// the fallback command if no other meta loads.  See docs/design/Feature-MetaChainWatch.md.
///
/// The config files (&lt;install&gt;\MetaChainWatch\Global.json and &lt;Character&gt;.json) are watched, and the
/// timeout checks and the .met chain state scan run on a timer thread.  Anything that touches the game is queued to
/// the game thread.  Every <see cref="MetaChainWatchState"/> call is made under <see cref="_lock"/>.
///
/// While on, the watch saves itself to &lt;install&gt;\MetaChainWatch\State\&lt;Character&gt;.json (on every change
/// and every <see cref="HeartbeatInterval"/>), and the first tick after the plugin starts resumes from it, so a crash
/// or relog in the middle of a chain doesn't turn the watch off.
///
/// Before the chain state jump (or the fallback) a timed out meta is recalled: VT stopped, nav cleared, peace mode,
/// then the recall gem (Halls of Introduction Gem by default) by its exact inventory name.  While recalling, every
/// tick reads the landcell on the game thread and the next tick checks it, until he is at the recall spot or the try
/// gives up.
/// </summary>
public class MetaChainWatcher : IDisposable
{
    private const string Prefix = "[RE] MetaChainWatch : ";
    private const int MillisecondsBeforeChainState = 1000;

    /// <summary>After /vt stop and peace mode, so VT and the combat mode switch are done before the gem use.</summary>
    private const int MillisecondsBeforeGemUse = 2000;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    /// <summary>How often the state is saved while on, even when nothing changed, so the run time is kept.</summary>
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    /// <summary>A saved state older than this isn't resumed: the chain is long over and someone may be playing by hand.</summary>
    internal static readonly TimeSpan ResumeMaxAge = TimeSpan.FromHours(4);

    private readonly object _lock = new();
    private readonly Timer _timer;
    private readonly MetaChainWatchState _state = new();
    private readonly WatchedFile _globalFile;
    private readonly WatchedFile _characterFile;
    private readonly string _stateFilePath;

    private MergedMetaChainWatchConfig _merged = MetaChainWatchRules.Merge(null, null);
    private bool _forceReload = true;
    private bool _resumePending = true;
    private int _savedRevision = -1;
    private DateTime _lastSaveAt = DateTime.MinValue;
    private bool _saveErrorLogged;
    private bool _disposed;
    private volatile bool _inPortalSpace;
    private MetaChainWatchLocation _lastLocation;

    public MetaChainWatcher()
        : this(CommandScheduler.InstallDirectory, REPlugin.Instance.CharacterFilter.Name)
    {
    }

    private MetaChainWatcher(string installDirectory, string characterName)
    {
        this._globalFile = new WatchedFile(MetaChainWatchRules.GlobalFilePath(installDirectory));
        this._characterFile = new WatchedFile(MetaChainWatchRules.CharacterFilePath(installDirectory, characterName));
        this._stateFilePath = MetaChainWatchRules.StateFilePath(installDirectory, characterName);
        REPlugin.Instance.Events.Decal.ChangePortalMode += Decal_ChangePortalMode;
        this._timer = new Timer(this.Tick, null, TimeSpan.FromSeconds(2), TickInterval);
    }

    public string GlobalFilePath => this._globalFile.Path;

    public string CharacterFilePath => this._characterFile.Path;

    public string StateFilePath => this._stateFilePath;

    /// <summary>The merged config as last loaded.  Treat it as read only; a reload replaces it.</summary>
    public MergedMetaChainWatchConfig MergedConfig
    {
        get
        {
            lock (this._lock)
            {
                return this._merged;
            }
        }
    }

    public void OnMetaLoaded(string metaName)
    {
        lock (this._lock)
        {
            this._state.OnMetaLoaded(metaName, DateTime.Now);
        }
    }

    public TurnOnResult TurnOn(double minutes)
    {
        lock (this._lock)
        {
            return this._state.TurnOn(minutes, DateTime.Now);
        }
    }

    public TurnOffResult TurnOff()
    {
        lock (this._lock)
        {
            return this._state.TurnOff();
        }
    }

    public MetaChainWatchStatus GetStatus()
    {
        lock (this._lock)
        {
            return this._state.GetStatus(DateTime.Now);
        }
    }

    /// <summary>Reload the config files on the next tick even if they don't look changed.</summary>
    public void RequestReload()
    {
        lock (this._lock)
        {
            this._forceReload = true;
        }
    }

    public void Dispose()
    {
        REPlugin.Instance.Events.Decal.ChangePortalMode -= Decal_ChangePortalMode;

        lock (this._lock)
        {
            // Keep the latest run time for the next login.  Holding the lock means no tick is half way through its
            // locked part.
            if (!this._disposed && !this._resumePending)
                this.SaveIfNeeded(DateTime.Now, force: true);

            this._disposed = true;
            this._timer.Dispose();
        }
    }

    private void Tick(object state)
    {
        MetaChainWatchAction action;
        MergedMetaChainWatchConfig merged;
        bool recalling;

        if (!Monitor.TryEnter(this._lock))
            return;

        try
        {
            if (this._disposed)
                return;

            if (this._resumePending)
                this.TryResume();

            this.ReloadIfChanged();

            merged = this._merged;
            action = this._inPortalSpace ? MetaChainWatchAction.None : this._state.Tick(DateTime.Now, merged, this._lastLocation);
            recalling = this._state.Phase == MetaChainWatchPhase.Recalling;

            if (!this._resumePending)
                this.SaveIfNeeded(DateTime.Now, force: false);
        }
        catch (Exception e)
        {
            REPlugin.Instance?.Error?.LogError(e);
            return;
        }
        finally
        {
            Monitor.Exit(this._lock);
        }

        try
        {
            // Where he is, for the next tick's arrival check.
            if (recalling)
                this.QueueLocationRead();

            switch (action.Kind)
            {
                case MetaChainWatchActionKind.Recall:
                    this.HandleRecall(action);
                    break;
                case MetaChainWatchActionKind.Timeout:
                    this.HandleTimeout(action, merged);
                    break;
                case MetaChainWatchActionKind.RunFallback:
                    QueueFallback(action);
                    break;
            }
        }
        catch (Exception e)
        {
            REPlugin.Instance?.Error?.LogError(e);
        }
    }

    /// <summary>
    /// Runs on the timer thread.  Stops VT, clears the nav and goes to peace mode, then uses the gem
    /// <see cref="MillisecondsBeforeGemUse"/> later, all on the game thread.
    /// </summary>
    private void HandleRecall(MetaChainWatchAction action)
    {
        var metaName = DisplayName(action.MetaName);
        if (action.RecallAttempt <= 1)
            QueueTimedOutChat(action);
        else
            QueueChat(string.Format(
                "{0}{1} no teleport within {2} s, trying {3} again",
                Prefix,
                metaName,
                (int)MetaChainWatchState.RecallWait.TotalSeconds,
                action.RecallGem));

        REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() =>
        {
            lock (this._lock)
            {
                if (this._disposed || !this._state.IsRecallCurrent(action.RecallId))
                    return;
            }

            VTActions.StopVT();
            VTActions.ClearNav();
            SimpleActions.CombatStatePeace();
            DelayedContinuation.ContinueAfterDelayOnGameThread(_ => this.UseRecallGem(action), MillisecondsBeforeGemUse, null);
        });
    }

    /// <summary>
    /// On the game thread.  Skips the gem when he is already at the recall spot, and finds it by its exact name, never
    /// a partial match.
    /// </summary>
    private void UseRecallGem(MetaChainWatchAction action)
    {
        var metaName = DisplayName(action.MetaName);
        string message;

        lock (this._lock)
        {
            if (this._disposed || !this._state.IsRecallCurrent(action.RecallId))
                return;

            var now = DateTime.Now;
            var landcell = REPlugin.Instance.Actions.Landcell;
            if (MetaChainWatchRules.IsAtRecallSpot(landcell, action.RecallLandblock))
            {
                this._state.OnRecallStarted(action.RecallId, MetaChainWatchRecallStart.AlreadyThere, now);
                message = string.Format("{0}{1} already at {2}, not using {3}", Prefix, metaName, MetaChainWatchRules.FormatLandcell(landcell), action.RecallGem);
            }
            else
            {
                var gem = action.RecallGem;
                var gems = WorldObjectExtensions.GetMatchInventoryItems(w => string.Equals(w.Name, gem, StringComparison.Ordinal));
                if (gems.Count == 0)
                {
                    this._state.OnRecallStarted(action.RecallId, MetaChainWatchRecallStart.GemMissing, now);
                    message = string.Format("{0}{1} no '{2}' in inventory, carrying on without the recall", Prefix, metaName, gem);
                }
                else
                {
                    // TODO: test in game that Decal's UseItem actually fires the gem (it hasn't been tried yet).  If it
                    // doesn't, or doesn't after /vt stop + peace from combat, switch to the documented `/mt use <gem>`.
                    REPlugin.Instance.Actions.UseItem(gems[0].Id, 0);
                    this._state.OnRecallStarted(action.RecallId, MetaChainWatchRecallStart.Used, now);
                    message = string.Format(
                        "{0}{1} using {2} (try {3} of {4})",
                        Prefix,
                        metaName,
                        gem,
                        action.RecallAttempt,
                        MetaChainWatchState.RecallTries);
                }
            }
        }

        REPlugin.Instance.Chat.WriteLine(message);
    }

    /// <summary>Reads where he is on the game thread, for the next tick.</summary>
    private void QueueLocationRead()
    {
        REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() =>
        {
            var location = new MetaChainWatchLocation
            {
                Landcell = REPlugin.Instance.Actions.Landcell,
                InPortalSpace = this._inPortalSpace,
                ReadAt = DateTime.Now,
            };

            lock (this._lock)
            {
                this._lastLocation = location;
            }
        });
    }

    private static void QueueTimedOutChat(MetaChainWatchAction action)
    {
        QueueChat(string.Format(
            "{0}{1} timed out after {2} min ({3})",
            Prefix,
            DisplayName(action.MetaName),
            FormatMinutes(action.Elapsed.TotalMinutes),
            action.IsOverride ? "override" : "default"));
    }

    /// <summary>Runs on the timer thread, outside the lock, so the .met read never holds up the game thread.</summary>
    private void HandleTimeout(MetaChainWatchAction action, MergedMetaChainWatchConfig merged)
    {
        var metaName = DisplayName(action.MetaName);
        switch (action.RecallOutcome)
        {
            case MetaChainWatchRecallOutcome.None:
                // No recall, so the timed out line hasn't been written yet.
                QueueTimedOutChat(action);
                break;
            case MetaChainWatchRecallOutcome.Arrived:
                QueueChat(string.Format(
                    "{0}{1} recalled, at {2}",
                    Prefix,
                    metaName,
                    action.RecallLandcell.HasValue ? MetaChainWatchRules.FormatLandcell(action.RecallLandcell.Value) : "the recall spot"));
                break;
            case MetaChainWatchRecallOutcome.TimedOut:
                QueueChat(string.Format(
                    "{0}{1} recall failed, not in {2} after {3} tries, carrying on",
                    Prefix,
                    metaName,
                    MetaChainWatchRules.FormatLandblock(action.RecallLandblock),
                    MetaChainWatchState.RecallTries));
                break;

            // AlreadyThere and GemMissing were written when the gem would have been used.
        }

        var chainState = action.ConfiguredChainState ?? FindChainStateInMetFile(action.MetaName, merged);

        MetaChainWatchAction next;
        lock (this._lock)
        {
            if (this._disposed)
                return;
            next = this._state.OnChainStateResult(chainState != null, chainState, DateTime.Now, merged);
        }

        if (chainState != null)
        {
            REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() =>
            {
                REPlugin.Instance.Chat.WriteLine("{0}{1} -> state '{2}'", Prefix, metaName, chainState);
                VTActions.StopVT();
                VTActions.ClearNav();
                DelayedContinuation.ContinueAfterDelayOnGameThread(
                    _ =>
                    {
                        VTActions.SetMetaState(chainState);
                        VTActions.EnableMeta();
                        VTActions.StartVT();
                    },
                    MillisecondsBeforeChainState,
                    null);
            });
        }

        if (next.Kind == MetaChainWatchActionKind.RunFallback)
            QueueFallback(next);
    }

    private static string FindChainStateInMetFile(string metaName, MergedMetaChainWatchConfig merged)
    {
        if (string.IsNullOrEmpty(metaName))
            return null;

        var path = Path.Combine(VTUtilities.VTankDirectory, metaName + ".met");
        try
        {
            if (!File.Exists(path))
                return null;
            return MetaChainWatchRules.FindChainState(File.ReadAllLines(path), merged.ChainStates);
        }
        catch (IOException e)
        {
            REPlugin.Instance?.Error?.LogError(e);
            return null;
        }
        catch (UnauthorizedAccessException e)
        {
            REPlugin.Instance?.Error?.LogError(e);
            return null;
        }
    }

    private static void QueueFallback(MetaChainWatchAction action)
    {
        var reason = action.Reason == MetaChainWatchAction.ReasonNoNextMeta && action.ChainState != null
            ? string.Format("{0} after state '{1}'", action.Reason, action.ChainState)
            : action.Reason;
        var message = string.Format("{0}{1} {2}, running fallback : {3}", Prefix, DisplayName(action.MetaName), reason, action.Command);
        var command = action.Command;

        REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() =>
        {
            REPlugin.Instance.Chat.WriteLine(message);
            VTActions.ClearNav();
            ACUtilities.ProcessArbitraryCommand(command);
        });
    }

    private static void QueueChat(string message)
    {
        REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() => REPlugin.Instance.Chat.WriteLine(message));
    }

    internal static string DisplayName(string metaName) => string.IsNullOrEmpty(metaName) ? "(no meta)" : metaName;

    internal static string FormatMinutes(double minutes) => minutes.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Under the lock, on the first tick.  Turns the watch back on from the saved state, if there is a recent one.
    /// A save that is too old or unreadable is left for <see cref="SaveIfNeeded"/> to delete.  If the file can't be
    /// read right now, tries again next tick.
    /// </summary>
    private void TryResume()
    {
        MetaChainWatchSnapshot saved;
        try
        {
            saved = MetaChainWatchFile.LoadState(this._stateFilePath);
        }
        catch (IOException)
        {
            return;
        }
        catch (Exception e) when (e is Newtonsoft.Json.JsonException or FormatException or InvalidCastException or UnauthorizedAccessException)
        {
            this._resumePending = false;
            QueueChat(string.Format("{0}saved state unreadable, staying off : {1} : {2}", Prefix, this._stateFilePath, e.Message));
            return;
        }

        this._resumePending = false;

        var now = DateTime.Now;
        var result = this._state.Resume(saved, now, ResumeMaxAge);
        if (result.Resumed)
        {
            var status = this._state.GetStatus(now);
            QueueChat(string.Format(
                "{0}resumed after restart, {1} min (meta: {2}, {3} min in, saved {4} min ago)",
                Prefix,
                FormatMinutes(status.MainMinutes),
                status.CurrentMeta ?? "none",
                FormatMinutes(status.Elapsed?.TotalMinutes ?? 0),
                FormatMinutes(Math.Max(0, result.Age.TotalMinutes))));

            // Save the resumed timing straight away.
            this._savedRevision = -1;
        }
        else if (result.Reason == ResumeResult.ReasonStale)
        {
            QueueChat(string.Format("{0}saved state from {1} min ago is too old, staying off", Prefix, FormatMinutes(result.Age.TotalMinutes)));
        }
        else if (result.Reason == ResumeResult.ReasonInvalid)
        {
            QueueChat(string.Format("{0}saved state is invalid, staying off : {1}", Prefix, this._stateFilePath));
        }
    }

    /// <summary>
    /// Under the lock.  Saves the state when it changed or the heartbeat is due, or deletes the saved state when the
    /// watch is off.  A failed save is retried next tick and logged once.
    /// </summary>
    private void SaveIfNeeded(DateTime now, bool force)
    {
        var revision = this._state.Revision;
        var snapshot = this._state.GetSnapshot(now);
        var due = revision != this._savedRevision || (snapshot != null && (force || now - this._lastSaveAt >= HeartbeatInterval));
        if (!due)
            return;

        try
        {
            if (snapshot == null)
                MetaChainWatchFile.DeleteState(this._stateFilePath);
            else
                MetaChainWatchFile.SaveState(this._stateFilePath, snapshot);

            this._savedRevision = revision;
            this._lastSaveAt = now;
            this._saveErrorLogged = false;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (!this._saveErrorLogged)
            {
                this._saveErrorLogged = true;
                REPlugin.Instance?.Error?.LogError(e);
            }
        }
    }

    /// <summary>Under the lock.  Reloads either file when its write time changed, then re-merges.</summary>
    private void ReloadIfChanged()
    {
        var force = this._forceReload;
        this._forceReload = false;

        var globalChanged = this._globalFile.ReloadIfChanged(force);
        var characterChanged = this._characterFile.ReloadIfChanged(force);
        if (!globalChanged && !characterChanged)
            return;

        this._merged = MetaChainWatchRules.Merge(this._globalFile.Data, this._characterFile.Data);

        if (this._globalFile.Data != null || this._characterFile.Data != null)
        {
            QueueChat(string.Format(
                "{0}config loaded : {1} timeout override{2}, fallback {3}",
                Prefix,
                this._merged.Timeouts.Count,
                this._merged.Timeouts.Count == 1 ? string.Empty : "s",
                this._merged.FallbackCommand));
        }
    }

    private void Decal_ChangePortalMode(object sender, ChangePortalModeEventArgs e)
    {
        var entered = e.Type == PortalEventType.EnterPortal;
        this._inPortalSpace = entered;

        lock (this._lock)
        {
            this._state.OnPortalSpace(entered, DateTime.Now);
        }
    }

    /// <summary>One config file and what was last loaded from it.</summary>
    private class WatchedFile
    {
        private DateTime? _loadedWriteTime;
        private bool _loadedOnce;

        public WatchedFile(string path)
        {
            this.Path = path;
        }

        public string Path { get; }

        /// <summary>Null when the file doesn't exist or is unreadable.</summary>
        public MetaChainWatchFileData Data { get; private set; }

        /// <summary>True when <see cref="Data"/> changed.</summary>
        public bool ReloadIfChanged(bool force)
        {
            DateTime? writeTime = File.Exists(this.Path) ? File.GetLastWriteTimeUtc(this.Path) : null;
            if (!force && this._loadedOnce && writeTime == this._loadedWriteTime)
                return false;

            try
            {
                this.Data = MetaChainWatchFile.Load(this.Path);
            }
            catch (IOException)
            {
                // Probably mid-write by rectl.  Try again next tick.
                return false;
            }
            catch (Exception e) when (e is Newtonsoft.Json.JsonException or FormatException or InvalidCastException)
            {
                // Use nothing from this file until it is fixed, and don't complain again until it changes.
                this.Data = null;
                QueueChat(string.Format("{0}config file unreadable, ignoring it : {1} : {2}", Prefix, this.Path, e.Message));
            }

            this._loadedWriteTime = writeTime;
            this._loadedOnce = true;
            return true;
        }
    }
}
