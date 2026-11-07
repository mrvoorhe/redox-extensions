using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Data;

public enum GiveItemOutcome
{
    Undefined,
    Successful,
    FailedUnknown,
    FailedCannotCarryAnymore,
    FailedBusy,

    /// <summary>
    /// The NPC examined the item with its refuse emote instead of accepting the give.  The emote may still
    /// have taken the item ("You hand over ...").
    /// </summary>
    Refused
}
