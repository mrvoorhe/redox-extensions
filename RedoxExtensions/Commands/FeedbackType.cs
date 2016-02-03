using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Commands
{
    public enum FeedbackType
    {
        Undefined,

        /// <summary>
        /// The command was ignored for one valid reason or another.
        /// </summary>
        Ignored,

        Failed,

        Successful
    }
}
