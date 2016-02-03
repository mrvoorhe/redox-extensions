using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Commands
{
    /// <summary>
    /// Contains the information about targetting of a command
    /// </summary>
    public interface ITargettingData
    {
        CommandType CommandType { get; }
        TargetType CommandTargetType { get; }
        bool UsesExplicitTargettingTag { get; }

        object ExplicitTargettingTag { get; }
    }
}
