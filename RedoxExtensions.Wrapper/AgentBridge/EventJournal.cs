using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace RedoxExtensions.Wrapper.AgentBridge;

public class JournalEvent
{
    public JournalEvent(long seq, DateTime timeUtc, string kind, string text, int? color)
    {
        this.Seq = seq;
        this.TimeUtc = timeUtc;
        this.Kind = kind;
        this.Text = text;
        this.Color = color;
    }

    public long Seq { get; }
    public DateTime TimeUtc { get; }
    public string Kind { get; }
    public string Text { get; }
    public int? Color { get; }

    public Dictionary<string, object> ToJson()
    {
        var result = new Dictionary<string, object>
        {
            ["seq"] = this.Seq,
            ["time"] = this.TimeUtc,
            ["kind"] = this.Kind,
            ["text"] = this.Text,
        };
        if (this.Color.HasValue)
            result["color"] = this.Color.Value;
        return result;
    }
}

/// <summary>
/// Fixed-size ring buffer of events with monotonically increasing sequence numbers.  Thread safe.
/// </summary>
public class EventJournal
{
    public const string KindChat = "chat";
    public const string KindDebug = "debug";
    public const string KindCommand = "command";

    private readonly object _lock = new();
    private readonly JournalEvent[] _buffer;
    private long _nextSeq = 1;

    public EventJournal(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        this._buffer = new JournalEvent[capacity];
    }

    public int Capacity => this._buffer.Length;

    /// <summary>
    /// The sequence number the next published event will get.  Pass as "since" to only see events after now.
    /// </summary>
    public long NextSeq
    {
        get
        {
            lock (this._lock)
                return this._nextSeq;
        }
    }

    public JournalEvent Publish(string kind, string text, int? color = null)
    {
        lock (this._lock)
        {
            var evt = new JournalEvent(this._nextSeq, DateTime.UtcNow, kind, text ?? string.Empty, color);
            this._buffer[this._nextSeq % this._buffer.Length] = evt;
            this._nextSeq++;
            Monitor.PulseAll(this._lock);
            return evt;
        }
    }

    /// <summary>
    /// Returns retained events with Seq >= since, oldest first.
    /// </summary>
    public List<JournalEvent> GetSince(long since, ICollection<string> kinds, int max)
    {
        lock (this._lock)
            return this.GetSinceLocked(since, kinds, null, max);
    }

    /// <summary>
    /// Blocks until an event with Seq >= since matches kind and pattern, or the timeout expires.  Returns null on timeout.
    /// </summary>
    public JournalEvent WaitFor(long since, ICollection<string> kinds, Regex pattern, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        lock (this._lock)
        {
            while (true)
            {
                var matches = this.GetSinceLocked(since, kinds, pattern, 1);
                if (matches.Count > 0)
                    return matches[0];

                since = Math.Max(since, this._nextSeq);

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    return null;
                Monitor.Wait(this._lock, remaining);
            }
        }
    }

    public void Clear()
    {
        lock (this._lock)
            Array.Clear(this._buffer, 0, this._buffer.Length);
    }

    private List<JournalEvent> GetSinceLocked(long since, ICollection<string> kinds, Regex pattern, int max)
    {
        var result = new List<JournalEvent>();
        var oldestRetained = Math.Max(1, this._nextSeq - this._buffer.Length);
        for (var seq = Math.Max(since, oldestRetained); seq < this._nextSeq && result.Count < max; seq++)
        {
            var evt = this._buffer[seq % this._buffer.Length];
            if (evt == null || evt.Seq != seq)
                continue;
            if (kinds != null && kinds.Count > 0 && !kinds.Contains(evt.Kind))
                continue;
            if (pattern != null && !pattern.IsMatch(evt.Text))
                continue;
            result.Add(evt);
        }
        return result;
    }
}
