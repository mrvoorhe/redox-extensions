using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events
{
    public class FellowshipJoinedYouEventArgs : EventArgs, ILoggableObject
    {
        public FellowshipJoinedYouEventArgs(string fellowshipName, string leader)
        {
            this.FellowshipName = fellowshipName;
            this.Leader = leader;
        }

        public string FellowshipName { get; private set; }
        public string Leader { get; private set; }

        DebugLevel ILoggableObject.MinimumRequiredDebugLevel
        {
            get { return DebugLevel.Low; }
        }

        string ILoggableObject.GetLoggableFormat()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(this.GetType().ToString());
            builder.AppendLine(string.Format("  FellowshipName = {0}", this.FellowshipName));
            builder.AppendLine(string.Format("  Leader = {0}", this.Leader));
            return builder.ToString();
        }
    }
}
