using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter;

namespace RedoxExtensions.Wrapper
{
    public class PluginBaseEventsHelper : IPluginBaseEvents
    {
        public event EventHandler<Decal.Adapter.NetworkMessageEventArgs> ClientDispatch;

        public event EventHandler<Decal.Adapter.NetworkMessageEventArgs> ServerDispatch;

        public void FireClientDispatch(object sender, NetworkMessageEventArgs eventArgs)
        {
            if (this.ClientDispatch != null)
            {
                this.ClientDispatch(sender, eventArgs);
            }
        }

        public void FireServerDispatch(object sender, NetworkMessageEventArgs eventArgs)
        {
            if (this.ServerDispatch != null)
            {
                this.ServerDispatch(sender, eventArgs);
            }
        }
    }
}
