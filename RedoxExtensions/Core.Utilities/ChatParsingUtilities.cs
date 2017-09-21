using System;
using System.Collections.Generic;
using System.Text;

using Mag.Shared;

namespace RedoxExtensions.Core.Utilities
{
    public static class ChatParsingUtilities
    {
        public const string FellowshipMessagePrefix = "[Fellowship] ";
        public const string FellowshipMessagePostfix = " to your fellowship";
        public const string FellowshipFromSelfMessagePrefix = "[Fellowship] You say,";

        private const string YouHaveCreatedFellowship = "You have created the Fellowship of";
        private const string IsNowAMemberOfYourFellowship = "is now a member of your Fellowship.";
        private const string HasLeftYourFellowship = "has left your Fellowship.";
        private const string DismissedFromFellowship = "has dismissed you from the Fellowship.";
        private const string DisbandFellowshipOther = "has disbanded your Fellowship.";
        private const string DisbandFellowshipSelf = "have disbanded your Fellowship.";
        private const string RecruitedToFellowship = "You have been recruited into the";
        private const string AFellowshipLeadBy = "a fellowship led by";
        private const string YouAreNoLongerMember = "You are no longer a member of the";

        private const string TellsYouString = "tells you,";

        private const string CannotCarryAnymore = "cannot carry anymore.";
        private const string YouGive = "You give";
        private const string YouSayComma = "You say,";

        private const string VIPrefix = "[VI]";

        #region Tell Parsing

        public static bool IsChatTextFromFellowship(string text, out bool fromSelf)
        {
            bool isFellowshipText = text.StartsWith(FellowshipMessagePrefix);
            fromSelf = false;

            if (isFellowshipText)
            {
                fromSelf = text.StartsWith(FellowshipFromSelfMessagePrefix);
            }

            // PhatAC messages are missing [Fellowship] at the beginning.
            var firstCommaIndex = text.IndexOf(", ");
            if (firstCommaIndex < 0)
            {
                fromSelf = false;
                return false;
            }

            var firstPart = text.Substring(0, firstCommaIndex);
            if (firstPart.EndsWith(FellowshipMessagePostfix))
            {
                fromSelf = firstPart.StartsWith("You ");
                return true;
            }

            return isFellowshipText;
        }

        public static string GetSourceOfChat(string text)
        {
            bool bugged;
            string tmp;
            return GetSourceOfChat(text, out bugged, out tmp);
        }

        public static string GetSourceOfChat(string text, out bool bugged, out string workAroundText)
        {
            var maybe = Util.GetSourceOfChat(text);

            // Might be a mag tools bug
            if (string.IsNullOrEmpty(maybe))
            {
                var indexOfTellsYou = text.IndexOf(TellsYouString);
                if (indexOfTellsYou > 0)
                {
                    bugged = true;
                    var realName = text.Substring(0, indexOfTellsYou).Trim();
                    workAroundText = text.Substring(text.IndexOf(',') + 1).Trim();
                    return realName;
                }

                // With PhatAC, the fellowship case is bugged also
                bool fromSelf;
                var isFellowship = IsChatTextFromFellowship(text, out fromSelf);
                if (isFellowship)
                {
                    bugged = true;
                    // There is no work around text for this case.
                    workAroundText = text;

                    if (fromSelf)
                        return "You";

                    var indexOfFellowshipPart = text.IndexOf(" says to your fellowship");
                    return text.Substring(0, indexOfFellowshipPart);
                }

                // MagTools fails to parse another case.
                // ex: You say, "blah"
                if (text.StartsWith(YouSayComma))
                {
                    bugged = true;
                    // There is no work around text for this case.
                    workAroundText = text;
                    return "You";

                }
            }

            bugged = false;
            workAroundText = text;
            return maybe;
        }

        public static bool IsChat(string text, Util.ChatFlags chatFilter)
        {
            bool bugged;
            string workAroundText;
            GetSourceOfChat(text, out bugged, out workAroundText);

            return Util.IsChat(workAroundText, chatFilter);
        }

        public static Util.ChatChannels GetChatChannel(string text)
        {
            bool bugged;
            string workAroundText;
            GetSourceOfChat(text, out bugged, out workAroundText);

            return Util.GetChatChannel(workAroundText);
        }

        public static bool IsChannel(string text, Util.ChatChannels channelFilter)
        {
            if (channelFilter == Util.ChatChannels.All)
            {
                return true;
            }

            if (channelFilter == Util.ChatChannels.None)
            {
                return false;
            }

            var channel = GetChatChannel(text);

            return (channel & channelFilter) == channelFilter;
        }

        public static ChatMessageType GetChatMessageType(string text)
        {
            if (Util.IsChat(text, Util.ChatFlags.NpcSays))
            {
                return ChatMessageType.NpcSays;
            }

            if (Util.IsChat(text, Util.ChatFlags.NpcTellsYou))
            {
                return ChatMessageType.NpcTellsYou;
            }

            if (Util.IsChat(text, Util.ChatFlags.PlayerSaysChannel))
            {
                return ChatMessageType.PlayerSaysChannel;
            }

            if (Util.IsChat(text, Util.ChatFlags.PlayerSaysLocal))
            {
                return ChatMessageType.PlayerSaysLocal;
            }

            if (Util.IsChat(text, Util.ChatFlags.PlayerTellsYou))
            {
                return ChatMessageType.PlayerTellsYou;
            }

            if (Util.IsChat(text, Util.ChatFlags.YouSay))
            {
                return ChatMessageType.YouSay;
            }

            if (Util.IsChat(text, Util.ChatFlags.YouTell))
            {
                return ChatMessageType.YouTell;
            }

            return ChatMessageType.Unknown;
        }

        /// <summary>
        /// Converts a message of:
        /// [Allegiance] &lt;Tell:IIDString:0:PlayerName>PlayerName&lt;\Tell> says, "kk"
        /// to:
        /// "kk"
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ParseMessageFromText(string text)
        {
            var split = text.Split(',');

            if (split.Length < 2)
            {
                // It's not a tell.  Maybe it's just a global message.  W/e return as is.
                return text;
            }

            // Need to do a bit more sanitizing.  Strip off the quotes.  We'll never care about these.
            return split[1].Trim().TrimStart('\"').TrimEnd('\"');
        }

        /// <summary>
        /// Returns true when text is a complex tell.  Ex:
        /// 
        /// [Allegiance] &lt;Tell:IIDString:0:PlayerName>PlayerName&lt;\Tell> says, "kk"
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsComplexTellFormat(string text)
        {
            return text.Contains("<Tell:IIDString");
        }

        /// <summary>
        /// Converts a message of:
        /// [Allegiance] &lt;Tell:IIDString:0:PlayerName>PlayerName&lt;\Tell> says, "kk"
        /// to:
        /// [Allegiance] PlayerName says, "kk"
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string CleanTellText(string text)
        {
            return Mag.Shared.Util.CleanMessage(text);
        }

        public static bool IsVIMessage(string text, out string channel, out string source, out string message)
        {
            if (text.StartsWith(VIPrefix))
            {
                var viPrefixRemoved = text.Substring(VIPrefix.Length + 1);

                var firstCloseBrace = viPrefixRemoved.IndexOf(']');

                channel = viPrefixRemoved.Substring(1, firstCloseBrace - 1);

                var unparsedMessage = viPrefixRemoved.Substring(firstCloseBrace + 1).Trim();

                var split = unparsedMessage.Split(',');

                // Say is only used when it's from yourself, so we can assume "You" as the source.
                if (split[0].EndsWith("say"))
                {
                    source = "You";
                }
                else
                {
                    // -5 because it will end with " says"
                    source = split[0].Substring(0, split[0].Length - 5);
                }


                message = ParseMessageFromText(unparsedMessage);
                return true;
            }

            channel = null;
            source = null;
            message = null;
            return false;
        }

        #endregion

        #region Misc Notification Message Parsing

        /// <summary>
        /// ex: Text = Trophy Cache cannot carry anymore.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsCannotCarryAnymore(string text)
        {
            return text.EndsWith(CannotCarryAnymore);
        }

        public static bool IsYouGive(string text)
        {
            return text.StartsWith(YouGive);
        }

        #endregion

        #region Fellowship Notification Message Parsing

        /// <summary>
        /// Ex: You have created the Fellowship of X.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fellowshipName"></param>
        /// <returns></returns>
        public static bool IsFellowshipCreated(string text, out string fellowshipName)
        {
            if (text.StartsWith(YouHaveCreatedFellowship))
            {
                fellowshipName = text.Substring(YouHaveCreatedFellowship.Length).Trim().TrimEnd('.');
                return true;
            }

            fellowshipName = null;
            return false;
        }

        /// <summary>
        /// Ex: Kreap is now a member of your Fellowship.
        //       Mini Bonsai is now a member of your Fellowship.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="characterName"></param>
        /// <returns></returns>
        public static bool IsPlayerJoinedFellowship(string text, out string characterName)
        {
            if (text.EndsWith(IsNowAMemberOfYourFellowship))
            {
                characterName = text.Substring(0, text.Length - IsNowAMemberOfYourFellowship.Length).Trim();
                return true;
            }

            characterName = null;
            return false;
        }

        /// <summary>
        /// Ex: Kreap has left your Fellowship.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="characterName"></param>
        /// <returns></returns>
        public static bool IsPlayerLeftFellowship(string text, out string characterName)
        {
            if (text.EndsWith(HasLeftYourFellowship))
            {
                characterName = text.Substring(0, text.Length - HasLeftYourFellowship.Length).Trim();
                return true;
            }

            characterName = null;
            return false;
        }

        /// <summary>
        /// Ex: You are no longer a member of the X Fellowship.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fellowshipName"></param>
        /// <returns></returns>
        public static bool IsYouLeftFellowship(string text, out string fellowshipName)
        {
            if (text.StartsWith(YouAreNoLongerMember))
            {
                var tmpfellowshipName = text.Substring(YouAreNoLongerMember.Length + 1);
                fellowshipName = tmpfellowshipName.Substring(0, tmpfellowshipName.Length - 12);
                return true;
            }

            fellowshipName = null;
            return false;
        }

        /// <summary>
        /// Ex: Kreap has dismissed you from the Fellowship.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="leader"></param>
        /// <returns></returns>
        public static bool IsYouHaveBeenDismissedFromFellowship(string text, out string leader)
        {
            if (text.EndsWith(DismissedFromFellowship))
            {
                leader = text.Substring(0, text.Length - DismissedFromFellowship.Length).Trim();
                return true;
            }

            leader = null;
            return false;
        }

        /// <summary>
        /// Ex: Kreap has disbanded your Fellowship.
        //  Ex: You have disbanded your Fellowship.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="leader"></param>
        /// <returns></returns>
        public static bool IsFellowshipDisbanded(string text, out string leader)
        {
            if (text.EndsWith(DisbandFellowshipSelf))
            {
                leader = "You";
                return true;
            }
            else if (text.EndsWith(DisbandFellowshipOther))
            {
                leader = text.Substring(0, text.Length - DisbandFellowshipOther.Length).Trim();
                return true;
            }

            leader = null;
            return false;
        }

        /// <summary>
        /// Ex: You have been recruited into the X fellowship, a fellowship led by Redox.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fellowshipName"></param>
        /// <param name="leader"></param>
        /// <returns></returns>
        public static bool IsYouHaveBeenRecruitedToFellowship(string text, out string fellowshipName, out string leader)
        {
            if (text.StartsWith(RecruitedToFellowship))
            {
                var split = text.Split(',');

                var leftPrefixRemoved = split[0].Substring(RecruitedToFellowship.Length);
                fellowshipName = leftPrefixRemoved.Substring(1, leftPrefixRemoved.Length - 12);

                leader = split[1].Trim().Substring(AFellowshipLeadBy.Length + 1).TrimEnd('.');
                return true;
            }

            fellowshipName = null;
            leader = null;
            return false;
        }

        #endregion
    }
}
