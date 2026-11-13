using System;
using System.Linq;
using System.Reflection;

namespace RedoxExtensions.VirindiInterop;

/// <summary>
/// The VTank internals behind the Monsters tab, found by reflection.  The names are the obfuscated ones in the 2016
/// build of utank2-i.dll (External/Virindi), the build Utility Belt patches.  VTank is no longer updated, so they
/// stay put.  Every member is checked by name, signature and type, so a different build fails here instead of
/// misbehaving in game.
/// </summary>
internal sealed class VTMonsterListMembers
{
    private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    private VTMonsterListMembers()
    {
    }

    /// <summary>PluginCore.dz : VTank's state, created when the character logs in.</summary>
    public FieldInfo State { get; private set; }

    /// <summary>dz.e : the monster list (type d1).</summary>
    public FieldInfo Monsters { get; private set; }

    /// <summary>dz.m : the loaded profiles (type da).</summary>
    public FieldInfo Profiles { get; private set; }

    /// <summary>da.m : the settings profile file name, empty when none is loaded.</summary>
    public FieldInfo SettingsProfileName { get; private set; }

    /// <summary>PluginCore.dt : set once VTank has finished loading the character, before which saves are skipped.</summary>
    public FieldInfo Ready { get; private set; }

    /// <summary>d1+a : one monster list entry.</summary>
    public Type Entry { get; private set; }

    public FieldInfo EntryName { get; private set; }

    public FieldInfo EntryPriority { get; private set; }

    public FieldInfo EntryIsDefault { get; private set; }

    /// <summary>d1.d(name) : whether an entry exists.  Names compare ignoring case.</summary>
    public MethodInfo Exists { get; private set; }

    /// <summary>d1.c(name) : a copy of the entry, or of the default entry when there is none.</summary>
    public MethodInfo Get { get; private set; }

    /// <summary>d1.a(name, entry) : updates the entry's row in place, or appends one, and clears the match cache.</summary>
    public MethodInfo Upsert { get; private set; }

    /// <summary>d1.a(name) : removes the entry (never the default one) and clears the match cache.</summary>
    public MethodInfo Remove { get; private set; }

    /// <summary>d1.f() : a new entry with the settings the Monsters tab gives one.</summary>
    public MethodInfo NewEntry { get; private set; }

    /// <summary>da.i() : saves the settings profile, what the Monsters tab calls after every edit.</summary>
    public MethodInfo SaveSettings { get; private set; }

    /// <summary>PluginCore.p() : redraws the Monsters tab list.</summary>
    public MethodInfo RedrawMonsterList { get; private set; }

    public static VTMonsterListMembers Resolve(Assembly vtank)
    {
        var pluginCore = Require(vtank.GetType("uTank2.PluginCore"), "uTank2.PluginCore");

        var members = new VTMonsterListMembers();
        members.State = RequireField(pluginCore, "dz", null, AnyStatic);
        var state = members.State.FieldType;
        members.Ready = RequireField(pluginCore, "dt", typeof(bool), AnyInstance);
        members.RedrawMonsterList = RequireMethod(pluginCore, "p", typeof(void));

        members.Monsters = RequireField(state, "e", null, AnyInstance);
        members.Profiles = RequireField(state, "m", null, AnyInstance);
        var monsters = members.Monsters.FieldType;
        var profiles = members.Profiles.FieldType;

        members.SettingsProfileName = RequireField(profiles, "m", typeof(string), AnyInstance);
        members.SaveSettings = RequireMethod(profiles, "i", typeof(void));

        members.Entry = Require(monsters.GetNestedType("a", BindingFlags.Public | BindingFlags.NonPublic), monsters.Name + "+a");
        members.EntryName = RequireField(members.Entry, "u", typeof(string), AnyInstance);
        members.EntryPriority = RequireField(members.Entry, "a", typeof(int), AnyInstance);
        members.EntryIsDefault = RequireField(members.Entry, "v", typeof(bool), AnyInstance);

        members.Exists = RequireMethod(monsters, "d", typeof(bool), typeof(string));
        members.Get = RequireMethod(monsters, "c", members.Entry, typeof(string));
        members.Upsert = RequireMethod(monsters, "a", typeof(void), typeof(string), members.Entry);
        members.Remove = RequireMethod(monsters, "a", typeof(void), typeof(string));
        members.NewEntry = RequireMethod(monsters, "f", members.Entry);

        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(monsters))
            throw new MissingMemberException(monsters.Name + " is not enumerable");

        return members;
    }

    private static T Require<T>(T value, string what) where T : class
        => value ?? throw new MissingMemberException("VTank has no " + what);

    private static FieldInfo RequireField(Type type, string name, Type fieldType, BindingFlags flags)
    {
        var field = Require(type.GetField(name, flags), type.Name + "." + name);
        if (fieldType != null && field.FieldType != fieldType)
            throw new MissingMemberException($"VTank's {type.Name}.{name} is a {field.FieldType.Name}, not a {fieldType.Name}");
        return field;
    }

    private static MethodInfo RequireMethod(Type type, string name, Type returns, params Type[] parameters)
    {
        var matches = type.GetMethods(AnyInstance)
            .Where(m => m.Name == name
                && m.ReturnType == returns
                && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameters))
            .ToList();

        if (matches.Count != 1)
        {
            var signature = $"{returns.Name} {type.Name}.{name}({string.Join(", ", parameters.Select(p => p.Name).ToArray())})";
            throw new MissingMemberException(matches.Count == 0 ? "VTank has no " + signature : "VTank has more than one " + signature);
        }

        return matches[0];
    }
}
