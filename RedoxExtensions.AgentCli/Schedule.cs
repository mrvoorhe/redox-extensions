using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using RedoxExtensions.AgentCli.Scheduling;

namespace RedoxExtensions.AgentCli;

/// <summary>
/// Reads and writes the plugin's per-character schedule files (&lt;install&gt;\ScheduledCommands\&lt;Character&gt;.json).
/// The plugin side is RedoxExtensions/Scheduling/ScheduleFile.cs.
/// </summary>
public static class ScheduleStore
{
    public const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss";

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new LocalDateTimeConverter() },
    };

    public static ScheduleFileData Load(string path)
    {
        if (!File.Exists(path))
            return new ScheduleFileData();

        var data = JsonSerializer.Deserialize<ScheduleFileData>(File.ReadAllText(path), Options) ?? new ScheduleFileData();
        data.Entries ??= new List<ScheduledCommand>();
        data.Entries.RemoveAll(e => e == null);
        return data;
    }

    /// <summary>Writes to a temp file and swaps it in, so the plugin never reads half a file.</summary>
    public static void Save(string path, ScheduleFileData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, path, true);
    }

    public static JsonNode ToJson(ScheduledCommand entry, DateTime now)
    {
        var node = JsonSerializer.SerializeToNode(entry, Options);
        node["status"] = ScheduleRules.Status(entry, now);
        var next = ScheduleRules.NextRun(entry, now);
        node["nextRun"] = next?.ToString(TimeFormat, CultureInfo.InvariantCulture);
        return node;
    }

    /// <summary>
    /// Parses --at: "HH:MM" (the next time it comes round), "tomorrow HH:MM", "YYYY-MM-DD HH:MM",
    /// "YYYY-MM-DDTHH:MM[:SS]" or "+&lt;n&gt;m" / "+&lt;n&gt;h" from now.
    /// </summary>
    public static DateTime ParseAt(string text, DateTime now)
    {
        text = text.Trim();

        var relative = Regex.Match(text, @"^\+(\d+)\s*([mh])$", RegexOptions.IgnoreCase);
        if (relative.Success)
        {
            var amount = int.Parse(relative.Groups[1].Value, CultureInfo.InvariantCulture);
            var at = relative.Groups[2].Value.ToLowerInvariant() == "h" ? now.AddHours(amount) : now.AddMinutes(amount);
            return TrimToSeconds(at);
        }

        var tomorrow = Regex.Match(text, @"^tomorrow\s+(.+)$", RegexOptions.IgnoreCase);
        if (tomorrow.Success)
            return now.Date.AddDays(1) + ParseTimeOfDay(tomorrow.Groups[1].Value, text);

        if (Regex.IsMatch(text, @"^\d{1,2}:\d{2}(:\d{2})?$"))
        {
            var at = now.Date + ParseTimeOfDay(text, text);
            return at > now ? at : at.AddDays(1);
        }

        string[] formats = { "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss" };
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact;

        throw new ArgumentException($"--at '{text}' isn't a time.  Use HH:MM, 'tomorrow HH:MM', YYYY-MM-DDTHH:MM or +<n>m");
    }

    private static TimeSpan ParseTimeOfDay(string value, string original)
    {
        if (TimeSpan.TryParseExact(value.Trim(), new[] { @"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out var time)
            && time < TimeSpan.FromDays(1))
            return time;
        throw new ArgumentException($"--at '{original}' has a bad time of day.  Use 24-hour HH:MM");
    }

    private static DateTime TrimToSeconds(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Unspecified);

    /// <summary>Local wall-clock times with no offset, the same format the plugin writes.</summary>
    private class LocalDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var parsed = DateTime.Parse(reader.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None);
            return DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(TimeFormat, CultureInfo.InvariantCulture));
        }
    }
}
