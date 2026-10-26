using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;
using RedoxLib.GameValues;

namespace RedoxLib.Objects;

/// <summary>
/// Wraps a WorldObject instance.  These objects can only be used on the main thread
/// </summary>
internal class WrappedWorldObject : IWorldObject
{
    private readonly WorldObject _wo;

    internal WrappedWorldObject(WorldObject wo)
    {
        this._wo = wo;
    }

    public int Id => _wo.Id;

    public string Name => _wo.Name;

    public ObjectClass ObjectClass => _wo.ObjectClass;

    public int ActiveSpellCount => _wo.ActiveSpellCount;

    public int SpellCount => _wo.SpellCount;

    public int Behavior => _wo.Behavior;

    public int Category => _wo.Category;

    public int Container => _wo.Container;

    public int GameDataFlags1 => _wo.GameDataFlags1;

    public int Icon => _wo.Icon;

    public int LastIdTime => _wo.LastIdTime;

    public int PhysicsDataFlags => _wo.PhysicsDataFlags;

    public int Type => _wo.Type;

    public bool HasIdData => _wo.HasIdData;

    public System.Collections.ObjectModel.ReadOnlyCollection<int> BoolKeys => _wo.BoolKeys.AsReadOnly();

    public System.Collections.ObjectModel.ReadOnlyCollection<int> LongKeys => _wo.LongKeys.AsReadOnly();

    public System.Collections.ObjectModel.ReadOnlyCollection<int> DoubleKeys => _wo.DoubleKeys.AsReadOnly();

    public System.Collections.ObjectModel.ReadOnlyCollection<int> StringKeys => _wo.StringKeys.AsReadOnly();
    public CoordsObject Coords => this.Coordinates();

    public int ActiveSpell(int index) => _wo.ActiveSpell(index);

    public int Spell(int index) => _wo.Spell(index);

    public bool Exists(BoolValueKey index) => _wo.Exists(index);

    public bool Exists(DoubleValueKey index) => _wo.Exists(index);

    public bool Exists(LongValueKey index) => _wo.Exists(index);

    public bool Exists(IntValueKey index) => Exists((LongValueKey)(int)index);

    public bool Exists(StringValueKey index) => _wo.Exists(index);

    public bool Values(BoolValueKey index) => _wo.Values(index);

    public double Values(DoubleValueKey index) => _wo.Values(index);

    public int Values(LongValueKey index) => _wo.Values(index);

    public int Values(IntValueKey index) => Values((LongValueKey)(int)index);

    public string Values(StringValueKey index) => _wo.Values(index);

    public bool Values(BoolValueKey index, bool defaultValue) => _wo.Values(index, defaultValue);

    public double Values(DoubleValueKey index, double defaultValue) => _wo.Values(index, defaultValue);

    public int Values(LongValueKey index, int defaultValue) => _wo.Values(index, defaultValue);

    public int Values(IntValueKey index, int defaultValue) => Values((LongValueKey)(int)index, defaultValue);

    public string Values(StringValueKey index, string defaultValue) => _wo.Values(index, defaultValue);

    public CoordsObject Coordinates() => _wo.Coordinates();

    public Vector3Object Offset() => _wo.Offset();

    public Vector4Object Orientation() => _wo.Orientation();

    public Vector3Object RawCoordinates() => _wo.RawCoordinates();

    public System.Collections.ObjectModel.ReadOnlyCollection<int> GetActiveSpells()
    {
        var tmp = new List<int>();
        for (int i = 0; i < _wo.ActiveSpellCount; i++)
        {
            tmp.Add(_wo.ActiveSpell(i));
        }

        return tmp.AsReadOnly();
    }

    public System.Collections.ObjectModel.ReadOnlyCollection<int> GetSpells()
    {
        var tmp = new List<int>();
        for (int i = 0; i < _wo.SpellCount; i++)
        {
            tmp.Add(_wo.Spell(i));
        }

        return tmp.AsReadOnly();
    }

    public bool TryGetValue(BoolValueKey index, out bool value)
    {
        if (_wo.Exists(index))
        {
            value = _wo.Values(index);
            return true;
        }

        value = false;
        return false;
    }

    public bool TryGetValue(DoubleValueKey index, out double value)
    {
        if (_wo.Exists(index))
        {
            value = _wo.Values(index);
            return true;
        }

        value = 0.0;
        return false;
    }

    public bool TryGetValue(LongValueKey index, out int value)
    {
        if (_wo.Exists(index))
        {
            value = _wo.Values(index);
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetValue(IntValueKey index, out int value) => TryGetValue((LongValueKey)(int)index, out value);

    public bool TryGetValue(StringValueKey index, out string value)
    {
        if (_wo.Exists(index))
        {
            value = _wo.Values(index);
            return true;
        }

        value = null;
        return false;
    }
}
