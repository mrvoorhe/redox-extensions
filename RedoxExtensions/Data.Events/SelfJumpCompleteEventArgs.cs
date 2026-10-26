using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Data.Events;

public class SelfJumpCompleteEventArgs : EventArgs, ILoggableObject
{
    public SelfJumpCompleteEventArgs(
        JumpData jumpData,
        IEnumerable<Location.Location> trajectory,
        Location.Location landingLocation,
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

    public ReadOnlyCollection<Location.Location> Trajectory { get; private set; }

    public Location.Location LandingLocation { get; private set; }

    public double MaxHeightInMeters { get; private set; }

    public double DistanceJumpedInMeters { get; private set; }

    public TimeSpan AirTime { get; private set; }

    DebugLevel ILoggableObject.MinimumRequiredDebugLevel => DebugLevel.Low;

    string ILoggableObject.GetLoggableFormat()
    {
        var builder = new StringBuilder();
        builder.AppendLine(this.GetType().ToString());
        builder.AppendLine($"  JumpData = {this.JumpData}");
        builder.AppendLine($"  Trajectory = {this.Trajectory.ToPrettyString()}");
        builder.AppendLine($"  LandingLocation = {this.LandingLocation}");
        builder.AppendLine($"  Height = {this.MaxHeightInMeters}");
        builder.AppendLine($"  Distance = {this.DistanceJumpedInMeters}");
        builder.AppendLine($"  AirTime(s) = {this.AirTime.TotalSeconds}");

        return builder.ToString();
    }
}
