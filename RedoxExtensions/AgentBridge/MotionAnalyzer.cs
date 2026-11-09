using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RedoxExtensions.AgentBridge;

/// <summary>What the client asked the character to do, from its last MoveToState.</summary>
internal struct MotionIntent
{
    public bool Forward;
    public bool Backward;
    public bool Sidestep;
    public bool Turn;
    public bool Run;

    public bool Translating => this.Forward || this.Backward || this.Sidestep;

    public override string ToString()
    {
        var parts = new List<string>();
        if (this.Forward)
            parts.Add("forward");
        if (this.Backward)
            parts.Add("backward");
        if (this.Sidestep)
            parts.Add("sidestep");
        if (this.Turn)
            parts.Add("turn");
        if (this.Run && this.Translating)
            parts.Add("run");
        return parts.Count == 0 ? "none" : string.Join(",", parts.ToArray());
    }
}

internal sealed class MotionSettings
{
    public double StuckSeconds = 2.5;

    /// <summary>Meters.  Moving less than this over StuckSeconds while trying to move is stuck.</summary>
    public double MinDisplacement = 0.5;

    /// <summary>Meters per second, averaged over the last second, to count as moving.</summary>
    public double MinSpeed = 0.5;

    public double PortalGraceSeconds = 5;

    public double StuckRepeatSeconds = 10;

    /// <summary>A jump this far between two samples outside portal space (recall, teleport) restarts the window.</summary>
    public double TeleportMeters = 20;
}

/// <summary>
/// Works out from position samples and the client's movement intent whether the character is idle, moving, stuck or
/// in portal space.  No Decal dependency, so it can be unit-tested; <see cref="MotionMonitor"/> feeds it.
/// Times are seconds on any monotonic clock.
/// </summary>
internal sealed class MotionAnalyzer
{
    public const string Idle = "idle";
    public const string Moving = "moving";
    public const string Stuck = "stuck";
    public const string Portal = "portal";

    private const int Capacity = 128;

    private readonly MotionSettings _settings;
    private readonly Sample[] _samples = new Sample[Capacity];
    private int _count;
    private int _head;

    private MotionIntent _intent;
    private double _intentSince;
    private bool _intentSeen;
    private bool _inPortal;
    private double _portalExitAt = double.NegativeInfinity;
    private double _lastStuckReport;

    public MotionAnalyzer(MotionSettings settings)
    {
        this._settings = settings;
        this.State = Idle;
    }

    public MotionSettings Settings => this._settings;

    public string State { get; private set; }

    public double StateSince { get; private set; }

    public double Speed { get; private set; }

    public MotionIntent Intent => this._intent;

    /// <summary>False until the first MoveToState, so the caller knows whether intent or VT nav state was used.</summary>
    public bool IntentSeen => this._intentSeen;

    public bool Navigating { get; private set; }

    public Sample? Latest => this._count == 0 ? null : this.At(0);

    public void SetIntent(double time, MotionIntent intent)
    {
        if (!this._intentSeen || intent.Translating != this._intent.Translating)
            this._intentSince = time;
        this._intent = intent;
        this._intentSeen = true;
    }

    public void SetPortal(double time, bool inPortal)
    {
        this._inPortal = inPortal;
        if (!inPortal)
            this._portalExitAt = time;
        this.Clear();
    }

    public void Clear()
    {
        this._count = 0;
        this._head = 0;
    }

    /// <summary>Adds a sample.  Returns the text of a "motion" event to journal, or null.</summary>
    public string AddSample(double time, int landcell, double x, double y, double z, double heading, bool navigating)
    {
        var sample = new Sample(time, landcell, x, y, z, heading);
        this.Navigating = navigating;

        if (this._count > 0 && Distance(this.At(0), sample) > this._settings.TeleportMeters)
            this.Clear();

        this._head = (this._head + 1) % Capacity;
        this._samples[this._head] = sample;
        if (this._count < Capacity)
            this._count++;

        this.Speed = this.SpeedOver(1.0);
        var next = this.Classify(time);

        if (next != this.State)
        {
            var previous = this.State;
            var lasted = time - this.StateSince;
            this.State = next;
            this.StateSince = time;
            if (next == Stuck)
                this._lastStuckReport = time;
            return this.Describe(sample, previous, lasted);
        }

        if (next == Stuck && time - this._lastStuckReport >= this._settings.StuckRepeatSeconds)
        {
            this._lastStuckReport = time;
            return this.Describe(sample, null, 0);
        }

        return null;
    }

    /// <summary>Recent samples, newest first, at most one per <paramref name="spacing"/> seconds.</summary>
    public List<Sample> Recent(int max, double spacing)
    {
        var result = new List<Sample>();
        for (var i = 0; i < this._count && result.Count < max; i++)
        {
            var s = this.At(i);
            if (result.Count == 0 || result[result.Count - 1].Time - s.Time >= spacing)
                result.Add(s);
        }
        return result;
    }

    private string Classify(double time)
    {
        if (this._inPortal || time - this._portalExitAt < this._settings.PortalGraceSeconds)
            return Portal;

        var wantsToMove = this._intentSeen ? this._intent.Translating : this.Navigating;
        var trying = this._intentSeen ? time - this._intentSince : time - this.OldestTime();

        if (wantsToMove && trying >= this._settings.StuckSeconds && this.Covers(this._settings.StuckSeconds)
            && this.MaxDisplacementOver(this._settings.StuckSeconds) < this._settings.MinDisplacement)
            return Stuck;

        if (this.Speed >= this._settings.MinSpeed)
            return Moving;

        // Trying to move but not long enough to call it stuck: keep the state rather than flicker through idle.
        if (wantsToMove && this.State is Moving or Stuck)
            return this.State;

        return Idle;
    }

    private string Describe(Sample s, string previous, double lasted)
    {
        var text = new StringBuilder(160);
        text.Append(this.State);
        if (this.State == Stuck)
            text.Append(" for=").Append(F(this.TryingFor(s.Time), 1)).Append('s');
        text.Append(" cell=").Append(((uint)s.Landcell).ToString("X8", CultureInfo.InvariantCulture))
            .Append(" x=").Append(F(s.X, 2))
            .Append(" y=").Append(F(s.Y, 2))
            .Append(" z=").Append(F(s.Z, 2))
            .Append(" heading=").Append(F(s.Heading, 0))
            .Append(" speed=").Append(F(this.Speed, 1))
            .Append(" intent=").Append(this._intentSeen ? this._intent.ToString() : "unknown")
            .Append(" nav=").Append(this.Navigating ? "true" : "false");
        if (previous != null)
            text.Append(" was=").Append(previous).Append(" after=").Append(F(lasted, 1)).Append('s');
        return text.ToString();
    }

    private double TryingFor(double time) => this._intentSeen ? time - this._intentSince : time - this.StateSince + this._settings.StuckSeconds;

    private bool Covers(double seconds) => this._count > 1 && this.At(0).Time - this.OldestTime() >= seconds * 0.9;

    private double OldestTime() => this._count == 0 ? 0 : this.At(this._count - 1).Time;

    private double MaxDisplacementOver(double seconds)
    {
        var newest = this.At(0);
        var max = 0.0;
        for (var i = 1; i < this._count; i++)
        {
            var s = this.At(i);
            if (newest.Time - s.Time > seconds)
                break;
            max = Math.Max(max, Distance(newest, s));
        }
        return max;
    }

    private double SpeedOver(double seconds)
    {
        if (this._count < 2)
            return 0;

        var newest = this.At(0);
        var oldest = newest;
        for (var i = 1; i < this._count; i++)
        {
            var s = this.At(i);
            oldest = s;
            if (newest.Time - s.Time >= seconds)
                break;
        }

        var dt = newest.Time - oldest.Time;
        return dt <= 0 ? 0 : HorizontalDistance(newest, oldest) / dt;
    }

    private Sample At(int age) => this._samples[(this._head - age + Capacity) % Capacity];

    private static double Distance(Sample a, Sample b)
    {
        var h = HorizontalDistance(a, b);
        var dz = a.Z - b.Z;
        return Math.Sqrt(h * h + dz * dz);
    }

    private static double HorizontalDistance(Sample a, Sample b)
    {
        var dx = a.GlobalX - b.GlobalX;
        var dy = a.GlobalY - b.GlobalY;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static string F(double value, int decimals) => value.ToString("F" + decimals, CultureInfo.InvariantCulture);

    internal readonly struct Sample
    {
        public Sample(double time, int landcell, double x, double y, double z, double heading)
        {
            this.Time = time;
            this.Landcell = landcell;
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Heading = heading;
        }

        public double Time { get; }
        public int Landcell { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double Heading { get; }

        /// <summary>Meters across landblocks (192 m each), so crossing a cell border isn't a jump.</summary>
        public double GlobalX => ((this.Landcell >> 24) & 0xFF) * 192.0 + this.X;

        public double GlobalY => ((this.Landcell >> 16) & 0xFF) * 192.0 + this.Y;
    }
}
