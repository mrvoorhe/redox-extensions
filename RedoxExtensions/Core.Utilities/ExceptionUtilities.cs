using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Actions;
using RedoxExtensions.Commands;
using RedoxExtensions.Data;
using System.Reflection;
using System.Diagnostics;

namespace RedoxExtensions.Core.Utilities
{
    public static class ExceptionUtilities
    {
        public static void HandleDisplayToUserException(DisplayToUserException e)
        {
            // Always show in local window
            REPlugin.Instance.WriteToChat(e.Message, 5, ChatMessageWindow.Main);

            // If the message happened due to a request made by someone else, we should tell them also
            // so that they know something went wrong
            if(e.Requestor != null && !e.Requestor.FromSelf)
            {
                e.Requestor.GiveFeedback(FeedbackType.Failed, e.Message);
            }
        }

        public static void HandleUnexpectedException(Exception e)
        {
            REPlugin.Instance.Error.LogError(e);
        }

        
    }
}
