using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

namespace RedoxLib.Objects
{
    /// <summary>
    /// Wraps a WorldObject instance.  These objects can only be used on the main thread
    /// </summary>
    public class WrappedWorldObject : IWorldObject
    {
        public CoordsObject Coords { get; }
    }
}
