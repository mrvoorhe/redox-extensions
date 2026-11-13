using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RedoxExtensions.VirindiInterop;

internal sealed class VTMonsterEntry
{
    public VTMonsterEntry(string name, int priority, bool isDefault)
    {
        this.Name = name;
        this.Priority = priority;
        this.IsDefault = isDefault;
    }

    public string Name { get; }

    public int Priority { get; }

    public bool IsDefault { get; }
}

/// <summary>
/// Reads and edits the monster list of VTank's loaded settings profile, in memory, the way the Monsters tab does:
/// each edit is followed by a save of the profile and a redraw of the tab.  Game thread only.
/// </summary>
internal static class VTMonsterList
{
    public const string DefaultEntryName = "<DEFAULT>";

    // The values the Monsters tab cycles through.
    public const int MinPriority = -1;
    public const int MaxPriority = 4;

    private static VTMonsterListMembers _members;
    private static string _resolveError;

    public static IList<VTMonsterEntry> List()
    {
        var monsters = GetMonsters(out var members);
        var entries = new List<VTMonsterEntry>();
        foreach (var entry in (IEnumerable)monsters)
            entries.Add(ToEntry(members, entry));
        return entries;
    }

    public static VTMonsterEntry Find(string name)
    {
        var monsters = GetMonsters(out var members);
        if (!Exists(members, monsters, name))
            return null;
        return ToEntry(members, Invoke(members.Get, monsters, name));
    }

    /// <summary>Returns the entry's previous priority.</summary>
    public static int SetPriority(string name, int priority)
    {
        CheckPriority(priority);
        var monsters = GetMonsters(out var members);
        if (!Exists(members, monsters, name))
            throw new InvalidOperationException($"'{name}' is not on the monster list");

        var entry = Invoke(members.Get, monsters, name);
        var previous = (int)members.EntryPriority.GetValue(entry);
        members.EntryPriority.SetValue(entry, priority);
        Invoke(members.Upsert, monsters, (string)members.EntryName.GetValue(entry), entry);
        SaveAndRedraw(members);
        return previous;
    }

    public static void Add(string name, int priority)
    {
        CheckPriority(priority);
        var monsters = GetMonsters(out var members);
        if (Exists(members, monsters, name))
            throw new InvalidOperationException($"'{name}' is already on the monster list");

        var entry = Invoke(members.NewEntry, monsters);
        members.EntryPriority.SetValue(entry, priority);
        Invoke(members.Upsert, monsters, name, entry);
        SaveAndRedraw(members);
    }

    /// <summary>Returns the removed entry's name as the list spelled it.</summary>
    public static string Remove(string name)
    {
        if (IsDefaultName(name))
            throw new InvalidOperationException("The default entry can't be removed");

        var monsters = GetMonsters(out var members);
        if (!Exists(members, monsters, name))
            throw new InvalidOperationException($"'{name}' is not on the monster list");

        var listed = (string)members.EntryName.GetValue(Invoke(members.Get, monsters, name));
        Invoke(members.Remove, monsters, name);
        SaveAndRedraw(members);
        return listed;
    }

    public static string SettingsProfileName()
    {
        var state = GetState(out var members);
        return (string)members.SettingsProfileName.GetValue(members.Profiles.GetValue(state));
    }

    public static bool IsDefaultName(string name) => string.Equals(name, DefaultEntryName, StringComparison.OrdinalIgnoreCase);

    private static void CheckPriority(int priority)
    {
        if (priority < MinPriority || priority > MaxPriority)
            throw new InvalidOperationException($"Priority must be from {MinPriority} to {MaxPriority}, like on the Monsters tab");
    }

    private static VTMonsterEntry ToEntry(VTMonsterListMembers members, object entry)
        => new(
            (string)members.EntryName.GetValue(entry),
            (int)members.EntryPriority.GetValue(entry),
            (bool)members.EntryIsDefault.GetValue(entry));

    private static bool Exists(VTMonsterListMembers members, object monsters, string name)
        => (bool)Invoke(members.Exists, monsters, name);

    private static void SaveAndRedraw(VTMonsterListMembers members)
    {
        Invoke(members.SaveSettings, members.Profiles.GetValue(members.State.GetValue(null)));
        try
        {
            Invoke(members.RedrawMonsterList, uTank2.PluginCore.PC);
        }
        catch (Exception ex)
        {
            REPlugin.Instance?.Error?.LogError(ex);
        }
    }

    private static object GetMonsters(out VTMonsterListMembers members)
    {
        var state = GetState(out members);
        return members.Monsters.GetValue(state) ?? throw new InvalidOperationException("VTank's monster list isn't loaded");
    }

    private static object GetState(out VTMonsterListMembers members)
    {
        if (!VTankFreeMethodsTest.VTConnector.IsVTankPresent())
            throw new InvalidOperationException("VTank isn't running");

        members = GetMembers();
        var state = members.State.GetValue(null);
        if (state == null || !(bool)members.Ready.GetValue(uTank2.PluginCore.PC))
            throw new InvalidOperationException("VTank hasn't finished loading the character");
        return state;
    }

    private static VTMonsterListMembers GetMembers()
    {
        if (_members == null && _resolveError == null)
        {
            try
            {
                _members = VTMonsterListMembers.Resolve(typeof(uTank2.PluginCore).Assembly);
            }
            catch (Exception ex)
            {
                _resolveError = ex.Message;
                REPlugin.Instance?.Error?.LogError(ex);
            }
        }

        return _members ?? throw new InvalidOperationException("This VTank build isn't supported: " + _resolveError);
    }

    private static object Invoke(MethodInfo method, object target, params object[] args)
    {
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw new InvalidOperationException("VTank failed: " + ex.InnerException.Message, ex.InnerException);
        }
    }
}
