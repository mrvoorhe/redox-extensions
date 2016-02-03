using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events
{
    public class JumpEventArgs : EventArgs, ILoggableObject
    {
        public JumpEventArgs(int characterId, JumpData jumpData, short numLogins, short totalJumps)
        {
            this.CharacterId = characterId;
            this.Data = jumpData;
            this.NumLogins = numLogins;
            this.TotalJumps = totalJumps;
        }

        public int CharacterId { get; private set; }
        public JumpData Data { get; private set; }
        public short NumLogins { get; private set; }
        public short TotalJumps { get; private set; }

        DebugLevel ILoggableObject.MinimumRequiredDebugLevel
        {
            get
            {
                return DebugLevel.Low;
            }
        }

        string ILoggableObject.GetLoggableFormat()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(this.GetType().ToString());
            builder.AppendLine(string.Format("  Id = {0}", this.CharacterId));
            builder.AppendLine(string.Format("  Data = {0}", this.Data));
            builder.AppendLine(string.Format("  NumLogins = {0}", this.NumLogins));
            builder.AppendLine(string.Format("  TotalJumps = {0}", this.TotalJumps));

            return builder.ToString();
        }
    }
}
