using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Listeners.Monitors
{
    public class WorldMonitor : IDisposable
    {
        public WorldMonitor()
        {
            REPlugin.Instance.Events.Decal.CreateObject += Decal_CreateObject;
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.Decal.CreateObject -= Decal_CreateObject;
        }

        void Decal_CreateObject(object sender, Decal.Adapter.Wrappers.CreateObjectEventArgs e)
        {
            // TODO : Check if stipend guy, if so, register a watcher to track it's location
        }
    }
}
