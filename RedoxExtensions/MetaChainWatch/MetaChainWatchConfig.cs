using System;
using System.Collections.Generic;
using System.IO;

// Shared by the plugin (net35) and rectl (net10.0, linked source).  Keep it free of serializer and Decal types.
// rectl compiles it into its own namespace so the tests, which reference both assemblies, can tell them apart.
#if REDOX_AGENTCLI
using RedoxExtensions.AgentCli.Scheduling;

namespace RedoxExtensions.AgentCli.MetaChainWatch;
#else
using RedoxExtensions.Scheduling;

namespace RedoxExtensions.MetaChainWatch;
#endif

/// <summary>
/// The contents of one meta chain watch config file: &lt;install&gt;\MetaChainWatch\Global.json or
/// &lt;install&gt;\MetaChainWatch\&lt;Character&gt;.json.  Every field is optional.
/// </summary>
public class MetaChainWatchFileData
{
    /// <summary>The command to run when a timed out meta can't be moved along its chain, e.g. "/re sm aFarm".</summary>
    public string FallbackCommand { get; set; }

    /// <summary>The state names to look for in a .met file, in order.</summary>
    public List<string> ChainStates { get; set; }

    /// <summary>Per meta timeout overrides, keyed by meta name (without .met).</summary>
    public Dictionary<string, MetaTimeoutEntry> Timeouts { get; set; } = new Dictionary<string, MetaTimeoutEntry>();
}

/// <summary>
/// A per meta timeout override.
/// </summary>
public class MetaTimeoutEntry
{
    /// <summary>The meta's own limit.  It only counts when it is shorter than the main timeout.</summary>
    public double Minutes { get; set; }

    /// <summary>The state to jump to when the meta times out.  When null the .met file is scanned for one.</summary>
    public string ChainState { get; set; }
}

/// <summary>
/// One timeout override after merging the global and character files.
/// </summary>
public class MergedMetaTimeout
{
    public string Name { get; set; }
    public double Minutes { get; set; }
    public string ChainState { get; set; }

    /// <summary><see cref="MetaChainWatchRules.SourceGlobal"/> or <see cref="MetaChainWatchRules.SourceCharacter"/>.</summary>
    public string Source { get; set; }
}

/// <summary>
/// The global and character files merged, with the defaults filled in.
/// </summary>
public class MergedMetaChainWatchConfig
{
    public string FallbackCommand { get; set; }
    public List<string> ChainStates { get; set; }

    /// <summary>Keyed by normalized meta name, case-insensitive.</summary>
    public Dictionary<string, MergedMetaTimeout> Timeouts { get; set; } =
        new Dictionary<string, MergedMetaTimeout>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// The limit that applies to one meta.
/// </summary>
public class MetaTimeLimit
{
    public TimeSpan Limit { get; set; }

    /// <summary>True when a per meta override set the limit, false when it is the main timeout.</summary>
    public bool IsOverride { get; set; }
}

public static class MetaChainWatchRules
{
    public const string DirectoryName = "MetaChainWatch";
    public const string GlobalFileName = "Global.json";
    public const string DefaultFallbackCommand = "/re sm aFarm";
    public const string SourceGlobal = "global";
    public const string SourceCharacter = "character";

    /// <summary>The chain state names our metas use, in the order they are looked for.</summary>
    public static readonly string[] DefaultChainStates = { "** Chain **", "* Chain *", "TT-chain", "Chain" };

    public static string GlobalFilePath(string installDirectory)
    {
        return Path.Combine(Path.Combine(installDirectory, DirectoryName), GlobalFileName);
    }

    public static string CharacterFilePath(string installDirectory, string characterName)
    {
        return Path.Combine(Path.Combine(installDirectory, DirectoryName), ScheduleRules.SafeFileName(characterName) + ".json");
    }

    /// <summary>
    /// Trims and removes a trailing .met (any case), the same as VTActions.NormalizeMetaName.  Null becomes empty.
    /// </summary>
    public static string NormalizeName(string metaName)
    {
        var trimmed = (metaName ?? string.Empty).Trim();
        return trimmed.EndsWith(".met", StringComparison.OrdinalIgnoreCase)
            ? trimmed.Substring(0, trimmed.Length - ".met".Length).Trim()
            : trimmed;
    }

    /// <summary>
    /// Merges the files.  Either may be null.  Character values replace global values, which replace the defaults.
    /// A character timeout replaces the whole global entry with the same (normalized, case-insensitive) name.
    /// Blank fallback commands and empty chain state lists count as not set.
    /// </summary>
    public static MergedMetaChainWatchConfig Merge(MetaChainWatchFileData global, MetaChainWatchFileData character)
    {
        var merged = new MergedMetaChainWatchConfig
        {
            FallbackCommand = FirstNonBlank(character?.FallbackCommand, global?.FallbackCommand) ?? DefaultFallbackCommand,
            ChainStates = CleanChainStates(character?.ChainStates)
                          ?? CleanChainStates(global?.ChainStates)
                          ?? new List<string>(DefaultChainStates),
        };

        AddTimeouts(merged, global, SourceGlobal);
        AddTimeouts(merged, character, SourceCharacter);
        return merged;
    }

    /// <summary>The override for a meta, or null.</summary>
    public static MergedMetaTimeout FindTimeout(MergedMetaChainWatchConfig merged, string metaName)
    {
        if (merged?.Timeouts == null)
            return null;
        merged.Timeouts.TryGetValue(NormalizeName(metaName), out var entry);
        return entry;
    }

    /// <summary>
    /// min(override, main).  An override counts only when it is positive and shorter than the main timeout.
    /// </summary>
    public static MetaTimeLimit EffectiveLimit(MergedMetaChainWatchConfig merged, string metaName, double mainMinutes)
    {
        var entry = FindTimeout(merged, metaName);
        if (entry != null && entry.Minutes > 0 && entry.Minutes < mainMinutes)
            return new MetaTimeLimit { Limit = TimeSpan.FromMinutes(entry.Minutes), IsOverride = true };

        return new MetaTimeLimit { Limit = TimeSpan.FromMinutes(mainMinutes), IsOverride = false };
    }

    /// <summary>
    /// The first candidate, in candidate order, that is a whole line of the .met file.  A trailing \r is ignored,
    /// otherwise the match is exact (case and whitespace).  Null when none match.
    /// </summary>
    public static string FindChainState(IEnumerable<string> metFileLines, IList<string> candidates)
    {
        if (metFileLines == null || candidates == null || candidates.Count == 0)
            return null;

        var lines = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in metFileLines)
        {
            if (line != null)
                lines.Add(line.TrimEnd('\r'));
        }

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && lines.Contains(candidate))
                return candidate;
        }

        return null;
    }

    private static void AddTimeouts(MergedMetaChainWatchConfig merged, MetaChainWatchFileData data, string source)
    {
        if (data?.Timeouts == null)
            return;

        foreach (var pair in data.Timeouts)
        {
            var name = NormalizeName(pair.Key);
            if (name.Length == 0 || pair.Value == null)
                continue;

            merged.Timeouts[name] = new MergedMetaTimeout
            {
                Name = name,
                Minutes = pair.Value.Minutes,
                ChainState = string.IsNullOrEmpty(pair.Value.ChainState) ? null : pair.Value.ChainState,
                Source = source,
            };
        }
    }

    private static string FirstNonBlank(params string[] values)
    {
        foreach (var value in values)
        {
            if (value != null && value.Trim().Length > 0)
                return value.Trim();
        }

        return null;
    }

    private static List<string> CleanChainStates(List<string> states)
    {
        if (states == null)
            return null;

        var cleaned = states.FindAll(s => !string.IsNullOrEmpty(s));
        return cleaned.Count == 0 ? null : cleaned;
    }
}
