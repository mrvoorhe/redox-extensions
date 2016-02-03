using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Wrapper
{
    public interface IRealPluginBase
    {
        CoreManager CoreManager { get; }
        PluginHost PluginHost { get; }
    }
}
