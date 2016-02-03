using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.VirindiInterop;
using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Core
{
    public class EventsManager : IDisposable
    {
        private DecalEventsProxy _decalEventsProxy;
        private VTEventProxy _vtEventsProxy;
        private REEvents _rtEvents;

        public EventsManager()
        {
            this._decalEventsProxy = new DecalEventsProxy();
            //this._vtEventsProxy = new VTEventProxy(EnableDebugEventLogger);
            this._rtEvents = new REEvents(this._decalEventsProxy);
        }

        internal IDecalEventsProxy Decal
        {
            get
            {
                return this._decalEventsProxy;
            }
        }

        internal IVTEventsProxy VT
        {
            get
            {
                return this._vtEventsProxy;
            }
        }

        internal IREEvents RE
        {
            get
            {
                return this._rtEvents;
            }
        }

        public void Dispose()
        {
            if(this._rtEvents != null)
            {
                this._rtEvents.Dispose();
            }

            if (this._vtEventsProxy != null)
            {
                this._vtEventsProxy.Dispose();
            }

            if (this._decalEventsProxy != null)
            {
                this._decalEventsProxy.Dispose();
            }
        }
    }
}
