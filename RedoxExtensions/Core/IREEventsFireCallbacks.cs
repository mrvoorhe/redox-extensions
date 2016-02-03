using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Core
{
    /// <summary>
    /// Callbacks to let other classes fire events on IRTEvents
    /// </summary>
    public interface IREEventsFireCallbacks
    {
        void FireBeginBusy(object sender, BeginBusyEventArgs e);

        void FireBeginIdle(object sender, BeginIdleEventArgs e);
    }
}
