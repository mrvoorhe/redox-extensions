using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

namespace RedoxLib.Objects
{
    public static class WorldObjectExtensionMethods
    {
        #region WorldObject Conversions

        public static IWorldObject Capture(this WorldObject wo)
        {
            if (wo == null)
            {
                return null;
            }

            return new CapturedWorldObject(wo);
        }

        public static IWorldObject Capture(this IWorldObject wo, IDecalPluginProvider provider)
        {
            if (wo == null)
            {
                return null;
            }

            if (wo is CapturedWorldObject asCaptured)
                return asCaptured;

            return wo.Id.ToWorldObject(provider).Capture();
        }

        public static IWorldObject Wrap(this WorldObject wo) => new WrappedWorldObject(wo);

        public static WorldObject ToWorldObject(this int objectId, IDecalPluginProvider provider) => provider.CoreManager.WorldFilter[objectId];

        #endregion
    }
}
