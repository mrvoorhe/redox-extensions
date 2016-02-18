using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;
using Newtonsoft.Json;
using RedoxLib.GameValues;

namespace RedoxLib.Objects
{
    /// <summary>
    /// An implementation used for serializing/deserializing
    /// </summary>
    internal class SerializedWorldObject : IWorldObjectIdentified
    {
        public Dictionary<int, bool> BoolValues = new Dictionary<int, bool>();
        public Dictionary<int, double> DoubleValues = new Dictionary<int, double>();
        public Dictionary<int, int> LongValues = new Dictionary<int, int>();
        public Dictionary<int, string> StringValues = new Dictionary<int, string>();

        public int Id { get; set; }
        public string Name { get; set; }
        public CoordsObject Coords { get; set; }
        public ObjectClass ObjectClass { get; set; }
        public int ActiveSpellCount { get; set; }
        public int SpellCount { get; set; }
        public int Behavior { get; set; }
        public int Category { get; set; }
        public int Container { get; set; }
        public int GameDataFlags1 { get; set; }
        public int Icon { get; set; }
        public int LastIdTime { get; set; }
        public int PhysicsDataFlags { get; set; }
        public int Type { get; set; }
        public bool HasIdData { get; }

        [JsonIgnore]
        public ReadOnlyCollection<int> BoolKeys
        {
            get
            {
                return this.BoolValues.Select(p => p.Key).ToList().AsReadOnly();
            }
        }

        [JsonIgnore]
        public ReadOnlyCollection<int> LongKeys
        {
            get
            {
                return this.LongValues.Select(p => p.Key).ToList().AsReadOnly();
            }
        }

        [JsonIgnore]
        public ReadOnlyCollection<int> DoubleKeys
        {
            get
            {
                return this.DoubleValues.Select(p => p.Key).ToList().AsReadOnly();
            }
        }

        [JsonIgnore]
        public ReadOnlyCollection<int> StringKeys
        {
            get
            {
                return this.StringValues.Select(p => p.Key).ToList().AsReadOnly();
            }
        }

        public int ActiveSpell(int index)
        {
            throw new NotImplementedException();
        }

        public int Spell(int index)
        {
            throw new NotImplementedException();
        }

        public bool Exists(BoolValueKey index)
        {
            throw new NotImplementedException();
        }

        public bool Exists(DoubleValueKey index)
        {
            throw new NotImplementedException();
        }

        public bool Exists(LongValueKey index)
        {
            throw new NotImplementedException();
        }

        public bool Exists(StringValueKey index)
        {
            throw new NotImplementedException();
        }

        public bool Values(BoolValueKey index)
        {
            throw new NotImplementedException();
        }

        public double Values(DoubleValueKey index)
        {
            throw new NotImplementedException();
        }

        public int Values(LongValueKey index)
        {
            throw new NotImplementedException();
        }

        public string Values(StringValueKey index)
        {
            throw new NotImplementedException();
        }

        public bool Values(BoolValueKey index, bool defaultValue)
        {
            throw new NotImplementedException();
        }

        public double Values(DoubleValueKey index, double defaultValue)
        {
            throw new NotImplementedException();
        }

        public int Values(LongValueKey index, int defaultValue)
        {
            throw new NotImplementedException();
        }

        public string Values(StringValueKey index, string defaultValue)
        {
            throw new NotImplementedException();
        }

        public CoordsObject Coordinates()
        {
            throw new NotImplementedException();
        }

        public Vector3Object Offset()
        {
            throw new NotImplementedException();
        }

        public Vector4Object Orientation()
        {
            throw new NotImplementedException();
        }

        public Vector3Object RawCoordinates()
        {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<int> GetActiveSpells()
        {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<int> GetSpells()
        {
            throw new NotImplementedException();
        }

        public bool Exists(IntValueKey index)
        {
            throw new NotImplementedException();
        }

        public int Values(IntValueKey index)
        {
            throw new NotImplementedException();
        }

        public int Values(IntValueKey index, int defaultValue)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(BoolValueKey index, out bool value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(DoubleValueKey index, out double value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(LongValueKey index, out int value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(IntValueKey index, out int value)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(StringValueKey index, out string value)
        {
            throw new NotImplementedException();
        }
    }
}
