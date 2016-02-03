using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Core;

namespace RedoxExtensions.Commands
{
    internal class CommandListener : IDisposable
    {
        private readonly IDecalEventsProxy _decalEventsProxy;

        internal CommandListener(IDecalEventsProxy decalEventsProxy)
        {
            this._decalEventsProxy = decalEventsProxy;
            this._decalEventsProxy.CommandLineText += _decalEventsProxy_CommandLineText;
            this._decalEventsProxy.ChatBoxMessage += _decalEventsProxy_ChatBoxMessage;
        }

        public void Dispose()
        {
            this._decalEventsProxy.CommandLineText -= _decalEventsProxy_CommandLineText;
            this._decalEventsProxy.ChatBoxMessage -= _decalEventsProxy_ChatBoxMessage;
        }

        void _decalEventsProxy_ChatBoxMessage(object sender, Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            ICommand command;
            if (CommandHelpers.TryParse(e, out command))
            {
                switch (command.CommandType)
                {
                    case CommandType.Foreign:
                        // Never eat ...Then I can't see the /f messages i'm causing...while others still can
                        Handlers.ForeignHandler.HandleCommand(command);
                        break;
                }
            }
        }

        void _decalEventsProxy_CommandLineText(object sender, Decal.Adapter.ChatParserInterceptEventArgs e)
        {
            ICommand command;
            if (CommandHelpers.TryParse(e, out command))
            {
                switch (command.CommandType)
                {
                    case CommandType.RedoxExtension:
                        e.Eat = Handlers.RedoxExtensionsHandler.HandleCommand(command);
                        break;
                    case CommandType.RedoxFellow:
                        e.Eat = Handlers.RedoxFellowHandler.HandleCommand(command);
                        break;
                }
            }
        }
    }
}
