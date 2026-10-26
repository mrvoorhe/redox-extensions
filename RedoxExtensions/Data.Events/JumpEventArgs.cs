using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events;

public class JumpEventArgs : EventArgs, ILoggableObject
{
    public JumpEventArgs(int characterId, JumpData jumpData, int numLogins, int totalJumps)
    {
        this.CharacterId = characterId;
        this.Data = jumpData;
        this.NumLogins = numLogins;
        this.TotalJumps = totalJumps;
    }

    public int CharacterId { get; private set; }
    public JumpData Data { get; private set; }
    public int NumLogins { get; private set; }
    public int TotalJumps { get; private set; }

    DebugLevel ILoggableObject.MinimumRequiredDebugLevel => DebugLevel.Low;

    string ILoggableObject.GetLoggableFormat()
    {
        var builder = new StringBuilder();
        builder.AppendLine(this.GetType().ToString());
        builder.AppendLine($"  Id = {this.CharacterId}");
        builder.AppendLine($"  Data = {this.Data}");
        builder.AppendLine($"  NumLogins = {this.NumLogins}");
        builder.AppendLine($"  TotalJumps = {this.TotalJumps}");

        return builder.ToString();
    }
}
