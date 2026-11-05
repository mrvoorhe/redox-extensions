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
    private const string WorldCmd = "world";
    private const string ObjectCmd = "object";

    public static void Register()
    {
        AgentBridgeService.Handlers.Register(StateCmd, State);
        AgentBridgeService.Handlers.Register(WorldCmd, World);
        AgentBridgeService.Handlers.Register(ObjectCmd, Object);
    }

    public static void Unregister()
    {
        AgentBridgeService.Handlers.Unregister(StateCmd);
        AgentBridgeService.Handlers.Unregister(WorldCmd);
        AgentBridgeService.Handlers.Unregister(ObjectCmd);
    }

    /// <summary>
    /// Landscape objects (sorted by distance) or inventory (sorted by name), filtered and paged.  See WorldQuery.
    /// </summary>
    private static object World(IDictionary<string, object> request)
    {
        var query = WorldQuery.Parse(request);
        var plugin = REPlugin.Instance;
        var selfId = plugin.CharacterFilter.Id;
        var landscape = query.Scope == WorldQuery.ScopeLandscape;

        var source = landscape ? plugin.WorldFilter.GetLandscape() : plugin.WorldFilter.GetInventory();
        var items = new List<WorldItem>();
        foreach (WorldObject wo in source)
            items.Add(ToWorldItem(wo, landscape ? selfId : 0));

        return query.Apply(items);
    }

    /// <summary>Everything Decal knows about one object: the summary fields plus every value key it has.</summary>
    private static object Object(IDictionary<string, object> request)
    {
        // Not "id": that's the request id the processor echoes back.
        var id = MiniJson.GetLong(request, "objectId") ?? throw new ArgumentException("object needs an objectId");
        var plugin = REPlugin.Instance;
        var wo = plugin.WorldFilter[(int)id] ?? throw new ArgumentException($"No object with id {id}");

        var result = ToWorldItem(wo, plugin.CharacterFilter.Id).ToJson();
        result["category"] = wo.Category;
        result["behavior"] = wo.Behavior;
        result["type"] = wo.Type;

        var longs = new Dictionary<string, object>();
        foreach (var key in wo.LongKeys)
            longs[KeyName(typeof(LongValueKey), key)] = wo.Values((LongValueKey)key);
        result["long"] = longs;

        var doubles = new Dictionary<string, object>();
        foreach (var key in wo.DoubleKeys)
            doubles[KeyName(typeof(DoubleValueKey), key)] = wo.Values((DoubleValueKey)key);
        result["double"] = doubles;

        var strings = new Dictionary<string, object>();
        foreach (var key in wo.StringKeys)
            strings[KeyName(typeof(StringValueKey), key)] = wo.Values((StringValueKey)key);
        result["string"] = strings;

        var bools = new Dictionary<string, object>();
        foreach (var key in wo.BoolKeys)
            bools[KeyName(typeof(BoolValueKey), key)] = wo.Values((BoolValueKey)key);
        result["bool"] = bools;

        return result;
    }

    /// <param name="selfId">
    /// Character id to measure distance from, or 0 to skip distance and coordinates.  Also skipped for objects in a
    /// container and for the character itself.
    /// </param>
    private static WorldItem ToWorldItem(WorldObject wo, int selfId)
    {
        var item = new WorldItem
        {
            Id = wo.Id,
            Name = wo.Name,
            ObjectClass = wo.ObjectClass.ToString(),
            Container = wo.Container,
            Wielder = wo.Values(LongValueKey.Wielder, 0),
            StackCount = wo.Values(LongValueKey.StackCount, 0),
            HasIdData = wo.HasIdData,
        };

        if (selfId != 0 && wo.Id != 0 && wo.Id != selfId && wo.Container == 0)
        {
            item.Distance = REPlugin.Instance.WorldFilter.Distance(selfId, wo.Id) * 240;

            var coords = wo.Coordinates();
            if (coords != null)
            {
                item.NorthSouth = coords.NorthSouth;
                item.EastWest = coords.EastWest;
            }
        }

        return item;
    }

    private static string KeyName(Type keyEnum, int key) => Enum.GetName(keyEnum, key) ?? key.ToString();

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
