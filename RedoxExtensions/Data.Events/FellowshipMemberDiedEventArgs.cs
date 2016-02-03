using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events
{
    public class FellowshipMemberDiedEventArgs : EventArgs, ILoggableObject
    {
        public FellowshipMemberDiedEventArgs(string characterName)
        {
            this.CharacterName = characterName;
        }

        public string CharacterName { get; private set; }

        DebugLevel ILoggableObject.MinimumRequiredDebugLevel
        {
            get { return DebugLevel.Low; }
        }

        string ILoggableObject.GetLoggableFormat()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(this.GetType().ToString());
            builder.AppendLine(string.Format("  CharacterName = {0}", this.CharacterName));
            return builder.ToString();
        }
    }
}
