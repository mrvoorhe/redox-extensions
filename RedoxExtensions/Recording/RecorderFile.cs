using System;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RedoxExtensions.Recording;

internal static class RecorderFile
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
        DateFormatString = "yyyy-MM-dd'T'HH:mm:ss",
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <summary>
    /// Null when there is no file, or when it is empty or corrupt (then <paramref name="warning"/> says so).  Never throws for bad content.
    /// </summary>
    public static RecorderState LoadState(string path, out string warning)
    {
        warning = null;
        if (!File.Exists(path))
            return null;

        try
        {
            var text = File.ReadAllText(path);
            if (text.Trim().Length == 0)
            {
                warning = "ignoring empty state file " + path;
                return null;
            }

            var state = JsonConvert.DeserializeObject<RecorderState>(text, Settings);
            if (state == null)
                warning = "ignoring empty state file " + path;
            return state;
        }
        catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
        {
            warning = "ignoring unreadable state file " + path + ": " + ex.Message;
            return null;
        }
    }

    public static void SaveState(string path, RecorderState state) => WriteAtomically(path, state);

    public static void DeleteState(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    /// <summary>
    /// Always returns a config with an output root.  A missing file, bad content or an empty root falls back to
    /// &lt;install&gt;\Recordings and sets <see cref="RecorderConfigResult.Warning"/>.
    /// </summary>
    public static RecorderConfigResult LoadConfig(string path, string installDirectory)
    {
        var fallback = RecorderRules.DefaultOutputRoot(installDirectory);
        RecorderConfig config = null;
        string problem = null;

        if (!File.Exists(path))
        {
            problem = "no config file " + path;
        }
        else
        {
            try
            {
                config = JsonConvert.DeserializeObject<RecorderConfig>(File.ReadAllText(path), Settings);
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
            {
                problem = "can't read " + path + ": " + ex.Message;
            }
        }

        config ??= new RecorderConfig();
        if (problem == null && string.IsNullOrEmpty(config.OutputRoot?.Trim()))
            problem = "outputRoot is not set in " + path;

        if (config.AfterStop != null && string.IsNullOrEmpty(config.AfterStop.Program?.Trim()))
            config.AfterStop = null;

        if (problem == null)
        {
            config.OutputRoot = config.OutputRoot.Trim();
            return new RecorderConfigResult { Config = config };
        }

        config.OutputRoot = fallback;
        return new RecorderConfigResult { Config = config, UsedFallback = true, Warning = problem + "; recording to " + fallback };
    }

    private static void WriteAtomically(string path, object data)
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
