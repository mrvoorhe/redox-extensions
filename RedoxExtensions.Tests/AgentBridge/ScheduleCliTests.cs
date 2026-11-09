using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

using NUnit.Framework;

using RedoxExtensions.AgentCli;

namespace RedoxExtensions.Tests.AgentBridge;

/// <summary>
/// rectl schedule ... against a temp install directory.  The plugin reads the same files (see SchedulingTests).
/// </summary>
[TestFixture]
public class ScheduleCliTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 20, 30, 0);

    private string _install;

    [SetUp]
    public void SetUp()
    {
        this._install = Path.Combine(Path.GetTempPath(), "rectl-schedule-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this._install);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(this._install, true);
    }

    private (int Exit, string Out, string Err) Run(params string[] args)
    {
        var env = new Dictionary<string, string> { ["REDOX_EXTENSIONS_INSTALL"] = this._install };
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = new Cli(output, error, name => env.TryGetValue(name, out var v) ? v : null, now: () => Now).Run(args);
        return (exit, output.ToString(), error.ToString());
    }

    private string FileFor(string character) => Path.Combine(this._install, "ScheduledCommands", character + ".json");

    [Test]
    public void AddWritesTheEntryAndPrintsItsNextRun()
    {
        var (exit, output, error) = this.Run("-c", "Some Char", "schedule", "add", "/re sm daily", "--at", "tomorrow 06:00", "--repeat", "daily");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var json = JsonNode.Parse(output);
        Assert.That(json["command"].GetValue<string>(), Is.EqualTo("/re sm daily"));
        Assert.That(json["at"].GetValue<string>(), Is.EqualTo("2026-11-09T06:00:00"));
        Assert.That(json["repeat"].GetValue<string>(), Is.EqualTo("daily"));
        Assert.That(json["graceMinutes"].GetValue<int>(), Is.EqualTo(15));
        Assert.That(json["nextRun"].GetValue<string>(), Is.EqualTo("2026-11-09T06:00:00"));
        Assert.That(json["status"].GetValue<string>(), Is.EqualTo("waiting"));
        Assert.That(error, Does.Contain("no schedule file yet"));

        var file = JsonNode.Parse(File.ReadAllText(this.FileFor("Some Char")));
        Assert.That(file["entries"].AsArray(), Has.Count.EqualTo(1));
        Assert.That(file["entries"][0]["id"].GetValue<string>(), Has.Length.EqualTo(6));
        Assert.That(file["entries"][0]["enabled"].GetValue<bool>(), Is.True);
    }

    [Test]
    public void AddJoinsAnUnquotedCommand()
    {
        var (exit, output, _) = this.Run("-c", "Char", "schedule", "add", "/re", "help", "--at", "+2m");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk));
        var json = JsonNode.Parse(output);
        Assert.That(json["command"].GetValue<string>(), Is.EqualTo("/re help"));
        Assert.That(json["at"].GetValue<string>(), Is.EqualTo("2026-11-08T20:32:00"));
        Assert.That(json["repeat"].GetValue<string>(), Is.EqualTo("once"));
    }

    [TestCase("06:00", "2026-11-09T06:00:00")]
    [TestCase("21:15", "2026-11-08T21:15:00")]
    [TestCase("tomorrow 7:05", "2026-11-09T07:05:00")]
    [TestCase("2026-11-10T06:00", "2026-11-10T06:00:00")]
    [TestCase("2026-11-10 06:00:30", "2026-11-10T06:00:30")]
    [TestCase("+1h", "2026-11-08T21:30:00")]
    public void ParseAtAcceptsTheDocumentedForms(string text, string expected)
    {
        Assert.That(ScheduleStore.ParseAt(text, Now).ToString(ScheduleStore.TimeFormat), Is.EqualTo(expected));
    }

    [TestCase("6am")]
    [TestCase("25:00")]
    [TestCase("tomorrow")]
    public void ParseAtRejectsOtherText(string text)
    {
        Assert.That(() => ScheduleStore.ParseAt(text, Now), Throws.ArgumentException);
    }

    [Test]
    public void AddRejectsBadInput()
    {
        Assert.That(this.Run("schedule", "add", "/re help", "--at", "06:00").Exit, Is.EqualTo(Cli.ExitUsage), "no -c");
        Assert.That(this.Run("-c", "Char", "schedule", "add", "--at", "06:00").Exit, Is.EqualTo(Cli.ExitUsage), "no command");
        Assert.That(this.Run("-c", "Char", "schedule", "add", "/re help").Exit, Is.EqualTo(Cli.ExitUsage), "no --at");
        Assert.That(this.Run("-c", "Char", "schedule", "add", "/re help", "--at", "2026-11-08T20:00").Exit, Is.EqualTo(Cli.ExitUsage), "past");
        Assert.That(this.Run("-c", "Char", "schedule", "add", "/re help", "--at", "06:00", "--repeat", "weekly").Exit, Is.EqualTo(Cli.ExitUsage), "repeat");
        Assert.That(File.Exists(this.FileFor("Char")), Is.False);
    }

    [Test]
    public void ListShowsEveryCharacterOrJustOne()
    {
        this.Run("-c", "Alpha", "schedule", "add", "/re help", "--at", "06:00");
        this.Run("-c", "Beta", "schedule", "add", "/re help", "--at", "07:00", "--repeat", "daily");

        var all = JsonNode.Parse(this.Run("schedule", "list").Out).AsArray();
        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all[0]["character"].GetValue<string>(), Is.EqualTo("Alpha"));
        Assert.That(all[1]["entries"][0]["repeat"].GetValue<string>(), Is.EqualTo("daily"));

        var one = JsonNode.Parse(this.Run("-c", "Beta", "schedule", "list").Out).AsArray();
        Assert.That(one, Has.Count.EqualTo(1));
        Assert.That(one[0]["entries"][0]["nextRun"].GetValue<string>(), Is.EqualTo("2026-11-09T07:00:00"));
    }

    [Test]
    public void RemoveEnableAndDisableChangeOneEntryAndKeepLastRun()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.FileFor("Char")));
        File.WriteAllText(this.FileFor("Char"), """
            { "entries": [
              { "id": "aaa111", "command": "/re sm daily", "at": "2026-11-01T06:00:00", "repeat": "daily",
                "graceMinutes": 15, "enabled": true, "lastRun": "2026-11-08T06:00:02" },
              { "id": "bbb222", "command": "/re help", "at": "2026-11-09T06:00:00", "repeat": "once",
                "graceMinutes": 15, "enabled": true } ] }
            """);

        var disabled = this.Run("-c", "Char", "schedule", "disable", "aaa111");
        Assert.That(disabled.Exit, Is.EqualTo(Cli.ExitOk), disabled.Err);
        Assert.That(JsonNode.Parse(disabled.Out)["status"].GetValue<string>(), Is.EqualTo("disabled"));

        var file = JsonNode.Parse(File.ReadAllText(this.FileFor("Char")));
        Assert.That(file["entries"][0]["enabled"].GetValue<bool>(), Is.False);
        Assert.That(file["entries"][0]["lastRun"].GetValue<string>(), Is.EqualTo("2026-11-08T06:00:02"));

        Assert.That(this.Run("-c", "Char", "schedule", "enable", "aaa111").Exit, Is.EqualTo(Cli.ExitOk));
        Assert.That(this.Run("-c", "Char", "schedule", "remove", "bbb222").Exit, Is.EqualTo(Cli.ExitOk));
        Assert.That(this.Run("-c", "Char", "schedule", "remove", "nope00").Exit, Is.EqualTo(Cli.ExitFailed));

        file = JsonNode.Parse(File.ReadAllText(this.FileFor("Char")));
        Assert.That(file["entries"].AsArray(), Has.Count.EqualTo(1));
        Assert.That(file["entries"][0]["enabled"].GetValue<bool>(), Is.True);
        Assert.That(file["entries"][0]["lastRun"].GetValue<string>(), Is.EqualTo("2026-11-08T06:00:02"));
    }

    [Test]
    public void ABrokenFileIsReportedAndLeftAlone()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.FileFor("Char")));
        File.WriteAllText(this.FileFor("Char"), "{ not json");

        var result = this.Run("-c", "Char", "schedule", "add", "/re help", "--at", "06:00");

        Assert.That(result.Exit, Is.EqualTo(Cli.ExitFailed));
        Assert.That(File.ReadAllText(this.FileFor("Char")), Is.EqualTo("{ not json"));
    }
}
