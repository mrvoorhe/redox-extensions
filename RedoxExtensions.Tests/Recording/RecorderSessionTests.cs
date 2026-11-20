using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using NUnit.Framework;

using RedoxExtensions.Recording;
using RedoxExtensions.Tests.TestUtilities;
using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.Recording;

[TestFixture]
public class RecorderSessionTests : TemporaryDirectoryPerTestTestSuite
{
    private static readonly DateTime T0 = new(2026, 11, 20, 7, 0, 0);

    private readonly List<string> _warnings = new();
    private DateTime _now;
    private string _file;

    [SetUp]
    public void SetUp()
    {
        this._now = T0;
        this._warnings.Clear();
        this._file = Path.Combine(this.TestTemporaryDirectory, "2026-11-20", "Cobble.jsonl");
    }

    private EventJournal NewJournal(int capacity = 100, string id = "1000-a")
    {
        return new EventJournal(capacity, id, () => DateTime.SpecifyKind(this._now, DateTimeKind.Utc));
    }

    private RecorderSession NewSession(EventJournal journal, RecorderState state = null, int pid = 1000, Func<object> stateProvider = null, string file = null)
    {
        var session = new RecorderSession(
            file ?? this._file,
            "Cobble",
            () => journal,
            () => this._now,
            stateProvider ?? (() => new Dictionary<string, object> { ["character"] = new Dictionary<string, object> { ["name"] = "Cobble" } }),
            pid,
            state ?? new RecorderState(),
            utcToLocal: t => DateTime.SpecifyKind(t, DateTimeKind.Unspecified));
        session.Warning += this._warnings.Add;
        return session;
    }

    private List<JsonElement> Lines(string file = null)
    {
        return File.ReadAllLines(file ?? this._file).Select(l => JsonDocument.Parse(l).RootElement.Clone()).ToList();
    }

    private static string Meta(JsonElement e) => e.GetProperty("kind").GetString() == "meta" ? e.GetProperty("what").GetString() : null;

    private static List<string> Texts(IEnumerable<JsonElement> lines)
    {
        return lines.Where(l => l.TryGetProperty("text", out _)).Select(l => l.GetProperty("text").GetString()).ToList();
    }

    private void Advance(double seconds) => this._now = this._now.AddSeconds(seconds);

    [Test]
    public void BackfillWritesRecentEventsBeforeCollectorStarted()
    {
        var journal = this.NewJournal();
        this._now = T0.AddMinutes(-10);
        journal.Publish("plugin", "too old");
        this._now = T0.AddSeconds(-90);
        journal.Publish("plugin", "[RE] StartMeta : Daily");
        this._now = T0.AddSeconds(-5);
        journal.Publish("chat", "just before");
        this._now = T0;

        var session = this.NewSession(journal);
        session.Start(T0.AddHours(5));
        this.Advance(1);
        journal.Publish("chat", "live");
        this.Advance(3);
        session.Pump();

        var lines = this.Lines();
        Assert.That(lines.Where(l => l.GetProperty("kind").GetString() != "state").Select(l => Meta(l) ?? l.GetProperty("text").GetString()), Is.EqualTo(
            new[] { "collector-armed", "[RE] StartMeta : Daily", "just before", "collector-started", "live" }));
        Assert.That(lines[0].GetProperty("start").GetString(), Is.EqualTo("2026-11-20T07:00:00"));
        Assert.That(lines[0].GetProperty("stop").GetString(), Is.EqualTo("2026-11-20T12:00:00"));
    }

    [Test]
    public void EveryEventIsWrittenOnceInOrder()
    {
        var journal = this.NewJournal();
        var session = this.NewSession(journal);
        session.Start(T0.AddHours(5));

        var expected = new List<string>();
        for (var pump = 0; pump < 5; pump++)
        {
            for (var i = 0; i < 7; i++)
            {
                var text = $"e{pump}-{i}";
                journal.Publish("chat", text);
                expected.Add(text);
            }

            this.Advance(3);
            session.Pump();
            session.Pump();
        }

        var lines = this.Lines();
        Assert.That(Texts(lines), Is.EqualTo(expected));
        var seqs = lines.Where(l => l.TryGetProperty("seq", out _)).Select(l => l.GetProperty("seq").GetInt64()).ToList();
        Assert.That(seqs, Is.EqualTo(Enumerable.Range(1, 35).Select(i => (long)i)));
        Assert.That(this._warnings, Is.Empty);
    }

    [Test]
    public void SmallJournalWritesOneGapMarkerWithExpectedAndGot()
    {
        var journal = this.NewJournal(capacity: 10);
        var session = this.NewSession(journal);
        session.Start(T0.AddHours(5));

        for (var i = 1; i <= 3; i++)
            journal.Publish("chat", "a" + i);
        this.Advance(3);
        session.Pump();

        for (var i = 1; i <= 25; i++)
            journal.Publish("chat", "b" + i);
        this.Advance(3);
        session.Pump();

        var gaps = this.Lines().Where(l => Meta(l) == "journal-gap").ToList();
        Assert.That(gaps, Has.Count.EqualTo(1));
        Assert.That(gaps[0].GetProperty("expected").GetInt64(), Is.EqualTo(4));
        Assert.That(gaps[0].GetProperty("got").GetInt64(), Is.EqualTo(19));
        Assert.That(gaps[0].GetProperty("pid").GetInt32(), Is.EqualTo(1000));
    }

    [Test]
    public void StateRecordEveryThirtySecondsOfFakeTime()
    {
        var journal = this.NewJournal();
        var session = this.NewSession(journal);
        session.Start(T0.AddHours(5));

        for (var i = 0; i < 40; i++)
        {
            this.Advance(3);
            session.Pump();
        }

        var states = this.Lines().Where(l => l.GetProperty("kind").GetString() == "state").ToList();
        Assert.That(states.Select(s => s.GetProperty("local").GetString()), Is.EqualTo(new[]
        {
            "2026-11-20T07:00:03", "2026-11-20T07:00:33", "2026-11-20T07:01:03", "2026-11-20T07:01:33",
        }));
        Assert.That(states[0].GetProperty("state").GetProperty("character").GetProperty("name").GetString(), Is.EqualTo("Cobble"));
        Assert.That(states[0].GetProperty("error").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public void FailingStateProviderWritesErrorRecord()
    {
        var session = this.NewSession(this.NewJournal(), stateProvider: () => throw new InvalidOperationException("not logged in"));
        session.Start(T0.AddHours(5));
        session.Pump();

        var state = this.Lines().Single(l => l.GetProperty("kind").GetString() == "state");
        Assert.That(state.GetProperty("state").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(state.GetProperty("error").GetString(), Is.EqualTo("not logged in"));
    }

    [TestCase("stopped")]
    [TestCase("timeout")]
    [TestCase("restarted")]
    [TestCase("expired-while-down")]
    public void EndWritesOneCollectorStoppedWithReasonAndNothingAfter(string reason)
    {
        var journal = this.NewJournal();
        var session = this.NewSession(journal);
        session.Start(T0.AddHours(5));
        journal.Publish("chat", "last words");

        session.End(reason);
        journal.Publish("chat", "too late");
        this.Advance(60);
        session.Pump();
        session.End("timeout");

        var lines = this.Lines();
        Assert.That(Meta(lines.Last()), Is.EqualTo("collector-stopped"));
        Assert.That(lines.Count(l => Meta(l) == "collector-stopped"), Is.EqualTo(1));
        Assert.That(lines.Last().GetProperty("reason").GetString(), Is.EqualTo(reason));
        Assert.That(Texts(lines), Is.EqualTo(new[] { "last words" }));
        Assert.That(session.IsEnded, Is.True);
    }

    [Test]
    public void ResumeWithSameJournalLosesNothingAndRepeatsNothing()
    {
        var journal = this.NewJournal();
        var state = new RecorderState();
        var first = this.NewSession(journal, state);
        first.Start(T0.AddHours(5));
        journal.Publish("chat", "one");
        journal.Publish("chat", "two");
        this.Advance(3);
        first.Pump();

        var saved = new RecorderState
        {
            File = state.File, StartedAt = state.StartedAt, TimeoutAt = state.TimeoutAt, JournalId = state.JournalId, LastSeq = state.LastSeq,
        };
        journal.Publish("chat", "while reloading");
        this.Advance(3);

        Assert.That(RecorderRules.DecideResume(saved, journal.Id, this._now), Is.EqualTo(RecorderResumeDecision.ContinueSameJournal));
        var second = this.NewSession(journal, saved);
        second.Resume(RecorderResumeDecision.ContinueSameJournal);
        journal.Publish("chat", "three");
        this.Advance(3);
        second.Pump();

        var lines = this.Lines();
        Assert.That(Texts(lines), Is.EqualTo(new[] { "one", "two", "while reloading", "three" }));
        Assert.That(lines.Any(l => Meta(l) == "client-found" || Meta(l) == "journal-gap"), Is.False);
    }

    [Test]
    public void ResumeWithNewJournalWritesClientFoundAndReadsFromTheFirstEvent()
    {
        var oldJournal = this.NewJournal(id: "1000-a");
        var state = new RecorderState();
        var first = this.NewSession(oldJournal, state);
        first.Start(T0.AddHours(5));
        oldJournal.Publish("chat", "before crash");
        this.Advance(3);
        first.Pump();
        Assert.That(state.LastSeq, Is.EqualTo(1));

        this.Advance(300);
        var newJournal = this.NewJournal(id: "2000-b");
        newJournal.Publish("plugin", "login");
        newJournal.Publish("chat", "after");

        Assert.That(RecorderRules.DecideResume(state, newJournal.Id, this._now), Is.EqualTo(RecorderResumeDecision.NewJournal));
        var second = this.NewSession(newJournal, state, pid: 2000);
        second.Resume(RecorderResumeDecision.NewJournal);
        this.Advance(3);
        second.Pump();

        var lines = this.Lines();
        var found = lines.Single(l => Meta(l) == "client-found");
        Assert.That(found.GetProperty("pid").GetInt32(), Is.EqualTo(2000));
        Assert.That(found.GetProperty("oldPid").GetInt32(), Is.EqualTo(1000));
        Assert.That(found.GetProperty("sameInstance").GetBoolean(), Is.False);
        Assert.That(Texts(lines), Is.EqualTo(new[] { "before crash", "login", "after" }));
        var after = lines.Skip(lines.IndexOf(found) + 1).Where(l => l.TryGetProperty("seq", out _)).ToList();
        Assert.That(after.Select(l => l.GetProperty("seq").GetInt64()), Is.EqualTo(new[] { 1L, 2L }));
        Assert.That(after.All(l => l.GetProperty("pid").GetInt32() == 2000), Is.True);
        Assert.That(state.JournalId, Is.EqualTo("2000-b"));
        Assert.That(lines.Any(l => Meta(l) == "journal-gap"), Is.False);
    }

    [Test]
    public void UnwritableFolderWarnsOnceDoesNotThrowAndRecoversLater()
    {
        var blocker = Path.Combine(this.TestTemporaryDirectory, "blocked");
        File.WriteAllText(blocker, "a file where a folder should be");
        var file = Path.Combine(blocker, "Cobble.jsonl");

        var journal = this.NewJournal();
        var session = this.NewSession(journal, file: file);
        Assert.DoesNotThrow(() => session.Start(T0.AddHours(5)));
        journal.Publish("chat", "queued");
        for (var i = 0; i < 3; i++)
        {
            this.Advance(3);
            Assert.DoesNotThrow(session.Pump);
        }

        Assert.That(this._warnings, Has.Count.EqualTo(1));
        Assert.That(this._warnings[0], Does.Contain(file));

        File.Delete(blocker);
        journal.Publish("chat", "after fix");
        this.Advance(3);
        session.Pump();

        var lines = this.Lines(file);
        Assert.That(Meta(lines[0]), Is.EqualTo("collector-armed"));
        Assert.That(Texts(lines), Is.EqualTo(new[] { "queued", "after fix" }));
        Assert.That(this._warnings, Has.Count.EqualTo(1));

        File.Delete(file);
        Directory.Delete(Path.GetDirectoryName(file));
        File.WriteAllText(blocker, "again");
        journal.Publish("chat", "second outage");
        this.Advance(40);
        Assert.DoesNotThrow(session.Pump);
        Assert.That(this._warnings, Has.Count.EqualTo(2));
    }

    [Test]
    public void EndOnUnwritableFolderDoesNotThrow()
    {
        var blocker = Path.Combine(this.TestTemporaryDirectory, "blocked");
        File.WriteAllText(blocker, "x");
        var session = this.NewSession(this.NewJournal(), file: Path.Combine(blocker, "Cobble.jsonl"));
        session.Start(T0.AddHours(5));
        Assert.DoesNotThrow(() => session.End("stopped"));
        Assert.That(session.IsEnded, Is.True);
    }

    private static readonly string[] EventKeys = { "seq", "time", "kind", "text", "pid", "local" };
    private static readonly string[] EventKeysWithColor = { "seq", "time", "kind", "text", "color", "pid", "local" };
    private static readonly string[] StateKeys = { "kind", "pid", "state", "error", "local" };

    private static readonly Dictionary<string, string[]> MetaKeys = new()
    {
        ["collector-armed"] = new[] { "kind", "what", "start", "stop", "local" },
        ["collector-started"] = new[] { "kind", "what", "local" },
        ["collector-stopped"] = new[] { "kind", "what", "reason", "local" },
        ["journal-gap"] = new[] { "kind", "what", "pid", "expected", "got", "local" },
        ["client-found"] = new[] { "kind", "what", "pid", "oldPid", "sameInstance", "downSince", "local" },
    };

    private static string FixturePath()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "meta-tools")))
                return Path.Combine(dir.FullName, "meta-tools", "tests", "fixtures", "recorder-session.jsonl");
        }

        throw new InvalidOperationException("mono repo root with meta-tools/ not found above " + AppContext.BaseDirectory);
    }

    private static Dictionary<string, object> Location(long landcell, double x, double y, double z) => new()
    {
        ["character"] = new Dictionary<string, object> { ["name"] = "Cobble", ["id"] = 1342177281L, ["server"] = "Coldeve", ["level"] = 275 },
        ["location"] = new Dictionary<string, object> { ["landcell"] = landcell, ["x"] = x, ["y"] = y, ["z"] = z, ["heading"] = 90.5 },
        ["busyState"] = "Idle",
        ["combatMode"] = "Peace",
    };

    private void Script()
    {
        var location = Location(2842886145L, 100.5, 50.25, 12);
        var journalA = this.NewJournal(id: "1000-2026-11-20T06:55:00");
        var state = new RecorderState();
        var first = this.NewSession(journalA, state, stateProvider: () => location);

        this._now = T0.AddSeconds(-20);
        journalA.Publish("plugin", "[RE] StartMeta : Daily");
        this._now = T0;
        first.Start(T0.AddHours(5));

        this._now = T0.AddSeconds(1);
        journalA.Publish("plugin", "[VTank] Meta executing action: ChatCommand /vt meta load aKTViridian", 1);
        this._now = T0.AddSeconds(2);
        journalA.Publish("plugin", "[VTank] Meta executing action: Chat Command: /vt meta load aKTTouTou");
        this.Advance(3);
        first.Pump();

        this._now = T0.AddSeconds(20);
        journalA.Publish("chat", "You have killed 1 Clay Golems! You must kill 3 to complete your task.", 22);
        journalA.Publish("chat", "Royal Guard gives you 8 Enlightened Coins.");
        this._now = T0.AddSeconds(36);
        location = Location(2842886145L, 130.25, 62.5, 12);
        first.Pump();

        this._now = T0.AddMinutes(5);
        var journalB = this.NewJournal(id: "2000-2026-11-20T07:05:00");
        journalB.Publish("plugin", "[RE] Login complete \"Cobble\"");
        var second = this.NewSession(journalB, state, pid: 2000, stateProvider: () => location);
        second.Resume(RecorderRules.DecideResume(state, journalB.Id, this._now));
        this._now = T0.AddMinutes(5).AddSeconds(2);
        journalB.Publish("chat", "Royal Guard gives you 8 Enlightened Coins.");
        this._now = T0.AddMinutes(5).AddSeconds(3);
        second.End(RecorderSession.ReasonStopped);
    }

    [Test]
    public void FixtureFileMatchesTheScriptedSession()
    {
        this.Script();
        var actual = File.ReadAllText(this._file);
        var fixture = FixturePath();

        if (Environment.GetEnvironmentVariable("RECORDER_FIXTURE_UPDATE") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fixture));
            File.WriteAllText(fixture, actual);
        }

        Assert.That(File.Exists(fixture), Is.True, "run once with RECORDER_FIXTURE_UPDATE=1 to create " + fixture);
        Assert.That(actual.Replace("\r\n", "\n"), Is.EqualTo(File.ReadAllText(fixture).Replace("\r\n", "\n")));
    }

    [Test]
    public void EveryRecordHasTheKeysMetachainRecordWrites()
    {
        this.Script();
        var lines = this.Lines();
        Assert.That(lines.Select(Meta).Where(m => m != null).Distinct(), Is.SupersetOf(MetaKeys.Keys.Where(k => k != "journal-gap")));

        foreach (var line in lines)
        {
            var keys = line.EnumerateObject().Select(p => p.Name).ToArray();
            var kind = line.GetProperty("kind").GetString();
            string[] expected;
            if (kind == "meta")
                expected = MetaKeys[line.GetProperty("what").GetString()];
            else if (kind == "state")
                expected = StateKeys;
            else
                expected = line.TryGetProperty("color", out _) ? EventKeysWithColor : EventKeys;
            Assert.That(keys, Is.EqualTo(expected), line.ToString());
        }
    }

    [Test]
    public void GapRecordHasTheKeysMetachainRecordWrites()
    {
        var journal = this.NewJournal(capacity: 5);
        var session = this.NewSession(journal);
        session.Start(T0.AddHours(5));
        journal.Publish("chat", "a");
        session.Pump();
        for (var i = 0; i < 12; i++)
            journal.Publish("chat", "b");
        session.Pump();

        var gap = this.Lines().Single(l => Meta(l) == "journal-gap");
        Assert.That(gap.EnumerateObject().Select(p => p.Name), Is.EqualTo(MetaKeys["journal-gap"]));
    }
}
