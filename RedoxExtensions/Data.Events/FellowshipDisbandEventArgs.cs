using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events
{
    public class FellowshipDisbandEventArgs : EventArgs, ILoggableObject
    {
        public FellowshipDisbandEventArgs(string leader)
        {
            this.Leader = leader;
        }

        public string Leader { get; private set; }

        DebugLevel ILoggableObject.MinimumRequiredDebugLevel
        {
            get { return DebugLevel.Low; }
        }

        string ILoggableObject.GetLoggableFormat()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(this.GetType().ToString());
            builder.AppendLine(string.Format("  Leader = {0}", this.Leader));
            return builder.ToString();
        }
    }
}
