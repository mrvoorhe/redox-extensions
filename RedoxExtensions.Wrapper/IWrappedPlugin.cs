using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Wrapper
{
    public interface IWrappedPlugin
    {
        void WrappedStartup(IRealPluginBase realPlugin, IPluginBaseEvents realPluginBaseEvents);

        void WrappedShutdown();

        void WrappedPluginInitComplete();
    }
}
