using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxLib
{
    public static class PluginProvider
    {
        private static IDecalPluginProvider _instance;

        public static IDecalPluginProvider Instance
        {
            get
            {
                return _instance;
            }
        }

        public static void Set(IDecalPluginProvider instance)
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("The previous instance was not cleared properly");
            }

            _instance = instance;
        }

        public static void Clear()
        {
            _instance = null;
        }
    }
}
