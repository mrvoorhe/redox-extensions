using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

using NUnit.Framework;

using RedoxExtensions.AgentCli;

using CliData = RedoxExtensions.AgentCli.MetaChainWatch;
using PluginData = RedoxExtensions.MetaChainWatch;

namespace RedoxExtensions.Tests.AgentBridge;

/// <summary>
/// rectl metatimeout ... against a temp install directory, plus checks that the plugin reads what rectl writes.
/// </summary>
[TestFixture]
public class MetaTimeoutCliTests
{
    private string _install;

    [SetUp]
    public void SetUp()
    {
        this._install = Path.Combine(Path.GetTempPath(), "rectl-metatimeout-tests-" + Guid.NewGuid().ToString("N"));
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
        var exit = new Cli(output, error, name => env.TryGetValue(name, out var v) ? v : null).Run(args);
        return (exit, output.ToString(), error.ToString());
    }

    private string GlobalFile => Path.Combine(this._install, "MetaChainWatch", "Global.json");

    private string FileFor(string character) => Path.Combine(this._install, "MetaChainWatch", character + ".json");

    private static JsonObject ReadFile(string path) => JsonNode.Parse(File.ReadAllText(path)).AsObject();

    [Test]
    public void SetWritesTheGlobalFileWithTheKeyCaseKept()
    {
        var (exit, output, error) = this.Run("metatimeout", "set", "Attributes4.0.met", "10");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var json = JsonNode.Parse(output);
        Assert.That(json["scope"].GetValue<string>(), Is.EqualTo("global"));
        Assert.That(json["timeout"]["name"].GetValue<string>(), Is.EqualTo("Attributes4.0"));
        Assert.That(json["file"].GetValue<string>(), Is.EqualTo(this.GlobalFile));

        var text = File.ReadAllText(this.GlobalFile);
        Assert.That(text, Does.Contain("\"Attributes4.0\""));
        Assert.That(text, Does.Not.Contain("fallbackCommand"), "nulls are left out");
        var file = ReadFile(this.GlobalFile);
        Assert.That(file["timeouts"]["Attributes4.0"]["minutes"].GetValue<double>(), Is.EqualTo(10));
        Assert.That(file["timeouts"]["Attributes4.0"]["chainState"], Is.Null);
        Assert.That(File.Exists(this.GlobalFile + ".tmp"), Is.False);
    }

    [Test]
    public void SetTakesDecimalMinutesAndAChainState()
    {
        var (exit, _, error) = this.Run("metatimeout", "set", "aKTTouTou", "0.5", "--chain-state", "TT-chain");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var entry = ReadFile(this.GlobalFile)["timeouts"]["aKTTouTou"];
        Assert.That(entry["minutes"].GetValue<double>(), Is.EqualTo(0.5));
        Assert.That(entry["chainState"].GetValue<string>(), Is.EqualTo("TT-chain"));
    }

    [Test]
    public void SetReplacesAnEntryWithTheSameNameInAnyCase()
    {
        this.Run("metatimeout", "set", "howard", "20");
        var (exit, output, error) = this.Run("metatimeout", "set", "Howard", "10");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(JsonNode.Parse(output)["replaced"][0].GetValue<string>(), Is.EqualTo("howard"));
        var timeouts = ReadFile(this.GlobalFile)["timeouts"].AsObject();
        Assert.That(timeouts.Select(p => p.Key), Is.EqualTo(new[] { "Howard" }));
        Assert.That(timeouts["Howard"]["minutes"].GetValue<double>(), Is.EqualTo(10));
    }

    [TestCase("0")]
    [TestCase("-5")]
    [TestCase("ten")]
    [TestCase("1,5")]
    [TestCase("NaN")]
    public void SetRejectsBadMinutes(string minutes)
    {
        var (exit, _, error) = this.Run("metatimeout", "set", "Howard", minutes);

        Assert.That(exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(error, Does.Contain("<minutes>"));
        Assert.That(File.Exists(this.GlobalFile), Is.False);
    }

    [Test]
    public void SetWithACharacterWritesThatCharactersFile()
    {
        var (exit, output, error) = this.Run("-c", "Some Char", "metatimeout", "set", "Howard", "5");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(JsonNode.Parse(output)["scope"].GetValue<string>(), Is.EqualTo("character"));
        Assert.That(error, Does.Contain("no meta chain watch file yet"));
        Assert.That(ReadFile(this.FileFor("Some Char"))["timeouts"]["Howard"]["minutes"].GetValue<double>(), Is.EqualTo(5));
        Assert.That(File.Exists(this.GlobalFile), Is.False);
    }

    [Test]
    public void RemoveDeletesTheEntryInAnyCase()
    {
        this.Run("metatimeout", "set", "Howard", "10");
        this.Run("metatimeout", "set", "LaLaRing", "10");

        var (exit, output, error) = this.Run("metatimeout", "rm", "HOWARD.met");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(JsonNode.Parse(output)["removed"][0].GetValue<string>(), Is.EqualTo("Howard"));
        Assert.That(ReadFile(this.GlobalFile)["timeouts"].AsObject().Select(p => p.Key), Is.EqualTo(new[] { "LaLaRing" }));
    }

    [Test]
    public void RemoveFailsWhenTheMetaIsNotThere()
    {
        this.Run("metatimeout", "set", "Howard", "10");

        var (exit, _, error) = this.Run("-c", "Some Char", "metatimeout", "remove", "Howard");

        Assert.That(exit, Is.EqualTo(Cli.ExitFailed));
        Assert.That(error, Does.Contain("No timeout for 'Howard'"));
        Assert.That(File.Exists(this.FileFor("Some Char")), Is.False);
    }

    [Test]
    public void FallbackJoinsTheCommandAndClearRemovesIt()
    {
        var (exit, _, error) = this.Run("-c", "Char", "metatimeout", "fallback", "/vt", "meta", "load", "zzMCW_End");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(ReadFile(this.FileFor("Char"))["fallbackCommand"].GetValue<string>(), Is.EqualTo("/vt meta load zzMCW_End"));

        (exit, _, error) = this.Run("-c", "Char", "metatimeout", "fallback", "--clear");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(ReadFile(this.FileFor("Char")).ContainsKey("fallbackCommand"), Is.False);
    }

    [Test]
    public void FallbackNeedsACommand()
    {
        var (exit, _, _) = this.Run("metatimeout", "fallback");

        Assert.That(exit, Is.EqualTo(Cli.ExitUsage));
    }

    [Test]
    public void ListWithoutACharacterShowsTheGlobalFileWithDefaults()
    {
        this.Run("metatimeout", "set", "Zeta", "10");
        this.Run("metatimeout", "set", "alpha", "20", "--chain-state", "* Chain *");

        var (exit, output, error) = this.Run("metatimeout", "list");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var json = JsonNode.Parse(output);
        Assert.That(json["globalFile"].GetValue<string>(), Is.EqualTo(this.GlobalFile));
        Assert.That(json["character"], Is.Null);
        Assert.That(json["fallbackCommand"].GetValue<string>(), Is.EqualTo("/re sm aFarm"));
        Assert.That(json["fallbackSource"].GetValue<string>(), Is.EqualTo("default"));
        Assert.That(json["chainStatesSource"].GetValue<string>(), Is.EqualTo("default"));
        Assert.That(json["chainStates"].AsArray().Select(s => s.GetValue<string>()), Is.EqualTo(new[] { "** Chain **", "* Chain *", "TT-chain", "Chain" }));
        var timeouts = json["timeouts"].AsArray();
        Assert.That(timeouts.Select(t => t["name"].GetValue<string>()), Is.EqualTo(new[] { "alpha", "Zeta" }));
        Assert.That(timeouts[0]["chainState"].GetValue<string>(), Is.EqualTo("* Chain *"));
        Assert.That(timeouts.Select(t => t["source"].GetValue<string>()), Is.All.EqualTo("global"));
    }

    [Test]
    public void ListWithACharacterShowsTheOverrideOverGlobal()
    {
        this.Run("metatimeout", "set", "Howard", "10");
        this.Run("metatimeout", "set", "LaLaRing", "15");
        this.Run("metatimeout", "fallback", "/re sm aFarm2");
        this.Run("-c", "Char", "metatimeout", "set", "howard", "0.5");

        var (exit, output, error) = this.Run("metatimeout", "list", "-c", "Char");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var json = JsonNode.Parse(output);
        Assert.That(json["characterFile"].GetValue<string>(), Is.EqualTo(this.FileFor("Char")));
        Assert.That(json["fallbackCommand"].GetValue<string>(), Is.EqualTo("/re sm aFarm2"));
        Assert.That(json["fallbackSource"].GetValue<string>(), Is.EqualTo("global"));
        var timeouts = json["timeouts"].AsArray();
        Assert.That(timeouts, Has.Count.EqualTo(2));
        Assert.That(timeouts[0]["name"].GetValue<string>(), Is.EqualTo("howard"), "the character's spelling wins");
        Assert.That(timeouts[0]["minutes"].GetValue<double>(), Is.EqualTo(0.5));
        Assert.That(timeouts[0]["source"].GetValue<string>(), Is.EqualTo("character"));
        Assert.That(timeouts[1]["name"].GetValue<string>(), Is.EqualTo("LaLaRing"));
        Assert.That(timeouts[1]["source"].GetValue<string>(), Is.EqualTo("global"));
    }

    [Test]
    public void ThePluginReadsWhatRectlWrites()
    {
        var path = this.FileFor("Char");
        MetaTimeoutStore.Save(path, new CliData.MetaChainWatchFileData
        {
            FallbackCommand = "/re sm aFarm",
            ChainStates = new List<string> { "TT-chain" },
            Timeouts = new Dictionary<string, CliData.MetaTimeoutEntry>
            {
                ["Attributes4.0"] = new() { Minutes = 10 },
                ["aKTTouTou"] = new() { Minutes = 27.5, ChainState = "TT-chain" },
            },
        });

        var loaded = PluginData.MetaChainWatchFile.Load(path);

        Assert.That(loaded.FallbackCommand, Is.EqualTo("/re sm aFarm"));
        Assert.That(loaded.ChainStates, Is.EqualTo(new[] { "TT-chain" }));
        Assert.That(loaded.Timeouts.Keys, Is.EquivalentTo(new[] { "Attributes4.0", "aKTTouTou" }));
        Assert.That(loaded.Timeouts["Attributes4.0"].Minutes, Is.EqualTo(10));
        Assert.That(loaded.Timeouts["Attributes4.0"].ChainState, Is.Null);
        Assert.That(loaded.Timeouts["aKTTouTou"].Minutes, Is.EqualTo(27.5));
        Assert.That(loaded.Timeouts["aKTTouTou"].ChainState, Is.EqualTo("TT-chain"));
    }

    [Test]
    public void RectlReadsWhatThePluginWrites()
    {
        var path = this.GlobalFile;
        PluginData.MetaChainWatchFile.Save(path, new PluginData.MetaChainWatchFileData
        {
            Timeouts = new Dictionary<string, PluginData.MetaTimeoutEntry>
            {
                ["Attributes4.0"] = new() { Minutes = 10 },
                ["aKTTouTou"] = new() { Minutes = 30, ChainState = "TT-chain" },
            },
        });

        var loaded = MetaTimeoutStore.Load(path);

        Assert.That(loaded.FallbackCommand, Is.Null);
        Assert.That(loaded.ChainStates, Is.Null);
        Assert.That(loaded.Timeouts.Keys, Is.EquivalentTo(new[] { "Attributes4.0", "aKTTouTou" }));
        Assert.That(loaded.Timeouts["aKTTouTou"].Minutes, Is.EqualTo(30));
        Assert.That(loaded.Timeouts["aKTTouTou"].ChainState, Is.EqualTo("TT-chain"));

        // And rectl can edit it in place.
        var (exit, _, error) = this.Run("metatimeout", "set", "attributes4.0", "12");
        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var reloaded = PluginData.MetaChainWatchFile.Load(path);
        Assert.That(reloaded.Timeouts.Keys, Is.EquivalentTo(new[] { "attributes4.0", "aKTTouTou" }));
    }

    [Test]
    public void LoadOfAMissingFileIsNull()
    {
        Assert.That(MetaTimeoutStore.Load(this.GlobalFile), Is.Null);
    }

    [Test]
    public void UnknownSubcommandIsAUsageError()
    {
        var (exit, _, _) = this.Run("metatimeout", "frob");

        Assert.That(exit, Is.EqualTo(Cli.ExitUsage));
    }
}
