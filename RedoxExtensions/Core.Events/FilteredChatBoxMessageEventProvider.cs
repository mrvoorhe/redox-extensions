using System;
using System.Collections.Generic;
using System.Text;

using Mag.Shared;

using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Core.Events
{
    public class FilteredChatBoxMessageEventProvider : IFilteredChatBoxMessageEventProvider, IDisposable
    {
        private readonly IDecalEventsProxy _decalEventsProxy;
        private readonly Util.ChatFlags _chatFilter;
        private readonly Util.ChatChannels _channelFilter;

        public FilteredChatBoxMessageEventProvider(IDecalEventsProxy decalEventsProxy, Util.ChatFlags chatFilter, Util.ChatChannels channelFilter)
        {
            this._decalEventsProxy = decalEventsProxy;
            this._chatFilter = chatFilter;
            this._channelFilter = channelFilter;
            this._decalEventsProxy.ChatBoxMessage += this.DecalEventsProxy_ChatBoxMessage;
        }

        public event EventHandler<Decal.Adapter.ChatTextInterceptEventArgs> FilteredChatBoxMessage;

        public event EventHandler<ParsedChatTextInterceptEventArgs> FilteredAndParsedChatBoxMessage;

        public void Dispose()
        {
            this._decalEventsProxy.ChatBoxMessage -= this.DecalEventsProxy_ChatBoxMessage;
        }

        private void DecalEventsProxy_ChatBoxMessage(object sender, Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            if (ChatParsingUtilities.IsChat(e.Text, this._chatFilter) && ChatParsingUtilities.IsChannel(e.Text, this._channelFilter))
            {
                if (this.FilteredChatBoxMessage != null)
                {
                    this.FilteredChatBoxMessage(sender, e);
                }

                // Only try to parse if someone is listening
                if (this.FilteredAndParsedChatBoxMessage != null)
                {
                    ParsedChatTextInterceptEventArgs parsedEventArgs = null;
                    if (ParsedChatTextInterceptEventArgs.TryCreateFrom(e, out parsedEventArgs))
                    {
                        this.FilteredAndParsedChatBoxMessage(sender, parsedEventArgs);

                        e.Eat = parsedEventArgs.Eat;
                    }
                }
            }
        }
    }
}
