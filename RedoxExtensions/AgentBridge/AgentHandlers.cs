using System;
using System.Collections.Generic;

using Decal.Adapter.Wrappers;

using RedoxExtensions.VirindiInterop;
using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.AgentBridge;

/// <summary>
/// Plugin-side requests for the agent bridge.  Registered while the plugin is loaded; run on the game thread.
/// </summary>
internal static class AgentHandlers
{
    private const string StateCmd = "state";

    public static void Register()
    {
        AgentBridgeService.Handlers.Register(StateCmd, State);
    }

    public static void Unregister()
    {
        AgentBridgeService.Handlers.Unregister(StateCmd);
    }

    private static object State(IDictionary<string, object> request)
    {
        var plugin = REPlugin.Instance;
        var character = plugin.CharacterFilter;
        var actions = plugin.Actions;

        var result = new Dictionary<string, object>
        {
            ["character"] = new Dictionary<string, object>
            {
                ["name"] = character.Name,
                ["id"] = character.Id,
                ["server"] = character.Server,
                ["level"] = character.Level,
            },
            ["vitals"] = new Dictionary<string, object>
            {
                ["health"] = character.Health,
                ["maxHealth"] = character.EffectiveVital[CharFilterVitalType.Health],
                ["stamina"] = character.Stamina,
                ["maxStamina"] = character.EffectiveVital[CharFilterVitalType.Stamina],
                ["mana"] = character.Mana,
                ["maxMana"] = character.EffectiveVital[CharFilterVitalType.Mana],
            },
            ["location"] = new Dictionary<string, object>
            {
                ["landcell"] = actions.Landcell,
                ["x"] = actions.LocationX,
                ["y"] = actions.LocationY,
                ["z"] = actions.LocationZ,
                ["heading"] = actions.Heading,
            },
            ["busyState"] = actions.BusyState,
            ["busyStateId"] = actions.BusyStateId,
            ["combatMode"] = actions.CombatMode.ToString(),
            ["selection"] = actions.CurrentSelection,
        };

        try
        {
            result["vt"] = VTUtilities.GetVTOptions(VTOptionNames.AllStateOptions);
        }
        catch (Exception e)
        {
            result["vt"] = new Dictionary<string, object> { ["error"] = e.Message };
        }

        return result;
    }
}
