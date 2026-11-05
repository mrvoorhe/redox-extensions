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
    public void BadOptionValueIsUsageError()
    {
        var (exit, _, error) = this.Run("ping", "--pid", "abc");

        Assert.That(exit, Is.EqualTo(Cli.ExitUsage));
        Assert.That(error, Does.Contain("--pid must be a number"));
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
