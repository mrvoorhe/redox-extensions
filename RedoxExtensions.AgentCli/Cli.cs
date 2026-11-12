using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace RedoxExtensions.AgentCli;

public partial class Cli
{
    public const int ExitOk = 0;
    public const int ExitFailed = 1;
    public const int ExitUsage = 2;

    private const int DefaultWaitTimeoutMs = 10000;
    private const int ReloadSettleTimeoutMs = 30000;

    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    private const string Usage = """
        Usage: rectl [options] <command> [args]

        Talks to a RedoxExtensions agent bridge running inside acclient.  Enable it in game with
        '/re agent on' (read-only), '/re agent rw' (allow commands) or '/re agent full'.

        Options:
          -c, --character <name>   Client to talk to (exact or prefix match)
          --pid <pid>              Client to talk to by process id
          --dir <path>             Discovery directory (default: <install>\Logs\agent-bridge)
          --timeout <ms>           Timeout for wait, exec --wait and reload (default 10000)

        Commands:
          list                         Clients with the bridge enabled
          paths                        Install, discovery and VirindiTank directories (VT_DIR overrides the last)
          ping                         Bridge status for the selected client
          state                        Character, vitals, location and busy state
          characters                   Account, server and the characters on the selected client's account
          world [name-regex] [--class <c1,c2>] [--near <meters>] [--container <id>] [--offset <n>] [--limit <n>]
                                       Nearby objects, closest first (paged, --limit up to 200)
          inventory [name-regex] [--class <c1,c2>] [--container <id>] [--offset <n>] [--limit <n>]
                                       Inventory and equipped items, by name
          object <id>                  Every value Decal has for one object
          move <name-regex> --to <main|pack<N>|container id>
                                       Move matching inventory items into your main pack or a side pack (pack0 is
                                       the first in the inventory window), one at a time, confirming each.  Needs rw.
          net [off] [--server <types>] [--client <types>]
                                       Show or set which network messages are journaled as "net" events.  Types
                                       are hex (F7B0 game events, F7B1 game actions) or "all".  Resets on reload.
          exec <text> [--wait <regex>] [--kinds <k1,k2>]
                                       Run a chat command as if typed, optionally wait for a matching event
          events [--since <seq>] [--kinds <k1,k2>] [--max <n>]
                                       Recent events (kinds: chat, plugin, debug, command, net)
          wait <regex> [--since <seq>] [--kinds <k1,k2>]
                                       Wait for an event matching regex
          screenshot [--out <path>] [--scale <n>]
                                       Save the client's window as PNG, shrunk n times (default %TEMP%\rectl\<character>-<time>.png).
                                       The window can be behind others but not minimized.
          reload                       Reload the plugin and wait until it's back
          dev [--repo <path>]          copy_dlls_debug.bat, then reload
          raw <json>                   Send a raw request (token is added)
          schedule add <command> --at <time> [--repeat once|daily] [--grace <min>]
                                       Schedule a chat command on the -c character (exact name; the bridge isn't
                                       needed).  <time>: HH:MM (next time it comes round), 'tomorrow HH:MM',
                                       YYYY-MM-DDTHH:MM or +<n>m.  Runs up to --grace minutes late (default 15).
          schedule list                All characters' scheduled commands, or only -c's
          schedule remove|enable|disable <id>
                                       Change one of -c's scheduled commands
          metatimeout list             Meta chain watch overrides from Global.json, or merged with -c's file
                                       (each timeout shows whether it comes from global or the character)
          metatimeout set <meta> <minutes> [--chain-state <state>]
                                       Set a meta's timeout (decimals allowed) in Global.json, or -c's file.
                                       Without --chain-state the plugin looks in the .met for one.
          metatimeout remove|rm <meta> Remove a meta's timeout from Global.json, or -c's file
          metatimeout fallback <command>|--clear
                                       Set (or clear) the command run when a timed out meta can't be moved on

        If exactly one client has the bridge enabled it's used automatically.
        """;

    private readonly TextWriter _out;
    private readonly TextWriter _err;
    private readonly Func<string, string> _getEnv;
    private readonly Func<int, bool> _isAlive;
    private readonly Func<DateTime> _now;

    public Cli(TextWriter output, TextWriter error, Func<string, string> getEnv = null, Func<int, bool> isAlive = null, Func<DateTime> now = null)
    {
        this._out = output;
        this._err = error;
        this._getEnv = getEnv ?? Environment.GetEnvironmentVariable;
        this._isAlive = isAlive;
        this._now = now ?? (() => DateTime.Now);
    }

    public int Run(string[] args)
    {
        try
        {
            return this.RunParsed(Arguments.Parse(args));
        }
        catch (ArgumentException e)
        {
            this._err.WriteLine(e.Message);
            return ExitUsage;
        }
    }

    private int RunParsed(Arguments parsed)
    {
        if (parsed.Command == null || parsed.Command is "help" or "-h" or "--help")
        {
            this._out.WriteLine(Usage);
            return parsed.Command == null ? ExitUsage : ExitOk;
        }

        var locator = new InstanceLocator(InstanceLocator.ResolveDirectory(parsed.Get("dir"), this._getEnv), this._isAlive);

        if (parsed.Command == "list")
            return this.List(locator);
        if (parsed.Command == "paths")
            return this.Paths(locator);
        if (parsed.Command == "schedule")
            return this.Schedule(locator, parsed);
        if (parsed.Command == "metatimeout")
            return this.MetaTimeout(locator, parsed);

        var instance = locator.Select(parsed.Get("character"), parsed.GetInt("pid"), out var selectError);
        if (instance == null)
        {
            this._err.WriteLine(selectError);
            return ExitUsage;
        }

        var timeoutMs = parsed.GetInt("timeout") ?? DefaultWaitTimeoutMs;

        try
        {
            return parsed.Command switch
            {
                "ping" => this.Simple(instance, "ping", null),
                "state" => this.Simple(instance, "state", null),
                "characters" => this.Simple(instance, "characters", null),
                "world" => this.World(instance, parsed, "landscape"),
                "inventory" => this.World(instance, parsed, "inventory"),
                "object" => this.Object(instance, parsed),
                "net" => this.Net(instance, parsed),
                "screenshot" => this.Screenshot(instance, parsed),
                "move" => this.Move(instance, parsed),
                "exec" => this.Exec(instance, parsed, timeoutMs),
                "events" => this.Events(instance, parsed),
                "wait" => this.Wait(instance, parsed, timeoutMs),
                "reload" => this.Reload(instance, timeoutMs),
                "dev" => this.Dev(instance, parsed, timeoutMs),
                "raw" => this.Raw(instance, parsed),
                _ => this.UnknownCommand(parsed.Command)
            };
        }
        catch (Exception e) when (e is IOException or JsonException or System.Net.Sockets.SocketException)
        {
            this._err.WriteLine($"Talking to {instance.Character} (pid {instance.Pid}, port {instance.Port}) failed: {e.Message}");
            return ExitFailed;
        }
    }

    private int UnknownCommand(string command)
    {
        this._err.WriteLine($"Unknown command '{command}'.  Run 'rectl help'.");
        return ExitUsage;
    }

    private int List(InstanceLocator locator)
    {
        var all = locator.FindAll();
        var array = new JsonArray(all.Select(i => (JsonNode)new JsonObject
        {
            ["character"] = i.Character,
            ["server"] = i.Server,
            ["pid"] = i.Pid,
            ["port"] = i.Port,
            ["mode"] = i.Mode,
        }).ToArray());
        this.WriteJson(array);
        return ExitOk;
    }

    private int Paths(InstanceLocator locator)
    {
        var vt = InstanceLocator.ResolveVTankDirectory(this._getEnv);
        this.WriteJson(new JsonObject
        {
            ["install"] = InstanceLocator.ResolveInstallDirectory(this._getEnv),
            ["discovery"] = locator.Directory,
            ["vt"] = vt,
            ["vtExists"] = vt != null && System.IO.Directory.Exists(vt),
        });
        return vt == null ? ExitFailed : ExitOk;
    }

    private int Simple(BridgeInstance instance, string cmd, Dictionary<string, JsonNode> fields)
    {
        using var client = Connect(instance, DefaultWaitTimeoutMs);
        return this.Report(client.Send(cmd, fields));
    }

    private int World(BridgeInstance instance, Arguments parsed, string scope)
    {
        var fields = new Dictionary<string, JsonNode> { ["scope"] = scope };
        if (parsed.Positionals.Count > 0)
            fields["name"] = string.Join(" ", parsed.Positionals);
        if (parsed.Get("class") is { } classes)
            fields["class"] = KindsArray(classes);
        if (parsed.GetLong("container") is { } container)
            fields["container"] = container;
        if (parsed.Get("near") is { } near)
            fields["maxDistance"] = near;
        if (parsed.GetInt("offset") is { } offset)
            fields["offset"] = offset;
        if (parsed.GetInt("limit") is { } limit)
            fields["limit"] = limit;

        return this.Simple(instance, "world", fields);
    }

    private int Object(BridgeInstance instance, Arguments parsed)
    {
        if (parsed.Positionals.Count != 1 || !long.TryParse(parsed.Positionals[0], out var objectId))
        {
            this._err.WriteLine("object needs one object id, e.g. rectl object -263199647");
            return ExitUsage;
        }

        return this.Simple(instance, "object", new Dictionary<string, JsonNode> { ["objectId"] = objectId });
    }

    private const int MoveConfirmTimeoutMs = 15000;
    private const int MoveConfirmPollMs = 150;
    private const int MoveMaxPasses = 5;

    private enum MoveOutcome { Arrived, Merged, TimedOut }

    /// <summary>
    /// Moves every inventory item whose name matches into one container, one at a time.  Each move is confirmed by
    /// watching the item before the next is sent.  Repeats until nothing matching is left outside the destination
    /// (stack merges can leave a remainder behind).  Stops at the first failure.
    /// </summary>
    private int Move(BridgeInstance instance, Arguments parsed)
    {
        var name = string.Join(" ", parsed.Positionals);
        var toText = parsed.Get("to");
        if (name.Length == 0 || string.IsNullOrEmpty(toText))
        {
            this._err.WriteLine("move needs a name regex and --to <main|pack<N>|container id>, e.g. rectl move \"^Titan Mana Charge$\" --to pack0");
            return ExitUsage;
        }

        using var client = Connect(instance, DefaultWaitTimeoutMs);

        long? to = this.ResolveContainer(client, toText, out var resolveError);
        if (to == null)
        {
            this._err.WriteLine(resolveError);
            return resolveError.StartsWith("--to") ? ExitUsage : ExitFailed;
        }

        var moved = 0;
        var merged = 0;
        var alreadyThere = 0;
        int? matched = null;
        JsonObject failure = null;

        // Stacks can merge with a stack already in the destination.  The moved item then disappears, or the server
        // tops up the destination stack and leaves the rest behind with a new stack size.  Either counts as merged,
        // and the next pass picks up whatever is still outside the destination.
        for (var pass = 0; pass < MoveMaxPasses && failure == null; pass++)
        {
            var items = this.FindInventory(client, name, out var queryError);
            if (items == null)
                return this.Report(queryError);

            var pending = items.Where(item => item["container"]?.GetValue<long>() != to).ToList();
            if (matched == null)
            {
                matched = items.Count;
                alreadyThere = items.Count - pending.Count;
            }

            if (pending.Count == 0)
                break;

            if (pass == MoveMaxPasses - 1)
            {
                failure = new JsonObject { ["error"] = $"{pending.Count} item(s) still not in {to} after {MoveMaxPasses} passes" };
                break;
            }

            foreach (var item in pending)
            {
                var id = item["id"].GetValue<long>();
                var response = client.Send("move", new Dictionary<string, JsonNode> { ["objectId"] = id, ["container"] = to.Value });
                if (!IsOk(response))
                {
                    failure = new JsonObject { ["objectId"] = id, ["error"] = response["error"]?.GetValue<string>() };
                    break;
                }

                var outcome = this.WaitForMove(client, id, to.Value, StackOf(item));
                if (outcome == MoveOutcome.TimedOut)
                {
                    failure = new JsonObject { ["objectId"] = id, ["error"] = $"Still not in {to} after {MoveConfirmTimeoutMs} ms" };
                    break;
                }

                if (outcome == MoveOutcome.Arrived)
                    moved++;
                else
                    merged++;
            }
        }

        var result = new JsonObject
        {
            ["matched"] = matched ?? 0,
            ["moved"] = moved,
            ["merged"] = merged,
            ["alreadyThere"] = alreadyThere,
            ["container"] = to.Value,
        };
        if (failure != null)
            result["failed"] = failure;

        this.WriteJson(result);
        return failure == null ? ExitOk : ExitFailed;
    }

    /// <summary>
    /// "main" is the character, "pack&lt;N&gt;" the side pack at slot N (0 = first in the inventory window, the same
    /// order mudsort uses), anything else a container id.
    /// </summary>
    private long? ResolveContainer(BridgeClient client, string text, out string error)
    {
        error = null;
        var lowered = text.Trim().ToLowerInvariant();

        if (lowered == "main")
        {
            var state = client.Send("state");
            if (!IsOk(state))
            {
                error = state["error"]?.GetValue<string>() ?? "state failed";
                return null;
            }
            return state["result"]["character"]["id"].GetValue<long>();
        }

        if (lowered.StartsWith("pack") && int.TryParse(lowered.Substring(4), out var slot))
        {
            var packs = client.Send("world", new Dictionary<string, JsonNode> { ["scope"] = "inventory", ["class"] = "Container", ["limit"] = 200 });
            if (!IsOk(packs))
            {
                error = packs["error"]?.GetValue<string>() ?? "world failed";
                return null;
            }

            foreach (var pack in packs["result"]["objects"].AsArray())
            {
                if (pack["slot"]?.GetValue<int>() == slot)
                    return pack["id"].GetValue<long>();
            }

            error = $"No side pack in slot {slot}";
            return null;
        }

        if (long.TryParse(text, out var id))
            return id;

        error = $"--to must be main, pack<N> or a container id, not '{text}'";
        return null;
    }

    /// <summary>All inventory items matching the name regex, across pages.  Null (with the failed response) on error.</summary>
    private List<JsonNode> FindInventory(BridgeClient client, string name, out JsonObject error)
    {
        error = null;
        var items = new List<JsonNode>();
        var offset = 0;
        while (true)
        {
            var page = client.Send("world", new Dictionary<string, JsonNode>
            {
                ["scope"] = "inventory",
                ["name"] = name,
                ["offset"] = offset,
                ["limit"] = 200,
            });
            if (!IsOk(page))
            {
                error = page;
                return null;
            }

            foreach (var item in page["result"]["objects"].AsArray())
                items.Add(item);

            if (page["result"]["nextOffset"] is JsonValue next)
                offset = next.GetValue<int>();
            else
                return items;
        }
    }

    /// <summary>Stack size as the bridge reports it; "stack" is left out for single items.</summary>
    private static int StackOf(JsonNode item) => item["stack"]?.GetValue<int>() ?? 1;

    /// <summary>
    /// Watches one moved item until it's in the destination (arrived), is gone or has a different stack size
    /// (merged), or the timeout runs out.
    /// </summary>
    private MoveOutcome WaitForMove(BridgeClient client, long objectId, long container, int stack)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(MoveConfirmTimeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(MoveConfirmPollMs);
            var response = client.Send("object", new Dictionary<string, JsonNode> { ["objectId"] = objectId });
            if (!IsOk(response))
            {
                if (response["error"]?.GetValue<string>()?.Contains("No object with id") == true)
                    return MoveOutcome.Merged;
                continue;
            }

            var result = response["result"];
            if (result["container"]?.GetValue<long>() == container)
                return MoveOutcome.Arrived;
            if (StackOf(result) != stack)
                return MoveOutcome.Merged;
        }
        return MoveOutcome.TimedOut;
    }

    private int Screenshot(BridgeInstance instance, Arguments parsed)
    {
        if (!OperatingSystem.IsWindows())
        {
            this._err.WriteLine("screenshot uses PrintWindow and only works on Windows.");
            return ExitUsage;
        }

        var path = parsed.Get("out") ?? Path.Combine(
            Path.GetTempPath(), "rectl", $"{instance.Character}-{DateTime.Now:yyyyMMdd-HHmmss}.png");

        try
        {
            var (width, height) = ScreenCapture.CaptureToPng(instance.Pid, path, parsed.GetInt("scale") ?? 1);
            this.WriteJson(new JsonObject
            {
                ["path"] = Path.GetFullPath(path),
                ["width"] = width,
                ["height"] = height,
                ["character"] = instance.Character,
                ["pid"] = instance.Pid,
            });
            return ExitOk;
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            this._err.WriteLine($"Screenshot of {instance.Character} (pid {instance.Pid}) failed: {e.Message}");
            return ExitFailed;
        }
    }

    private int Net(BridgeInstance instance, Arguments parsed)
    {
        var fields = new Dictionary<string, JsonNode>();
        foreach (var positional in parsed.Positionals)
        {
            if (!positional.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                this._err.WriteLine($"Unknown net argument '{positional}'.  Use: rectl net [off] [--server <types>] [--client <types>]");
                return ExitUsage;
            }
            fields["off"] = true;
        }

        if (parsed.Get("server") is { } server)
            fields["server"] = KindsArray(server);
        if (parsed.Get("client") is { } client)
            fields["client"] = KindsArray(client);

        return this.Simple(instance, "net", fields);
    }

    private int Exec(BridgeInstance instance, Arguments parsed, int timeoutMs)
    {
        var text = string.Join(" ", parsed.Positionals);
        if (text.Length == 0)
        {
            this._err.WriteLine("exec needs the command text, e.g. rectl exec /vt start");
            return ExitUsage;
        }

        using var client = Connect(instance, timeoutMs);
        var execResponse = client.Send("exec", new Dictionary<string, JsonNode> { ["text"] = text });

        var waitRegex = parsed.Get("wait");
        if (waitRegex == null || !IsOk(execResponse))
            return this.Report(execResponse);

        var waitResponse = client.Send("wait", this.WaitFields(waitRegex, execResponse["result"]?["seq"]?.GetValue<double>(), parsed, timeoutMs));
        if (!IsOk(waitResponse))
            return this.Report(waitResponse);

        var combined = new JsonObject
        {
            ["exec"] = execResponse["result"]?.DeepClone(),
            ["wait"] = waitResponse["result"]?.DeepClone(),
        };
        this.WriteJson(combined);
        return IsMatched(waitResponse) ? ExitOk : this.NotMatched(waitRegex);
    }

    private int Events(BridgeInstance instance, Arguments parsed)
    {
        var fields = new Dictionary<string, JsonNode>();
        if (parsed.GetLong("since") is { } since)
            fields["since"] = since;
        if (parsed.GetInt("max") is { } max)
            fields["max"] = max;
        if (parsed.Get("kinds") is { } kinds)
            fields["kinds"] = KindsArray(kinds);

        return this.Simple(instance, "events", fields);
    }

    private int Wait(BridgeInstance instance, Arguments parsed, int timeoutMs)
    {
        var regex = string.Join(" ", parsed.Positionals);
        if (regex.Length == 0)
        {
            this._err.WriteLine("wait needs a regex, e.g. rectl wait \"Route saved\"");
            return ExitUsage;
        }

        using var client = Connect(instance, timeoutMs);
        var response = client.Send("wait", this.WaitFields(regex, parsed.GetLong("since"), parsed, timeoutMs));
        var exit = this.Report(response);
        return exit == ExitOk && !IsMatched(response) ? this.NotMatched(regex) : exit;
    }

    private int Reload(BridgeInstance instance, int timeoutMs)
    {
        using (var client = Connect(instance, timeoutMs + ReloadSettleTimeoutMs))
        {
            var response = client.Send("reload");
            if (!IsOk(response) && !IsGameThreadTimeout(response))
                return this.Report(response);
        }

        var deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(timeoutMs, ReloadSettleTimeoutMs));
        JsonObject lastPing = null;
        while (DateTime.UtcNow < deadline)
        {
            using (var client = Connect(instance, DefaultWaitTimeoutMs))
            {
                lastPing = client.Send("ping");
                var result = lastPing["result"];
                if (IsOk(lastPing) && result?["pluginLoaded"]?.GetValue<bool>() == true && result["handlers"] is JsonArray handlers && handlers.Count > 0)
                    return this.Report(lastPing);
            }
            Thread.Sleep(250);
        }

        this._err.WriteLine("Plugin did not come back after reload.");
        if (lastPing != null)
            this.WriteJson(lastPing);
        return ExitFailed;
    }

    private int Dev(BridgeInstance instance, Arguments parsed, int timeoutMs)
    {
        var repo = parsed.Get("repo") ?? FindRepoRoot(Directory.GetCurrentDirectory());
        if (repo == null)
        {
            this._err.WriteLine("Couldn't find the repo root (RedoxExtensions.slnx).  Pass --repo <path>.");
            return ExitUsage;
        }

        if (!OperatingSystem.IsWindows())
        {
            this._err.WriteLine("dev runs copy_dlls_debug.bat and only works on Windows.");
            return ExitUsage;
        }

        // Full path: cmd.exe won't look in the current directory when NoDefaultCurrentDirectoryInExePath is set.
        var startInfo = new ProcessStartInfo("cmd.exe", $"/c \"\"{Path.Combine(repo, "copy_dlls_debug.bat")}\"\"")
        {
            WorkingDirectory = repo,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using (var process = Process.Start(startInfo))
        {
            process.OutputDataReceived += (_, e) => { if (e.Data != null) this._err.WriteLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) this._err.WriteLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                this._err.WriteLine($"copy_dlls_debug.bat failed with exit code {process.ExitCode}; not reloading.");
                return ExitFailed;
            }
        }

        return this.Reload(instance, timeoutMs);
    }

    private int Raw(BridgeInstance instance, Arguments parsed)
    {
        var text = string.Join(" ", parsed.Positionals);
        if (JsonNode.Parse(text) is not JsonObject request)
        {
            this._err.WriteLine("raw needs a JSON object, e.g. rectl raw '{\"cmd\":\"ping\"}'");
            return ExitUsage;
        }

        using var client = Connect(instance, DefaultWaitTimeoutMs + MaxTimeout(request));
        return this.Report(client.SendRaw(request));
    }

    private Dictionary<string, JsonNode> WaitFields(string regex, double? since, Arguments parsed, int timeoutMs)
    {
        var fields = new Dictionary<string, JsonNode>
        {
            ["regex"] = regex,
            ["timeoutMs"] = timeoutMs,
        };
        if (since.HasValue)
            fields["since"] = since.Value;
        if (parsed.Get("kinds") is { } kinds)
            fields["kinds"] = KindsArray(kinds);
        return fields;
    }

    private int Report(JsonObject response)
    {
        if (IsOk(response))
        {
            this.WriteJson(response["result"]);
            return ExitOk;
        }

        this._err.WriteLine(response["error"]?.GetValue<string>() ?? response.ToJsonString());
        return ExitFailed;
    }

    private int NotMatched(string regex)
    {
        this._err.WriteLine($"Timed out waiting for /{regex}/");
        return ExitFailed;
    }

    private void WriteJson(JsonNode node)
    {
        this._out.WriteLine(node?.ToJsonString(PrettyJson) ?? "null");
    }

    private static BridgeClient Connect(BridgeInstance instance, int timeoutMs) => new(instance.Port, instance.Token, timeoutMs + 5000);

    private static bool IsOk(JsonObject response) => response["ok"]?.GetValue<bool>() == true;

    private static bool IsMatched(JsonObject response) => response["result"]?["matched"]?.GetValue<bool>() == true;

    private static bool IsGameThreadTimeout(JsonObject response) => response["error"]?.GetValue<string>()?.StartsWith("TimeoutException") == true;

    private static int MaxTimeout(JsonObject request) => request["timeoutMs"] is JsonValue v && v.TryGetValue<double>(out var ms) ? (int)ms : 0;

    private static JsonArray KindsArray(string kinds) =>
        new(kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(k => (JsonNode)k).ToArray());

    private static string FindRepoRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RedoxExtensions.slnx")))
                return dir.FullName;
        }
        return null;
    }

    private class Arguments
    {
        private static readonly Dictionary<string, string> OptionNames = new()
        {
            ["-c"] = "character",
            ["--character"] = "character",
            ["--pid"] = "pid",
            ["--dir"] = "dir",
            ["--timeout"] = "timeout",
            ["--wait"] = "wait",
            ["--kinds"] = "kinds",
            ["--since"] = "since",
            ["--max"] = "max",
            ["--repo"] = "repo",
            ["--class"] = "class",
            ["--container"] = "container",
            ["--near"] = "near",
            ["--offset"] = "offset",
            ["--limit"] = "limit",
            ["--server"] = "server",
            ["--client"] = "client",
            ["--out"] = "out",
            ["--scale"] = "scale",
            ["--to"] = "to",
            ["--at"] = "at",
            ["--repeat"] = "repeat",
            ["--grace"] = "grace",
            ["--chain-state"] = "chain-state",
        };

        private readonly Dictionary<string, string> _options = new();

        public string Command { get; private set; }

        public List<string> Positionals { get; } = new();

        public static Arguments Parse(string[] args)
        {
            var result = new Arguments();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (result.Command != "raw" && OptionNames.TryGetValue(arg, out var name))
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException($"{arg} needs a value");
                    result._options[name] = args[++i];
                }
                else if (result.Command == null)
                {
                    result.Command = arg.ToLowerInvariant();
                }
                else
                {
                    result.Positionals.Add(arg);
                }
            }
            return result;
        }

        public string Get(string name) => this._options.TryGetValue(name, out var value) ? value : null;

        public int? GetInt(string name) => this.Get(name) is { } v ? int.TryParse(v, out var i) ? i : throw new ArgumentException($"--{name} must be a number") : null;

        public long? GetLong(string name) => this.Get(name) is { } v ? long.TryParse(v, out var l) ? l : throw new ArgumentException($"--{name} must be a number") : null;
    }
}
