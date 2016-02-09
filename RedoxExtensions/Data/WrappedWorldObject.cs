using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;
using RedoxLib.GameValues;

namespace RedoxExtensions.Data
{
    public class WrappedWorldObject : IWorldObject
    {
        private readonly WorldObject _wo;

        public WrappedWorldObject(WorldObject wo)
        {
            this._wo = wo;
        }

        public int Id
        {
            get
            {
                return _wo.Id;
            }
        }

        public string Name
        {
            get
            {
                return _wo.Name;
            }
        }

        public ObjectClass ObjectClass
        {
            get
            {
                return _wo.ObjectClass;
            }
        }

        public int ActiveSpellCount
        {
            get
            {
                return _wo.ActiveSpellCount;
            }
        }

        public int SpellCount
        {
            get
            {
                return _wo.SpellCount;
            }
        }

        public int Behavior
        {
            get
            {
                return _wo.Behavior;
            }
        }

        public int Category
        {
            get
            {
                return _wo.Category;
            }
        }

        public int Container
        {
            get
            {
                return _wo.Container;
            }
        }

        public int GameDataFlags1
        {
            get
            {
                return _wo.GameDataFlags1;
            }
        }

        public int Icon
        {
            get
            {
                return _wo.Icon;
            }
        }

        public int LastIdTime
        {
            get
            {
                return _wo.LastIdTime;
            }
        }

        public int PhysicsDataFlags
        {
            get
            {
                return _wo.PhysicsDataFlags;
            }
        }

        public int Type
        {
            get
            {
                return _wo.Type;
            }
        }

        public bool HasIdData
        {
            get
            {
                return _wo.HasIdData;
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<int> BoolKeys
        {
            get
            {
                return _wo.BoolKeys.AsReadOnly();
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<int> LongKeys
        {
            get
            {
                return _wo.LongKeys.AsReadOnly();
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<int> DoubleKeys
        {
            get
            {
                return _wo.DoubleKeys.AsReadOnly();
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<int> StringKeys
        {
            get
            {
                return _wo.StringKeys.AsReadOnly();
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
            return _wo.ActiveSpell(index);
        }

        public int Spell(int index)
        {
            return _wo.Spell(index);
        }

        public bool Exists(BoolValueKey index)
        {
            return _wo.Exists(index);
        }

        public bool Exists(DoubleValueKey index)
        {
            return _wo.Exists(index);
        }

        public bool Exists(LongValueKey index)
        {
            return _wo.Exists(index);
        }

        public bool Exists(IntValueKey index)
        {
            return Exists((LongValueKey)(int)index);
        }

        public bool Exists(StringValueKey index)
        {
            return _wo.Exists(index);
        }

        public bool Values(BoolValueKey index)
        {
            return _wo.Values(index);
        }

        public double Values(DoubleValueKey index)
        {
            return _wo.Values(index);
        }

        public int Values(LongValueKey index)
        {
            return _wo.Values(index);
        }

        public int Values(IntValueKey index)
        {
            return Values((LongValueKey)(int)index);
        }

        public string Values(StringValueKey index)
        {
            return _wo.Values(index);
        }

        public bool Values(BoolValueKey index, bool defaultValue)
        {
            return _wo.Values(index, defaultValue);
        }

        public double Values(DoubleValueKey index, double defaultValue)
        {
            return _wo.Values(index, defaultValue);
        }

        public int Values(LongValueKey index, int defaultValue)
        {
            return _wo.Values(index, defaultValue);
        }

        public int Values(IntValueKey index, int defaultValue)
        {
            return Values((LongValueKey)(int)index, defaultValue);
        }

        public string Values(StringValueKey index, string defaultValue)
        {
            return _wo.Values(index, defaultValue);
        }

        public CoordsObject Coordinates()
        {
            return _wo.Coordinates();
        }

        public Vector3Object Offset()
        {
            return _wo.Offset();
        }

        public Vector4Object Orientation()
        {
            return _wo.Orientation();
        }

        public Vector3Object RawCoordinates()
        {
            return _wo.RawCoordinates();
        }

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

        public bool TryGetValue(IntValueKey index, out int value)
        {
            return TryGetValue((LongValueKey)(int)index, out value);
        }

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
}
