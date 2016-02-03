using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace RedoxLib
{
    public interface IDecalPluginProvider
    {
        CoreManager CoreManager { get; }

        PluginHost PluginHost { get; }

        CharacterFilter CharacterFilter { get; }

        WorldFilter WorldFilter { get; }

        HooksWrapper Actions { get; }
    }
}
