using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Decal.Adapter.Wrappers;

namespace RedoxExtensions.Waiters
{
    public class PortalSpaceWaiter : IWaiter
    {
        private readonly AutoResetEvent _event = new AutoResetEvent(false);
        private readonly PortalEventType _expectedPortalEventType;

        public PortalSpaceWaiter(PortalEventType expectedPortalEventType)
        {
            this._expectedPortalEventType = expectedPortalEventType;
            REPlugin.Instance.CharacterFilter.ChangePortalMode += this.CharacterFilter_ChangePortalMode;
        }

        public static IWaiter Begin(PortalEventType expecPortalEventType)
        {
            return new PortalSpaceWaiter(expecPortalEventType);
        }

        public void Wait()
        {
            this._event.WaitOne();
        }

        public bool Wait(int millisecondsTimeout)
        {
            return this._event.WaitOne(millisecondsTimeout);
        }

        public void Dispose()
        {
            REPlugin.Instance.CharacterFilter.ChangePortalMode -= this.CharacterFilter_ChangePortalMode;
        }

        private void CharacterFilter_ChangePortalMode(object sender, Decal.Adapter.Wrappers.ChangePortalModeEventArgs e)
        {
            if (e.Type == this._expectedPortalEventType)
            {
                this._event.Set();
            }
        }
    }
}
