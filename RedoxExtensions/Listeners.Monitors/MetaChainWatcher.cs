using System;
using System.Globalization;
using System.IO;
using System.Threading;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Continuations;
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
/// </summary>
public class MetaChainWatcher : IDisposable
{
    private const string Prefix = "[RE] MetaChainWatch : ";
    private const int MillisecondsBeforeChainState = 1000;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    private readonly object _lock = new();
    private readonly Timer _timer;
    private readonly MetaChainWatchState _state = new();
    private readonly WatchedFile _globalFile;
    private readonly WatchedFile _characterFile;

    private MergedMetaChainWatchConfig _merged = MetaChainWatchRules.Merge(null, null);
    private bool _forceReload = true;
    private bool _disposed;
    private volatile bool _inPortalSpace;

    public MetaChainWatcher()
        : this(CommandScheduler.InstallDirectory, REPlugin.Instance.CharacterFilter.Name)
    {
    }

    private MetaChainWatcher(string installDirectory, string characterName)
    {
        this._globalFile = new WatchedFile(MetaChainWatchRules.GlobalFilePath(installDirectory));
        this._characterFile = new WatchedFile(MetaChainWatchRules.CharacterFilePath(installDirectory, characterName));
        REPlugin.Instance.Events.Decal.ChangePortalMode += Decal_ChangePortalMode;
        this._timer = new Timer(this.Tick, null, TimeSpan.FromSeconds(2), TickInterval);
    }

    public string GlobalFilePath => this._globalFile.Path;

    public string CharacterFilePath => this._characterFile.Path;

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
            // Holding the lock means no tick is half way through its locked part.
            this._disposed = true;
            this._timer.Dispose();
        }
    }

    private void Tick(object state)
    {
        MetaChainWatchAction action;
        MergedMetaChainWatchConfig merged;

        if (!Monitor.TryEnter(this._lock))
            return;

        try
        {
            if (this._disposed)
                return;

            this.ReloadIfChanged();

            if (this._inPortalSpace)
                return;

            merged = this._merged;
            action = this._state.Tick(DateTime.Now, merged);
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
            switch (action.Kind)
            {
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

    /// <summary>Runs on the timer thread, outside the lock, so the .met read never holds up the game thread.</summary>
    private void HandleTimeout(MetaChainWatchAction action, MergedMetaChainWatchConfig merged)
    {
        var metaName = DisplayName(action.MetaName);
        QueueChat(string.Format(
            "{0}{1} timed out after {2} min ({3})",
            Prefix,
            metaName,
            FormatMinutes(action.Elapsed.TotalMinutes),
            action.IsOverride ? "override" : "default"));

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
        this._inPortalSpace = e.Type == PortalEventType.EnterPortal;
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
