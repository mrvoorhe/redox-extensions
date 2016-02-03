using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;

using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Core.Events
{
    public interface IFilteredChatBoxMessageEventProvider
    {
        event EventHandler<ChatTextInterceptEventArgs> FilteredChatBoxMessage;

        event EventHandler<ParsedChatTextInterceptEventArgs> FilteredAndParsedChatBoxMessage;
    }
}
