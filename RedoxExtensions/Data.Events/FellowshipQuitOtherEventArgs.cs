using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events
{
    public class FellowshipQuitOtherEventArgs : EventArgs, ILoggableObject
    {
        public FellowshipQuitOtherEventArgs(string characterName)
        {
            this.CharacterName = characterName;
        }

        public string CharacterName { get; private set; }

        DebugLevel ILoggableObject.MinimumRequiredDebugLevel => DebugLevel.Low;

        string ILoggableObject.GetLoggableFormat()
        {
            var builder = new StringBuilder();
            builder.AppendLine(this.GetType().ToString());
            builder.AppendLine($"  CharacterName = {this.CharacterName}");
            return builder.ToString();
        }
    }
}
