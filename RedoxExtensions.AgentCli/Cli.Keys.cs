using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;

namespace RedoxExtensions.AgentCli;

public partial class Cli
{
    private const int DefaultKeyHoldMs = 100;

    /// <summary>Time past the hold to wait, so the plugin's key up has been posted before rectl returns.</summary>
    private const int KeyReleaseSettleMs = 50;

    /// <summary>rectl camera moves and the keymap actions they press.</summary>
    public static readonly IReadOnlyDictionary<string, string> CameraActions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["left"] = "CameraRotateLeft",
        ["right"] = "CameraRotateRight",
        ["up"] = "CameraRotateUp",
        ["down"] = "CameraRotateDown",
        ["closer"] = "CameraMoveToward",
        ["farther"] = "CameraMoveAway",
        ["default"] = "CameraViewDefault",
        ["firstperson"] = "CameraViewFirstPerson",
        ["lookdown"] = "CameraViewLookDown",
    };

    private int Key(BridgeInstance instance, Arguments parsed)
    {
        if (parsed.Positionals.Count != 1)
        {
            this._err.WriteLine("key needs one key name, e.g. rectl key NUMPAD4 --hold 500");
            return ExitUsage;
        }

        return this.PressKey(instance, parsed.Positionals[0], parsed.GetInt("hold") ?? DefaultKeyHoldMs, null);
    }

    /// <summary>
    /// How long a camera rotate key takes to turn the camera one degree, measured on Cowhead (2026-11-17): a 562 ms hold
    /// from the default view shows his face either way (180 degrees) and 1125 ms brings the default view back round
    /// (a full turn), so about 320 degrees a second.  The same hold lands within about 5 degrees from one try to the
    /// next, foreground or background, so this is for ballpark angles.
    /// </summary>
    public const double CameraMsPerDegree = CameraFullTurnMs / 360.0;

    public const double CameraFullTurnMs = 1125;

    /// <summary>Time for the default view to take hold before turning from it.</summary>
    private const int CameraResetSettleMs = 1000;

    public const int CameraRotateStep = 10;

    /// <summary>
    /// The camera rotate key and hold time for turning <paramref name="degrees"/> from the default view: positive
    /// turns with CameraRotateRight, negative with CameraRotateLeft.  Degrees are multiples of 10 from -360 to 360.
    /// </summary>
    public static bool TryCameraRotation(string text, out string move, out int holdMs, out string error)
    {
        move = null;
        holdMs = 0;
        error = null;
        if (!int.TryParse(text, out var degrees) || degrees % CameraRotateStep != 0 || degrees < -360 || degrees > 360)
        {
            error = $"rotate takes degrees in steps of {CameraRotateStep} from -360 to 360, not '{text}'";
            return false;
        }

        move = degrees >= 0 ? "right" : "left";
        holdMs = (int)Math.Round(Math.Abs(degrees) * CameraMsPerDegree);
        return true;
    }

    private int CameraRotate(BridgeInstance instance, Arguments parsed, string path)
    {
        string move = null, error = null;
        var holdMs = 0;
        if (parsed.Positionals.Count != 2 || !TryCameraRotation(parsed.Positionals[1], out move, out holdMs, out error))
        {
            this._err.WriteLine(error ?? "camera rotate needs degrees, e.g. rectl camera rotate 90 (or -90 for the other way)");
            return ExitUsage;
        }

        var reset = Keymap.FindKey(File.ReadAllLines(path), CameraActions["default"], "CameraControls");
        var turn = Keymap.FindKey(File.ReadAllLines(path), CameraActions[move], "CameraControls");
        if (reset == null || turn == null)
        {
            this._err.WriteLine($"{path} needs plain keyboard keys for {CameraActions["default"]} and {CameraActions[move]}");
            return ExitFailed;
        }

        using var client = Connect(instance, DefaultWaitTimeoutMs);
        var response = client.Send("key", new Dictionary<string, JsonNode> { ["key"] = reset, ["holdMs"] = DefaultKeyHoldMs });
        if (!IsOk(response))
            return this.Report(response);
        Thread.Sleep(DefaultKeyHoldMs + CameraResetSettleMs);

        if (holdMs > 0)
        {
            response = client.Send("key", new Dictionary<string, JsonNode> { ["key"] = turn, ["holdMs"] = holdMs });
            if (!IsOk(response))
                return this.Report(response);
            Thread.Sleep(holdMs + KeyReleaseSettleMs);
        }

        this.WriteJson(new JsonObject
        {
            ["degrees"] = int.Parse(parsed.Positionals[1]),
            ["action"] = holdMs > 0 ? CameraActions[move] : CameraActions["default"],
            ["key"] = holdMs > 0 ? turn : reset,
            ["holdMs"] = holdMs,
        });
        return ExitOk;
    }

    private int Camera(BridgeInstance instance, Arguments parsed)
    {
        if (parsed.Positionals.Count == 2 && parsed.Positionals[0].Equals("rotate", StringComparison.OrdinalIgnoreCase))
        {
            var keymap = parsed.Get("keymap") ?? DefaultKeymapPath();
            if (!File.Exists(keymap))
            {
                this._err.WriteLine($"No keymap at {keymap}.  Use --keymap <path>");
                return ExitFailed;
            }
            return this.CameraRotate(instance, parsed, keymap);
        }

        if (parsed.Positionals.Count != 1 || !CameraActions.TryGetValue(parsed.Positionals[0], out var action))
        {
            this._err.WriteLine($"camera needs one of: {string.Join(", ", CameraActions.Keys)}, e.g. rectl camera left --hold 500");
            return ExitUsage;
        }

        var path = parsed.Get("keymap") ?? DefaultKeymapPath();
        if (!File.Exists(path))
        {
            this._err.WriteLine($"No keymap at {path}.  Use --keymap <path>");
            return ExitFailed;
        }

        var key = Keymap.FindKey(File.ReadAllLines(path), action, "CameraControls");
        if (key == null)
        {
            this._err.WriteLine($"{path} binds no plain keyboard key to {action}");
            return ExitFailed;
        }

        return this.PressKey(instance, key, parsed.GetInt("hold") ?? DefaultKeyHoldMs, action);
    }

    private int PressKey(BridgeInstance instance, string key, int holdMs, string action)
    {
        using var client = Connect(instance, DefaultWaitTimeoutMs);
        var response = client.Send("key", new Dictionary<string, JsonNode> { ["key"] = key, ["holdMs"] = holdMs });
        if (!IsOk(response))
            return this.Report(response);

        // The plugin posts the key up on a timer and answers straight away.  Wait it out, so whatever runs next
        // (a screenshot, another key) sees the key let go.
        Thread.Sleep(Math.Max(0, holdMs) + KeyReleaseSettleMs);

        var result = response["result"].DeepClone().AsObject();
        if (action != null)
            result["action"] = action;
        this.WriteJson(result);
        return ExitOk;
    }

    private static string DefaultKeymapPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Asheron's Call", "ACClient.keymap");
}

/// <summary>
/// Reads key bindings from an AC keymap file.  Bindings look like
/// <c>CameraRotateLeft [ "" [ 0 DIK_NUMPAD4 ] ]</c>: action, then device (0 = keyboard), key, and optional subcontrol
/// and metakey fields.  A line holding only a name starts a section (e.g. CameraControls).
/// </summary>
public static class Keymap
{
    private static readonly System.Text.RegularExpressions.Regex Binding = new(
        @"^\s*(?<action>\w+)\s*\[\s*""[^""]*""\s*\[\s*(?<device>\d+)\s+(?<key>DIK_\w+)(?<rest>[^\]]*)\]");

    private static readonly System.Text.RegularExpressions.Regex Section = new(@"^\s*(?<name>\w+)\s*$");

    /// <summary>
    /// The first keyboard key bound to <paramref name="action"/> with no metakeys, preferring
    /// <paramref name="preferredSection"/>, or null.  Bindings that need Shift, Ctrl or Alt are skipped: rectl key
    /// presses one key.
    /// </summary>
    public static string FindKey(IEnumerable<string> lines, string action, string preferredSection)
    {
        string section = null;
        string fallback = null;
        foreach (var line in lines)
        {
            var sectionMatch = Section.Match(line);
            if (sectionMatch.Success)
            {
                section = sectionMatch.Groups["name"].Value;
                continue;
            }

            var match = Binding.Match(line);
            if (!match.Success || match.Groups["action"].Value != action || match.Groups["device"].Value != "0")
                continue;
            if (!NoMetakeys(match.Groups["rest"].Value))
                continue;

            var key = match.Groups["key"].Value;
            if (section == preferredSection)
                return key;
            fallback ??= key;
        }

        return fallback;
    }

    /// <summary>The subcontrol and metakey fields, if any, are all zero.</summary>
    private static bool NoMetakeys(string rest)
    {
        foreach (var field in rest.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
        {
            var text = field.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? field.Substring(2) : field;
            if (!long.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out var value) || value != 0)
                return false;
        }
        return true;
    }
}
