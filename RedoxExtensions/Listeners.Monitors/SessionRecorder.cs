using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using RedoxExtensions.AgentBridge;
using RedoxExtensions.Recording;
using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Listeners.Monitors;

/// <summary>
/// Writes this character's event log to disk.  Commands arrive on the game thread and ticks on a timer thread, so
/// every access to the session is under one lock.  All decisions live in <see cref="RecorderRules"/> and
/// <see cref="RecorderSession"/>; this class only wires them to the clock, the files and the game.
/// </summary>
public class SessionRecorder : IRecorderControl, IDisposable
{
    private const string Prefix = "[RE] Recorder : ";
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(10);

    private readonly object _lock = new();
    private readonly string _installDirectory;
    private readonly string _characterName;
    private readonly string _vtankDirectory;
    private readonly Func<DateTime> _clock;
    private readonly Func<string> _journalId;
    private readonly Func<EventJournal> _journal;
    private readonly IProcessLauncher _launcher;
    private readonly Action<string> _chat;
    private readonly Action<Action> _queueToGameThread;
    private readonly Func<object> _buildState;
    private readonly int _pid;
    private readonly Timer _timer;

    private RecorderSession _session;
    private bool _resumeDone;
    private bool _disposed;
    private DateTime _lastSnapshotAt = DateTime.MinValue;
    private object _snapshot;
    private string _snapshotError;

    public SessionRecorder()
        : this(
            Wrapper.WrapperUtilities.RedoxExtensionsBinDirectory,
            REPlugin.Instance.CharacterFilter.Name,
            VirindiInterop.VTUtilities.VTankDirectory,
            () => DateTime.Now,
            () => AgentBridgeService.JournalId,
            () => AgentBridgeService.Journal,
            new ProcessLauncher(),
            line => REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(new Dispatching.Legacy.DispatchedActionDelegate(() => REPlugin.Instance.Chat.WriteLine("{0}", line))),
            action => REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(new Dispatching.Legacy.DispatchedActionDelegate(action)),
            AgentHandlers.BuildState,
            Process.GetCurrentProcess().Id,
            true)
    {
    }

    /// <param name="chat">Receives whole lines from any thread and must get them to the game thread.</param>
    /// <param name="queueToGameThread">Runs the action on the game thread.</param>
    /// <param name="buildState">Reads the game, so it only runs through <paramref name="queueToGameThread"/>.</param>
    /// <param name="startTimer">False in tests, which call <see cref="Tick"/> themselves.</param>
    internal SessionRecorder(
        string installDirectory,
        string characterName,
        string vtankDirectory,
        Func<DateTime> clock,
        Func<string> journalId,
        Func<EventJournal> journal,
        IProcessLauncher launcher,
        Action<string> chat,
        Action<Action> queueToGameThread,
        Func<object> buildState,
        int processId,
        bool startTimer)
    {
        this._installDirectory = installDirectory;
        this._characterName = characterName;
        this._vtankDirectory = vtankDirectory;
        this._clock = clock;
        this._journalId = journalId;
        this._journal = journal;
        this._launcher = launcher;
        this._chat = chat;
        this._queueToGameThread = queueToGameThread;
        this._buildState = buildState;
        this._pid = processId;

        if (startTimer)
            this._timer = new Timer(this.Tick, null, TimeSpan.FromSeconds(2), TickInterval);
    }

    public bool IsRecording
    {
        get
        {
            lock (this._lock)
                return this._session != null;
        }
    }

    public void Start(TimeSpan timeout)
    {
        lock (this._lock)
        {
            this.EnsureResumed();

            var config = RecorderFile.LoadConfig(RecorderRules.ConfigPath(this._installDirectory), this._installDirectory);
            if (config.Warning != null)
                this.Say(config.Warning);

            if (this._session != null)
                this.EndSession(RecorderSession.ReasonRestarted);

            var now = this._clock();
            var folder = RecorderRules.SessionFolder(config.Config.OutputRoot, now);
            var name = RecorderRules.NextFileName(folder, this._characterName);
            var file = Path.Combine(folder, name + ".jsonl");

            this.CopyMetas(folder);

            var session = this.NewSession(file, new RecorderState(), this._journal);
            this._session = session;
            this._lastSnapshotAt = DateTime.MinValue;
            session.Start(now + timeout);
            this.Say("started " + file + ", stops by " + session.State.TimeoutAt.ToString("HH:mm"));
        }
    }

    public void Stop(TimeSpan? delay)
    {
        lock (this._lock)
        {
            this.EnsureResumed();

            var now = this._clock();
            var decision = RecorderRules.ApplyStop(this._session?.State, delay, now);
            switch (decision.Kind)
            {
                case RecorderStopKind.NotRecording:
                    this.Say("not recording");
                    return;

                case RecorderStopKind.StopNow:
                    var file = this._session.State.File;
                    this.EndSession(RecorderSession.ReasonStopped);
                    this.Say("stopped " + file);
                    return;

                default:
                    this._session.State.StopAt = decision.StopAt;
                    this.SaveState(this._session.State);
                    this.Say("stopping at " + decision.StopAt.Value.ToString("HH:mm:ss"));
                    return;
            }
        }
    }

    public void Status()
    {
        lock (this._lock)
        {
            this.EnsureResumed();

            if (this._session == null)
            {
                this.Say("not recording");
                return;
            }

            var state = this._session.State;
            this.Say("recording " + state.File);
            this.Chat("  started : " + state.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            this.Chat("  timeout : " + state.TimeoutAt.ToString("yyyy-MM-dd HH:mm:ss"));
            this.Chat("  pending stop : " + (state.StopAt.HasValue ? state.StopAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "none"));
            this.Chat("  events written : through journal seq " + state.LastSeq);
        }
    }

    /// <summary>One timer tick.  Never throws.</summary>
    internal void Tick(object unused = null)
    {
        if (!Monitor.TryEnter(this._lock))
            return;

        try
        {
            if (this._disposed)
                return;

            this.EnsureResumed();
            if (this._session == null)
                return;

            var now = this._clock();
            if (now - this._lastSnapshotAt >= SnapshotInterval)
            {
                this._lastSnapshotAt = now;
                this._queueToGameThread(this.RefreshSnapshot);
            }

            this._session.Pump();

            var state = this._session.State;
            if (RecorderRules.IsEnded(state, now))
                this.EndSession(state.StopAt.HasValue && state.StopAt.Value < state.TimeoutAt ? RecorderSession.ReasonStopped : RecorderSession.ReasonTimeout);
        }
        catch (Exception e)
        {
            REPlugin.Instance?.Error?.LogError(e);
        }
        finally
        {
            Monitor.Exit(this._lock);
        }
    }

    /// <summary>Saves the session so a reload continues from the last seq written.  Does not end it.</summary>
    public void Dispose()
    {
        lock (this._lock)
        {
            if (this._disposed)
                return;

            this._disposed = true;
            if (this._session != null && !this._session.IsEnded)
            {
                this._session.Pump();
                this.SaveState(this._session.State);
            }

            this._timer?.Dispose();
        }
    }

    private void RefreshSnapshot()
    {
        try
        {
            var state = this._buildState();
            lock (this._lock)
            {
                this._snapshot = state;
                this._snapshotError = null;
            }
        }
        catch (Exception e)
        {
            lock (this._lock)
                this._snapshotError = e.Message;
        }
    }

    private object CachedSnapshot()
    {
        lock (this._lock)
        {
            if (this._snapshotError != null)
                throw new InvalidOperationException(this._snapshotError);
            return this._snapshot;
        }
    }

    private void EnsureResumed()
    {
        if (this._resumeDone)
            return;

        var statePath = RecorderRules.StatePath(this._installDirectory, this._characterName);
        var saved = RecorderFile.LoadState(statePath, out var warning);
        if (warning != null)
        {
            this.Say(warning);
            this.DeleteState();
        }

        if (saved == null)
        {
            this._resumeDone = true;
            return;
        }

        var journalId = this._journalId();
        if (journalId == null)
            return;

        this._resumeDone = true;
        var decision = RecorderRules.DecideResume(saved, journalId, this._clock());
        switch (decision)
        {
            case RecorderResumeDecision.ContinueSameJournal:
            case RecorderResumeDecision.NewJournal:
                this._session = this.NewSession(saved.File, saved, this._journal);
                this._session.Resume(decision);
                this.Say("resumed " + saved.File);
                break;

            case RecorderResumeDecision.Expired:
                var expired = this.NewSession(saved.File, saved, () => null);
                this._session = expired;
                this.EndSession(RecorderSession.ReasonExpiredWhileDown);
                break;

            case RecorderResumeDecision.Invalid:
                this.Say("ignoring invalid state file " + statePath);
                this.DeleteState();
                break;
        }
    }

    private RecorderSession NewSession(string file, RecorderState state, Func<EventJournal> journal)
    {
        var session = new RecorderSession(file, this._characterName, journal, this._clock, this.CachedSnapshot, this._pid, state, this.SaveState);
        session.Warning += this.Say;
        return session;
    }

    private void EndSession(string reason)
    {
        var session = this._session;
        var state = session.State;
        var file = state.File;
        var date = RecorderRules.DateText(state.StartedAt);

        session.End(reason);
        this._session = null;
        this.DeleteState();
        this.RunAfterStop(file, date);
    }

    private void RunAfterStop(string file, string date)
    {
        try
        {
            var afterStop = RecorderFile.LoadConfig(RecorderRules.ConfigPath(this._installDirectory), this._installDirectory).Config.AfterStop;
            if (afterStop == null)
                return;

            var dir = Path.GetDirectoryName(file);
            var name = Path.GetFileNameWithoutExtension(file);
            var args = AfterStopArgs.Expand(afterStop.Args, file, dir, name, this._characterName, date);
            var pid = this._launcher.Launch(afterStop.Program, args, dir);
            this.Say("afterStop started (pid " + pid + ")");
        }
        catch (Exception e)
        {
            this.Say("afterStop failed: " + e.Message);
        }
    }

    private void CopyMetas(string folder)
    {
        try
        {
            if (string.IsNullOrEmpty(this._vtankDirectory) || !Directory.Exists(this._vtankDirectory))
                return;

            var target = Path.Combine(folder, "metas");
            Directory.CreateDirectory(target);
            foreach (var path in Directory.GetFiles(this._vtankDirectory, "*.met"))
            {
                // Windows treats "*.met" as also matching longer extensions such as ".meta".
                if (Path.GetExtension(path).Equals(".met", StringComparison.OrdinalIgnoreCase))
                    File.Copy(path, Path.Combine(target, Path.GetFileName(path)), true);
            }
        }
        catch (Exception e)
        {
            this.Say("couldn't copy the metas: " + e.Message);
        }
    }

    private void SaveState(RecorderState state)
    {
        try
        {
            RecorderFile.SaveState(RecorderRules.StatePath(this._installDirectory, this._characterName), state);
        }
        catch (Exception e)
        {
            this.Say("couldn't save the state file: " + e.Message);
        }
    }

    private void DeleteState()
    {
        try
        {
            RecorderFile.DeleteState(RecorderRules.StatePath(this._installDirectory, this._characterName));
        }
        catch (Exception e)
        {
            this.Say("couldn't delete the state file: " + e.Message);
        }
    }

    private void Say(string text) => this._chat(Prefix + text);

    private void Chat(string text) => this._chat(text);
}
