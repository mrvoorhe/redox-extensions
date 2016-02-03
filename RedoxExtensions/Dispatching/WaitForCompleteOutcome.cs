using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Dispatching
{
    public enum WaitForCompleteOutcome
    {
        Undefined,
        Success,
        Failed,
        TooBusy,
        Timeout
    }
}
