using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using RedoxExtensions.Recording;

namespace RedoxExtensions.Tests.Recording;

[TestFixture]
public class RecorderRulesTests
{
    private static readonly DateTime T0 = new(2026, 11, 21, 1, 0, 0);

    private static RecorderState Session(TimeSpan? stopIn = null) => new()
    {
        File = @"E:\Rec\2026-11-21\Cobble.jsonl",
        StartedAt = T0,
        TimeoutAt = T0.AddHours(5),
        StopAt = stopIn.HasValue ? T0 + stopIn.Value : null,
        JournalId = "100-a",
        LastSeq = 50,
    };

    [Test]
    public void EffectiveEndIsEarlierOfTimeoutAndStop()
    {
        Assert.That(RecorderRules.EffectiveEnd(Session()), Is.EqualTo(T0.AddHours(5)));
        Assert.That(RecorderRules.EffectiveEnd(Session(TimeSpan.FromHours(2))), Is.EqualTo(T0.AddHours(2)));
        Assert.That(RecorderRules.EffectiveEnd(Session(TimeSpan.FromHours(9))), Is.EqualTo(T0.AddHours(5)));
    }

    [Test]
    public void StopWithDelayPastTimeoutKeepsTimeout()
    {
        var decision = RecorderRules.ApplyStop(Session(), TimeSpan.FromHours(6), T0.AddHours(1));
        Assert.That(decision.Kind, Is.EqualTo(RecorderStopKind.Pending));
        Assert.That(decision.StopAt, Is.EqualTo(T0.AddHours(5)));
    }

    [Test]
    public void StopWithDelaySetsPendingStop()
    {
        var decision = RecorderRules.ApplyStop(Session(), TimeSpan.FromMinutes(15), T0.AddHours(3));
        Assert.That(decision.Kind, Is.EqualTo(RecorderStopKind.Pending));
        Assert.That(decision.StopAt, Is.EqualTo(T0.AddHours(3).AddMinutes(15)));
    }

    [Test]
    public void SecondStopReplacesFirstEarlierOrLaterButNotPastTimeout()
    {
        var state = Session();
        var now = T0.AddHours(1);

        state.StopAt = RecorderRules.ApplyStop(state, TimeSpan.FromHours(2), now).StopAt;
        Assert.That(RecorderRules.EffectiveEnd(state), Is.EqualTo(now.AddHours(2)));

        state.StopAt = RecorderRules.ApplyStop(state, TimeSpan.FromMinutes(10), now).StopAt;
        Assert.That(RecorderRules.EffectiveEnd(state), Is.EqualTo(now.AddMinutes(10)));

        state.StopAt = RecorderRules.ApplyStop(state, TimeSpan.FromHours(3), now).StopAt;
        Assert.That(RecorderRules.EffectiveEnd(state), Is.EqualTo(now.AddHours(3)));

        state.StopAt = RecorderRules.ApplyStop(state, TimeSpan.FromHours(20), now).StopAt;
        Assert.That(RecorderRules.EffectiveEnd(state), Is.EqualTo(T0.AddHours(5)));
    }

    [Test]
    public void StopWithNoDelayStopsNow()
    {
        Assert.That(RecorderRules.ApplyStop(Session(), null, T0).Kind, Is.EqualTo(RecorderStopKind.StopNow));
    }

    [Test]
    public void StopWithNoSessionIsNoOp()
    {
        Assert.That(RecorderRules.ApplyStop(null, null, T0).Kind, Is.EqualTo(RecorderStopKind.NotRecording));
        var decision = RecorderRules.ApplyStop(null, TimeSpan.FromMinutes(15), T0);
        Assert.That(decision.Kind, Is.EqualTo(RecorderStopKind.NotRecording));
        Assert.That(decision.StopAt, Is.Null);
    }

    [Test]
    public void ResumeNoStateFile()
    {
        Assert.That(RecorderRules.DecideResume(null, "100-a", T0), Is.EqualTo(RecorderResumeDecision.None));
    }

    [Test]
    public void ResumeDeadlineAheadSameJournal()
    {
        Assert.That(RecorderRules.DecideResume(Session(), "100-a", T0.AddHours(1)), Is.EqualTo(RecorderResumeDecision.ContinueSameJournal));
    }

    [Test]
    public void ResumeDeadlineAheadNewJournal()
    {
        Assert.That(RecorderRules.DecideResume(Session(), "200-b", T0.AddHours(1)), Is.EqualTo(RecorderResumeDecision.NewJournal));
    }

    [Test]
    public void ResumeDeadlinePassed()
    {
        Assert.That(RecorderRules.DecideResume(Session(), "100-a", T0.AddHours(5)), Is.EqualTo(RecorderResumeDecision.Expired));
        Assert.That(RecorderRules.DecideResume(Session(TimeSpan.FromHours(2)), "100-a", T0.AddHours(3)), Is.EqualTo(RecorderResumeDecision.Expired));
    }

    [Test]
    public void ResumePendingStopAheadOfTimeoutCountsAsDeadline()
    {
        Assert.That(RecorderRules.DecideResume(Session(TimeSpan.FromHours(2)), "100-a", T0.AddHours(1)), Is.EqualTo(RecorderResumeDecision.ContinueSameJournal));
    }

    [Test]
    public void ResumeStartedInTheFutureBeyondSkewIsInvalid()
    {
        Assert.That(RecorderRules.DecideResume(Session(), "100-a", T0.AddMinutes(-6)), Is.EqualTo(RecorderResumeDecision.Invalid));
        Assert.That(RecorderRules.DecideResume(Session(), "100-a", T0.AddMinutes(-4)), Is.EqualTo(RecorderResumeDecision.ContinueSameJournal));
    }

    [Test]
    public void ResumeMalformedStateIsInvalid()
    {
        var noFile = Session();
        noFile.File = null;
        Assert.That(RecorderRules.DecideResume(noFile, "100-a", T0), Is.EqualTo(RecorderResumeDecision.Invalid));

        var backwards = Session();
        backwards.TimeoutAt = T0.AddMinutes(-1);
        Assert.That(RecorderRules.DecideResume(backwards, "100-a", T0), Is.EqualTo(RecorderResumeDecision.Invalid));
    }

    [Test]
    public void NextFileNameNeverReusesAName()
    {
        var existing = new HashSet<string>();
        Func<string, bool> exists = p => existing.Contains(Path.GetFileName(p));

        Assert.That(RecorderRules.NextFileName("d", "Cobble", exists), Is.EqualTo("Cobble"));
        existing.Add("Cobble.jsonl");
        Assert.That(RecorderRules.NextFileName("d", "Cobble", exists), Is.EqualTo("Cobble-2"));
        existing.Add("Cobble-2.jsonl");
        Assert.That(RecorderRules.NextFileName("d", "Cobble", exists), Is.EqualTo("Cobble-3"));
        existing.Remove("Cobble.jsonl");
        Assert.That(RecorderRules.NextFileName("d", "Cobble", exists), Is.EqualTo("Cobble"));
    }

    [Test]
    public void NextFileNameChecksTheDisk()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RedoxExtensions-" + Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Cobble.jsonl"), "");
            Assert.That(RecorderRules.NextFileName(folder, "Cobble"), Is.EqualTo("Cobble-2"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Test]
    public void FolderDateIsSessionStartDate()
    {
        var started = new DateTime(2026, 11, 20, 23, 50, 0);
        var folder = RecorderRules.SessionFolder("root", started);
        Assert.That(folder, Is.EqualTo(Path.Combine("root", "2026-11-20")));
        Assert.That(RecorderRules.SessionFolder("root", started), Is.EqualTo(folder));
        Assert.That(RecorderRules.DateText(started), Is.EqualTo("2026-11-20"));
    }

    [Test]
    public void Paths()
    {
        Assert.That(RecorderRules.StatePath("inst", "Cow/head"), Is.EqualTo(Path.Combine("inst", "Recorder", "State", "Cow_head.json")));
        Assert.That(RecorderRules.ConfigPath("inst"), Is.EqualTo(Path.Combine("inst", "Recorder", "Global.json")));
    }

    [TestCase(null, null)]
    [TestCase("", "")]
    [TestCase(null, "100-a")]
    public void MissingJournalIdNeverContinues(string saved, string current)
    {
        var state = Session();
        state.JournalId = saved;
        Assert.That(RecorderRules.DecideResume(state, current, T0.AddMinutes(1)), Is.EqualTo(RecorderResumeDecision.NewJournal));
    }
}
