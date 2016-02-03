using System;
using System.Collections.Generic;
using System.Text;

using uTank2;
using RedoxExtensions.VirindiInterop.Events;

namespace RedoxExtensions.VirindiInterop
{
    public interface IVTEventsProxy
    {
        event PluginCore.EmptyDelegate ProfileChanged;
        event PluginCore.NavRouteChangedDelegate NavRouteChanged;

        event PluginCore.EmptyDelegate MacroStateChanged;

        event EventHandler<SpellCastAttemptingEventArgs> SpellCastAttempting;
        event EventHandler<SpellCastCompleteEventArgs> SpellCastComplete;
    }
}
