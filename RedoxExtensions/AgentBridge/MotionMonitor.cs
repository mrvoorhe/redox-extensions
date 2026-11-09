using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.AgentBridge;

/// <summary>
/// Journals "motion" events (idle, moving, stuck, portal) for the character, so an agent can wait for "stuck" instead
/// of polling positions or taking screenshots.  Off until the 'motion' request turns it on; resets on /re reload.
/// Samples the position at about 10 Hz from RenderFrame and reads movement intent from the client's MoveToState.
/// </summary>
internal static class MotionMonitor
{
    public const string Kind = "motion";

    private const double SampleInterval = 0.1;

    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    private static MotionAnalyzer _analyzer;
    private static double _lastSample = double.NegativeInfinity;

    public static bool IsOn => _analyzer != null;

    private static double Now => Clock.Elapsed.TotalSeconds;

    /// <summary>
    /// 'motion' request.  "on" / "off" (true or false), "stuckSeconds", "minSpeed" (m/s), "minDisplacement" (m).
    /// With no fields it only reports.  Returns the status.
    /// </summary>
    public static object Configure(IDictionary<string, object> request)
    {
        var on = GetBool(request, "on");
        if (GetBool(request, "off") == true)
            on = false;

        var settings = _analyzer?.Settings ?? new MotionSettings();
        if (GetDouble(request, "stuckSeconds") is { } stuck)
            settings.StuckSeconds = Positive(stuck, "stuckSeconds");
        if (GetDouble(request, "minSpeed") is { } speed)
            settings.MinSpeed = Positive(speed, "minSpeed");
        if (GetDouble(request, "minDisplacement") is { } displacement)
            settings.MinDisplacement = Positive(displacement, "minDisplacement");

        if (on == true && _analyzer == null)
            Start(settings);
        else if (on == false)
            Stop();

        return Status();
    }

    public static void Stop()
    {
        if (_analyzer == null)
            return;

        _analyzer = null;
        REPlugin.Instance.Events.Decal.RenderFrame -= Decal_RenderFrame;
        REPlugin.Instance.Events.Decal.ChangePortalMode -= Decal_ChangePortalMode;
    }

    /// <summary>The 'motion' block of the state response, or null when the monitor is off.</summary>
    public static Dictionary<string, object> Summary()
    {
        var a = _analyzer;
        if (a == null)
            return null;

        return new Dictionary<string, object>
        {
            ["state"] = a.State,
            ["seconds"] = Round(Now - a.StateSince, 1),
            ["speed"] = Round(a.Speed, 2),
            ["intent"] = a.IntentSeen ? a.Intent.ToString() : "unknown",
            ["navigating"] = a.Navigating,
        };
    }

    public static void OnClientDispatch(NetworkMessageEventArgs e)
    {
        var a = _analyzer;
        if (a == null || e.Message.Type != MoveToStateParser.GameActionType)
            return;

        if (MoveToStateParser.TryParse(e.Message.RawData, out var intent))
            a.SetIntent(Now, intent);
    }

    private static void Start(MotionSettings settings)
    {
        _analyzer = new MotionAnalyzer(settings);
        _lastSample = double.NegativeInfinity;
        REPlugin.Instance.Events.Decal.RenderFrame += Decal_RenderFrame;
        REPlugin.Instance.Events.Decal.ChangePortalMode += Decal_ChangePortalMode;
    }

    private static Dictionary<string, object> Status()
    {
        var result = new Dictionary<string, object> { ["on"] = _analyzer != null };
        var a = _analyzer;
        if (a == null)
            return result;

        foreach (var pair in Summary())
            result[pair.Key] = pair.Value;

        result["stuckSeconds"] = a.Settings.StuckSeconds;
        result["minSpeed"] = a.Settings.MinSpeed;
        result["minDisplacement"] = a.Settings.MinDisplacement;

        var now = Now;
        var recent = new List<object>();
        foreach (var s in a.Recent(5, 1.0))
        {
            recent.Add(new Dictionary<string, object>
            {
                ["ago"] = Round(now - s.Time, 1),
                ["landcell"] = ((uint)s.Landcell).ToString("X8", CultureInfo.InvariantCulture),
                ["x"] = Round(s.X, 2),
                ["y"] = Round(s.Y, 2),
                ["z"] = Round(s.Z, 2),
                ["heading"] = Round(s.Heading, 0),
            });
        }
        result["recent"] = recent;
        return result;
    }

    private static void Decal_RenderFrame(object sender, EventArgs e)
    {
        var a = _analyzer;
        var now = Now;
        if (a == null || now - _lastSample < SampleInterval)
            return;
        _lastSample = now;

        var plugin = REPlugin.Instance;
        var actions = plugin.Actions;
        var navigating = plugin.MonitorManager.CharacterState.VTRunning.WaitOne(0);
        var text = a.AddSample(now, actions.Landcell, actions.LocationX, actions.LocationY, actions.LocationZ, actions.Heading, navigating);

        if (text != null && AgentBridgeService.IsEnabled)
            AgentBridgeService.Publish(Kind, text);
    }

    private static void Decal_ChangePortalMode(object sender, ChangePortalModeEventArgs e)
    {
        _analyzer?.SetPortal(Now, e.Type == PortalEventType.EnterPortal);
    }

    private static bool? GetBool(IDictionary<string, object> request, string key)
    {
        var value = MiniJson.GetString(request, key);
        if (value == null)
            return null;
        return value.ToLowerInvariant() switch
        {
            "true" or "1" or "on" => true,
            "false" or "0" or "off" => false,
            _ => throw new ArgumentException($"{key} must be true or false"),
        };
    }

    private static double? GetDouble(IDictionary<string, object> request, string key)
    {
        var value = MiniJson.GetString(request, key);
        if (value == null)
            return null;
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            throw new ArgumentException($"{key} must be a number");
        return d;
    }

    private static double Positive(double value, string key) =>
        value > 0 ? value : throw new ArgumentException($"{key} must be more than 0");

    private static double Round(double value, int decimals) => Math.Round(value, decimals);
}
