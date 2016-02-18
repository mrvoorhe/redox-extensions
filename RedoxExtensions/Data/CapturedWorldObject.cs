using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using RedoxLib.GameValues;
using RedoxLib.General;

namespace RedoxExtensions.Data
{
    /// <summary>
    /// Captures a snapshot of a world object.  This is useful if you want to use the data on a background thread,
    /// or if you need to retain information about the object beyond the point that it is still in your control/view.
    /// </summary>
    public class CapturedWorldObject : IWorldObject
    {
        private readonly Dictionary<int, bool> _boolValues = new Dictionary<int, bool>();
        private readonly Dictionary<int, double> _doubleValues = new Dictionary<int, double>();
        private readonly Dictionary<int, int> _longValues = new Dictionary<int, int>();
        private readonly Dictionary<int, string> _stringValues = new Dictionary<int, string>();
        private readonly ReadOnlyCollection<int> _activeSpells;
        private readonly ReadOnlyCollection<int> _spells;

        private readonly CoordsObject _coordinates;
        private readonly Vector4Object _orientation;
        private readonly Vector3Object _offset;
        private readonly Vector3Object _rawCoordinates;

        public CapturedWorldObject(WorldObject wo)
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

            this._activeSpells = ListOperations.Capture(tmpIntList).AsReadOnly();

            tmpIntList.Clear();
            for (int i = 0; i < wo.SpellCount; i++)
            {
                tmpIntList.Add(wo.Spell(i));
            }

            this._spells = ListOperations.Capture(tmpIntList).AsReadOnly();

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

        public int ActiveSpellCount
        {
            get
            {
                return this._activeSpells.Count;
            }
        }

        public int SpellCount
        {
            get
            {
                return this._spells.Count;
            }
        }

        public int Behavior { get; private set; }

        public int Category { get; private set; }

        public int Container { get; private set; }

        public int GameDataFlags1 { get; private set; }

        public int Icon { get; private set; }

        public int LastIdTime { get; private set; }

        public int PhysicsDataFlags { get; private set; }

        public int Type { get; private set; }

        public bool HasIdData { get; private set; }

        public ReadOnlyCollection<int> BoolKeys
        {
            get
            {
                return this._boolValues.Select(p => p.Key).ToList().AsReadOnly();
            }
        }

        public ReadOnlyCollection<int> LongKeys
        {
            get
            {
                return this._longValues.Select(p => p.Key).ToList().AsReadOnly();
            }
        }

        public ReadOnlyCollection<int> DoubleKeys
        {
            get
            {
                return this._doubleValues.Select(p => p.Key).ToList().AsReadOnly();
            }
        }

        public ReadOnlyCollection<int> StringKeys
        {
            get
            {
                return this._stringValues.Select(p => p.Key).ToList().AsReadOnly();
            }
        }

        public CoordsObject Coords
        {
            get
            {
                return this.Coordinates();
            }
        }

        public int ActiveSpell(int index)
        {
            return this._activeSpells[index];
        }

        public int Spell(int index)
        {
            return this._spells[index];
        }

        public bool Exists(BoolValueKey index)
        {
            return this._boolValues.ContainsKey((int)index);
        }

        public bool Exists(DoubleValueKey index)
        {
            return this._doubleValues.ContainsKey((int)index);
        }

        public bool Exists(LongValueKey index)
        {
            return this._longValues.ContainsKey((int)index);
        }

        public bool Exists(IntValueKey index)
        {
            return this._longValues.ContainsKey((int)index);
        }

        public bool Exists(StringValueKey index)
        {
            return this._stringValues.ContainsKey((int)index);
        }

        public bool Values(BoolValueKey index)
        {
            return this.Values(index, false);
        }

        public double Values(DoubleValueKey index)
        {
            return this.Values(index, 0.0);
        }

        public int Values(LongValueKey index)
        {
            return this.Values(index, 0);
        }

        public int Values(IntValueKey index)
        {
            return this.Values(index, 0);
        }

        public string Values(StringValueKey index)
        {
            return this.Values(index, null);
        }

        public bool Values(BoolValueKey index, bool defaultValue)
        {
            bool value;
            if (this._boolValues.TryGetValue((int)index, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public double Values(DoubleValueKey index, double defaultValue)
        {
            double value;
            if (this._doubleValues.TryGetValue((int)index, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public int Values(LongValueKey index, int defaultValue)
        {
            int value;
            if (this._longValues.TryGetValue((int)index, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public int Values(IntValueKey index, int defaultValue)
        {
            int value;
            if (this._longValues.TryGetValue((int)index, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public string Values(StringValueKey index, string defaultValue)
        {
            string value;
            if (this._stringValues.TryGetValue((int)index, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public CoordsObject Coordinates()
        {
            return this._coordinates;
        }

        public Vector3Object Offset()
        {
            return this._offset;
        }

        public Vector4Object Orientation()
        {
            return this._orientation;
        }

        public Vector3Object RawCoordinates()
        {
            return this._rawCoordinates;
        }

        public ReadOnlyCollection<int> GetActiveSpells()
        {
            return this._activeSpells;
        }

        public ReadOnlyCollection<int> GetSpells()
        {
            return this._spells;
        }

        public bool TryGetValue(BoolValueKey index, out bool value)
        {
            return this._boolValues.TryGetValue((int)index, out value);
        }

        public bool TryGetValue(DoubleValueKey index, out double value)
        {
            return this._doubleValues.TryGetValue((int)index, out value);
        }

        public bool TryGetValue(LongValueKey index, out int value)
        {
            return this._longValues.TryGetValue((int)index, out value);
        }

        public bool TryGetValue(IntValueKey index, out int value)
        {
            return this._longValues.TryGetValue((int)index, out value);
        }

        public bool TryGetValue(StringValueKey index, out string value)
        {
            return this._stringValues.TryGetValue((int)index, out value);
        }
    }
}
