using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;
using RedoxLib.GameValues;

namespace RedoxLib.Objects;

/// <summary>
/// A copied WorldObject that is safe to use from any thread
/// </summary>
internal class CapturedWorldObject : IWorldObject
{
    private readonly Dictionary<int, bool> _boolValues = new();
    private readonly Dictionary<int, double> _doubleValues = new();
    private readonly Dictionary<int, int> _longValues = new();
    private readonly Dictionary<int, string> _stringValues = new();
    private readonly ReadOnlyCollection<int> _activeSpells;
    private readonly ReadOnlyCollection<int> _spells;

    private readonly CoordsObject _coordinates;
    private readonly Vector4Object _orientation;
    private readonly Vector3Object _offset;
    private readonly Vector3Object _rawCoordinates;

    internal CapturedWorldObject(WorldObject wo)
    {
        this.Id = wo.Id;
        this.Name = wo.Name;
        this.ObjectClass = wo.ObjectClass;

        this.Behavior = wo.Behavior;
        this.Category = wo.Category;
        this.Container = wo.Container;
        this.GameDataFlags1 = wo.GameDataFlags1;
        this.Icon = wo.Icon;
        this.LastIdTime = wo.LastIdTime;
        this.PhysicsDataFlags = wo.PhysicsDataFlags;
        this.Type = wo.Type;

        this.HasIdData = wo.HasIdData;

        this._coordinates = wo.Coordinates();
        this._orientation = wo.Orientation();
        this._offset = wo.Offset();
        this._rawCoordinates = wo.RawCoordinates();

        var tmpIntList = new List<int>();
        for (int i = 0; i < wo.ActiveSpellCount; i++)
        {
            tmpIntList.Add(wo.ActiveSpell(i));
        }

        this._activeSpells = General.ListOperations.Capture(tmpIntList).AsReadOnly();

        tmpIntList.Clear();
        for (int i = 0; i < wo.SpellCount; i++)
        {
            tmpIntList.Add(wo.Spell(i));
        }

        this._spells = General.ListOperations.Capture(tmpIntList).AsReadOnly();

        foreach (var key in wo.BoolKeys)
            _boolValues.Add(key, wo.Values((BoolValueKey)key));

        foreach (var key in wo.DoubleKeys)
            _doubleValues.Add(key, wo.Values((DoubleValueKey)key));

        foreach (var key in wo.LongKeys)
            this._longValues.Add(key, wo.Values((LongValueKey)key));

        foreach (var key in wo.StringKeys)
            _stringValues.Add(key, wo.Values((StringValueKey)key));
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public ObjectClass ObjectClass { get; private set; }

    public int ActiveSpellCount => this._activeSpells.Count;

    public int SpellCount => this._spells.Count;

    public int Behavior { get; private set; }

    public int Category { get; private set; }

    public int Container { get; private set; }

    public int GameDataFlags1 { get; private set; }

    public int Icon { get; private set; }

    public int LastIdTime { get; private set; }

    public int PhysicsDataFlags { get; private set; }

    public int Type { get; private set; }

    public bool HasIdData { get; private set; }

    public ReadOnlyCollection<int> BoolKeys => this._boolValues.Select(p => p.Key).ToList().AsReadOnly();

    public ReadOnlyCollection<int> LongKeys => this._longValues.Select(p => p.Key).ToList().AsReadOnly();

    public ReadOnlyCollection<int> DoubleKeys => this._doubleValues.Select(p => p.Key).ToList().AsReadOnly();

    public ReadOnlyCollection<int> StringKeys => this._stringValues.Select(p => p.Key).ToList().AsReadOnly();

    public CoordsObject Coords => this.Coordinates();

    public int ActiveSpell(int index) => this._activeSpells[index];

    public int Spell(int index) => this._spells[index];

    public bool Exists(BoolValueKey index) => this._boolValues.ContainsKey((int)index);

    public bool Exists(DoubleValueKey index) => this._doubleValues.ContainsKey((int)index);

    public bool Exists(LongValueKey index) => this._longValues.ContainsKey((int)index);

    public bool Exists(IntValueKey index) => this._longValues.ContainsKey((int)index);

    public bool Exists(StringValueKey index) => this._stringValues.ContainsKey((int)index);

    public bool Values(BoolValueKey index) => this.Values(index, false);

    public double Values(DoubleValueKey index) => this.Values(index, 0.0);

    public int Values(LongValueKey index) => this.Values(index, 0);

    public int Values(IntValueKey index) => this.Values(index, 0);

    public string Values(StringValueKey index) => this.Values(index, null);

    public bool Values(BoolValueKey index, bool defaultValue)
    {
        if (this._boolValues.TryGetValue((int)index, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public double Values(DoubleValueKey index, double defaultValue)
    {
        if (this._doubleValues.TryGetValue((int)index, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public int Values(LongValueKey index, int defaultValue)
    {
        if (this._longValues.TryGetValue((int)index, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public int Values(IntValueKey index, int defaultValue)
    {
        if (this._longValues.TryGetValue((int)index, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public string Values(StringValueKey index, string defaultValue)
    {
        if (this._stringValues.TryGetValue((int)index, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public CoordsObject Coordinates() => this._coordinates;

    public Vector3Object Offset() => this._offset;

    public Vector4Object Orientation() => this._orientation;

    public Vector3Object RawCoordinates() => this._rawCoordinates;

    public ReadOnlyCollection<int> GetActiveSpells() => this._activeSpells;

    public ReadOnlyCollection<int> GetSpells() => this._spells;

    public bool TryGetValue(BoolValueKey index, out bool value) => this._boolValues.TryGetValue((int)index, out value);

    public bool TryGetValue(DoubleValueKey index, out double value) => this._doubleValues.TryGetValue((int)index, out value);

    public bool TryGetValue(LongValueKey index, out int value) => this._longValues.TryGetValue((int)index, out value);

    public bool TryGetValue(IntValueKey index, out int value) => this._longValues.TryGetValue((int)index, out value);

    public bool TryGetValue(StringValueKey index, out string value) => this._stringValues.TryGetValue((int)index, out value);
}
