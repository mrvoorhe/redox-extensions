using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data.Events
{
    public class UsingObjectEventArgs : EventArgs
    {
        public UsingObjectEventArgs(string objectName, int objectId)
        {
            this.ObjectName = objectName;
            this.ObjectId = objectId;
        }

        public string ObjectName { get; private set; }
        public int ObjectId { get; private set; }
    }
}
