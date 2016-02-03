using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Diagnostics
{
    public interface ILoggableObject
    {
        DebugLevel MinimumRequiredDebugLevel { get; }

        string GetLoggableFormat();
    }
}
