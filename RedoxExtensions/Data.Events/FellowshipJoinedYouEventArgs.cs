using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events;

public class FellowshipJoinedYouEventArgs : EventArgs, ILoggableObject
{
    public FellowshipJoinedYouEventArgs(string fellowshipName, string leader)
    {
        this.FellowshipName = fellowshipName;
        this.Leader = leader;
    }

    public string FellowshipName { get; private set; }
    public string Leader { get; private set; }

    DebugLevel ILoggableObject.MinimumRequiredDebugLevel => DebugLevel.Low;

    string ILoggableObject.GetLoggableFormat()
    {
        var builder = new StringBuilder();
        builder.AppendLine(this.GetType().ToString());
        builder.AppendLine($"  FellowshipName = {this.FellowshipName}");
        builder.AppendLine($"  Leader = {this.Leader}");
        return builder.ToString();
    }
}
