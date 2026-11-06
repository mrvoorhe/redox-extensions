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
    private const string NetCmd = "net";
    private const string MoveCmd = "move";

    public static void Register()
    {
        AgentBridgeService.Handlers.Register(StateCmd, State);
        AgentBridgeService.Handlers.Register(WorldCmd, World);
        AgentBridgeService.Handlers.Register(ObjectCmd, Object);
        AgentBridgeService.Handlers.Register(NetCmd, NetCapture.Configure);
        AgentBridgeService.Handlers.Register(MoveCmd, Move);
    }

    public static void Unregister()
    {
        AgentBridgeService.Handlers.Unregister(StateCmd);
        AgentBridgeService.Handlers.Unregister(WorldCmd);
        AgentBridgeService.Handlers.Unregister(ObjectCmd);
        AgentBridgeService.Handlers.Unregister(NetCmd);
        AgentBridgeService.Handlers.Unregister(MoveCmd);
    }

    /// <summary>
    /// Asks the server to move one item between the character's own containers.  Returns once the request is sent;
    /// the caller confirms by watching the item's container.  Refuses anything that could take an item out of the
    /// character's inventory: the item must be in the main pack or a side pack, not equipped, and the destination
    /// must be the character or one of its side packs.
    /// </summary>
    private static object Move(IDictionary<string, object> request)
    {
        RequireWriteMode();

        var plugin = REPlugin.Instance;
        var selfId = plugin.CharacterFilter.Id;

        var objectId = (int)(MiniJson.GetLong(request, "objectId") ?? throw new ArgumentException("move needs an objectId"));
        var destinationId = (int)(MiniJson.GetLong(request, "container") ?? throw new ArgumentException("move needs a container"));

        var item = plugin.WorldFilter[objectId] ?? throw new ArgumentException($"No object with id {objectId}");
        if (!IsOwnContainer(item.Container, selfId))
            throw new ArgumentException($"{item.Name} ({objectId}) isn't in your main pack or a side pack");
        if (item.Values(LongValueKey.EquippedSlots, 0) > 0 || item.Values(LongValueKey.Wielder, 0) != 0)
            throw new ArgumentException($"{item.Name} ({objectId}) is equipped");
        if (item.ObjectClass == ObjectClass.Container)
            throw new ArgumentException("Moving packs isn't supported");
        if (!IsOwnContainer(destinationId, selfId))
            throw new ArgumentException($"Container {destinationId} isn't your main pack or one of your side packs");

        if (item.Container == destinationId)
            return new Dictionary<string, object> { ["objectId"] = objectId, ["container"] = destinationId, ["alreadyThere"] = true };

        // The classic Hooks.MoveItem.  The two-argument overload (MoveItemEx) sent nothing at all on this client.
        plugin.Actions.MoveItem(objectId, destinationId, 0, false);
        return new Dictionary<string, object> { ["objectId"] = objectId, ["container"] = destinationId, ["requested"] = true };
    }

    /// <summary>
    /// The wrapper only checks read access before calling plugin handlers, so a handler that changes game state checks
    /// the mode itself, from the discovery file the wrapper keeps current.
    /// TODO: move this into AgentRequestProcessor (handlers declaring they write) at the next wrapper change.
    /// </summary>
    private static void RequireWriteMode()
    {
        string mode = null;
        try
        {
            var path = AgentDiscovery.FilePathFor(AgentDiscovery.DefaultDirectory, System.Diagnostics.Process.GetCurrentProcess().Id);
            mode = MiniJson.GetString(MiniJson.Parse(System.IO.File.ReadAllText(path)) as IDictionary<string, object>, "mode");
        }
        catch (Exception)
        {
            // Treated as read-only below.
        }

        if (mode != "rw" && mode != "full")
            throw new InvalidOperationException($"This needs '/re agent rw' (bridge mode is {mode ?? "unknown"})");
    }

    private static bool IsOwnContainer(int containerId, int selfId)
    {
        if (containerId == selfId)
            return true;

        var container = REPlugin.Instance.WorldFilter[containerId];
        return container != null && container.ObjectClass == ObjectClass.Container && container.Container == selfId;
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
            ObjectClassId = (int)wo.ObjectClass,
            Container = wo.Container,
            Wielder = wo.Values(LongValueKey.Wielder, 0),
            StackCount = wo.Values(LongValueKey.StackCount, 0),
            HasIdData = wo.HasIdData,
        };

        // Slot is computed by Decal, so it isn't in LongKeys and the object dump never shows it.  Side packs use it
        // for their order in the inventory window (mudsort's "pack N" is the pack with Slot N - 1).
        if (wo.Container != 0)
            item.Slot = wo.Values(LongValueKey.Slot, -1);

        if (selfId != 0 && wo.Id != 0 && wo.Id != selfId && wo.Container == 0)
        {
            item.Distance = REPlugin.Instance.WorldFilter.Distance(selfId, wo.Id) * 240;

            var coords = wo.Coordinates();
            if (coords != null)
            {
                item.NorthSouth = coords.NorthSouth;
                item.EastWest = coords.EastWest;
            }

            var raw = wo.RawCoordinates();
            if (raw != null)
                item.NavZ = raw.Z / 240.0;
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
            ["openedContainer"] = actions.OpenedContainer,
        };

        // VT state is left out for now.  VTUtilities.GetVTOptions listens on CommandLineText for VT's
        // "[VTank] Option" replies, which VT doesn't raise there, so it always came back empty.  The real
        // source is uTank2 PluginCore.GetExternalInterface(); see "Still to check on Windows" in
        // docs/design/Feature-AgentBridge.md.

        return result;
    }
}
