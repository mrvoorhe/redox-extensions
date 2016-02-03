using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter;

using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.Core.Extensions
{
    public static class ChatParsingExtensions
    {

        public static bool IsYouGive(this ChatTextInterceptEventArgs eventArgs)
        {
            return ChatParsingUtilities.IsYouGive(eventArgs.Text);
        }

        public static bool IsCannotCarryAnymore(this ChatTextInterceptEventArgs eventArgs)
        {
            return ChatParsingUtilities.IsCannotCarryAnymore(eventArgs.Text);
        }

        public static bool IsFellowshipCreated(this ChatTextInterceptEventArgs eventArgs, out string fellowshipName)
        {
            return ChatParsingUtilities.IsFellowshipCreated(eventArgs.Text, out fellowshipName);
        }

        public static bool IsPlayerJoinedFellowship(this ChatTextInterceptEventArgs eventArgs, out string characterName)
        {
            return ChatParsingUtilities.IsPlayerJoinedFellowship(eventArgs.Text, out characterName);
        }

        public static bool IsPlayerLeftFellowship(this ChatTextInterceptEventArgs eventArgs, out string characterName)
        {
            return ChatParsingUtilities.IsPlayerLeftFellowship(eventArgs.Text, out characterName);
        }

        public static bool IsYouLeftFellowship(this ChatTextInterceptEventArgs eventArgs, out string fellowshipName)
        {
            return ChatParsingUtilities.IsYouLeftFellowship(eventArgs.Text, out fellowshipName);
        }

        public static bool IsYouHaveBeenDismissedFromFellowship(this ChatTextInterceptEventArgs eventArgs, out string leader)
        {
            return ChatParsingUtilities.IsYouHaveBeenDismissedFromFellowship(eventArgs.Text, out leader);
        }

        public static bool IsFellowshipDisbanded(this ChatTextInterceptEventArgs eventArgs, out string leader)
        {
            return ChatParsingUtilities.IsFellowshipDisbanded(eventArgs.Text, out leader);
        }

        public static bool IsYouHaveBeenRecruitedToFellowship(this ChatTextInterceptEventArgs eventArgs, out string fellowshipName, out string leader)
        {
            return ChatParsingUtilities.IsYouHaveBeenRecruitedToFellowship(eventArgs.Text, out fellowshipName, out leader);
        }
    }
}
