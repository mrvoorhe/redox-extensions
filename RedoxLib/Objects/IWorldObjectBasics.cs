using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxLib.Objects
{
    /// <summary>
    /// A barebones reference to an object in the world.
    /// 
    /// Useful for code that should only care about the id of an object and maybe a few other things
    /// </summary>
    public interface IWorldObjectBasics
    {
        int Id { get; }

        string Name { get; }
    }
}
