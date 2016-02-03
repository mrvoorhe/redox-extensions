using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Core
{
    /// <summary>
    /// A static class that provides information about which thread you are currently on
    /// </summary>
    public static class CurrentThreadContext
    {
        public static bool OnGameThread
        {
            get
            {
                // Note by Mike (3/5/2015) : The managed thread id for the game thread always seems to be 1.
                // so this might be good enough.  If this ends up not being enough, I'll have to do something similar
                // to the FW's CurrentContext.  Could Push & Pop from the IEventProxy classes.
                return System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
            }
        }
    }
}
