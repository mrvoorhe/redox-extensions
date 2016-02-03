using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data.Events
{
    public class UsingPortalEventArgs : EventArgs
    {
        public UsingPortalEventArgs(string portalName, int portalId)
        {
            this.PortalName = portalName;
            this.PortalId = portalId;
        }

        public string PortalName { get; private set; }
        public int PortalId { get; private set; }
    }
}
