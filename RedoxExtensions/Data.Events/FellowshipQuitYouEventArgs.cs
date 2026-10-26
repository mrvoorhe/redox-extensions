using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events
{
    public class FellowshipQuitYouEventArgs : EventArgs, ILoggableObject
    {
        public FellowshipQuitYouEventArgs(string fellowshipName)
        {
            this.FellowshipName = fellowshipName;
        }

        public string FellowshipName { get; private set; }

        DebugLevel ILoggableObject.MinimumRequiredDebugLevel => DebugLevel.Low;

        string ILoggableObject.GetLoggableFormat()
        {
            var builder = new StringBuilder();
            builder.AppendLine(this.GetType().ToString());
            builder.AppendLine($"  FellowshipName = {this.FellowshipName}");
            return builder.ToString();
        }
    }
}
