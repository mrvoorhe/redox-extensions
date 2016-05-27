using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

namespace RedoxLib.Objects.Extensions
{
    public static class CoordsObjectExtensions
    {
        public static CoordsObject Shift(this CoordsObject coords, double ns, double ew)
        {
           return new CoordsObject(coords.NorthSouth + ns, coords.EastWest + ew);
        }

        public static bool EqualsWithinMarginOfError(this CoordsObject coords, CoordsObject other)
        {
            if (!HasMinimalDifference(coords.NorthSouth, other.NorthSouth))
                return false;

            return HasMinimalDifference(coords.EastWest, other.EastWest);
        }

        private static bool HasMinimalDifference(double value1, double value2)
        {
            var v1Rounded = Math.Round(value1, 3);
            var v2Rounded = Math.Round(value2, 3);
            return v1Rounded == v2Rounded;
        }
    }
}
