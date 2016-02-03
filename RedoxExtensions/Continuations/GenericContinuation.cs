using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Continuations
{

    /// <summary>
    /// TODO : Can idea for a generic continuation so that I don't have to implement a new type for each
    // event
    /// </summary>
    /// <typeparam name="TEventArgs"></typeparam>
    public class GenericContinuation<TEventArgs> where TEventArgs : EventArgs
    {
        // TODO : How to pass in the event to subcribe to generically?
        // Would it be the method Endeavor has to use to subscribe to events since F#
        // doesn't have the syntax sugar of +=?
    }
}
