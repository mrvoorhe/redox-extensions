using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RedoxExtensions.Core.Utilities;
using RedoxLib.Location;

namespace RedoxExtensions.PhatACInterop
{
    public static class PhatACActions
    {
        public static void TeleTo(UserFacingLocation location)
        {
            TeleTo(location.NorthSouth.ToString(), location.EastWest.ToString());
        }

        public static void TeleTo(string ns, string ew)
        {
            ACUtilities.ProcessNativeCommand($"/teleto {ns} {ew}");
        }

        public static void TeleTown(string name)
        {
            ACUtilities.ProcessNativeCommand($"/teletown {name}");
        }
    }
}
