using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;

using NUnit.Framework;

using RedoxExtensions.AgentCli;
using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

/// <summary>
/// Runs the real rectl CLI against a real bridge server on loopback, with a fake game host.
/// </summary>
[TestFixture]
public class CliTests
{
    private string _directory;
    private readonly List<IDisposable> _servers = new();
    private readonly HashSet<int> _alivePids = new();

    [SetUp]
    public void SetUp()
    {
        this._directory = Path.Combine(Path.GetTempPath(), "rectl-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this._directory);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var server in this._servers)
            server.Dispose();
        this._servers.Clear();
        this._alivePids.Clear();
        Directory.Delete(this._directory, true);
    }

    [Test]
    public void PathsDerivesTheVTankDirectoryFromTheInstallDirectory()
    {
        var install = Path.Combine(this._directory, "Games", "RedoxExtensions");
        var env = new Dictionary<string, string> { ["REDOX_EXTENSIONS_INSTALL"] = install };
        var output = new StringWriter();
        var exit = new Cli(output, new StringWriter(), name => env.TryGetValue(name, out var v) ? v : null).Run(new[] { "paths" });

        var json = JsonNode.Parse(output.ToString());
        Assert.That(exit, Is.EqualTo(Cli.ExitOk));
        Assert.That(json["install"].GetValue<string>(), Is.EqualTo(install));
        Assert.That(json["discovery"].GetValue<string>(), Is.EqualTo(Path.Combine(install, "Logs", "agent-bridge")));
        Assert.That(json["vt"].GetValue<string>(), Is.EqualTo(Path.Combine(this._directory, "Games", "VirindiPlugins", "VirindiTank")));
        Assert.That(json["vtExists"].GetValue<bool>(), Is.False);

        env["VT_DIR"] = this._directory;
        output = new StringWriter();
        new Cli(output, new StringWriter(), name => env.TryGetValue(name, out var v) ? v : null).Run(new[] { "paths" });
        Assert.That(JsonNode.Parse(output.ToString())["vtExists"].GetValue<bool>(), Is.True);
    }

    [Test]
    public void AutoSelectsTheOnlyEnabledClient()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadOnly);

        var (exit, output, _) = this.Run("ping");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk));
        Assert.That(JsonNode.Parse(output)["character"].GetValue<string>(), Is.EqualTo("Alpha"));
    }

    [Test]
    public void RequiresSelectionWhenSeveralClientsAreEnabled()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadOnly);
        this.StartBridge(200, "Bravo", AgentMode.ReadOnly);

        var (exit, _, error) = this.Run("ping");

        Assert.That(exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(error, Does.Contain("More than one").And.Contain("Alpha (pid 100").And.Contain("Bravo (pid 200"));
    }

    [Test]
    public void SelectsByCharacterPrefixOrPid()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadOnly);
        this.StartBridge(200, "Bravo", AgentMode.ReadOnly);

        var (byName, nameOutput, _) = this.Run("-c", "br", "ping");
        var (byPid, pidOutput, _) = this.Run("ping", "--pid", "100");

        Assert.That(byName, Is.EqualTo(Cli.ExitOk));
        Assert.That(JsonNode.Parse(nameOutput)["character"].GetValue<string>(), Is.EqualTo("Bravo"));
        Assert.That(byPid, Is.EqualTo(Cli.ExitOk));
        Assert.That(JsonNode.Parse(pidOutput)["character"].GetValue<string>(), Is.EqualTo("Alpha"));
    }

    [Test]
    public void IgnoresDiscoveryFilesForDeadProcesses()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadOnly);
        this.StartBridge(200, "Bravo", AgentMode.ReadOnly);
        this._alivePids.Remove(200);

        var (exit, output, _) = this.Run("ping");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk));
        Assert.That(JsonNode.Parse(output)["character"].GetValue<string>(), Is.EqualTo("Alpha"));
    }

    [Test]
    public void ReportsWhenNoClientIsEnabled()
    {
        var (exit, _, error) = this.Run("ping");

        Assert.That(exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(error, Does.Contain("/re agent on"));
    }

    [Test]
    public void ListShowsEnabledClients()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadWrite);

        var (exit, output, _) = this.Run("list");
        var list = JsonNode.Parse(output).AsArray();

        Assert.That(exit, Is.EqualTo(Cli.ExitOk));
        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list[0]["mode"].GetValue<string>(), Is.EqualTo("rw"));
    }

    [Test]
    public void ExecWithWaitSeesEventCausedByTheCommand()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        bridge.Host.OnExec = text => bridge.Journal.Publish("chat", "You said: " + text);

        var (exit, output, error) = this.Run("exec", "/say", "hello", "--wait", "you said: /say hello", "--kinds", "chat");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(bridge.Host.Executed, Is.EqualTo(new[] { "/say hello" }));
        Assert.That(JsonNode.Parse(output)["wait"]["matched"].GetValue<bool>(), Is.True);
    }

    [Test]
    public void ExecFailsInReadOnlyMode()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadOnly);

        var (exit, _, error) = this.Run("exec", "/vt start");

        Assert.That(exit, Is.EqualTo(Cli.ExitFailed));
        Assert.That(error, Does.Contain("read-only"));
        Assert.That(bridge.Host.Executed, Is.Empty);
    }

    [Test]
    public void WaitTimesOutWithFailureExit()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadOnly);

        var (exit, _, error) = this.Run("wait", "never", "--timeout", "50");

        Assert.That(exit, Is.EqualTo(Cli.ExitFailed));
        Assert.That(error, Does.Contain("Timed out"));
    }

    [Test]
    public void EventsReturnsJournal()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadOnly);
        bridge.Journal.Publish("chat", "one");
        bridge.Journal.Publish("debug", "two");

        var (exit, output, _) = this.Run("events", "--kinds", "debug");
        var events = JsonNode.Parse(output)["events"].AsArray();

        Assert.That(exit, Is.EqualTo(Cli.ExitOk));
        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0]["text"].GetValue<string>(), Is.EqualTo("two"));
    }

    [Test]
    public void ReloadWaitsForPluginHandlersToReturn()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        bridge.Host.OnReload = () =>
        {
            bridge.Handlers.Unregister("state");
            new Thread(() =>
            {
                Thread.Sleep(300);
                bridge.Handlers.Register("state", _ => null);
            }).Start();
        };

        var (exit, output, error) = this.Run("reload");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(bridge.Host.ReloadCount, Is.EqualTo(1));
        Assert.That(JsonNode.Parse(output)["handlers"].AsArray().Count, Is.EqualTo(1));
    }

    [Test]
    public void RawSendsRequestWithToken()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadOnly);
        bridge.Handlers.Register("echo", request => MiniJson.GetString(request, "value"));

        var (exit, output, _) = this.Run("raw", "{\"cmd\":\"echo\",\"value\":\"hi\"}");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk));
        Assert.That(JsonNode.Parse(output).GetValue<string>(), Is.EqualTo("hi"));
    }

    [Test]
    public void CharactersReturnsTheAccountList()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadOnly);
        bridge.Handlers.Register("characters", _ => new Dictionary<string, object>
        {
            ["account"] = "acct",
            ["current"] = "Alpha",
            ["characters"] = new List<object>
            {
                new Dictionary<string, object> { ["name"] = "Alpha", ["id"] = 1, ["index"] = 0 },
                new Dictionary<string, object> { ["name"] = "Beta", ["id"] = 2, ["index"] = 1 },
            },
        });

        var (exit, output, error) = this.Run("characters");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var result = JsonNode.Parse(output);
        Assert.That(result["account"].GetValue<string>(), Is.EqualTo("acct"));
        Assert.That(result["characters"][1]["name"].GetValue<string>(), Is.EqualTo("Beta"));
    }

    [Test]
    public void WorldSendsFiltersThatWorldQueryApplies()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadOnly);
        IDictionary<string, object> received = null;
        bridge.Handlers.Register("world", request =>
        {
            received = request;
            return RedoxExtensions.AgentBridge.WorldQuery.Parse(request).Apply(new[]
            {
                new RedoxExtensions.AgentBridge.WorldItem { Id = 1, Name = "Drudge Sneaker", ObjectClass = "Monster", Distance = 20 },
                new RedoxExtensions.AgentBridge.WorldItem { Id = 2, Name = "Drudge Ravener", ObjectClass = "Monster", Distance = 5 },
                new RedoxExtensions.AgentBridge.WorldItem { Id = 3, Name = "Drudge Charm", ObjectClass = "Misc", Distance = 1 },
            });
        });

        var (exit, output, error) = this.Run("world", "drudge", "--class", "Monster", "--near", "30", "--limit", "1");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(MiniJson.GetString(received, "scope"), Is.EqualTo("landscape"));
        Assert.That(MiniJson.GetString(received, "maxDistance"), Is.EqualTo("30"));
        var result = JsonNode.Parse(output);
        Assert.That(result["total"].GetValue<int>(), Is.EqualTo(2));
        Assert.That(result["objects"][0]["name"].GetValue<string>(), Is.EqualTo("Drudge Ravener"));
        Assert.That(result["nextOffset"].GetValue<int>(), Is.EqualTo(1));
    }

    [Test]
    public void InventoryUsesInventoryScopeAndObjectSendsObjectId()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadOnly);
        var scopes = new List<string>();
        long? objectId = null;
        bridge.Handlers.Register("world", request => { scopes.Add(MiniJson.GetString(request, "scope")); return null; });
        bridge.Handlers.Register("object", request => { objectId = MiniJson.GetLong(request, "objectId"); return null; });

        var (inventoryExit, _, _) = this.Run("inventory");
        var (objectExit, _, objectError) = this.Run("object", "-263199647");
        var (badExit, _, badError) = this.Run("object", "abc");

        Assert.That(inventoryExit, Is.EqualTo(Cli.ExitOk));
        Assert.That(scopes, Is.EqualTo(new[] { "inventory" }));
        Assert.That(objectExit, Is.EqualTo(Cli.ExitOk), objectError);
        Assert.That(objectId, Is.EqualTo(-263199647));
        Assert.That(badExit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(badError, Does.Contain("object needs one object id"));
    }

    [Test]
    public void MoveMovesEachMatchConfirmsAndSkipsItemsAlreadyThere()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var containers = new Dictionary<long, long> { [1] = 50, [2] = 60, [3] = 50 };
        var moveRequests = new List<long>();
        RegisterFakeInventory(bridge, containers, moveRequests, failOn: null);

        var (exit, output, error) = this.Run("move", "Charge", "--to", "60");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var result = JsonNode.Parse(output);
        Assert.That(result["matched"].GetValue<int>(), Is.EqualTo(3));
        Assert.That(result["moved"].GetValue<int>(), Is.EqualTo(2));
        Assert.That(result["alreadyThere"].GetValue<int>(), Is.EqualTo(1));
        Assert.That(moveRequests, Is.EqualTo(new long[] { 1, 3 }));
        Assert.That(containers.Values, Is.All.EqualTo(60));
    }

    [Test]
    public void MoveStopsAtTheFirstRefusal()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var containers = new Dictionary<long, long> { [1] = 50, [2] = 50, [3] = 50 };
        var moveRequests = new List<long>();
        RegisterFakeInventory(bridge, containers, moveRequests, failOn: 2);

        var (exit, output, _) = this.Run("move", "Charge", "--to", "60");

        Assert.That(exit, Is.EqualTo(Cli.ExitFailed));
        var result = JsonNode.Parse(output);
        Assert.That(result["moved"].GetValue<int>(), Is.EqualTo(1));
        Assert.That(result["failed"]["objectId"].GetValue<long>(), Is.EqualTo(2));
        Assert.That(result["failed"]["error"].GetValue<string>(), Does.Contain("equipped"));
        Assert.That(moveRequests, Is.EqualTo(new long[] { 1, 2 }));
    }

    [Test]
    public void MoveCountsAStackThatMergesAwayAsMerged()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var stacks = new Dictionary<long, (long Container, int Stack)> { [1] = (50, 10), [2] = (60, 20) };
        RegisterFakeStacks(bridge, stacks, onMove: (id, to) =>
        {
            stacks[2] = (to, stacks[2].Stack + stacks[id].Stack);
            stacks.Remove(id);
        });

        var (exit, output, error) = this.Run("move", "Scarab", "--to", "60");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var result = JsonNode.Parse(output);
        Assert.That(result["moved"].GetValue<int>(), Is.EqualTo(0));
        Assert.That(result["merged"].GetValue<int>(), Is.EqualTo(1));
        Assert.That(result["alreadyThere"].GetValue<int>(), Is.EqualTo(1));
        Assert.That(stacks[2].Stack, Is.EqualTo(30));
    }

    [Test]
    public void MoveMovesTheRemainderAPartialMergeLeavesBehind()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var stacks = new Dictionary<long, (long Container, int Stack)> { [1] = (50, 10), [2] = (60, 95) };
        RegisterFakeStacks(bridge, stacks, onMove: (id, to) =>
        {
            // The first move tops the destination stack up to 100 and leaves the rest where it was.
            if (stacks[2].Stack < 100)
            {
                var room = 100 - stacks[2].Stack;
                stacks[2] = (to, 100);
                stacks[id] = (stacks[id].Container, stacks[id].Stack - room);
            }
            else
            {
                stacks[id] = (to, stacks[id].Stack);
            }
        });

        var (exit, output, error) = this.Run("move", "Scarab", "--to", "60");

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var result = JsonNode.Parse(output);
        Assert.That(result["moved"].GetValue<int>(), Is.EqualTo(1));
        Assert.That(result["merged"].GetValue<int>(), Is.EqualTo(1));
        Assert.That(stacks[1], Is.EqualTo((60L, 5)));
    }

    private static void RegisterFakeStacks(FakeBridge bridge, Dictionary<long, (long Container, int Stack)> stacks, Action<long, long> onMove)
    {
        bridge.Handlers.Register("world", request => RedoxExtensions.AgentBridge.WorldQuery.Parse(request).Apply(
            stacks.Select(kv => new RedoxExtensions.AgentBridge.WorldItem
            {
                Id = (int)kv.Key,
                Name = "Mana Scarab",
                ObjectClass = "SpellComponent",
                Container = (int)kv.Value.Container,
                StackCount = kv.Value.Stack,
            }).ToList()));

        bridge.Handlers.Register("move", request =>
        {
            onMove(MiniJson.GetLong(request, "objectId").Value, MiniJson.GetLong(request, "container").Value);
            return null;
        });

        bridge.Handlers.Register("object", request =>
        {
            var id = MiniJson.GetLong(request, "objectId").Value;
            if (!stacks.TryGetValue(id, out var entry))
                throw new ArgumentException($"No object with id {id}");
            return new Dictionary<string, object> { ["id"] = id, ["container"] = entry.Container, ["stack"] = entry.Stack };
        });
    }

    [Test]
    public void MoveResolvesPackSlotsAndMain()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var targets = new List<long>();
        bridge.Handlers.Register("state", _ => new Dictionary<string, object>
        {
            ["character"] = new Dictionary<string, object> { ["id"] = 999 },
        });
        bridge.Handlers.Register("world", request =>
        {
            var query = RedoxExtensions.AgentBridge.WorldQuery.Parse(request);
            return query.Apply(new[]
            {
                new RedoxExtensions.AgentBridge.WorldItem { Id = 70, Name = "Pack", ObjectClass = "Container", Container = 999, Slot = 1 },
                new RedoxExtensions.AgentBridge.WorldItem { Id = 80, Name = "Pack", ObjectClass = "Container", Container = 999, Slot = 0 },
                new RedoxExtensions.AgentBridge.WorldItem { Id = 5, Name = "Charge", ObjectClass = "ManaStone", Container = 70 },
            });
        });
        bridge.Handlers.Register("move", request =>
        {
            targets.Add(MiniJson.GetLong(request, "container").Value);
            throw new ArgumentException("stop here");
        });

        this.Run("move", "Charge", "--to", "pack0");
        this.Run("move", "Charge", "--to", "main");
        var (missingExit, _, missingError) = this.Run("move", "Charge", "--to", "pack7");
        var (badExit, _, badError) = this.Run("move", "Charge", "--to", "somewhere");

        Assert.That(targets, Is.EqualTo(new long[] { 80, 999 }));
        Assert.That(missingExit, Is.EqualTo(Cli.ExitFailed));
        Assert.That(missingError, Does.Contain("No side pack in slot 7"));
        Assert.That(badExit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(badError, Does.Contain("--to must be"));
    }

    [Test]
    public void MoveNeedsANameAndDestination()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadWrite);

        var (exit, _, error) = this.Run("move", "Charge");

        Assert.That(exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(error, Does.Contain("--to"));
    }

    private static void RegisterFakeInventory(FakeBridge bridge, Dictionary<long, long> containers, List<long> moveRequests, long? failOn)
    {
        bridge.Handlers.Register("world", request => RedoxExtensions.AgentBridge.WorldQuery.Parse(request).Apply(
            containers.Select(kv => new RedoxExtensions.AgentBridge.WorldItem
            {
                Id = (int)kv.Key,
                Name = "Titan Mana Charge",
                ObjectClass = "ManaStone",
                Container = (int)kv.Value,
            }).ToList()));

        bridge.Handlers.Register("move", request =>
        {
            var id = MiniJson.GetLong(request, "objectId").Value;
            moveRequests.Add(id);
            if (id == failOn)
                throw new ArgumentException($"Item {id} is equipped");
            containers[id] = MiniJson.GetLong(request, "container").Value;
            return null;
        });

        bridge.Handlers.Register("object", request =>
        {
            var id = MiniJson.GetLong(request, "objectId").Value;
            return new Dictionary<string, object> { ["id"] = id, ["container"] = containers[id] };
        });
    }

    [Test]
    public void CameraPressesTheKeyTheKeymapBindsAndWaitsForTheRelease()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var requests = new List<(string Key, long Hold)>();
        bridge.Handlers.Register("key", request =>
        {
            requests.Add((MiniJson.GetString(request, "key"), MiniJson.GetLong(request, "holdMs").Value));
            return new Dictionary<string, object> { ["key"] = "NUMPAD4", ["holdMs"] = MiniJson.GetLong(request, "holdMs").Value };
        });

        var keymap = Path.Combine(this._directory, "test.keymap");
        File.WriteAllLines(keymap, new[]
        {
            "  CameraControls",
            "  [",
            "    CameraRotateLeft [ \"\" [ 0 DIK_NUMPAD4 ] ]",
            "  ]",
        });

        var started = DateTime.UtcNow;
        var (exit, output, error) = this.Run("camera", "left", "--hold", "300", "--keymap", keymap);

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(requests, Is.EqualTo(new[] { ("DIK_NUMPAD4", 300L) }));
        Assert.That(DateTime.UtcNow - started, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(300)));
        Assert.That(JsonNode.Parse(output)["action"].GetValue<string>(), Is.EqualTo("CameraRotateLeft"));
    }

    [Test]
    public void CameraRotateResetsThenHoldsTheTurnKeyForTheAngle()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var requests = new List<(string Key, long Hold)>();
        bridge.Handlers.Register("key", request =>
        {
            requests.Add((MiniJson.GetString(request, "key"), MiniJson.GetLong(request, "holdMs").Value));
            return null;
        });

        var keymap = Path.Combine(this._directory, "test.keymap");
        File.WriteAllLines(keymap, new[]
        {
            "  CameraControls",
            "  [",
            "    CameraRotateLeft [ \"\" [ 0 DIK_NUMPAD4 ] ]",
            "    CameraRotateRight [ \"\" [ 0 DIK_NUMPAD6 ] ]",
            "    CameraViewDefault [ \"\" [ 0 DIK_NUMPAD0 ] ]",
            "  ]",
        });

        var (exit, output, error) = this.Run("camera", "rotate", "-90", "--keymap", keymap);

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var quarter = (long)System.Math.Round(90 * Cli.CameraMsPerDegree);
        Assert.That(requests, Is.EqualTo(new[] { ("DIK_NUMPAD0", 100L), ("DIK_NUMPAD4", quarter) }));
        Assert.That(JsonNode.Parse(output)["presses"][0]["action"].GetValue<string>(), Is.EqualTo("CameraRotateLeft"));
    }

    [Test]
    public void CameraRotateTiltAndZoomShareOneReset()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var keys = new List<string>();
        bridge.Handlers.Register("key", request =>
        {
            keys.Add(MiniJson.GetString(request, "key"));
            return null;
        });

        var keymap = Path.Combine(this._directory, "test.keymap");
        File.WriteAllLines(keymap, new[]
        {
            "  CameraControls",
            "  [",
            "    CameraRotateRight [ \"\" [ 0 DIK_NUMPAD6 ] ]",
            "    CameraRotateUp [ \"\" [ 0 DIK_NUMPAD8 ] ]",
            "    CameraMoveAway [ \"\" [ 0 DIK_NUMPADPLUS ] ]",
            "    CameraViewDefault [ \"\" [ 0 DIK_NUMPAD0 ] ]",
            "  ]",
        });

        var (exit, output, error) = this.Run("camera", "tilt", "30", "--rotate", "90", "--zoom", "-50", "--keymap", keymap);

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(keys, Is.EqualTo(new[] { "DIK_NUMPAD0", "DIK_NUMPAD6", "DIK_NUMPAD8", "DIK_NUMPADPLUS" }));
        var json = JsonNode.Parse(output);
        Assert.That(json["tilt"].GetValue<int>(), Is.EqualTo(30));
        Assert.That(json["zoom"].GetValue<int>(), Is.EqualTo(-50));

        Assert.That(this.Run("camera", "zoom", "50", "--zoom", "20", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitUsage));
    }

    [TestCase(Cli.CameraAxis.Tilt, "90", "up", true)]
    [TestCase(Cli.CameraAxis.Tilt, "-30", "down", true)]
    [TestCase(Cli.CameraAxis.Tilt, "100", null, false)]
    [TestCase(Cli.CameraAxis.Zoom, "100", "closer", true)]
    [TestCase(Cli.CameraAxis.Zoom, "-40", "farther", true)]
    [TestCase(Cli.CameraAxis.Zoom, "-110", null, false)]
    [TestCase(Cli.CameraAxis.Zoom, "15", null, false)]
    public void CameraTiltAndZoomTakeTenSteps(Cli.CameraAxis axis, string value, string move, bool ok)
    {
        Assert.That(Cli.TryCameraStep(axis, value, out var actual, out var hold, out _), Is.EqualTo(ok));
        Assert.That(actual, Is.EqualTo(move));
        if (axis == Cli.CameraAxis.Zoom && value == "100")
            Assert.That(hold, Is.EqualTo((int)Cli.CameraZoomRangeMs));
    }

    [TestCase("90", "right", true)]
    [TestCase("-360", "left", true)]
    [TestCase("0", "right", true)]
    [TestCase("45", null, false)]
    [TestCase("370", null, false)]
    [TestCase("-370", null, false)]
    [TestCase("ninety", null, false)]
    public void CameraRotationTakesTenDegreeStepsUpTo360EitherWay(string degrees, string move, bool ok)
    {
        Assert.That(Cli.TryCameraRotation(degrees, out var actual, out var hold, out _), Is.EqualTo(ok));
        Assert.That(actual, Is.EqualTo(move));
        if (degrees == "-360")
            Assert.That(hold, Is.EqualTo((int)Cli.CameraFullTurnMs));
    }

    [Test]
    public void CameraNeedsAKnownMoveAndABoundKey()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var keymap = Path.Combine(this._directory, "empty.keymap");
        File.WriteAllText(keymap, "");

        Assert.That(this.Run("camera", "sideways", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitUsage));

        var (exit, _, error) = this.Run("camera", "left", "--keymap", keymap);
        Assert.That(exit, Is.EqualTo(Cli.ExitFailed));
        Assert.That(error, Does.Contain("CameraRotateLeft"));
    }

    [Test]
    public void KeyReportsThePluginsRefusal()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        bridge.Handlers.Register("key", _ => throw new System.ArgumentException("Unknown key 'BOGUS'"));

        var (exit, _, error) = this.Run("key", "BOGUS");

        Assert.That(exit, Is.EqualTo(Cli.ExitFailed));
        Assert.That(error, Does.Contain("Unknown key 'BOGUS'"));
    }

    [Test]
    public void BadOptionValueIsUsageError()
    {
        var (exit, _, error) = this.Run("ping", "--pid", "abc");

        Assert.That(exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(error, Does.Contain("--pid must be a number"));
    }

    private string MovementKeymap()
    {
        var keymap = Path.Combine(this._directory, "test.keymap");
        File.WriteAllLines(keymap, new[]
        {
            "  MovementCommands",
            "  [",
            "    MovementJump [ \"\" [ 0 DIK_SPACE ] ]",
            "    MovementForward [ \"\" [ 0 DIK_W ] ]",
            "    MovementBackup [ \"\" [ 0 DIK_X ] ]",
            "    MovementTurnLeft [ \"\" [ 0 DIK_A ] ]",
            "    MovementTurnRight [ \"\" [ 0 DIK_D ] ]",
            "    MovementStrafeLeft [ \"\" [ 0 DIK_Z ] ]",
            "    MovementStrafeLeft [ \"\" [ 0 DIK_A ] 0x00000004 ]",
            "    MovementStrafeRight [ \"\" [ 0 DIK_C ] ]",
            "    MovementStop [ \"\" [ 0 DIK_S ] ]",
            "    MovementRunLock [ \"\" [ 0 DIK_Q ] ]",
            "  ]",
        });
        return keymap;
    }

    [Test]
    public void GoHoldsEachMoveForItsOwnTimeAllStartingTogether()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var requests = new List<(string Key, long Hold)>();
        bridge.Handlers.Register("key", request =>
        {
            lock (requests)
                requests.Add((MiniJson.GetString(request, "key"), MiniJson.GetLong(request, "holdMs") ?? -1));
            return null;
        });

        var (exit, output, error) = this.Run("go", "turnright:300", "strafeleft", "stop", "--keymap", this.MovementKeymap());

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(requests, Is.EqualTo(new[] { ("DIK_D", 300L), ("DIK_Z", 1000L), ("DIK_S", 100L) }));
        Assert.That(JsonNode.Parse(output)["presses"][1]["action"].GetValue<string>(), Is.EqualTo("MovementStrafeLeft"));
    }

    [Test]
    public void GoRenewsTheLeaseOfALongHoldSoItEndsOnTime()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var holds = new List<long>();
        bridge.Handlers.Register("key", request =>
        {
            lock (holds)
                holds.Add(MiniJson.GetLong(request, "holdMs") ?? -1);
            return null;
        });

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var (exit, _, error) = this.Run("go", "forward:2500", "--keymap", this.MovementKeymap());

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(clock.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(2500));
        Assert.That(holds.Count, Is.GreaterThan(2));
        Assert.That(holds, Has.All.InRange(1L, (long)Cli.MoveLeaseMs));
    }

    [Test]
    public void GoNeedsKnownMovesAndBoundKeys()
    {
        this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var keymap = this.MovementKeymap();

        Assert.That(this.Run("go", "fly", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(this.Run("go", "forward:x", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(this.Run("go", "forward", "forward:200", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(this.Run("go", "turn", "400", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(this.Run("go", "face", "360", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(this.Run("go", "jump", "--meters", "5", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(this.Run("go", "walk", "--keymap", keymap).Exit, Is.EqualTo(Cli.ExitFailed), "walk is not bound in this keymap");
    }

    [Test]
    public void GoFaceTurnsTheShortWayAcrossNorthAndStopsNearTheHeading()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var character = new SimulatedCharacter(heading: 340);
        character.Register(bridge);

        var (exit, output, error) = this.Run("go", "face", "30", "--keymap", this.MovementKeymap());

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(character.KeysPressed, Does.Not.Contain("DIK_A"), "30 is to the right of 340");
        Assert.That(Math.Abs(Cli.SignedAngle(character.Heading - 30)), Is.LessThanOrEqualTo(Cli.TurnToleranceDegrees));
        Assert.That(JsonNode.Parse(output)["turned"].GetValue<double>(), Is.EqualTo(50).Within(Cli.TurnToleranceDegrees));
        Assert.That(character.Held, Is.Empty);
    }

    [Test]
    public void GoTurnTurnsLeftByTheAngle()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var character = new SimulatedCharacter(heading: 10);
        character.Register(bridge);

        var (exit, _, error) = this.Run("go", "turn", "-90", "--keymap", this.MovementKeymap());

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        Assert.That(Math.Abs(Cli.SignedAngle(character.Heading - 280)), Is.LessThanOrEqualTo(Cli.TurnToleranceDegrees));
    }

    [Test]
    public void GoMetersStopsAtTheDistance()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var character = new SimulatedCharacter(heading: 90);
        character.Register(bridge);

        var (exit, output, error) = this.Run("go", "forward", "--meters", "3", "--keymap", this.MovementKeymap());

        Assert.That(exit, Is.EqualTo(Cli.ExitOk), error);
        var moved = JsonNode.Parse(output)["moved"].GetValue<double>();
        Assert.That(moved, Is.EqualTo(3).Within(0.6));
        Assert.That(character.East, Is.EqualTo(moved).Within(0.01), "heading 90 walks east");
        Assert.That(character.Held, Is.Empty);
    }

    [Test]
    public void GoMetersGivesUpWhenStuckAndLetsGo()
    {
        var bridge = this.StartBridge(100, "Alpha", AgentMode.ReadWrite);
        var character = new SimulatedCharacter(heading: 0) { Blocked = true };
        character.Register(bridge);

        var (exit, output, error) = this.Run("go", "back", "--meters", "5", "--keymap", this.MovementKeymap());

        Assert.That(exit, Is.EqualTo(Cli.ExitFailed));
        Assert.That(error, Does.Contain("Stuck"));
        Assert.That(JsonNode.Parse(output)["stuck"].GetValue<bool>(), Is.True);
        Assert.That(character.KeysPressed, Does.Contain("DIK_X"));
        Assert.That(character.Held, Is.Empty);
    }

    /// <summary>
    /// A character on the fake bridge that turns 90 degrees a second while a turn key is held and moves 4 m a second
    /// while a move key is held, keys held the way KeyInput holds them (a lease, renewed by a new press).
    /// </summary>
    private sealed class SimulatedCharacter
    {
        private const double TurnRate = 0.09;   // degrees a ms
        private const double Speed = 0.004;     // meters a ms

        private readonly object _lock = new();
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        private readonly Dictionary<string, long> _heldUntil = new();
        private long _updatedAt;
        private double _heading;
        private double _x;
        private double _y;

        public SimulatedCharacter(double heading) => this._heading = heading;

        public bool Blocked { get; set; }

        public List<string> KeysPressed { get; } = new();

        public double Heading { get { lock (this._lock) { this.Update(); return this._heading; } } }

        public double East { get { lock (this._lock) { this.Update(); return this._x; } } }

        public List<string> Held { get { lock (this._lock) { this.Update(); return this._heldUntil.Keys.ToList(); } } }

        public void Register(FakeBridge bridge)
        {
            bridge.Handlers.Register("key", request =>
            {
                lock (this._lock)
                {
                    this.Update();
                    var key = MiniJson.GetString(request, "key");
                    if (request.TryGetValue("release", out var release) && release is true)
                    {
                        this._heldUntil.Remove(key);
                        return null;
                    }
                    this.KeysPressed.Add(key);
                    var hold = MiniJson.GetLong(request, "holdMs") ?? 100;
                    if (hold > 0)
                        this._heldUntil[key] = this._clock.ElapsedMilliseconds + hold;
                    return null;
                }
            });
            bridge.Handlers.Register("state", _ =>
            {
                lock (this._lock)
                {
                    this.Update();
                    return new Dictionary<string, object>
                    {
                        ["location"] = new Dictionary<string, object>
                        {
                            ["landcell"] = 0x7F7F0010L,
                            ["x"] = 100 + this._x,
                            ["y"] = 100 + this._y,
                            ["z"] = 0.0,
                            ["heading"] = this._heading,
                        },
                    };
                }
            });
        }

        /// <summary>Moves the character for the time since the last update, each key up to the end of its lease.</summary>
        private void Update()
        {
            var now = this._clock.ElapsedMilliseconds;
            foreach (var (key, until) in this._heldUntil.ToList())
            {
                var ms = Math.Min(now, until) - this._updatedAt;
                if (ms > 0)
                {
                    var rad = this._heading * Math.PI / 180;
                    var step = this.Blocked ? 0 : Speed * ms;
                    switch (key)
                    {
                        case "DIK_D": this._heading = (this._heading + TurnRate * ms + 360) % 360; break;
                        case "DIK_A": this._heading = (this._heading - TurnRate * ms + 360) % 360; break;
                        case "DIK_W": this._x += step * Math.Sin(rad); this._y += step * Math.Cos(rad); break;
                        case "DIK_X": this._x -= step * Math.Sin(rad); this._y -= step * Math.Cos(rad); break;
                    }
                }
                if (until <= now)
                    this._heldUntil.Remove(key);
            }
            this._updatedAt = now;
        }
    }

    private (int Exit, string Output, string Error) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var cli = new Cli(output, error, name => name == "REDOX_AGENT_DIR" ? this._directory : null, pid => this._alivePids.Contains(pid));
        var exit = cli.Run(args);
        return (exit, output.ToString(), error.ToString());
    }

    private FakeBridge StartBridge(int pid, string character, AgentMode mode)
    {
        var bridge = new FakeBridge(pid, character, mode);
        this._servers.Add(bridge);
        this._alivePids.Add(pid);
        AgentDiscovery.Write(this._directory, pid, character, "TestServer", bridge.Port, bridge.Token, mode, DateTime.UtcNow);
        return bridge;
    }

    private class FakeBridge : IDisposable
    {
        private readonly AgentBridgeServer _server;

        public FakeBridge(int pid, string character, AgentMode mode)
        {
            this.Host = new FakeHost(pid, character);
            this.Handlers.Register("state", _ => null);
            var policy = new AgentPolicy { Mode = mode };
            var processor = new AgentRequestProcessor(this.Host, policy, this.Journal, this.Handlers, this.Token);
            this._server = new AgentBridgeServer(processor.Process, null);
            this._server.Start();
        }

        public FakeHost Host { get; }
        public EventJournal Journal { get; } = new(100);
        public AgentHandlerRegistry Handlers { get; } = new();
        public string Token { get; } = Guid.NewGuid().ToString("N");
        public int Port => this._server.Port;

        public void Dispose() => this._server.Dispose();
    }

    private class FakeHost : IAgentBridgeHost
    {
        private readonly int _pid;
        private readonly string _character;

        public FakeHost(int pid, string character)
        {
            this._pid = pid;
            this._character = character;
        }

        public List<string> Executed { get; } = new();
        public int ReloadCount { get; private set; }
        public Action<string> OnExec { get; set; }
        public Action OnReload { get; set; }

        public object InvokeOnGameThread(Func<object> func) => func();

        public void Exec(string commandText)
        {
            this.Executed.Add(commandText);
            this.OnExec?.Invoke(commandText);
        }

        public void Reload()
        {
            this.ReloadCount++;
            this.OnReload?.Invoke();
        }

        public Dictionary<string, object> Describe() => new() { ["pid"] = this._pid, ["character"] = this._character, ["pluginLoaded"] = true };
    }
}
