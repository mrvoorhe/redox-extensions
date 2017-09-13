using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

namespace RedoxExtensions.Location
{
    /// <summary>
    /// Coords in the format you seen in the radar
    /// </summary>
    public class UserFacingLocation
    {
        public readonly UserFacingCoord NorthSouth;
        public readonly UserFacingCoord EastWest;

        public UserFacingLocation(UserFacingCoord northSouth, UserFacingCoord eastWest)
        {
            if (northSouth.Direction == Direction.E || northSouth.Direction == Direction.W)
                throw new ArgumentException(nameof(northSouth.Direction));

            if (eastWest.Direction == Direction.N || eastWest.Direction == Direction.S)
                throw new ArgumentException(nameof(eastWest.Direction));

            
            NorthSouth = northSouth;
            EastWest = eastWest;
        }

        public CoordsObject ToCoords()
        {
            // TODO by Mike : I think one direction is positive and the other negative.  Need to look up
            // so I can implement the conversion
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{NorthSouth}, {EastWest}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = (UserFacingLocation)obj;
            return NorthSouth == other.NorthSouth && EastWest == other.EastWest;
        }

        public static bool operator ==(UserFacingLocation a, UserFacingLocation b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(UserFacingLocation a, UserFacingLocation b)
        {
            return !(a == b);
        }

        public static bool TryParse(string value, out UserFacingLocation location)
        {
            location = default(UserFacingLocation);

            if (string.IsNullOrEmpty(value))
                return false;

            var split = value.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 2)
                return false;

            // Case when only a comma is used.  Ex: 1.4n,5.4e
            if (split.Length == 1)
                split = value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

            UserFacingCoord ns;
            if (!UserFacingCoord.TryParse(split[0], out ns))
                return false;

            UserFacingCoord ew;
            if (!UserFacingCoord.TryParse(split[1], out ew))
                return false;

            location = new UserFacingLocation(ns, ew);
            return true;
        }
    }
}
