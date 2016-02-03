using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Commands
{
    public class Command : ICommand
    {
        #region Constructors

        public Command(
            string sourceCharacter,
            string name,
            IEnumerable<string> arguments,
            string rawValue,
            CommandType commandType,
            CommandChannel channel,
            object channelTag,
            TargetType targetType,
            object targettingTag)
        {
            this.SourceCharacter = sourceCharacter;
            this.FromSelf = sourceCharacter == CommandHelpers.DefaultYouValue;
            this.Name = name;
            this.Arguments = arguments.ToList().AsReadOnly();
            this.RawValue = rawValue;
            this.CommandType = commandType;
            this.Channel = channel;
            this.ChannelTag = channelTag;
            this.UsesChannelTag = channelTag != null;
            this.CommandTargetType = targetType;
            this.ExplicitTargettingTag = targettingTag;
            this.UsesExplicitTargettingTag = targettingTag != null;
        }

        //public Command(
        //    string sourceCharacter,
        //    string name,
        //    IEnumerable<string> arguments,
        //    string rawValue,
        //    CommandType commandType,
        //    CommandChannel channel,
        //    CommandTargetType targetType)
        //    : this(sourceCharacter, name, arguments, rawValue, commandType, channel, null, targetType, null)
        //{
        //}

        #endregion

        #region Public Static Methods

        public static ICommand Create(
            string sourceCharacter,
            string name,
            IEnumerable<string> arguments,
            string rawValue,
            CommandType commandType,
            CommandChannel channel,
            object channelTag,
            TargetType targetType,
            object targettingTag)
        {
            return new Command(sourceCharacter, name, arguments, rawValue, commandType, channel, channelTag, targetType, targettingTag);
        }

        public static ICommand CreateForDirectEntry(
            string name,
            IEnumerable<string> arguments,
            string rawValue,
            CommandType commandType)
        {
            return CreateForDirectEntry(name, arguments, rawValue, commandType, null);
        }

        public static ICommand CreateForDirectEntry(
            string name,
            IEnumerable<string> arguments,
            string rawValue,
            CommandType commandType,
            object targettingTag)
        {
            return new Command(CommandHelpers.DefaultYouValue, name, arguments, rawValue, commandType, CommandChannel.DirectEntry, null, TargetType.SelfOnly, targettingTag);
        }

        #region Easy of use overloads

        public static ICommand CreateRedoxFellow(
            string rawValue,
            string name,
            IEnumerable<string> arguments = null,
            object targettingTag = null)
        {
            return new Command(CommandHelpers.DefaultYouValue, name, arguments ?? new List<string>(), rawValue, CommandType.RedoxFellow, CommandChannel.DirectEntry, null, TargetType.SelfOnly, targettingTag);
        }

        public static ICommand CreateRedoxExtension(
            string rawValue,
            string name,
            IEnumerable<string> arguments = null,
            object targettingTag = null)
        {
            return new Command(CommandHelpers.DefaultYouValue, name, arguments ?? new List<string>(), rawValue, CommandType.RedoxExtension, CommandChannel.DirectEntry, null, TargetType.SelfOnly, targettingTag);
        }

        #endregion

        #endregion

        #region Public Properties

        public string SourceCharacter { get; private set; }

        /// <summary>
        /// TODO : SUPPORT USING META DATA.
        /// </summary>
        public int SourceCharacterId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool FromSelf { get; private set; }

        public CommandChannel Channel { get; private set; }

        public bool UsesChannelTag { get; private set; }

        public object ChannelTag { get; private set; }

        public string Name { get; private set; }

        public string RawValue { get; private set; }

        public ReadOnlyCollection<string> Arguments { get; private set; }

        public CommandType CommandType { get; private set; }

        public TargetType CommandTargetType { get; private set; }

        public bool UsesExplicitTargettingTag { get; private set; }

        public object ExplicitTargettingTag { get; private set; }

        /// <summary>
        /// TODO : SUPPORT USING META DATA.
        /// </summary>
        public bool IsSourceIdAvailable
        {
            get
            {
                // TODO : Implement
                return false;
            }
        }

        #endregion

        #region Public Methods

        public void GiveFeedback(FeedbackType feedbackType, string message)
        {
            CommandResponseHandler.TellSource(this, string.Format("[{0}] {1}", feedbackType, message));
        }

        public void GiveFeedback(FeedbackType feedbackType, string message, params object[] args)
        {
            CommandResponseHandler.TellSource(this, string.Format("[{0}] {1}", feedbackType, string.Format(message, args)));
        }

        #endregion

        #region Explicit ILoggableObject

        DebugLevel ILoggableObject.MinimumRequiredDebugLevel
        {
            get { return DebugLevel.Low; }
        }

        string ILoggableObject.GetLoggableFormat()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(this.GetType().ToString());
            builder.AppendLine(string.Format("  Source = {0}", this.SourceCharacter));
            builder.AppendLine(string.Format("  Name = {0}", this.Name));
            return builder.ToString();
        }

        #endregion
    }
}
