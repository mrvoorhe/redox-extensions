using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using RedoxExtensions.Scheduling;

namespace RedoxExtensions.Recording;

public enum RecorderResumeDecision
{
    None,
    ContinueSameJournal,
    NewJournal,
    Expired,
    Invalid,
}

public enum RecorderStopKind
{
    NotRecording,
    StopNow,
    Pending,
}

public class RecorderStopDecision
{
    public RecorderStopKind Kind { get; set; }

    /// <summary>The new pending stop, or null when the kind isn't Pending.</summary>
    public DateTime? StopAt { get; set; }
}

public static class RecorderRules
{
    public const string DirectoryName = "Recorder";
    public const string StateDirectoryName = "State";
    public const string ConfigFileName = "Global.json";
    public const string DefaultOutputDirectoryName = "Recordings";
    public const string DateFormat = "yyyy-MM-dd";

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromHours(5);
    public static readonly TimeSpan ClockSkewAllowance = TimeSpan.FromMinutes(5);

    public static string StatePath(string installDirectory, string characterName)
    {
        return Path.Combine(Path.Combine(Path.Combine(installDirectory, DirectoryName), StateDirectoryName), ScheduleRules.SafeFileName(characterName) + ".json");
    }

    public static string ConfigPath(string installDirectory) => Path.Combine(Path.Combine(installDirectory, DirectoryName), ConfigFileName);

    public static string DefaultOutputRoot(string installDirectory) => Path.Combine(installDirectory, DefaultOutputDirectoryName);

    public static DateTime EffectiveEnd(DateTime timeoutAt, DateTime? stopAt)
    {
        return stopAt.HasValue && stopAt.Value < timeoutAt ? stopAt.Value : timeoutAt;
    }

    public static DateTime EffectiveEnd(RecorderState state) => EffectiveEnd(state.TimeoutAt, state.StopAt);

    /// <summary>Null state means nothing is recording.  A null delay stops now.  A pending stop never goes past the timeout.</summary>
    public static RecorderStopDecision ApplyStop(RecorderState state, TimeSpan? delay, DateTime now)
    {
        if (state == null)
            return new RecorderStopDecision { Kind = RecorderStopKind.NotRecording };

        if (!delay.HasValue)
            return new RecorderStopDecision { Kind = RecorderStopKind.StopNow };

        var stopAt = now + delay.Value;
        if (stopAt > state.TimeoutAt)
            stopAt = state.TimeoutAt;

        return new RecorderStopDecision { Kind = RecorderStopKind.Pending, StopAt = stopAt };
    }

    public static bool IsEnded(RecorderState state, DateTime now) => EffectiveEnd(state) <= now;

    public static RecorderResumeDecision DecideResume(RecorderState saved, string currentJournalId, DateTime now)
    {
        return DecideResume(saved, currentJournalId, now, ClockSkewAllowance);
    }

    public static RecorderResumeDecision DecideResume(RecorderState saved, string currentJournalId, DateTime now, TimeSpan clockSkewAllowance)
    {
        if (saved == null)
            return RecorderResumeDecision.None;

        if (string.IsNullOrEmpty(saved.File) || saved.StartedAt == default(DateTime) || saved.TimeoutAt <= saved.StartedAt
            || saved.StartedAt - now > clockSkewAllowance || saved.LastSeq < 0)
            return RecorderResumeDecision.Invalid;

        if (EffectiveEnd(saved) <= now)
            return RecorderResumeDecision.Expired;

        return !string.IsNullOrEmpty(saved.JournalId) && saved.JournalId == currentJournalId
            ? RecorderResumeDecision.ContinueSameJournal
            : RecorderResumeDecision.NewJournal;
    }

    /// <summary>The folder a session writes to, named for the day it started.</summary>
    public static string SessionFolder(string outputRoot, DateTime startedAt)
    {
        return Path.Combine(outputRoot, startedAt.ToString(DateFormat, CultureInfo.InvariantCulture));
    }

    public static string DateText(DateTime startedAt) => startedAt.ToString(DateFormat, CultureInfo.InvariantCulture);

    /// <summary>"Char", then "Char-2", "Char-3"...: the first whose .jsonl doesn't exist.  <paramref name="exists"/> gets the full path.</summary>
    public static string NextFileName(string folder, string characterName, Func<string, bool> exists)
    {
        var baseName = ScheduleRules.SafeFileName(characterName);
        var name = baseName;
        for (var n = 2; exists(Path.Combine(folder, name + ".jsonl")); n++)
            name = baseName + "-" + n;
        return name;
    }

    public static string NextFileName(string folder, string characterName) => NextFileName(folder, characterName, File.Exists);
}

public static class AfterStopArgs
{
    private static readonly Regex Placeholder = new Regex(@"\{(file|dir|name|char|date)\}");

    public static string Expand(string args, string file, string dir, string name, string character, string date)
    {
        if (string.IsNullOrEmpty(args))
            return args ?? string.Empty;

        return Placeholder.Replace(args, m =>
        {
            switch (m.Groups[1].Value)
            {
                case "file": return file ?? string.Empty;
                case "dir": return dir ?? string.Empty;
                case "name": return name ?? string.Empty;
                case "char": return character ?? string.Empty;
                default: return date ?? string.Empty;
            }
        });
    }
}
