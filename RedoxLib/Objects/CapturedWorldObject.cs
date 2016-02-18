using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

namespace RedoxLib.Objects
{
    /// <summary>
    /// A copied WorldObject that is safe to use from any thread
    /// </summary>
    public class CapturedWorldObject : IWorldObject
    {
        public CoordsObject Coords { get; }
    }
}
