using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Location
{
    public struct Dungeon
    {
        public readonly string Name;

        public readonly UserFacingLocation Location;

        public readonly FullLocation DropLocation;

        public static bool TryParse(string value, out Dungeon dungeon)
        {
            dungeon = default(Dungeon);
            if (string.IsNullOrEmpty(value))
                return false;

            return DungeonDatabase.TryFind(value, out dungeon);
        }
    }
}
