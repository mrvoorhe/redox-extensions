using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RedoxExtensions.MetaChainWatch;

/// <summary>
/// Reads and writes a meta chain watch config file (global or per character).
/// </summary>
internal static class MetaChainWatchFile
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new CamelCaseKeepDictionaryKeysResolver(),
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <summary>
    /// Null when the file doesn't exist, so callers can tell "no file" from "empty file".
    /// <see cref="MetaChainWatchRules.Merge"/> accepts null.  A file that exists always loads with a non-null
    /// <see cref="MetaChainWatchFileData.Timeouts"/>.
    /// </summary>
    public static MetaChainWatchFileData Load(string path)
    {
        if (!File.Exists(path))
            return null;

        var data = JsonConvert.DeserializeObject<MetaChainWatchFileData>(File.ReadAllText(path), Settings) ?? new MetaChainWatchFileData();
        data.Timeouts ??= new Dictionary<string, MetaTimeoutEntry>();
        return data;
    }

    /// <summary>
    /// Writes to a temp file and swaps it in, so a reader never sees half a file.
    /// </summary>
    public static void Save(string path, MetaChainWatchFileData data) => WriteAtomically(path, data);

    /// <summary>The saved watch state (<see cref="MetaChainWatchRules.StateFilePath"/>), or null when there is none.</summary>
    public static MetaChainWatchSnapshot LoadState(string path)
    {
        return File.Exists(path)
            ? JsonConvert.DeserializeObject<MetaChainWatchSnapshot>(File.ReadAllText(path), Settings)
            : null;
    }

    public static void SaveState(string path, MetaChainWatchSnapshot snapshot) => WriteAtomically(path, snapshot);

    public static void DeleteState(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
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

    /// <summary>
    /// camelCase property names, but meta names used as timeout keys are written as they are.
    /// (CamelCasePropertyNamesContractResolver would turn "Attributes4.0" into "attributes4.0".)
    /// </summary>
    private class CamelCaseKeepDictionaryKeysResolver : CamelCasePropertyNamesContractResolver
    {
        protected override string ResolveDictionaryKey(string dictionaryKey) => dictionaryKey;
    }
}
