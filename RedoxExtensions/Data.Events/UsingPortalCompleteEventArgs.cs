using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data.Events
{
    public class UsingPortalCompleteEventArgs : EventArgs
    {
        public UsingPortalCompleteEventArgs(bool successful)
        {
            this.Successful = successful;
        }

        public bool Successful { get; private set; }
    }
}
