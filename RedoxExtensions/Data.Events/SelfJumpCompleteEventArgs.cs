using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;
using RedoxLib.Objects;

namespace RedoxExtensions.Data.Events
{
    public class SelfJumpCompleteEventArgs : EventArgs, ILoggableObject
    {
        public SelfJumpCompleteEventArgs(
            JumpData jumpData,
            IEnumerable<Location> trajectory,
            Location landingLocation,
            double maxHeightInMeters,
            double distanceJumpedInMeters,
            TimeSpan airTime)
        {
            this.JumpData = jumpData;
            this.Trajectory = trajectory.ToList().AsReadOnly();
            this.LandingLocation = landingLocation;
            this.MaxHeightInMeters = maxHeightInMeters;
            this.DistanceJumpedInMeters = distanceJumpedInMeters;
            this.AirTime = airTime;
        }

        public JumpData JumpData { get; private set; }

        public ReadOnlyCollection<Location> Trajectory { get; private set; }

        public Location LandingLocation { get; private set; }

        public double MaxHeightInMeters { get; private set; }

        public double DistanceJumpedInMeters { get; private set; }

        public TimeSpan AirTime { get; private set; }

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
            builder.AppendLine(string.Format("  JumpData = {0}", this.JumpData));
            builder.AppendLine(string.Format("  Trajectory = {0}", this.Trajectory.ToPrettyString()));
            builder.AppendLine(string.Format("  LandingLocation = {0}", this.LandingLocation));
            builder.AppendLine(string.Format("  Height = {0}", this.MaxHeightInMeters));
            builder.AppendLine(string.Format("  Distance = {0}", this.DistanceJumpedInMeters));
            builder.AppendLine(string.Format("  AirTime(s) = {0}", this.AirTime.TotalSeconds));

            return builder.ToString();
        }
    }
}
