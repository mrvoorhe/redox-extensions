using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data.Events
{
    public class BeginGiveItemEventArgs : EventArgs
    {
        public BeginGiveItemEventArgs(int objectId)
        {
            this.ObjectId = objectId;
        }

        public int ObjectId { get; private set; }
    }
}
