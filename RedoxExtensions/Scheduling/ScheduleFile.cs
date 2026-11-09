using System;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RedoxExtensions.Scheduling;

/// <summary>
/// Reads and writes a schedule file.  rectl (RedoxExtensions.AgentCli/Schedule.cs) writes the same format.
/// </summary>
internal static class ScheduleFile
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
        DateFormatString = "yyyy-MM-dd'T'HH:mm:ss",
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static ScheduleFileData Load(string path)
    {
        if (!File.Exists(path))
            return new ScheduleFileData();

        var data = JsonConvert.DeserializeObject<ScheduleFileData>(File.ReadAllText(path), Settings) ?? new ScheduleFileData();
        data.Entries ??= new();
        data.Entries.RemoveAll(e => e == null);
        return data;
    }

    /// <summary>
    /// Writes to a temp file and swaps it in, so a reader never sees half a file.
    /// </summary>
    public static void Save(string path, ScheduleFileData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonConvert.SerializeObject(data, Settings));

        if (File.Exists(path))
            File.Replace(temp, path, null);
        else
            File.Move(temp, path);
    }
}
