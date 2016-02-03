using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;

using Mag.Shared;

using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.Data.Events
{
    public class ParsedChatTextInterceptEventArgs : EventArgs
    {
        public ParsedChatTextInterceptEventArgs(string rawText, int target, int color, string source, Util.ChatChannels channel, ChatMessageType messageType)
        {
            this.RawText = rawText;
            this.Target = target;
            this.Color = color;
            this.Source = source;
            this.Channel = channel;
            this.MessageType = messageType;
        }

        public ParsedChatTextInterceptEventArgs(ChatTextInterceptEventArgs eventArgs, string source, Util.ChatChannels channel, ChatMessageType messageType)
            : this(eventArgs.Text, eventArgs.Target, eventArgs.Color, source, channel, messageType)
        {
        }

        public static bool TryCreateFrom(ChatTextInterceptEventArgs eventArgs, out ParsedChatTextInterceptEventArgs parsedEventArgs)
        {
            return TryCreateFrom(eventArgs, Util.ChatFlags.All, out parsedEventArgs);
        }

        public static bool TryCreateFrom(ChatTextInterceptEventArgs eventArgs, Util.ChatFlags chatTypeFilter, out ParsedChatTextInterceptEventArgs parsedEventArgs)
        {
            if (Util.IsChat(eventArgs.Text, chatTypeFilter))
            {
                Util.ChatChannels channel = ChatParsingUtilities.GetChatChannel(eventArgs.Text);
                ChatMessageType messageType = ChatParsingUtilities.GetChatMessageType(eventArgs.Text);
                string source = ChatParsingUtilities.GetSourceOfChat(eventArgs.Text);

                parsedEventArgs = new ParsedChatTextInterceptEventArgs(eventArgs, source, channel, messageType);
                return true;
            }

            parsedEventArgs = null;
            return false;
        }

        public bool Eat { get; set; }

        public Util.ChatChannels Channel { get; private set; }
        public ChatMessageType MessageType { get; private set; }
        public int Target { get; private set; }
        public int Color { get; private set; }
        public string Source { get; private set; }
        public string RawText { get; private set; }
    }
}
