using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter;

using Mag.Shared;

using RedoxExtensions.Core.Utilities;
using RedoxExtensions.General.Utilities;

namespace RedoxExtensions.Commands
{
    public static class CommandHelpers
    {
        private static readonly ISupportFeedback _self = new SelfCommandSource();

        public const string DefaultYouValue = "You";

        public const string ForeignCommandAllPrefix = "!";
        public const string ForeignCommandSlavesOnlyPrefix = "#";
        public const string ForeignCommandMasterOnlyPrefix = "$";

        private const char ForeignCommandArgumentSeparator = '|';
        private const char DirectEntryCommandArgumentSeparator = ' ';

        private const char MetaDataArgumentSeparator = '|';

        private const string RedoxExtensionsCommandPrefix = "/re";
        private const string RedoxFellowCommandPrefix = "/rf";

        public static ISupportFeedback Self
        {
            get
            {
                return _self;
            }
        }

        #region Command Text Creation

        public static string CreateRedoxExtensionsCommandText(string cmdName)
        {
            return string.Format("/re {0}", cmdName);
        }

        public static string CreateRedoxExtensionsCommandText(string cmdName, params object[] args)
        {
            var collapsedArgs = CollapseDirectEntryCommandArguments(args);
            return string.Format("{0} {1}", CreateRedoxExtensionsCommandText(cmdName), collapsedArgs);
        }

        public static string CreateRedoxFellowCommandText(string cmdName)
        {
            return string.Format("/rf {0}", cmdName);
        }

        public static string CreateRedoxFellowCommandText(string cmdName, params object[] args)
        {
            var collapsedArgs = CollapseDirectEntryCommandArguments(args);
            return string.Format("{0} {1}", CreateRedoxFellowCommandText(cmdName), collapsedArgs);
        }

        public static string CreateForeignCommandText(string cmdName)
        {
            return string.Format("{0}{1}", ForeignCommandAllPrefix, cmdName);
        }

        public static string CreateForeignCommandText(string cmdName, params object[] args)
        {
            var collapsedArgs = CollapseForeignCommandArguments(args);
            return string.Format("{0} {1}", CreateForeignCommandText(cmdName), collapsedArgs);
        }

        public static string CreateForeignSlaveOnlyCommandText(string cmdName)
        {
            return string.Format("{0}{1}", ForeignCommandSlavesOnlyPrefix, cmdName);
        }

        public static string CreateForeignSlaveOnlyCommandText(string cmdName, params object[] args)
        {
            var collapsedArgs = CollapseForeignCommandArguments(args);
            return string.Format("{0} {1}", CreateForeignSlaveOnlyCommandText(cmdName), collapsedArgs);
        }

        public static string CreateForeignMasterOnlyCommandText(string cmdName)
        {
            return string.Format("{0}{1}", ForeignCommandMasterOnlyPrefix, cmdName);
        }

        public static string CreateForeignMasterOnlyCommandText(string cmdName, params object[] args)
        {
            var collapsedArgs = CollapseForeignCommandArguments(args);
            return string.Format("{0} {1}", CreateForeignMasterOnlyCommandText(cmdName), collapsedArgs);
        }

        #region Conversions

        public static string ConvertToForeignCommandText(ICommand command, TargetType targetType = TargetType.All)
        {
            return ConvertToForeignCommandText(command, targetType, null);
        }

        public static string ConvertToForeignCommandTextAppendCurrentSelect(ICommand command, TargetType targetType = TargetType.All)
        {
            return ConvertToForeignCommandText(command, targetType, REPlugin.Instance.PluginHost.Actions.CurrentSelection);
        }

        public static string ConvertToForeignCommandText(ICommand command, params object[] appendAdditionalArgs)
        {
            return ConvertToForeignCommandText(command, TargetType.All, appendAdditionalArgs);
        }

        public static string ConvertToForeignCommandText(ICommand command, TargetType targetType, params object[] appendAdditionalArgs)
        {
            var prefix = GetForeignPrefixForTargetType(targetType);
            var args = command.Arguments.ToList();

            if (appendAdditionalArgs != null)
            {
                args.AddRange(appendAdditionalArgs.Select(a => a.ToString()));
            }

            var collapsedArgs = CollapseForeignCommandArguments(args.Cast<object>());

            return string.Format("{0}{1} {2}", prefix, command.Name, collapsedArgs).TrimEnd();
        }

        #endregion

        #endregion

        #region Meta Data

        public static string AppendMetaData(string command, params string[] data)
        {
            var collapsedData = CollapseMetaData(data);

            // Append to the of the command
            return string.Format("{0} {1}", command, collapsedData);
        }

        public static string StripMetaData(string message, out IEnumerable<string> data)
        {
            if (!message.EndsWith(")"))
            {
                // There is no meta data.  Return
                data = new List<string>();
                return message;
            }

            var indexOfMetaStart = message.IndexOf('(');

            var strippedMessage = message.Substring(0, indexOfMetaStart).Trim();

            var metaData = message.Substring(indexOfMetaStart);

            data = SplitMetaData(metaData);
            return strippedMessage;
        }

        #endregion

        #region ICommand Parsing

        public static bool TryParse(ChatParserInterceptEventArgs eventArgs, out ICommand command)
        {
            return TryParse(eventArgs.Text, out command);
        }

        public static bool TryParse(ChatTextInterceptEventArgs eventArgs, out ICommand command)
        {
            return TryParse(eventArgs.Text, out command);
        }

        public static bool TryParse(string text, out ICommand command)
        {
            // Check for direct entry
            if (text.StartsWith(RedoxExtensionsCommandPrefix))
            {
                command = ParseDirectEntry(text, text.Substring(RedoxExtensionsCommandPrefix.Length + 1), CommandType.RedoxExtension);
                return true;
            }

            if (text.StartsWith(RedoxFellowCommandPrefix))
            {
                command = ParseDirectEntry(text, text.Substring(RedoxFellowCommandPrefix.Length + 1), CommandType.RedoxFellow);
                return true;
            }

            var cleanedText = text;

            // First, simplify to the simple tell format.  Removing the IIDString stuff.
            if (ChatParsingUtilities.IsComplexTellFormat(cleanedText))
            {
                cleanedText = ChatParsingUtilities.CleanTellText(cleanedText);
            }


            // Remove any newlines in the cleaned version
            cleanedText = cleanedText.Trim('\n');
            var originalTextMinuxNewline = text.Trim('\n');

            string viChannel, source, message;
            if (ChatParsingUtilities.IsVIMessage(cleanedText, out viChannel, out source, out message))
            {
                // It's a VI message.
                return TryParseFromVITell(text, viChannel, source, message, out command);
            }

            // It's not a VI message, let's see if it's any of the native tells we care about
            return TryParseFromNativeTell(text, originalTextMinuxNewline, cleanedText, out command);
        }

        #endregion

        #region Outcome Reporting

        public static void ReportOutcome(ISourceInformation source, string text)
        {
            throw new NotImplementedException();
        }

        public static void ReportOutcome(ISourceInformation source, string text, params object[] args)
        {
            ReportOutcome(source, string.Format(text, args));
        }

        #endregion

        #region Argument Collapsing

        public static string CollapseDirectEntryCommandArguments(IEnumerable<string> args)
        {
            return CollapseDirectEntryCommandArguments(args.Cast<object>());
        }

        public static string CollapseDirectEntryCommandArguments(IEnumerable<object> args)
        {
            return args.Aggregate(string.Empty, (accum, next) => string.Format("{0}{1}{2}", accum, DirectEntryCommandArgumentSeparator, next)).Trim();
        }

        public static string CollapseForeignCommandArguments(IEnumerable<string> args)
        {
            return CollapseForeignCommandArguments(args.Cast<object>());
        }

        public static string CollapseForeignCommandArguments(IEnumerable<object> args)
        {
            return args.Aggregate(string.Empty, (accum, next) => string.Format("{0}{1}{2}", accum, ForeignCommandArgumentSeparator, next)).Trim().TrimStart(ForeignCommandArgumentSeparator);
        }

        public static string CollapseMetaData(IEnumerable<string> data)
        {
            var joinedData = data.Aggregate(string.Empty, (accum, next) => string.Format("{0}{1}{2}", accum, MetaDataArgumentSeparator, next)).Trim().TrimStart(MetaDataArgumentSeparator);
            return string.Format("({0})", joinedData);
        }

        #endregion

        #region Command & Argument Splitting

        /// <summary>
        /// Ex: "give keys"
        ///         returns ("give", "keys")
        /// 
        ///      "!give keys"
        ///         returns ("!give", "keys")
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static General.Utilities.Pair<string, string> SplitCommandAndArguments(string message)
        {

            string commandName = string.Empty;
            string commandArguments = string.Empty;
            var firstSpaceIndex = message.IndexOf(' ');
            if (firstSpaceIndex > 0)
            {
                commandName = message.Substring(0, firstSpaceIndex);
                var beginningOfValue = commandName.Length + 1;
                commandArguments = message.Substring(beginningOfValue, (message.Length - beginningOfValue)).Trim();
            }
            else
            {
                // The command has no arguments.
                commandName = message.Trim();
            }

            return new Pair<string, string>(commandName, commandArguments);
        }

        public static IEnumerable<string> SplitDirectEntryArguments(string argumentString)
        {
            if (string.IsNullOrEmpty(argumentString))
            {
                return new List<string>();
            }

            var tmp = argumentString.Split(DirectEntryCommandArgumentSeparator);
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = tmp[i].Trim();
            }

            return tmp;
        }

        public static IEnumerable<string> SplitForeignArguments(string argumentString)
        {
            if (string.IsNullOrEmpty(argumentString))
            {
                return new List<string>();
            }

            var tmp = argumentString.Split(ForeignCommandArgumentSeparator);
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = tmp[i].Trim();
            }

            return tmp;
        }

        /// <summary>
        /// Expected format : (meta1|meta2)
        /// </summary>
        /// <param name="metaData"></param>
        /// <returns></returns>
        public static IEnumerable<string> SplitMetaData(string metaData)
        {
            var trimmedMeta = metaData.TrimStart('(').TrimEnd(')');
            if (string.IsNullOrEmpty(trimmedMeta))
            {
                return new List<string>();
            }

            var tmp = trimmedMeta.Split(ForeignCommandArgumentSeparator);
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = tmp[i].Trim();
            }

            return tmp;
        }

        /// <summary>
        /// Ex: "!give keys"
        ///         returns "give keys"
        /// </summary>
        /// <param name="messageOrCommand"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static string StripTargetTypeFromForeignCommand(string messageOrCommand, out TargetType targetType)
        {
            if (messageOrCommand.StartsWith(ForeignCommandAllPrefix))
            {
                targetType = TargetType.All;
                return messageOrCommand.Substring(1);
            }
            
            if (messageOrCommand.StartsWith(ForeignCommandSlavesOnlyPrefix))
            {
                targetType = TargetType.SlavesOnly;
                return messageOrCommand.Substring(1);
            }
            
            if (messageOrCommand.StartsWith(ForeignCommandMasterOnlyPrefix))
            {
                targetType = TargetType.MasterOnly;
                return messageOrCommand.Substring(1);
            }

            throw new ArgumentException("messageOrCommand was not a valid foreign command");
        }

        #endregion

        #region Private Methods

        private static string GetForeignPrefixForTargetType(TargetType targetType)
        {
            switch (targetType)
            {
                case TargetType.All:
                    return ForeignCommandAllPrefix;
                case TargetType.SlavesOnly:
                    return ForeignCommandSlavesOnlyPrefix;
                case TargetType.MasterOnly:
                    return ForeignCommandMasterOnlyPrefix;
                default:
                    throw new NotImplementedException();
            }
        }

        #region Command Parsing

        private static bool IsMessageForeignCommand(string message)
        {
            if (message.StartsWith(ForeignCommandAllPrefix)
                || message.StartsWith(ForeignCommandMasterOnlyPrefix)
                || message.StartsWith(ForeignCommandSlavesOnlyPrefix))
            {
                return true;
            }

            return false;
        }

        #region Direct Entry

        private static ICommand ParseDirectEntry(string originalText, string message, CommandType commandType)
        {
            var cmdAndArgsPair = SplitCommandAndArguments(message);
            var args = SplitDirectEntryArguments(cmdAndArgsPair.Item2);

            var command = Command.CreateForDirectEntry(
                cmdAndArgsPair.Item1,
                args,
                originalText,
                commandType,
                null);

            return command;
        }

        #endregion

        #region VI Tell

        private static bool TryParseFromVITell(string rawOriginalText, string viChannel, string source, string message, out ICommand command)
        {
            // Don't support commands over VI general chat.
            if (viChannel == "General")
            {
                command = null;
                return false;
            }

            if (!IsMessageForeignCommand(message))
            {
                command = null;
                return false;
            }

            command = ParseFromVITell(rawOriginalText, viChannel, source, message);
            return true;
        }

        private static ICommand ParseFromVITell(string rawOriginalText, string viChannel, string source, string message)
        {
            var cmdArgumentPair = SplitCommandAndArguments(message);

            TargetType targetType;
            var cleanedCommandName = StripTargetTypeFromForeignCommand(cmdArgumentPair.Item1, out targetType);
            var arguments = SplitForeignArguments(cmdArgumentPair.Item2);

            var command = Command.Create(
                source,
                cleanedCommandName,
                arguments,
                rawOriginalText,
                CommandType.Foreign,
                CommandChannel.VirindiFellowship,
                viChannel,
                targetType,
                null);

            return command;
        }

        #endregion

        #region Native Tell

        private static bool TryParseFromNativeTell(string rawOriginalText, string originalTextNewLineRemoved, string cleanedText, out ICommand command)
        {
            // MagTools IsChat helper doesn't handle your Fellowship message parsing correctly, so we need to special case this.

            bool isFellowFromSelfMessage;
            bool isFellowMessage = ChatParsingUtilities.IsChatTextFromFellowship(cleanedText, out isFellowFromSelfMessage);

            bool validCommandChannel;
            Util.ChatChannels chatChannel = Util.ChatChannels.None;

            if (isFellowMessage)
            {
                validCommandChannel = true;
                chatChannel = Util.ChatChannels.Fellowship;
            }
            else
            {
                validCommandChannel = IsFromValidCommandChannel(originalTextNewLineRemoved, out chatChannel);
            }

            if (!validCommandChannel)
            {
                command = null;
                return false;
            }

            var message = ChatParsingUtilities.ParseMessageFromText(cleanedText);

            if (!IsMessageForeignCommand(message))
            {
                command = null;
                return false;
            }

            command = ParseFromNativeTell(rawOriginalText, originalTextNewLineRemoved, chatChannel, message, isFellowFromSelfMessage);
            return true;
        }

        private static ICommand ParseFromNativeTell(string rawOriginalText, string originalTextNewLineRemoved, Util.ChatChannels channel, string message, bool isFellowFromSelfMessage)
        {
            var source = isFellowFromSelfMessage ? DefaultYouValue : ChatParsingUtilities.GetSourceOfChat(originalTextNewLineRemoved);

            var cmdArgumentPair = SplitCommandAndArguments(message);

            TargetType targetType;
            var cleanedCommandName = StripTargetTypeFromForeignCommand(cmdArgumentPair.Item1, out targetType);
            var arguments = SplitForeignArguments(cmdArgumentPair.Item2);

            var commandChannel = ChatChannelToCommandChannel(channel);

            var command = Command.Create(
                source,
                cleanedCommandName,
                arguments,
                rawOriginalText,
                CommandType.Foreign,
                commandChannel,
                null,
                targetType,
                null);

            return command;
        }

        #endregion

        private static bool IsFromValidCommandChannel(string originalText, out Util.ChatChannels chatChannel)
        {
            // see if it's a tell.  Don't care about YouTell because YouTell's would always be
            // targetting a specific character
            var validFlags =
                Util.ChatFlags.PlayerTellsYou
                | Util.ChatFlags.PlayerSaysLocal
                | Util.ChatFlags.PlayerSaysChannel
                | Util.ChatFlags.YouSay;

            if (!Util.IsChat(originalText, validFlags))
            {
                chatChannel = Util.ChatChannels.None;
                return false;
            }

            // If we get here, it's one of the chat channels that we support receiving commands over

            // MagTools util is sensitive, we need the uncleaned version of the string, but new lines need to be removed from the
            // end in order for it to work correctly
            chatChannel = ChatParsingUtilities.GetChatChannel(originalText);

            // Check for all of the cases we don't support, and if it's one of them, return
            switch (chatChannel)
            {
                case Util.ChatChannels.None:
                case Util.ChatChannels.Allegiance:
                case Util.ChatChannels.General:
                case Util.ChatChannels.LFG:
                case Util.ChatChannels.Roleplay:
                case Util.ChatChannels.Society:
                case Util.ChatChannels.Trade:
                case Util.ChatChannels.All:
                    return false;

            }

            return true;
        }

        private static CommandChannel ChatChannelToCommandChannel(Util.ChatChannels chatChannel)
        {
            switch (chatChannel)
            {
                case Util.ChatChannels.Fellowship:
                    return CommandChannel.Fellowship;
                case Util.ChatChannels.Area:
                    return CommandChannel.AreaChat;
                case Util.ChatChannels.Tells:
                    return CommandChannel.Tell;
                default:
                    throw new InvalidOperationException("Can't map : " + chatChannel);
            }
        }

        #endregion

        #endregion

        #region Inner Classes

        private class SelfCommandSource : ISupportFeedback
        {
            public string SourceCharacter
            {
                get
                {
                    return DefaultYouValue;
                }
            }

            public bool FromSelf
            {
                get
                {
                    return true;
                }
            }

            public CommandChannel Channel
            {
                get
                {
                    return CommandChannel.DirectEntry;
                }
            }

            public object ChannelTag
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public bool UsesChannelTag
            {
                get
                {
                    return false;
                }
            }

            public int SourceCharacterId
            {
                get
                {
                    return REPlugin.Instance.CharacterFilter.Id;
                }
            }

            public bool IsSourceIdAvailable
            {
                get
                {
                    return true;
                }
            }

            public void GiveFeedback(FeedbackType feedbackType, string message)
            {
                CommandResponseHandler.TellSource(this, string.Format("[{0}] {1}", feedbackType, message));
            }

            public void GiveFeedback(FeedbackType feedbackType, string message, params object[] args)
            {
                CommandResponseHandler.TellSource(this, string.Format("[{0}] {1}", feedbackType, string.Format(message, args)));
            }
        }

        #endregion
    }
}
