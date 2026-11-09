using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using RedoxExtensions.AgentCli.Scheduling;

namespace RedoxExtensions.AgentCli;

/// <summary>
/// rectl schedule ...: edits the per-character schedule files directly.  The plugin notices the change within
/// a few seconds, so none of this needs the bridge.
/// </summary>
public partial class Cli
{
    private int Schedule(InstanceLocator locator, Arguments parsed)
    {
        var install = InstanceLocator.ResolveInstallDirectory(this._getEnv);
        if (install == null)
        {
            this._err.WriteLine("No RedoxExtensions install directory found.  Set REDOX_EXTENSIONS_INSTALL.");
            return ExitFailed;
        }

        if (parsed.Positionals.Count == 0)
        {
            this._err.WriteLine("Usage: rectl schedule add|list|remove|enable|disable ...  Run 'rectl help'.");
            return ExitUsage;
        }

        var sub = parsed.Positionals[0].ToLowerInvariant();
        var character = parsed.Get("character");

        try
        {
            if (sub == "list")
                return this.ScheduleList(install, character);

            if (string.IsNullOrWhiteSpace(character))
            {
                this._err.WriteLine($"schedule {sub} needs -c <character> (the exact character name).");
                return ExitUsage;
            }

            return sub switch
            {
                "add" => this.ScheduleAdd(locator, install, character, parsed),
                "remove" or "rm" => this.ScheduleChange(install, character, parsed, "remove"),
                "enable" => this.ScheduleChange(install, character, parsed, "enable"),
                "disable" => this.ScheduleChange(install, character, parsed, "disable"),
                _ => this.UnknownCommand("schedule " + sub),
            };
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            this._err.WriteLine($"Schedule file error: {e.Message}");
            return ExitFailed;
        }
    }

    private int ScheduleAdd(InstanceLocator locator, string install, string character, Arguments parsed)
    {
        var command = string.Join(" ", parsed.Positionals.Skip(1)).Trim();
        if (command.Length == 0)
            throw new ArgumentException("schedule add needs a command, e.g. rectl -c <character> schedule add \"/re sm daily\" --at 06:00");

        var atText = parsed.Get("at") ?? throw new ArgumentException("schedule add needs --at <time>");
        var now = this._now();
        var at = ScheduleStore.ParseAt(atText, now);
        if (at <= now)
            throw new ArgumentException($"--at {at.ToString(ScheduleStore.TimeFormat)} is in the past");

        var repeat = (parsed.Get("repeat") ?? ScheduleRules.RepeatOnce).ToLowerInvariant();
        if (!ScheduleRules.IsValidRepeat(repeat))
            throw new ArgumentException($"--repeat must be {ScheduleRules.RepeatOnce} or {ScheduleRules.RepeatDaily}");

        var grace = parsed.GetInt("grace") ?? ScheduleRules.DefaultGraceMinutes;
        if (grace < 0)
            throw new ArgumentException("--grace must be 0 or more minutes");

        var path = ScheduleRules.FilePath(install, character);
        var known = File.Exists(path) || locator.FindAll().Any(i => string.Equals(i.Character, character, StringComparison.OrdinalIgnoreCase));
        if (!known)
            this._err.WriteLine($"Note: '{character}' has no schedule file yet and isn't running with the bridge on.  The name must match the character exactly.");

        var data = ScheduleStore.Load(path);
        var entry = new ScheduledCommand
        {
            Id = NewId(data),
            Command = command,
            At = at,
            Repeat = repeat,
            GraceMinutes = grace,
            Enabled = true,
        };
        data.Entries.Add(entry);
        ScheduleStore.Save(path, data);

        var json = ScheduleStore.ToJson(entry, now);
        json["character"] = character;
        json["file"] = path;
        this.WriteJson(json);
        return ExitOk;
    }

    private int ScheduleList(string install, string character)
    {
        var now = this._now();
        var directory = Path.Combine(install, ScheduleRules.DirectoryName);

        string[] files;
        if (!string.IsNullOrWhiteSpace(character))
            files = new[] { ScheduleRules.FilePath(install, character) };
        else
            files = Directory.Exists(directory) ? Directory.GetFiles(directory, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray() : Array.Empty<string>();

        var result = new JsonArray();
        foreach (var file in files)
        {
            var data = ScheduleStore.Load(file);
            result.Add(new JsonObject
            {
                ["character"] = character ?? Path.GetFileNameWithoutExtension(file),
                ["file"] = file,
                ["entries"] = new JsonArray(data.Entries.Select(e => ScheduleStore.ToJson(e, now)).ToArray()),
            });
        }

        this.WriteJson(result);
        return ExitOk;
    }

    private int ScheduleChange(string install, string character, Arguments parsed, string action)
    {
        if (parsed.Positionals.Count < 2)
            throw new ArgumentException($"schedule {action} needs an entry id (see rectl schedule list)");

        var id = parsed.Positionals[1];
        var path = ScheduleRules.FilePath(install, character);
        var data = ScheduleStore.Load(path);
        var entry = data.Entries.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            this._err.WriteLine($"No entry '{id}' in {path}");
            return ExitFailed;
        }

        switch (action)
        {
            case "remove":
                data.Entries.Remove(entry);
                break;
            case "enable":
                entry.Enabled = true;
                break;
            case "disable":
                entry.Enabled = false;
                break;
        }

        ScheduleStore.Save(path, data);

        var json = ScheduleStore.ToJson(entry, this._now());
        json["action"] = action;
        json["character"] = character;
        this.WriteJson(json);
        return ExitOk;
    }

    private static string NewId(ScheduleFileData data)
    {
        while (true)
        {
            var id = Guid.NewGuid().ToString("N").Substring(0, 6);
            if (!data.Entries.Any(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)))
                return id;
        }
    }
}
