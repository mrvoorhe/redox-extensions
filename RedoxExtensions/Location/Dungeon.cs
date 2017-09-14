using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Location
{
    public struct Dungeon
    {
        public readonly string KeyName;

        public readonly string PrettyName;

        public readonly UserFacingLocation[] Locations;

        public readonly FullLocation[] DropLocations;

        public Dungeon(string keyName, string prettyName, UserFacingLocation[] locations, FullLocation[] dropLocations)
        {
            KeyName = keyName;
            PrettyName = prettyName;
            Locations = locations;
            DropLocations = dropLocations;
        }

        public static bool TryParse(string value, out Dungeon dungeon)
        {
            dungeon = default(Dungeon);
            if (string.IsNullOrEmpty(value))
                return false;

            return REPlugin.Instance.LocationDatabase.Dungeons.TryGetValue(value.Trim().ToLower(), out dungeon);
        }
    }
}
