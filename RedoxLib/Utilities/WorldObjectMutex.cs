using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using RedoxLib.Objects;

namespace RedoxLib.Utilities
{
    /// <summary>
    /// A mutex that synchronizes access to a world object.
    /// 
    /// Note: Currently this implementation uses a global mutex, so it will only provide protection
    /// for instances running on the same machine.
    /// </summary>
    public class WorldObjectMutex : IDisposable
    {
        public readonly IWorldObject WorldObject;
        private readonly Mutex _mutex;

        private WorldObjectMutex(IWorldObject wo, Mutex mutex)
        {
            _mutex = mutex;
            WorldObject = wo;
        }

        public static WorldObjectMutex Obtain(IWorldObject wo)
        {
            bool owned;
            var mutex = new Mutex(true, string.Format("Global\\re_{0}", wo.Id), out owned);

            if (!owned)
                mutex.WaitOne();

            return new WorldObjectMutex(wo, mutex);
        }

        public void Dispose()
        {
            _mutex.ReleaseMutex();
            _mutex.Close();
        }
    }
}
