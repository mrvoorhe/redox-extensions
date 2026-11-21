using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Recording;

/// <summary>
/// One recording session's pump.  No timers, no Decal: the caller ticks <see cref="Pump"/>.  Writes the same
/// JSON lines as <c>metachain.py record</c>.  <see cref="RecorderState.LastSeq"/> is the last journal seq written
/// (or skipped by backfill); 0 means nothing yet.
/// </summary>
public sealed class RecorderSession
{
    public const string ReasonStopped = "stopped";
    public const string ReasonTimeout = "timeout";
    public const string ReasonRestarted = "restarted";
    public const string ReasonExpiredWhileDown = "expired-while-down";

    public static readonly TimeSpan BackfillWindow = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan StateInterval = TimeSpan.FromSeconds(30);

    private const int ReadBatch = 500;
    private const string LocalFormat = "yyyy-MM-dd'T'HH:mm:ss";

    private readonly string _filePath;
    private readonly Func<EventJournal> _journal;
    private readonly Func<DateTime> _clock;
    private readonly Func<object> _stateProvider;
    private readonly int _pid;
    private readonly Action<RecorderState> _persist;
    private readonly Func<DateTime, DateTime> _utcToLocal;
    private readonly List<string> _pending = new();
    private DateTime? _lastStateAt;
    private DateTime _lastPersistAt;
    private bool _failing;
    private bool _ended;

    public RecorderSession(
        string filePath,
        string characterName,
        Func<EventJournal> journal,
        Func<DateTime> clock,
        Func<object> stateProvider,
        int processId,
        RecorderState state,
        Action<RecorderState> persist = null,
        Func<DateTime, DateTime> utcToLocal = null)
    {
        this._filePath = filePath;
        this.CharacterName = characterName;
        this._journal = journal;
        this._clock = clock;
        this._stateProvider = stateProvider;
        this._pid = processId;
        this.State = state;
        this._persist = persist;
        this._utcToLocal = utcToLocal ?? (t => t.ToLocalTime());
    }

    public string CharacterName { get; }
    public RecorderState State { get; }
    public bool IsEnded => this._ended;

    /// <summary>Raised once when writing starts failing, and again if it fails again after having recovered.</summary>
    public event Action<string> Warning;

    public void Start(DateTime timeoutAt)
    {
        var now = this._clock();
        this.State.File = this._filePath;
        this.State.StartedAt = now;
        this.State.TimeoutAt = timeoutAt;
        this.State.LastSeq = 0;

        this.AddMeta("collector-armed", Field("start", Iso(now)), Field("stop", Iso(timeoutAt)));

        var journal = this._journal();
        if (journal != null)
        {
            this.State.JournalId = journal.Id;
            var events = journal.GetSince(1, null, int.MaxValue);
            var cutoff = now - BackfillWindow;
            foreach (var evt in events)
            {
                if (this._utcToLocal(evt.TimeUtc) >= cutoff)
                    this.AddEvent(evt);
            }
            this.State.LastSeq = events.Count > 0 ? events[events.Count - 1].Seq : journal.NextSeq - 1;
        }

        this.AddMeta("collector-started");
        this.Persist(now);
        this.Flush();
    }

    public void Resume(RecorderResumeDecision decision)
    {
        if (decision == RecorderResumeDecision.ContinueSameJournal)
            return;

        if (decision != RecorderResumeDecision.NewJournal)
            throw new ArgumentException("Only ContinueSameJournal and NewJournal resume a session", nameof(decision));

        var now = this._clock();
        this.AddMeta(
            "client-found",
            Field("pid", this._pid),
            Field("oldPid", OldPid(this.State.JournalId)),
            Field("sameInstance", false),
            Field("downSince", null));
        this.State.JournalId = this._journal()?.Id;
        this.State.LastSeq = 0;
        this.Persist(now);
        this.Flush();
    }

    /// <summary>One tick.  Never throws.</summary>
    public void Pump()
    {
        if (this._ended)
            return;

        try
        {
            this.PumpCore();
        }
        catch (Exception ex)
        {
            this.Fail(ex);
        }
    }

    public void End(string reason)
    {
        if (this._ended)
            return;

        try
        {
            this.Flush();
            if (this._pending.Count == 0)
                this.ReadEvents();
        }
        catch (Exception ex)
        {
            this.Fail(ex);
        }

        this._ended = true;
        this.AddMeta("collector-stopped", Field("reason", reason));
        this.Flush();
    }

    private void PumpCore()
    {
        var now = this._clock();
        this.Flush();
        if (this._pending.Count > 0)
            return;

        this.ReadEvents();

        if (!this._lastStateAt.HasValue || now - this._lastStateAt.Value >= StateInterval)
        {
            // A provider with nothing yet (no game-thread snapshot at the start) is not a record: try again next pump.
            if (this.AddState())
                this._lastStateAt = now;
        }

        if (now - this._lastPersistAt >= StateInterval)
            this.Persist(now);

        this.Flush();
    }

    private void ReadEvents()
    {
        var journal = this._journal();
        if (journal == null)
            return;

        if (this.State.JournalId == null)
            this.State.JournalId = journal.Id;

        while (true)
        {
            var expected = this.State.LastSeq + 1;
            var events = journal.GetSince(expected, null, ReadBatch);
            if (events.Count == 0)
                return;

            if (this.State.LastSeq > 0 && events[0].Seq > expected)
                this.AddMeta("journal-gap", Field("pid", this._pid), Field("expected", expected), Field("got", events[0].Seq));

            foreach (var evt in events)
                this.AddEvent(evt);
            this.State.LastSeq = events[events.Count - 1].Seq;

            if (events.Count < ReadBatch)
                return;
        }
    }

    private void AddEvent(JournalEvent evt)
    {
        var record = evt.ToJson();
        record["time"] = evt.TimeUtc;
        record["pid"] = this._pid;
        this.AddRecord(record);
    }

    private bool AddState()
    {
        object state = null;
        string error = null;
        try
        {
            state = this._stateProvider();
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        if (state == null && error == null)
            return false;

        this.AddRecord(new Dictionary<string, object>
        {
            ["kind"] = "state",
            ["pid"] = this._pid,
            ["state"] = state,
            ["error"] = error,
        });
        return true;
    }

    private void AddMeta(string what, params KeyValuePair<string, object>[] fields)
    {
        var record = new Dictionary<string, object> { ["kind"] = "meta", ["what"] = what };
        foreach (var field in fields)
            record[field.Key] = field.Value;
        this.AddRecord(record);
    }

    private void AddRecord(Dictionary<string, object> record)
    {
        record["local"] = Iso(this._clock());
        this._pending.Add(RecorderJson.Serialize(record));
    }

    private void Flush()
    {
        if (this._pending.Count == 0)
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this._filePath));
            using var stream = new FileStream(this._filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            var text = new StringBuilder();
            foreach (var line in this._pending)
                text.Append(line).Append('\n');
            var bytes = new UTF8Encoding(false).GetBytes(text.ToString());
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
            this._pending.Clear();
            this._failing = false;
        }
        catch (Exception ex)
        {
            this.Fail(ex);
        }
    }

    private void Persist(DateTime now)
    {
        this._lastPersistAt = now;
        if (this._persist == null)
            return;

        try
        {
            this._persist(this.State);
        }
        catch (Exception ex)
        {
            this.Fail(ex);
        }
    }

    private void Fail(Exception ex)
    {
        if (this._failing)
            return;

        this._failing = true;
        this.Warning?.Invoke("can't write " + this._filePath + ": " + ex.Message);
    }

    private static KeyValuePair<string, object> Field(string key, object value) => new KeyValuePair<string, object>(key, value);

    private static string Iso(DateTime t) => t.ToString(LocalFormat, System.Globalization.CultureInfo.InvariantCulture);

    private static object OldPid(string journalId)
    {
        if (string.IsNullOrEmpty(journalId))
            return null;

        var dash = journalId.IndexOf('-');
        var head = dash < 0 ? journalId : journalId.Substring(0, dash);
        return int.TryParse(head, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var pid) ? (object)pid : null;
    }
}
