using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.VirindiInterop
{
    static class VWTActions
    {
        public static void Apply(string name)
        {
            ACUtilities.ProcessArbitraryCommand($"/vwt apply {name}");
        }
    }
}
