using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events;

public class FellowshipDisbandEventArgs : EventArgs, ILoggableObject
{
    public FellowshipDisbandEventArgs(string leader)
    {
        this.Leader = leader;
    }

    public string Leader { get; private set; }

    DebugLevel ILoggableObject.MinimumRequiredDebugLevel => DebugLevel.Low;

    string ILoggableObject.GetLoggableFormat()
    {
        var builder = new StringBuilder();
        builder.AppendLine(this.GetType().ToString());
        builder.AppendLine($"  Leader = {this.Leader}");
        return builder.ToString();
    }
}
