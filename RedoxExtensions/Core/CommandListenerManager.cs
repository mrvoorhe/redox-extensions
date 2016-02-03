using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Commands;

namespace RedoxExtensions.Core
{
    public class CommandListenerManager : IDisposable
    {
        private Commands.CommandListener _commandListener;

        public CommandListenerManager(IDecalEventsProxy decalEventsProxy)
        {
            this._commandListener = new CommandListener(decalEventsProxy);
        }

        public void Dispose()
        {
            this._commandListener.Dispose();
        }
    }
}
