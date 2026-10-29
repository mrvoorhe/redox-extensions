using System;
using System.Collections.Generic;
using System.IO;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// One file per enabled client so tools can find the port and token by character name or pid.
/// </summary>
public static class AgentDiscovery
{
    public const string DirectoryName = "agent-bridge";

    public static string DefaultDirectory => Path.Combine(WrapperUtilities.LogDirectory, DirectoryName);

    public static string FilePathFor(string directory, int pid) => Path.Combine(directory, pid + ".json");

    public static void Write(string directory, int pid, string character, string server, int port, string token, AgentMode mode, DateTime startedUtc)
    {
        Directory.CreateDirectory(directory);

        var info = new Dictionary<string, object>
        {
            ["pid"] = pid,
            ["character"] = character,
            ["server"] = server,
            ["port"] = port,
            ["token"] = token,
            ["mode"] = AgentPolicy.ModeName(mode),
            ["startedUtc"] = startedUtc,
        };

        var path = FilePathFor(directory, pid);
        var tmpPath = path + ".tmp";
        File.WriteAllText(tmpPath, MiniJson.Serialize(info));
        if (File.Exists(path))
            File.Delete(path);
        File.Move(tmpPath, path);
    }

    public static void Delete(string directory, int pid)
    {
        var path = FilePathFor(directory, pid);
        if (File.Exists(path))
            File.Delete(path);
    }
}
