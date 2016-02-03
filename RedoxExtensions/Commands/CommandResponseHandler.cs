using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Actions;

namespace RedoxExtensions.Commands
{
    /// <summary>
    /// A static class to deal with replying to a command
    /// </summary>
    internal static class CommandResponseHandler
    {
        internal static void TellSource(ISourceInformation command, string message, params object[] args)
        {
            TellSource(command, string.Format(message, args));
        }

        internal static void TellSource(ISourceInformation command, string message)
        {
            if (command.Channel == CommandChannel.Fellowship)
            {
                TellActions.TellNativeFellow(message);
                return;
            }

            if (command.Channel == CommandChannel.VirindiFellowship)
            {
                TellActions.TellVirindiFellow((string)command.ChannelTag, message);
                return;
            }

            if (command.Channel == CommandChannel.AreaChat)
            {
                // Respond directly to the player even those it was area chat
                TellActions.TellPlayer(command.SourceCharacter, message);
                return;
            }

            if (command.Channel == CommandChannel.Tell)
            {
                TellActions.TellPlayer(command.SourceCharacter, message);
                return;
            }

            if(command.Channel == CommandChannel.DirectEntry)
            {
                REPlugin.Instance.Chat.WriteLine(message);
            }

            throw new NotImplementedException(string.Format("Support for channel {0} has not been implemented", command.Channel));
        }
    }
}
