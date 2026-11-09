using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Scheduling;

namespace RedoxExtensions.Listeners.Monitors;

/// <summary>
/// Runs this character's scheduled commands from &lt;install&gt;\ScheduledCommands\&lt;Character&gt;.json.
///
/// The file is watched and the due checks run on a timer thread, so no file IO happens on the game thread.  A due
/// command's lastRun is saved before the command is queued to the game thread, so it runs at most once even if the
/// plugin reloads right after.
/// </summary>
public class CommandScheduler : IDisposable
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    private readonly object _lock = new();
    private readonly string _path;
    private readonly Timer _timer;

    // lastRun values this session has written, so a stale file (rewritten by rectl from an older read) can't make
    // an entry run twice.
    private readonly Dictionary<string, DateTime> _ranThisSession = new();

    private ScheduleFileData _data = new();
    private DateTime? _loadedWriteTime;
    private bool _forceReload = true;
    private bool _disposed;
    private volatile bool _inPortalSpace;

    public CommandScheduler()
        : this(ScheduleRules.FilePath(InstallDirectory, REPlugin.Instance.CharacterFilter.Name))
    {
    }

    private CommandScheduler(string path)
    {
        this._path = path;
        REPlugin.Instance.Events.Decal.ChangePortalMode += Decal_ChangePortalMode;
        this._timer = new Timer(this.Tick, null, TimeSpan.FromSeconds(2), TickInterval);
    }

    // The plugin itself runs from a TmpDlls shadow copy, so use the wrapper's directory (as VTUtilities does).
    public static string InstallDirectory => Wrapper.WrapperUtilities.RedoxExtensionsBinDirectory;

    public string FilePath => this._path;

    /// <summary>A copy of the entries as last loaded.</summary>
    public List<ScheduledCommand> Snapshot()
    {
        lock (this._lock)
        {
            return this._data.Entries.ToList();
        }
    }

    /// <summary>Reload the file on the next tick even if it doesn't look changed.</summary>
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
            // Holding the lock means no tick is half way through.
            this._disposed = true;
            this._timer.Dispose();
        }
    }

    private void Tick(object state)
    {
        if (!Monitor.TryEnter(this._lock))
            return;

        try
        {
            if (this._disposed)
                return;

            var reloadMessage = this.ReloadIfChanged();
            if (reloadMessage != null)
                REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() => REPlugin.Instance.Chat.WriteLine(reloadMessage));

            if (this._inPortalSpace)
                return;

            var now = DateTime.Now;
            var due = this._data.Entries.Where(e => ScheduleRules.IsDue(e, now)).ToList();
            if (due.Count == 0)
                return;

            // Re-read right before writing to keep the window for losing a concurrent rectl edit small.
            this.Load();
            due = this._data.Entries.Where(e => ScheduleRules.IsDue(e, now)).ToList();
            if (due.Count == 0)
                return;

            foreach (var entry in due)
            {
                entry.LastRun = now;
                if (!string.IsNullOrEmpty(entry.Id))
                    this._ranThisSession[entry.Id] = now;
            }

            // If this throws nothing runs, and the next tick tries again.
            ScheduleFile.Save(this._path, this._data);
            this._loadedWriteTime = System.IO.File.GetLastWriteTimeUtc(this._path);

            foreach (var entry in due)
            {
                var id = entry.Id;
                var command = entry.Command;
                REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() => Run(id, command));
            }
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

    private static void Run(string id, string command)
    {
        REPlugin.Instance.Chat.WriteLine("[RE] Scheduled : {0} {1}", id, command);
        ACUtilities.ProcessArbitraryCommand(command);
    }

    /// <summary>Returns a chat line when the file was (re)loaded, else null.</summary>
    private string ReloadIfChanged()
    {
        DateTime? writeTime = System.IO.File.Exists(this._path) ? System.IO.File.GetLastWriteTimeUtc(this._path) : null;
        if (!this._forceReload && writeTime == this._loadedWriteTime)
            return null;

        try
        {
            this.Load();
        }
        catch (System.IO.IOException)
        {
            // Probably mid-write by rectl.  Try again next tick.
            return null;
        }
        catch (Exception e) when (e is Newtonsoft.Json.JsonException or FormatException or InvalidCastException)
        {
            // Run nothing until the file is fixed, and don't complain again until it changes.
            this._data = new ScheduleFileData();
            this._loadedWriteTime = writeTime;
            this._forceReload = false;
            return "[RE] Schedule file unreadable, nothing scheduled : " + e.Message;
        }

        this._forceReload = false;
        if (writeTime == null)
            return null;

        var count = this._data.Entries.Count;
        return string.Format("[RE] Schedule loaded : {0} entr{1}", count, count == 1 ? "y" : "ies");
    }

    private void Load()
    {
        DateTime? writeTime = System.IO.File.Exists(this._path) ? System.IO.File.GetLastWriteTimeUtc(this._path) : null;
        var data = ScheduleFile.Load(this._path);

        foreach (var entry in data.Entries)
        {
            if (entry.Id != null && this._ranThisSession.TryGetValue(entry.Id, out var ran) && (!entry.LastRun.HasValue || entry.LastRun.Value < ran))
                entry.LastRun = ran;
        }

        this._data = data;
        this._loadedWriteTime = writeTime;
    }

    private void Decal_ChangePortalMode(object sender, ChangePortalModeEventArgs e)
    {
        this._inPortalSpace = e.Type == PortalEventType.EnterPortal;
    }
}
