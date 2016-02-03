using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Commands
{
    public enum TargetType
    {
        NotDefined,
        SelfOnly,
        All,
        SlavesOnly,
        MasterOnly,
        ExplicitByName,
        ExplicitByShutcutAccountId,
        ExplicitByObjectId
    }
}
