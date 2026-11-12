using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using RedoxExtensions.AgentCli.MetaChainWatch;

namespace RedoxExtensions.AgentCli;

/// <summary>
/// rectl metatimeout ...: edits the meta chain watch config files directly (Global.json, or &lt;Character&gt;.json with -c).
/// The plugin notices the change on its next tick, so none of this needs the bridge.
/// </summary>
public partial class Cli
{
    private int MetaTimeout(InstanceLocator locator, Arguments parsed)
    {
        var install = InstanceLocator.ResolveInstallDirectory(this._getEnv);
        if (install == null)
        {
            this._err.WriteLine("No RedoxExtensions install directory found.  Set REDOX_EXTENSIONS_INSTALL.");
            return ExitFailed;
        }

        if (parsed.Positionals.Count == 0)
        {
            this._err.WriteLine("Usage: rectl [-c <character>] metatimeout list|set|remove|fallback ...  Run 'rectl help'.");
            return ExitUsage;
        }

        var sub = parsed.Positionals[0].ToLowerInvariant();
        var character = parsed.Get("character");
        if (string.IsNullOrWhiteSpace(character))
            character = null;

        try
        {
            return sub switch
            {
                "list" => this.MetaTimeoutList(install, character),
                "set" => this.MetaTimeoutSet(locator, install, character, parsed),
                "remove" or "rm" => this.MetaTimeoutRemove(install, character, parsed),
                "fallback" => this.MetaTimeoutFallback(locator, install, character, parsed),
                _ => this.UnknownCommand("metatimeout " + sub),
            };
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            this._err.WriteLine($"Meta chain watch file error: {e.Message}");
            return ExitFailed;
        }
    }

    private int MetaTimeoutList(string install, string character)
    {
        var globalPath = MetaChainWatchRules.GlobalFilePath(install);
        var global = MetaTimeoutStore.Load(globalPath);
        var characterPath = character == null ? null : MetaChainWatchRules.CharacterFilePath(install, character);
        var characterData = characterPath == null ? null : MetaTimeoutStore.Load(characterPath);

        var merged = MetaChainWatchRules.Merge(global, characterData);

        var result = new JsonObject
        {
            ["globalFile"] = globalPath,
            ["globalFileExists"] = global != null,
        };
        if (character != null)
        {
            result["character"] = character;
            result["characterFile"] = characterPath;
            result["characterFileExists"] = characterData != null;
        }

        result["fallbackCommand"] = merged.FallbackCommand;
        result["fallbackSource"] = MetaTimeoutStore.FallbackSource(global, characterData);
        result["chainStates"] = new JsonArray(merged.ChainStates.Select(s => (JsonNode)s).ToArray());
        result["chainStatesSource"] = MetaTimeoutStore.ChainStatesSource(global, characterData);
        result["timeouts"] = new JsonArray(merged.Timeouts.Values
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => (JsonNode)TimeoutJson(t.Name, t.Minutes, t.ChainState, t.Source))
            .ToArray());

        this.WriteJson(result);
        return ExitOk;
    }

    private int MetaTimeoutSet(InstanceLocator locator, string install, string character, Arguments parsed)
    {
        if (parsed.Positionals.Count != 3)
            throw new ArgumentException("metatimeout set needs <meta> <minutes>, e.g. rectl metatimeout set Attributes4.0 10 [--chain-state \"** Chain **\"]");

        var name = MetaChainWatchRules.NormalizeName(parsed.Positionals[1]);
        if (name.Length == 0)
            throw new ArgumentException("metatimeout set needs a meta name");

        var minutesText = parsed.Positionals[2];
        if (!double.TryParse(minutesText, NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes)
            || double.IsNaN(minutes) || double.IsInfinity(minutes) || minutes <= 0)
            throw new ArgumentException($"<minutes> must be a number greater than 0 (decimals allowed, e.g. 0.5), not '{minutesText}'");

        var chainState = parsed.Get("chain-state");
        if (chainState != null && chainState.Length == 0)
            chainState = null;

        var path = this.MetaTimeoutPath(locator, install, character, warnIfUnknown: true);
        var data = MetaTimeoutStore.Load(path) ?? new MetaChainWatchFileData();
        var replaced = MetaTimeoutStore.MatchingKeys(data, name);
        foreach (var key in replaced)
            data.Timeouts.Remove(key);
        data.Timeouts[name] = new MetaTimeoutEntry { Minutes = minutes, ChainState = chainState };
        MetaTimeoutStore.Save(path, data);

        var json = this.MetaTimeoutChangeJson("set", character, path);
        json["timeout"] = TimeoutJson(name, minutes, chainState, null);
        if (replaced.Count > 0)
            json["replaced"] = new JsonArray(replaced.Select(k => (JsonNode)k).ToArray());
        this.WriteJson(json);
        return ExitOk;
    }

    private int MetaTimeoutRemove(string install, string character, Arguments parsed)
    {
        if (parsed.Positionals.Count != 2)
            throw new ArgumentException("metatimeout remove needs a meta name (see rectl metatimeout list)");

        var name = MetaChainWatchRules.NormalizeName(parsed.Positionals[1]);
        var path = this.MetaTimeoutPath(null, install, character, warnIfUnknown: false);
        var data = MetaTimeoutStore.Load(path);
        var keys = MetaTimeoutStore.MatchingKeys(data, name);
        if (keys.Count == 0)
        {
            this._err.WriteLine($"No timeout for '{name}' in {path}");
            return ExitFailed;
        }

        foreach (var key in keys)
            data.Timeouts.Remove(key);
        MetaTimeoutStore.Save(path, data);

        var json = this.MetaTimeoutChangeJson("remove", character, path);
        json["removed"] = new JsonArray(keys.Select(k => (JsonNode)k).ToArray());
        this.WriteJson(json);
        return ExitOk;
    }

    private int MetaTimeoutFallback(InstanceLocator locator, string install, string character, Arguments parsed)
    {
        var args = parsed.Positionals.Skip(1).ToList();
        var clear = args.Count == 1 && args[0] == "--clear";
        var command = clear ? null : string.Join(" ", args).Trim();
        if (!clear && command.Length == 0)
            throw new ArgumentException("metatimeout fallback needs a command, e.g. rectl metatimeout fallback /re sm aFarm (or --clear)");

        var path = this.MetaTimeoutPath(locator, install, character, warnIfUnknown: !clear);
        var data = MetaTimeoutStore.Load(path);
        if (clear && data == null)
        {
            this._err.WriteLine($"No fallback command to clear: {path} doesn't exist");
            return ExitFailed;
        }

        data ??= new MetaChainWatchFileData();
        data.FallbackCommand = command;
        MetaTimeoutStore.Save(path, data);

        var json = this.MetaTimeoutChangeJson(clear ? "fallback-clear" : "fallback", character, path);
        json["fallbackCommand"] = command;
        this.WriteJson(json);
        return ExitOk;
    }

    /// <summary>Global.json without -c, else the character's file (with a note when the name may be wrong).</summary>
    private string MetaTimeoutPath(InstanceLocator locator, string install, string character, bool warnIfUnknown)
    {
        if (character == null)
            return MetaChainWatchRules.GlobalFilePath(install);

        var path = MetaChainWatchRules.CharacterFilePath(install, character);
        if (warnIfUnknown && locator != null && !File.Exists(path)
            && !locator.FindAll().Any(i => string.Equals(i.Character, character, StringComparison.OrdinalIgnoreCase)))
            this._err.WriteLine($"Note: '{character}' has no meta chain watch file yet and isn't running with the bridge on.  The name must match the character exactly.");
        return path;
    }

    private JsonObject MetaTimeoutChangeJson(string action, string character, string path)
    {
        var json = new JsonObject
        {
            ["action"] = action,
            ["scope"] = character == null ? MetaChainWatchRules.SourceGlobal : MetaChainWatchRules.SourceCharacter,
        };
        if (character != null)
            json["character"] = character;
        json["file"] = path;
        return json;
    }

    private static JsonObject TimeoutJson(string name, double minutes, string chainState, string source)
    {
        var json = new JsonObject { ["name"] = name, ["minutes"] = minutes };
        if (chainState != null)
            json["chainState"] = chainState;
        if (source != null)
            json["source"] = source;
        return json;
    }
}
