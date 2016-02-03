using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter;

namespace RedoxExtensions.Wrapper
{
    public interface IPluginBaseEvents
    {
        event EventHandler<NetworkMessageEventArgs> ClientDispatch;
        event EventHandler<NetworkMessageEventArgs> ServerDispatch;
        //event EventHandler GraphicsReset;
    }
}
