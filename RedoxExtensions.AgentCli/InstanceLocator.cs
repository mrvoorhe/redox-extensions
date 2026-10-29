using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RedoxExtensions.AgentCli;

public record BridgeInstance(int Pid, string Character, string Server, int Port, string Token, string Mode, string FilePath);

/// <summary>
/// Reads the discovery files the plugin writes to &lt;install&gt;\Logs\agent-bridge and picks one client.
/// </summary>
public class InstanceLocator
{
    private static readonly string[] KnownInstallDirectories =
    {
        @"D:\Games\Decal Plugins\RedoxExtensions",
        @"C:\Games\Decal Plugins\RedoxExtensions",
        @"E:\ac\Games\RedoxExtensions",
    };

    private readonly string _directory;
    private readonly Func<int, bool> _isAlive;

    public InstanceLocator(string directory, Func<int, bool> isAlive = null)
    {
        this._directory = directory;
        this._isAlive = isAlive ?? IsAcClientRunning;
    }

    public string Directory => this._directory;

    public static string ResolveDirectory(string explicitDirectory, Func<string, string> getEnv)
    {
        if (!string.IsNullOrEmpty(explicitDirectory))
            return explicitDirectory;

        var agentDir = getEnv("REDOX_AGENT_DIR");
        if (!string.IsNullOrEmpty(agentDir))
            return agentDir;

        var install = getEnv("REDOX_EXTENSIONS_INSTALL");
        if (!string.IsNullOrEmpty(install))
            return Path.Combine(install, "Logs", "agent-bridge");

        foreach (var known in KnownInstallDirectories)
        {
            if (System.IO.Directory.Exists(known))
                return Path.Combine(known, "Logs", "agent-bridge");
        }

        return null;
    }

    public List<BridgeInstance> FindAll()
    {
        var result = new List<BridgeInstance>();
        if (string.IsNullOrEmpty(this._directory) || !System.IO.Directory.Exists(this._directory))
            return result;

        foreach (var file in System.IO.Directory.GetFiles(this._directory, "*.json"))
        {
            var instance = TryRead(file);
            if (instance == null || instance.Mode == "off")
                continue;
            if (!this._isAlive(instance.Pid))
                continue;
            result.Add(instance);
        }

        return result.OrderBy(i => i.Character, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public BridgeInstance Select(string character, int? pid, out string error)
    {
        error = null;
        var all = this.FindAll();

        if (pid.HasValue)
        {
            var byPid = all.FirstOrDefault(i => i.Pid == pid.Value);
            if (byPid == null)
                error = $"No client with agent bridge enabled has pid {pid.Value}.{Describe(all)}";
            return byPid;
        }

        if (!string.IsNullOrEmpty(character))
        {
            var exact = all.Where(i => string.Equals(i.Character, character, StringComparison.OrdinalIgnoreCase)).ToList();
            var matches = exact.Count > 0 ? exact : all.Where(i => i.Character != null && i.Character.StartsWith(character, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 1)
                return matches[0];
            error = matches.Count == 0
                ? $"No client with agent bridge enabled matches '{character}'.{Describe(all)}"
                : $"'{character}' matches more than one client.  Use --pid or a longer name.{Describe(matches)}";
            return null;
        }

        if (all.Count == 1)
            return all[0];

        error = all.Count == 0
            ? $"No clients have the agent bridge enabled.  Run '/re agent on' (or rw) in game.  Looked in: {this._directory ?? "(no install directory found; set REDOX_EXTENSIONS_INSTALL or pass --dir)"}"
            : $"More than one client has the agent bridge enabled.  Pick one with -c <character> or --pid.{Describe(all)}";
        return null;
    }

    private static string Describe(IEnumerable<BridgeInstance> instances)
    {
        var lines = instances.Select(i => $"  {i.Character} (pid {i.Pid}, {i.Mode})").ToList();
        return lines.Count == 0 ? string.Empty : Environment.NewLine + string.Join(Environment.NewLine, lines);
    }

    private static BridgeInstance TryRead(string file)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            return new BridgeInstance(
                root.GetProperty("pid").GetInt32(),
                root.TryGetProperty("character", out var c) ? c.GetString() : null,
                root.TryGetProperty("server", out var s) ? s.GetString() : null,
                root.GetProperty("port").GetInt32(),
                root.GetProperty("token").GetString(),
                root.TryGetProperty("mode", out var m) ? m.GetString() : null,
                file);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsAcClientRunning(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited && string.Equals(process.ProcessName, "acclient", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
