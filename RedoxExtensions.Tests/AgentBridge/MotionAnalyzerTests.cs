using System;
using System.Collections.Generic;

using NUnit.Framework;

using RedoxExtensions.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class MotionAnalyzerTests
{
    private const int Cell = 0x01A30153;
    private const double Step = 0.1;

    private static readonly MotionIntent Forward = new() { Forward = true, Run = true };

    private MotionAnalyzer _analyzer;
    private double _time;
    private List<string> _events;

    [SetUp]
    public void SetUp()
    {
        this._analyzer = new MotionAnalyzer(new MotionSettings());
        this._time = 0;
        this._events = new List<string>();
    }

    private void Run(double seconds, double vx, double vy = 0, int landcell = Cell, double x0 = double.NaN, double heading = 90, bool navigating = false)
    {
        var latest = this._analyzer.Latest;
        var x = double.IsNaN(x0) ? latest?.X ?? 10 : x0;
        var y = latest?.Y ?? 10;
        var cell = landcell;
        for (var t = 0.0; t < seconds - 1e-9; t += Step)
        {
            this._time += Step;
            x += vx * Step;
            y += vy * Step;
            if (this._analyzer.AddSample(this._time, cell, x, y, 0, heading, navigating) is { } text)
                this._events.Add(text);
        }
    }

    [Test]
    public void StandingStillIsIdle()
    {
        this.Run(5, 0);

        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Idle));
        Assert.That(this._events, Is.Empty);
    }

    [Test]
    public void RunningIsMoving()
    {
        this._analyzer.SetIntent(this._time, Forward);
        this.Run(3, 4);

        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Moving));
        Assert.That(this._events, Has.Count.EqualTo(1));
        Assert.That(this._events[0], Does.StartWith("moving "));
        Assert.That(this._events[0], Does.Contain("intent=forward,run"));
        Assert.That(this._analyzer.Speed, Is.EqualTo(4).Within(0.01));
    }

    [Test]
    public void RunningIntoAWallIsStuckAfterTheThreshold()
    {
        this._analyzer.SetIntent(this._time, Forward);
        this.Run(2, 4);
        this.Run(2, 0);
        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Moving), "not stuck before 2.5 s without moving");

        this.Run(1, 0);

        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Stuck));
        Assert.That(this._events[this._events.Count - 1], Does.StartWith("stuck for="));
        Assert.That(this._events[this._events.Count - 1], Does.Contain("cell=01A30153"));
    }

    [Test]
    public void StandingStillWithoutIntentIsNeverStuck()
    {
        this._analyzer.SetIntent(this._time, new MotionIntent { Turn = true });
        this.Run(10, 0, navigating: true);

        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Idle));
    }

    [Test]
    public void JitterBelowTheThresholdStillCountsAsStuck()
    {
        this._analyzer.SetIntent(this._time, Forward);
        for (var i = 0; i < 40; i++)
            this.Run(Step, i % 2 == 0 ? 2 : -2);

        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Stuck));
    }

    [Test]
    public void MovingAgainLeavesStuck()
    {
        this._analyzer.SetIntent(this._time, Forward);
        this.Run(4, 0);
        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Stuck));

        this.Run(1.5, 4);

        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Moving));
        Assert.That(this._events[this._events.Count - 1], Does.Contain("was=stuck"));
    }

    [Test]
    public void StuckIsRepeatedWhileItLasts()
    {
        this._analyzer.SetIntent(this._time, Forward);
        this.Run(25, 0);

        Assert.That(this._events.FindAll(e => e.StartsWith("stuck")), Has.Count.EqualTo(3));
    }

    [Test]
    public void WithoutIntentVTNavigatingStandsIn()
    {
        this.Run(4, 0, navigating: true);

        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Stuck));
        Assert.That(this._events[0], Does.Contain("intent=unknown"));
        Assert.That(this._events[0], Does.Contain("nav=true"));
    }

    [Test]
    public void CrossingALandblockBorderIsNotAJump()
    {
        this._analyzer.SetIntent(this._time, Forward);
        this.Run(1, 4, landcell: 0x01A30001, x0: 188);
        this.Run(2, 4, landcell: 0x02A30001, x0: 0.4);

        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Moving));
        Assert.That(this._analyzer.Speed, Is.EqualTo(4).Within(0.5));
    }

    [Test]
    public void PortalSpaceAndTheGraceAfterItAreNeverStuck()
    {
        this._analyzer.SetIntent(this._time, Forward);
        this._analyzer.SetPortal(this._time, true);
        this.Run(5, 0);
        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Portal));

        this._analyzer.SetPortal(this._time, false);
        this.Run(4.5, 0);
        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Portal));

        this.Run(3.5, 0);
        Assert.That(this._analyzer.State, Is.EqualTo(MotionAnalyzer.Stuck));
    }

    [Test]
    public void ATeleportRestartsTheWindow()
    {
        this._analyzer.SetIntent(this._time, Forward);
        this.Run(2, 0);
        this.Run(Step, 0, x0: 150);
        this.Run(1, 0);

        Assert.That(this._analyzer.State, Is.Not.EqualTo(MotionAnalyzer.Stuck));
    }

    [Test]
    public void RecentIsNewestFirstAndSpaced()
    {
        this.Run(5, 1);

        var recent = this._analyzer.Recent(3, 1.0);

        Assert.That(recent, Has.Count.EqualTo(3));
        Assert.That(recent[0].Time - recent[1].Time, Is.GreaterThanOrEqualTo(1.0 - 1e-9));
        Assert.That(recent[0].Time, Is.GreaterThan(recent[2].Time));
    }
}

[TestFixture]
public class MoveToStateParserTests
{
    private static byte[] Message(bool withType, params uint[] payload)
    {
        var words = new List<uint>();
        if (withType)
            words.Add(0xF7B1);
        words.Add(42);
        words.Add(0xF61C);
        words.AddRange(payload);
        var bytes = new byte[words.Count * 4];
        for (var i = 0; i < words.Count; i++)
            BitConverter.GetBytes(words[i]).CopyTo(bytes, i * 4);
        return bytes;
    }

    [Test]
    public void ReadsRunningForward()
    {
        // CurrentHoldKey (Run) | ForwardCommand (RunForward), then the autonomous/position fields that follow.
        var raw = Message(true, 0x001 | 0x004, 2, 0x44000007, 0, 0);

        Assert.That(MoveToStateParser.TryParse(raw, out var intent), Is.True);
        Assert.That(intent.Forward && intent.Run, Is.True);
        Assert.That(intent.ToString(), Is.EqualTo("forward,run"));
    }

    [Test]
    public void SkipsTheStyleBeforeTheForwardCommand()
    {
        var raw = Message(false, 0x002 | 0x004, 0x8000003D, 0x45000006);

        Assert.That(MoveToStateParser.TryParse(raw, out var intent), Is.True);
        Assert.That(intent.Backward, Is.True);
        Assert.That(intent.Forward, Is.False);
        Assert.That(intent.Run, Is.False);
    }

    [Test]
    public void NoFlagsMeansStopped()
    {
        Assert.That(MoveToStateParser.TryParse(Message(true, 0, 0, 0), out var intent), Is.True);
        Assert.That(intent.Translating, Is.False);
        Assert.That(intent.ToString(), Is.EqualTo("none"));
    }

    [Test]
    public void SidestepAndTurnFlags()
    {
        Assert.That(MoveToStateParser.TryParse(Message(true, 0x020 | 0x100, 0x6500000F, 0x6500000D), out var intent), Is.True);
        Assert.That(intent.Sidestep && intent.Turn, Is.True);
    }

    [Test]
    public void IgnoresOtherActionsAndTruncatedMessages()
    {
        var other = new byte[16];
        BitConverter.GetBytes(0xF7B1u).CopyTo(other, 0);
        BitConverter.GetBytes(0xF753u).CopyTo(other, 8);

        Assert.That(MoveToStateParser.TryParse(other, out _), Is.False);
        Assert.That(MoveToStateParser.TryParse(Message(true, 0x004), out _), Is.False);
        Assert.That(MoveToStateParser.TryParse(null, out _), Is.False);
    }
}
