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

        DebugLevel ILoggableObject.MinimumRequiredDebugLevel
        {
            get { return DebugLevel.Low; }
        }

        string ILoggableObject.GetLoggableFormat()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(this.GetType().ToString());
            builder.AppendLine(string.Format("  FellowshipName = {0}", this.FellowshipName));
            return builder.ToString();
        }
    }
}
