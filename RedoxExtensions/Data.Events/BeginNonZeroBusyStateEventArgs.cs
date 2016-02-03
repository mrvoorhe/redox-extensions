using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data.Events
{
    public class BeginNonZeroBusyStateEventArgs : EventArgs
    {
        public BeginNonZeroBusyStateEventArgs(int busyState, int busyStateId)
        {
            this.BusyState = busyState;
            this.BusyStateId = busyStateId;
        }

        public int BusyState { get; private set; }
        public int BusyStateId { get; private set; }
    }
}
