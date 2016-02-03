using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Decal.Adapter.Wrappers;

namespace RedoxExtensions.Data
{
    public interface IWorldObject : IHaveCoordsObject
    {
        #region Standard Decal Members

        int Id { get; }
        string Name { get; }
        ObjectClass ObjectClass { get; }

        int ActiveSpellCount { get; }
        int SpellCount { get; }

        int Behavior { get; }
        int Category { get; }
        int Container { get; }
        int GameDataFlags1 { get; }
        int Icon { get; }
        int LastIdTime { get; }
        int PhysicsDataFlags { get; }
        int Type { get; }

        bool HasIdData { get; }

        ReadOnlyCollection<int> BoolKeys { get; }
        ReadOnlyCollection<int> LongKeys { get; }
        ReadOnlyCollection<int> DoubleKeys { get; }
        ReadOnlyCollection<int> StringKeys { get; }

        int ActiveSpell(int index);
        int Spell(int index);

        bool Exists(BoolValueKey index);
        bool Exists(DoubleValueKey index);
        bool Exists(LongValueKey index);
        bool Exists(StringValueKey index);

        bool Values(BoolValueKey index);
        double Values(DoubleValueKey index);
        int Values(LongValueKey index);
        string Values(StringValueKey index);

        bool Values(BoolValueKey index, bool defaultValue);
        double Values(DoubleValueKey index, double defaultValue);
        int Values(LongValueKey index, int defaultValue);
        string Values(StringValueKey index, string defaultValue);

        CoordsObject Coordinates();
        Vector3Object Offset();
        Vector4Object Orientation();
        Vector3Object RawCoordinates();

        #endregion

        #region My Additional Members

        ReadOnlyCollection<int> GetActiveSpells();
        ReadOnlyCollection<int> GetSpells();

        bool TryGetValue(BoolValueKey index, out bool value);
        bool TryGetValue(DoubleValueKey index, out double value);
        bool TryGetValue(LongValueKey index, out int value);
        bool TryGetValue(StringValueKey index, out string value);

        #endregion
    }
}
