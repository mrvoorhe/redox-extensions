using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Data
{
    public enum GiveItemOutcome
    {
        Undefined,
        Successful,
        FailedUnknown,
        FailedCannotCarryAnymore,
        FailedBusy
    }
}
