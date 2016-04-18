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
        private bool _owned;

        private WorldObjectMutex(IWorldObject wo, Mutex mutex, bool owned)
        {
            _mutex = mutex;
            WorldObject = wo;
            _owned = owned;
        }

        public static WorldObjectMutex Create(IWorldObject wo)
        {
            var mutex = new Mutex(false, string.Format("Global\\re_{0}", wo.Id));
            return new WorldObjectMutex(wo, mutex, false);
        }

        public static WorldObjectMutex Obtain(IWorldObject wo)
        {
            bool owned;
            var mutex = new Mutex(true, string.Format("Global\\re_{0}", wo.Id), out owned);

            if (!owned)
                mutex.WaitOne();

            return new WorldObjectMutex(wo, mutex, true);
        }

        public void Obtain()
        {
            if (_owned)
                return;

            _mutex.WaitOne();
            _owned = true;
        }

        public bool TryObtain(int millisecondsTimeout)
        {
            if (_owned)
                return true;

            _owned = _mutex.WaitOne(millisecondsTimeout);
            return _owned;
        }

        public void Dispose()
        {
            _mutex.ReleaseMutex();
            _mutex.Close();
        }
    }
}
