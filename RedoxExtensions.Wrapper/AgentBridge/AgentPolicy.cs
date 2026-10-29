using System;
using System.Collections.Generic;

namespace RedoxExtensions.Wrapper.AgentBridge;

public enum AgentMode
{
    Off,
    ReadOnly,
    ReadWrite,
    Full
}

public class AgentPolicy
{
    // Prefixes of documented commands (docs/ref/InGameCommands) that move items or currency, or change the
    // character or session in ways that are hard to undo.  Prefix matching also covers suffix variants like /ub givep.
    private static readonly string[] UnsafeCommandPrefixes =
    {
        "/re give",
        "/ub give",
        "/ub ig",
        "/ub autotrade",
        "/ub autovendor",
        "/ub vendor",
        "/ub autosalvage",
        "/ub clearbugged",
        "/ub swearallegiance",
        "/ub breakallegiance",
        "/ub logout",
        "/ub quit",
        "/mt give",
        "/mt drop",
        "/mt trade",
        "/mt vendor",
        "/mt send",
        "/mt click",
        "/b d",
        "/b w",
        "/b t",
        "/bank",
        "/clap",
        "/enl",
    };

    private const string UbDelayPrefix = "/ub delay ";

    private readonly Func<DateTime> _clock;
    private readonly int _maxExecsPerWindow;
    private readonly TimeSpan _rateWindow;
    private readonly Queue<DateTime> _recentExecs = new();
    private readonly object _lock = new();

    public AgentPolicy(Func<DateTime> clock = null, int maxExecsPerWindow = 20, TimeSpan? rateWindow = null)
    {
        this._clock = clock ?? (() => DateTime.UtcNow);
        this._maxExecsPerWindow = maxExecsPerWindow;
        this._rateWindow = rateWindow ?? TimeSpan.FromSeconds(10);
    }

    public AgentMode Mode { get; set; } = AgentMode.Off;

    public static bool TryParseMode(string text, out AgentMode mode)
    {
        switch ((text ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "":
            case "on":
            case "ro":
                mode = AgentMode.ReadOnly;
                return true;
            case "rw":
                mode = AgentMode.ReadWrite;
                return true;
            case "full":
                mode = AgentMode.Full;
                return true;
            case "off":
                mode = AgentMode.Off;
                return true;
            default:
                mode = AgentMode.Off;
                return false;
        }
    }

    public static string ModeName(AgentMode mode) => mode switch
    {
        AgentMode.ReadOnly => "ro",
        AgentMode.ReadWrite => "rw",
        AgentMode.Full => "full",
        _ => "off"
    };

    public static bool IsUnsafeCommand(string commandText)
    {
        var normalized = NormalizeCommand(commandText);

        if (normalized.StartsWith(UbDelayPrefix, StringComparison.Ordinal))
        {
            var parts = normalized.Substring(UbDelayPrefix.Length).Split(new[] { ' ' }, 2);
            return parts.Length < 2 || IsUnsafeCommand(parts[1]);
        }

        foreach (var prefix in UnsafeCommandPrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>
    /// /re reload unloads the plugin from underneath whatever called it, so it has to go through the reload request instead.
    /// </summary>
    public static bool IsPluginLifecycleCommand(string commandText)
    {
        var normalized = NormalizeCommand(commandText);

        if (normalized.StartsWith(UbDelayPrefix, StringComparison.Ordinal))
        {
            var parts = normalized.Substring(UbDelayPrefix.Length).Split(new[] { ' ' }, 2);
            return parts.Length == 2 && IsPluginLifecycleCommand(parts[1]);
        }

        foreach (var prefix in new[] { "/re", "/rew" })
        {
            foreach (var verb in new[] { "reload", "load", "unload", "agent" })
            {
                var full = $"{prefix} {verb}";
                if (normalized == full || normalized.StartsWith(full + " ", StringComparison.Ordinal))
                    return true;
            }
        }
        return false;
    }

    public bool CanRead(out string error)
    {
        error = this.Mode == AgentMode.Off ? "Agent bridge is off" : null;
        return error == null;
    }

    public bool CanExec(string commandText, out string error)
    {
        if (!this.CanRead(out error))
            return false;

        if (this.Mode == AgentMode.ReadOnly)
        {
            error = "Agent bridge is read-only.  Use '/re agent rw' in game to allow commands";
            return false;
        }

        if (string.IsNullOrEmpty(commandText) || commandText.Trim().Length == 0)
        {
            error = "Missing command text";
            return false;
        }

        if (IsPluginLifecycleCommand(commandText))
        {
            error = "Plugin lifecycle and agent commands can't be sent through exec.  Use the reload request instead";
            return false;
        }

        if (this.Mode != AgentMode.Full && IsUnsafeCommand(commandText))
        {
            error = "Command moves items or currency.  Use '/re agent full' in game to allow it";
            return false;
        }

        if (!this.TryConsumeRate())
        {
            error = $"Rate limited: more than {this._maxExecsPerWindow} commands in {this._rateWindow.TotalSeconds}s";
            return false;
        }

        return true;
    }

    public bool CanReload(out string error)
    {
        if (!this.CanRead(out error))
            return false;
        if (this.Mode == AgentMode.ReadOnly)
        {
            error = "Agent bridge is read-only.  Use '/re agent rw' in game to allow reload";
            return false;
        }
        return true;
    }

    private bool TryConsumeRate()
    {
        lock (this._lock)
        {
            var now = this._clock();
            while (this._recentExecs.Count > 0 && now - this._recentExecs.Peek() >= this._rateWindow)
                this._recentExecs.Dequeue();
            if (this._recentExecs.Count >= this._maxExecsPerWindow)
                return false;
            this._recentExecs.Enqueue(now);
            return true;
        }
    }

    private static string NormalizeCommand(string commandText)
    {
        var parts = (commandText ?? string.Empty).Trim().ToLowerInvariant().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }
}
