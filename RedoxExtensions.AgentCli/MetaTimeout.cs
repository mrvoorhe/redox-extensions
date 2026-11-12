using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using RedoxExtensions.AgentCli.MetaChainWatch;

namespace RedoxExtensions.AgentCli;

/// <summary>
/// Reads and writes the plugin's meta chain watch config files (&lt;install&gt;\MetaChainWatch\Global.json and
/// &lt;install&gt;\MetaChainWatch\&lt;Character&gt;.json).  The plugin side is RedoxExtensions/MetaChainWatch/MetaChainWatchFile.cs.
/// </summary>
public static class MetaTimeoutStore
{
    /// <summary>
    /// camelCase property names.  DictionaryKeyPolicy is left unset on purpose so meta names used as timeout keys
    /// keep their case, the same as the plugin's Newtonsoft settings.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>
    /// Null when the file doesn't exist.  A file that exists always loads with a non-null Timeouts.
    /// </summary>
    public static MetaChainWatchFileData Load(string path)
    {
        if (!File.Exists(path))
            return null;

        var data = JsonSerializer.Deserialize<MetaChainWatchFileData>(File.ReadAllText(path), Options) ?? new MetaChainWatchFileData();
        data.Timeouts ??= new Dictionary<string, MetaTimeoutEntry>();
        return data;
    }

    /// <summary>Writes to a temp file and swaps it in, so the plugin never reads half a file.</summary>
    public static void Save(string path, MetaChainWatchFileData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, path, true);
    }

    /// <summary>The keys in <paramref name="data"/> that name the same meta as <paramref name="name"/> (normalized, any case).</summary>
    public static List<string> MatchingKeys(MetaChainWatchFileData data, string name)
    {
        var normalized = MetaChainWatchRules.NormalizeName(name);
        if (data?.Timeouts == null || normalized.Length == 0)
            return new List<string>();

        return data.Timeouts.Keys
            .Where(k => string.Equals(MetaChainWatchRules.NormalizeName(k), normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Where the merged fallback command comes from: character, global or default (mirrors Merge).</summary>
    public static string FallbackSource(MetaChainWatchFileData global, MetaChainWatchFileData character)
    {
        if (!string.IsNullOrWhiteSpace(character?.FallbackCommand))
            return MetaChainWatchRules.SourceCharacter;
        if (!string.IsNullOrWhiteSpace(global?.FallbackCommand))
            return MetaChainWatchRules.SourceGlobal;
        return SourceDefault;
    }

    /// <summary>Where the merged chain state list comes from: character, global or default (mirrors Merge).</summary>
    public static string ChainStatesSource(MetaChainWatchFileData global, MetaChainWatchFileData character)
    {
        if (character?.ChainStates?.Any(s => !string.IsNullOrEmpty(s)) == true)
            return MetaChainWatchRules.SourceCharacter;
        if (global?.ChainStates?.Any(s => !string.IsNullOrEmpty(s)) == true)
            return MetaChainWatchRules.SourceGlobal;
        return SourceDefault;
    }

    public const string SourceDefault = "default";
}
