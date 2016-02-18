using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

namespace RedoxLib.Objects
{
    public interface IHaveCoordsObject
    {
        CoordsObject Coords { get; }
    }
}
