using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Commands
{
    /// <summary>
    /// Ideally this will be the bare bones data need to perform a command.  Some other code should have already
    /// done any filtering based on the target
    /// </summary>
    public interface IExecutionData
    {
        string Name { get; }
        string RawValue { get; }
        ReadOnlyCollection<string> Arguments { get; }
    }
}
