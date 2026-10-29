using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace RedoxExtensions.AgentCli;

public class Cli
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
          ping                         Bridge status for the selected client
          state                        Character, vitals, location, busy state and VT state
          exec <text> [--wait <regex>] [--kinds <k1,k2>]
                                       Run a chat command as if typed, optionally wait for a matching event
          events [--since <seq>] [--kinds <k1,k2>] [--max <n>]
                                       Recent events (kinds: chat, plugin, debug, command)
          wait <regex> [--since <seq>] [--kinds <k1,k2>]
                                       Wait for an event matching regex
          reload                       Reload the plugin and wait until it's back
          dev [--repo <path>]          copy_dlls_debug.bat, then reload
          raw <json>                   Send a raw request (token is added)

        If exactly one client has the bridge enabled it's used automatically.
        """;

    private readonly TextWriter _out;
    private readonly TextWriter _err;
    private readonly Func<string, string> _getEnv;
    private readonly Func<int, bool> _isAlive;

    public Cli(TextWriter output, TextWriter error, Func<string, string> getEnv = null, Func<int, bool> isAlive = null)
    {
        this._out = output;
        this._err = error;
        this._getEnv = getEnv ?? Environment.GetEnvironmentVariable;
        this._isAlive = isAlive;
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

    private int Simple(BridgeInstance instance, string cmd, Dictionary<string, JsonNode> fields)
    {
        using var client = Connect(instance, DefaultWaitTimeoutMs);
        return this.Report(client.Send(cmd, fields));
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

        var startInfo = new ProcessStartInfo("cmd.exe", "/c copy_dlls_debug.bat")
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
