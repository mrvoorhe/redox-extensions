using System;
using System.Collections.Generic;

using Decal.Adapter.Wrappers;

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

        // VT state is left out for now.  VTUtilities.GetVTOptions listens on CommandLineText for VT's
        // "[VTank] Option" replies, which VT doesn't raise there, so it always came back empty.  The real
        // source is uTank2 PluginCore.GetExternalInterface(); see "Still to check on Windows" in
        // docs/design/Feature-AgentBridge.md.

        return result;
    }
}
