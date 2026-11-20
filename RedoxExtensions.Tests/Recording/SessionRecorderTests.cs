using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using NUnit.Framework;

using RedoxExtensions.Listeners.Monitors;
using RedoxExtensions.Recording;
using RedoxExtensions.Tests.TestUtilities;
using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.Recording;

[TestFixture]
public class SessionRecorderTests : TemporaryDirectoryPerTestTestSuite
{
    private static readonly DateTime T0 = new(2026, 11, 20, 7, 0, 0);

    private sealed class FakeLauncher : IProcessLauncher
    {
        public readonly List<(string Program, string Args, string Dir, string LastLine)> Calls = new();
        public bool Throw;

        public int Launch(string program, string arguments, string workingDirectory)
        {
            var file = arguments.Split('|')[0];
            this.Calls.Add((program, arguments, workingDirectory, File.Exists(file) ? File.ReadAllLines(file).Last() : null));
            if (this.Throw)
                throw new InvalidOperationException("no such program");
            return 4242;
        }
    }

    private readonly List<string> _chat = new();
    private readonly FakeLauncher _launcher = new();
    private DateTime _now;
    private EventJournal _journal;
    private string _install;
    private string _vtank;
    private string _out;
    private string _day;

    [SetUp]
    public void SetUp()
    {
        this._now = T0;
        this._chat.Clear();
        this._launcher.Calls.Clear();
        this._launcher.Throw = false;
        this._journal = new EventJournal(1000, "1000-a");
        this._install = Path.Combine(this.TestTemporaryDirectory, "install");
        this._vtank = Path.Combine(this.TestTemporaryDirectory, "vtank");
        this._out = Path.Combine(this.TestTemporaryDirectory, "out");
        this._day = Path.Combine(this._out, "2026-11-20");
        Directory.CreateDirectory(this._install);
        Directory.CreateDirectory(this._vtank);
        this.WriteConfig(null);
        File.WriteAllText(Path.Combine(this._vtank, "Daily.met"), "daily v1");
        File.WriteAllText(Path.Combine(this._vtank, "aFarm.MET"), "afarm v1");
        File.WriteAllText(Path.Combine(this._vtank, "notes.meta"), "not a meta");
    }

    private void WriteConfig(string afterStop)
    {
        var after = afterStop == null ? string.Empty : $", \"afterStop\": {{ \"program\": \"py\", \"args\": \"{afterStop}\" }}";
        Directory.CreateDirectory(Path.Combine(this._install, "Recorder"));
        File.WriteAllText(
            Path.Combine(this._install, "Recorder", "Global.json"),
            "{ \"outputRoot\": " + JsonSerializer.Serialize(this._out) + after + " }");
    }

    private SessionRecorder NewRecorder(Func<string> journalId = null)
    {
        return new SessionRecorder(
            this._install,
            "Cobble",
            this._vtank,
            () => this._now,
            journalId ?? (() => this._journal.Id),
            () => this._journal,
            this._launcher,
            this._chat.Add,
            action => action(),
            () => new Dictionary<string, object> { ["location"] = new Dictionary<string, object> { ["x"] = 1 } },
            1000,
            false);
    }

    private string StatePath => RecorderRules.StatePath(this._install, "Cobble");

    private List<JsonElement> Lines(string name = "Cobble")
    {
        return File.ReadAllLines(Path.Combine(this._day, name + ".jsonl")).Select(l => JsonDocument.Parse(l).RootElement.Clone()).ToList();
    }

    private static string Reason(JsonElement last) => last.GetProperty("reason").GetString();

    private void Advance(TimeSpan by, SessionRecorder recorder)
    {
        this._now += by;
        recorder.Tick();
    }

    [Test]
    public void Start_CreatesFileAndCopiesMetas()
    {
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromHours(5));

        var lines = this.Lines();
        Assert.That(lines.First().GetProperty("what").GetString(), Is.EqualTo("collector-armed"));
        Assert.That(lines.Last().GetProperty("what").GetString(), Is.EqualTo("collector-started"));
        Assert.That(File.ReadAllText(Path.Combine(this._day, "metas", "Daily.met")), Is.EqualTo("daily v1"));
        Assert.That(File.Exists(Path.Combine(this._day, "metas", "aFarm.MET")), Is.True);
        Assert.That(Directory.GetFiles(Path.Combine(this._day, "metas")).Length, Is.EqualTo(2));
        Assert.That(File.Exists(this.StatePath), Is.True);
        Assert.That(this._chat.Last(), Does.Contain("started").And.Contain("stops by 12:00"));
    }

    [Test]
    public void Start_Twice_RestartsFirstSessionAndOverwritesMetas()
    {
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromHours(5));
        File.WriteAllText(Path.Combine(this._vtank, "Daily.met"), "daily v2");
        this._now += TimeSpan.FromMinutes(1);
        recorder.Start(TimeSpan.FromHours(5));

        var first = this.Lines().Last();
        Assert.That(first.GetProperty("what").GetString(), Is.EqualTo("collector-stopped"));
        Assert.That(Reason(first), Is.EqualTo("restarted"));
        Assert.That(this.Lines("Cobble-2").Last().GetProperty("what").GetString(), Is.EqualTo("collector-started"));
        Assert.That(File.ReadAllText(Path.Combine(this._day, "metas", "Daily.met")), Is.EqualTo("daily v2"));
        Assert.That(recorder.IsRecording, Is.True);
    }

    [Test]
    public void Stop_WithDelay_EndsAtTheRightTime()
    {
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromHours(1));
        recorder.Stop(TimeSpan.FromMinutes(10));
        Assert.That(this._chat.Last(), Does.Contain("stopping at 07:10:00"));

        this.Advance(TimeSpan.FromMinutes(9), recorder);
        Assert.That(recorder.IsRecording, Is.True);
        Assert.That(File.ReadAllText(this.StatePath), Does.Contain("stopAt"));

        this.Advance(TimeSpan.FromMinutes(1), recorder);
        Assert.That(recorder.IsRecording, Is.False);
        Assert.That(Reason(this.Lines().Last()), Is.EqualTo("stopped"));
        Assert.That(File.Exists(this.StatePath), Is.False);
    }

    [Test]
    public void Stop_WithoutDelay_EndsNow()
    {
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromHours(1));
        recorder.Stop(null);

        Assert.That(recorder.IsRecording, Is.False);
        Assert.That(Reason(this.Lines().Last()), Is.EqualTo("stopped"));
        Assert.That(File.Exists(this.StatePath), Is.False);
    }

    [Test]
    public void Stop_WhenIdle_SaysNotRecording()
    {
        using var recorder = this.NewRecorder();
        recorder.Stop(TimeSpan.FromMinutes(15));

        Assert.That(this._chat, Is.EqualTo(new[] { "[RE] Recorder : not recording" }));
        Assert.That(Directory.Exists(this._out), Is.False);
    }

    [Test]
    public void Timeout_EndsTheSession()
    {
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromMinutes(1));

        this.Advance(TimeSpan.FromSeconds(30), recorder);
        Assert.That(recorder.IsRecording, Is.True);
        this.Advance(TimeSpan.FromSeconds(31), recorder);

        Assert.That(recorder.IsRecording, Is.False);
        Assert.That(Reason(this.Lines().Last()), Is.EqualTo("timeout"));
    }

    [Test]
    public void Ticks_WriteJournalEventsAndStateRecords()
    {
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromHours(1));
        this._journal.Publish("chat", "hello");
        this.Advance(TimeSpan.FromSeconds(3), recorder);

        var lines = this.Lines();
        Assert.That(lines.Any(l => l.TryGetProperty("text", out var t) && t.GetString() == "hello"), Is.True);
        Assert.That(lines.Any(l => l.GetProperty("kind").GetString() == "state" && l.GetProperty("state").GetProperty("location").GetProperty("x").GetInt32() == 1), Is.True);
    }

    [Test]
    public void Dispose_DoesNotEnd_AndNewInstanceContinuesWithSameJournal()
    {
        var first = this.NewRecorder();
        first.Start(TimeSpan.FromHours(1));
        this._journal.Publish("chat", "one");
        this._journal.Publish("chat", "two");
        this._now += TimeSpan.FromSeconds(1);
        this._journal.Publish("chat", "three");
        first.Dispose();

        Assert.That(File.Exists(this.StatePath), Is.True);
        Assert.That(this.Lines().Any(l => l.GetProperty("kind").GetString() == "meta" && l.GetProperty("what").GetString() == "collector-stopped"), Is.False);

        this._journal.Publish("chat", "four");
        using var second = this.NewRecorder();
        this.Advance(TimeSpan.FromSeconds(3), second);

        var texts = this.Lines().Where(l => l.GetProperty("kind").GetString() == "chat").Select(l => l.GetProperty("text").GetString()).ToList();
        Assert.That(texts, Is.EqualTo(new[] { "one", "two", "three", "four" }));
        Assert.That(second.IsRecording, Is.True);
        Assert.That(this.Lines().Count(l => l.GetProperty("kind").GetString() == "meta" && l.GetProperty("what").GetString() == "collector-started"), Is.EqualTo(1));
    }

    [Test]
    public void Resume_WithNewJournal_WritesClientFoundAndReadsFromStart()
    {
        var first = this.NewRecorder();
        first.Start(TimeSpan.FromHours(1));
        this._journal.Publish("chat", "old");
        first.Dispose();

        this._journal = new EventJournal(1000, "2000-b");
        this._journal.Publish("chat", "login");
        using var second = this.NewRecorder();
        this.Advance(TimeSpan.FromSeconds(3), second);

        var lines = this.Lines();
        var found = lines.Single(l => l.GetProperty("kind").GetString() == "meta" && l.GetProperty("what").GetString() == "client-found");
        Assert.That(found.GetProperty("sameInstance").GetBoolean(), Is.False);
        Assert.That(lines.Last(l => l.GetProperty("kind").GetString() == "chat").GetProperty("text").GetString(), Is.EqualTo("login"));
        Assert.That(second.IsRecording, Is.True);
    }

    [Test]
    public void Resume_DefersUntilTheJournalExists()
    {
        var first = this.NewRecorder();
        first.Start(TimeSpan.FromHours(1));
        first.Dispose();

        string id = null;
        using var second = this.NewRecorder(() => id);
        this.Advance(TimeSpan.FromSeconds(3), second);
        Assert.That(second.IsRecording, Is.False);

        id = this._journal.Id;
        this.Advance(TimeSpan.FromSeconds(3), second);
        Assert.That(second.IsRecording, Is.True);
    }

    [Test]
    public void Resume_AfterDeadline_WritesExpiredWhileDownAndRunsAfterStop()
    {
        this.WriteConfig("{file}|{name}");
        var first = this.NewRecorder();
        first.Start(TimeSpan.FromHours(1));
        first.Dispose();

        this._journal = new EventJournal(1000, "2000-b");
        this._journal.Publish("chat", "must not be written");
        this._now += TimeSpan.FromHours(2);
        using var second = this.NewRecorder();
        second.Tick();

        var last = this.Lines().Last();
        Assert.That(Reason(last), Is.EqualTo("expired-while-down"));
        Assert.That(this.Lines().Any(l => l.TryGetProperty("text", out var t) && t.GetString() == "must not be written"), Is.False);
        Assert.That(File.Exists(this.StatePath), Is.False);
        Assert.That(second.IsRecording, Is.False);
        Assert.That(this._launcher.Calls.Count, Is.EqualTo(1));
    }

    [Test]
    public void AfterStop_RunsOncePerEnd_WithExpandedArgs_AfterTheLastLine()
    {
        this._out = Path.Combine(this.TestTemporaryDirectory, "out dir");
        this._day = Path.Combine(this._out, "2026-11-20");
        this.WriteConfig("{file}|{dir}|{name}|{char}|{date}");
        using var recorder = this.NewRecorder();

        recorder.Start(TimeSpan.FromHours(1));
        recorder.Start(TimeSpan.FromHours(1));
        recorder.Stop(null);
        recorder.Stop(null);

        var first = Path.Combine(this._day, "Cobble.jsonl");
        var second = Path.Combine(this._day, "Cobble-2.jsonl");
        Assert.That(this._launcher.Calls.Count, Is.EqualTo(2));
        Assert.That(this._launcher.Calls[0].Program, Is.EqualTo("py"));
        Assert.That(this._launcher.Calls[0].Args, Is.EqualTo($"{first}|{this._day}|Cobble|Cobble|2026-11-20"));
        Assert.That(this._launcher.Calls[0].Dir, Is.EqualTo(this._day));
        Assert.That(this._launcher.Calls[1].Args, Does.StartWith(second + "|").And.Contain("|Cobble-2|"));
        Assert.That(this._launcher.Calls.All(c => c.LastLine.Contains("collector-stopped")), Is.True);
        Assert.That(this._chat, Does.Contain("[RE] Recorder : afterStop started (pid 4242)"));
    }

    [Test]
    public void AfterStop_ThrowingLauncher_StillEndsCleanly()
    {
        this.WriteConfig("{file}");
        this._launcher.Throw = true;
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromHours(1));
        recorder.Stop(null);

        Assert.That(recorder.IsRecording, Is.False);
        Assert.That(File.Exists(this.StatePath), Is.False);
        Assert.That(this.Lines().Last().GetProperty("what").GetString(), Is.EqualTo("collector-stopped"));
        Assert.That(this._chat.Count(c => c.Contains("afterStop failed")), Is.EqualTo(1));
    }

    [Test]
    public void InvalidStateFile_IsWarnedAndDeleted()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.StatePath));
        File.WriteAllText(this.StatePath, "{ not json");
        using var recorder = this.NewRecorder();
        recorder.Tick();

        Assert.That(this._chat.Count(c => c.StartsWith("[RE] Recorder : ")), Is.EqualTo(1));
        Assert.That(File.Exists(this.StatePath), Is.False);
        Assert.That(recorder.IsRecording, Is.False);
    }

    [Test]
    public void StateFileWithFutureStart_IsInvalidAndDeleted()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.StatePath));
        RecorderFile.SaveState(this.StatePath, new RecorderState
        {
            File = Path.Combine(this._day, "Cobble.jsonl"),
            StartedAt = T0.AddHours(3),
            TimeoutAt = T0.AddHours(8),
            JournalId = this._journal.Id,
        });
        using var recorder = this.NewRecorder();
        recorder.Tick();

        Assert.That(this._chat.Single(), Does.Contain("invalid"));
        Assert.That(File.Exists(this.StatePath), Is.False);
    }

    [Test]
    public void Start_WithoutConfig_WarnsEveryTimeAndUsesDefaultFolder()
    {
        File.Delete(Path.Combine(this._install, "Recorder", "Global.json"));
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromHours(1));
        recorder.Start(TimeSpan.FromHours(1));

        Assert.That(this._chat.Count(c => c.Contains("no config file")), Is.EqualTo(2));
        Assert.That(File.Exists(Path.Combine(this._install, "Recordings", "2026-11-20", "Cobble-2.jsonl")), Is.True);
    }

    [Test]
    public void Status_ShowsTheSession()
    {
        using var recorder = this.NewRecorder();
        recorder.Start(TimeSpan.FromHours(1));
        recorder.Stop(TimeSpan.FromMinutes(5));
        this._chat.Clear();
        recorder.Status();

        Assert.That(string.Join("\n", this._chat), Does.Contain("Cobble.jsonl").And.Contain("pending stop : 2026-11-20 07:05:00").And.Contain("journal seq"));
    }
}
