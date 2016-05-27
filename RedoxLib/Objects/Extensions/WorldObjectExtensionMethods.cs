using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

namespace RedoxLib.Objects.Extensions
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

            var asCaptured = wo as CapturedWorldObject;
            if (asCaptured != null)
                return asCaptured;

            return wo.Id.ToWorldObject(provider).Capture();
        }

        public static IWorldObject Wrap(this WorldObject wo)
        {
            return new WrappedWorldObject(wo);
        }

        public static WorldObject ToWorldObject(this int objectId, IDecalPluginProvider provider)
        {
            return provider.CoreManager.WorldFilter[objectId];
        }

        #endregion
    }
}
