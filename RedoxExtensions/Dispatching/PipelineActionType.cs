using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Dispatching
{
    public enum PipelineActionType
    {
        /// <summary>
        /// These are normally basic commands that can be executed regardless of state
        /// 
        /// Examples:
        /// 
        /// /vt opt get Blah
        /// /f Hello
        /// </summary>
        NonFailable,

        /// <summary>
        /// These are the 90% case.  These are why the PipelineDispatcher is used in the first place.
        /// </summary>
        Normal
    }
}
