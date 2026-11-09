using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Shared by the plugin (net35) and rectl (net10.0, linked source).  Keep it free of serializer and Decal types.
// rectl compiles it into its own namespace so the tests, which reference both assemblies, can tell them apart.
#if REDOX_AGENTCLI
namespace RedoxExtensions.AgentCli.Scheduling;
#else
namespace RedoxExtensions.Scheduling;
#endif

/// <summary>
/// The contents of one character's schedule file, &lt;install&gt;\ScheduledCommands\&lt;Character&gt;.json.
/// </summary>
public class ScheduleFileData
{
    public List<ScheduledCommand> Entries { get; set; } = new List<ScheduledCommand>();
}

/// <summary>
/// One scheduled command.  All times are local wall-clock times with no offset.
/// </summary>
public class ScheduledCommand
{
    public string Id { get; set; }

    /// <summary>The chat command to run, as if typed, e.g. "/re sm daily".</summary>
    public string Command { get; set; }

    /// <summary>The first (or only) time to run.  Daily entries repeat at this time of day.</summary>
    public DateTime At { get; set; }

    /// <summary>"once" or "daily".</summary>
    public string Repeat { get; set; } = ScheduleRules.RepeatOnce;

    /// <summary>How late a run may still happen, e.g. when the character logs in after the time.</summary>
    public int GraceMinutes { get; set; } = ScheduleRules.DefaultGraceMinutes;

    public bool Enabled { get; set; } = true;

    /// <summary>When the plugin last ran it.  Written only by the plugin.</summary>
    public DateTime? LastRun { get; set; }
}

public static class ScheduleRules
{
    public const string RepeatOnce = "once";
    public const string RepeatDaily = "daily";
    public const int DefaultGraceMinutes = 15;
    public const string DirectoryName = "ScheduledCommands";

    public static bool IsValidRepeat(string repeat) => repeat == RepeatOnce || repeat == RepeatDaily;

    public static string FilePath(string installDirectory, string characterName)
    {
        return Path.Combine(Path.Combine(installDirectory, DirectoryName), SafeFileName(characterName) + ".json");
    }

    public static string SafeFileName(string characterName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(characterName.Length);
        foreach (var c in characterName)
            builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        return builder.ToString();
    }

    /// <summary>The latest scheduled time at or before now, or null if there is none yet.</summary>
    public static DateTime? LatestOccurrence(ScheduledCommand entry, DateTime now)
    {
        if (entry.At > now)
            return null;

        if (entry.Repeat != RepeatDaily)
            return entry.At;

        var occurrence = now.Date + entry.At.TimeOfDay;
        if (occurrence > now)
            occurrence = occurrence.AddDays(-1);
        return occurrence < entry.At ? entry.At : occurrence;
    }

    /// <summary>
    /// True when the latest occurrence hasn't run yet and is no more than the grace period late.
    /// </summary>
    public static bool IsDue(ScheduledCommand entry, DateTime now)
    {
        if (!entry.Enabled || string.IsNullOrEmpty(entry.Command) || !IsValidRepeat(entry.Repeat))
            return false;

        var occurrence = LatestOccurrence(entry, now);
        if (occurrence == null)
            return false;

        if (entry.LastRun.HasValue && entry.LastRun.Value >= occurrence.Value)
            return false;

        return now - occurrence.Value <= TimeSpan.FromMinutes(Math.Max(0, entry.GraceMinutes));
    }

    /// <summary>
    /// When the entry will run next: now-ish if it is due, else its next future time.  Null when a once entry is
    /// done or missed, or the entry is disabled or invalid.
    /// </summary>
    public static DateTime? NextRun(ScheduledCommand entry, DateTime now)
    {
        if (!entry.Enabled || !IsValidRepeat(entry.Repeat))
            return null;

        if (IsDue(entry, now))
            return LatestOccurrence(entry, now);

        if (entry.At > now)
            return entry.At;

        if (entry.Repeat == RepeatDaily)
            return LatestOccurrence(entry, now).Value.AddDays(1);

        return null;
    }

    /// <summary>A short human status: "due", "waiting", "done", "missed" or "disabled".</summary>
    public static string Status(ScheduledCommand entry, DateTime now)
    {
        if (!entry.Enabled)
            return "disabled";
        if (!IsValidRepeat(entry.Repeat))
            return "invalid";
        if (IsDue(entry, now))
            return "due";
        if (entry.Repeat == RepeatOnce && entry.At <= now)
            return entry.LastRun.HasValue ? "done" : "missed";
        return "waiting";
    }
}
