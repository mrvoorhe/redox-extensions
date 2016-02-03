using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Data.Events
{
    public class ObjectIdEventArgs : EventArgs
    {
        public ObjectIdEventArgs(int objectId)
        {
            this.ObjectId = objectId;
        }

        public int ObjectId { get; private set; }
    }
}
