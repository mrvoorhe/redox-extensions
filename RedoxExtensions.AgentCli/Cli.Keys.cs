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
    /// next, foreground or background, so this is for ballpark angles.  Tilting is taken to go at the same rate: from
    /// the default view, up reaches top-down in about 210 ms and down reaches its limit (looking up past the character)
    /// in about 260 ms, which fits a default view about 20 degrees below level.
    /// </summary>
    public const double CameraMsPerDegree = CameraFullTurnMs / 360.0;

    public const double CameraFullTurnMs = 1125;

    /// <summary>
    /// The hold that takes the camera from the default distance to the closest (just behind the head) or the farthest,
    /// measured on Cowhead indoors: both took about 200 ms.  Indoors the walls may stop it short of the farthest.
    /// </summary>
    public const double CameraZoomRangeMs = 200;

    /// <summary>Time for the default view to take hold before moving from it.</summary>
    private const int CameraResetSettleMs = 1000;

    public const int CameraStep = 10;

    public enum CameraAxis { Rotate, Tilt, Zoom }

    private static readonly string[] CameraAxisNames = { "rotate", "tilt", "zoom" };

    /// <summary>
    /// The camera move and hold time for one axis, from the default view, in steps of 10:
    /// rotate -360..360 degrees (positive CameraRotateRight, negative CameraRotateLeft),
    /// tilt -90..90 degrees (positive CameraRotateUp, toward top-down; negative CameraRotateDown, toward looking up),
    /// zoom -100..100 percent of the way to the limit (positive CameraMoveToward, in; negative CameraMoveAway, out).
    /// Holding past a tilt or zoom limit just stays at the limit.
    /// </summary>
    public static bool TryCameraStep(CameraAxis axis, string text, out string move, out int holdMs, out string error)
    {
        move = null;
        holdMs = 0;
        error = null;
        var max = axis switch { CameraAxis.Rotate => 360, CameraAxis.Tilt => 90, _ => 100 };
        var unit = axis == CameraAxis.Zoom ? "percent" : "degrees";
        var name = CameraAxisNames[(int)axis];
        if (!int.TryParse(text, out var value) || value % CameraStep != 0 || value < -max || value > max)
        {
            error = $"{name} takes {unit} in steps of {CameraStep} from -{max} to {max}, not '{text}'";
            return false;
        }

        var size = Math.Abs(value);
        switch (axis)
        {
            case CameraAxis.Rotate:
                move = value >= 0 ? "right" : "left";
                holdMs = (int)Math.Round(size * CameraMsPerDegree);
                break;
            case CameraAxis.Tilt:
                move = value >= 0 ? "up" : "down";
                holdMs = (int)Math.Round(size * CameraMsPerDegree);
                break;
            default:
                move = value >= 0 ? "closer" : "farther";
                holdMs = (int)Math.Round(size / 100.0 * CameraZoomRangeMs);
                break;
        }
        return true;
    }

    public static bool TryCameraRotation(string text, out string move, out int holdMs, out string error) =>
        TryCameraStep(CameraAxis.Rotate, text, out move, out holdMs, out error);

    /// <summary>
    /// rectl camera rotate|tilt|zoom &lt;n&gt; [--rotate n] [--tilt n] [--zoom n]: reset to the default view, then rotate,
    /// tilt and zoom from it, in that order.
    /// </summary>
    private int CameraView(BridgeInstance instance, Arguments parsed, string path)
    {
        var values = new string[3];
        values[Array.IndexOf(CameraAxisNames, parsed.Positionals[0].ToLowerInvariant())] = parsed.Positionals[1];
        for (var i = 0; i < CameraAxisNames.Length; i++)
        {
            if (parsed.Get(CameraAxisNames[i]) is not { } option)
                continue;
            if (values[i] != null)
            {
                this._err.WriteLine($"{CameraAxisNames[i]} is given twice");
                return ExitUsage;
            }
            values[i] = option;
        }

        var lines = File.ReadAllLines(path);
        var reset = Keymap.FindKey(lines, CameraActions["default"], "CameraControls");
        if (reset == null)
        {
            this._err.WriteLine($"{path} binds no plain keyboard key to {CameraActions["default"]}");
            return ExitFailed;
        }

        var presses = new List<(string Action, string Key, int HoldMs)>();
        var summary = new JsonObject();
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] == null)
                continue;
            if (!TryCameraStep((CameraAxis)i, values[i], out var move, out var holdMs, out var error))
            {
                this._err.WriteLine(error);
                return ExitUsage;
            }

            summary[CameraAxisNames[i]] = int.Parse(values[i]);
            if (holdMs == 0)
                continue;

            var action = CameraActions[move];
            var key = Keymap.FindKey(lines, action, "CameraControls");
            if (key == null)
            {
                this._err.WriteLine($"{path} binds no plain keyboard key to {action}");
                return ExitFailed;
            }
            presses.Add((action, key, holdMs));
        }

        using var client = Connect(instance, DefaultWaitTimeoutMs);
        var response = client.Send("key", new Dictionary<string, JsonNode> { ["key"] = reset, ["holdMs"] = DefaultKeyHoldMs });
        if (!IsOk(response))
            return this.Report(response);
        Thread.Sleep(DefaultKeyHoldMs + CameraResetSettleMs);

        var done = new JsonArray();
        foreach (var (action, key, holdMs) in presses)
        {
            response = client.Send("key", new Dictionary<string, JsonNode> { ["key"] = key, ["holdMs"] = holdMs });
            if (!IsOk(response))
                return this.Report(response);
            Thread.Sleep(holdMs + KeyReleaseSettleMs);
            done.Add(new JsonObject { ["action"] = action, ["key"] = key, ["holdMs"] = holdMs });
        }

        summary["presses"] = done;
        this.WriteJson(summary);
        return ExitOk;
    }

    private int Camera(BridgeInstance instance, Arguments parsed)
    {
        if (parsed.Positionals.Count == 2 && Array.IndexOf(CameraAxisNames, parsed.Positionals[0].ToLowerInvariant()) >= 0)
        {
            var keymap = parsed.Get("keymap") ?? DefaultKeymapPath();
            if (!File.Exists(keymap))
            {
                this._err.WriteLine($"No keymap at {keymap}.  Use --keymap <path>");
                return ExitFailed;
            }
            return this.CameraView(instance, parsed, keymap);
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
