using System;
using System.IO;

using NUnit.Framework;

using RedoxExtensions.Scheduling;
using RedoxExtensions.Tests.TestUtilities;

namespace RedoxExtensions.Tests;

[TestFixture]
public class SchedulingTests : TemporaryDirectoryPerTestTestSuite
{
    private static readonly DateTime SixAm = new(2026, 11, 9, 6, 0, 0);

    private static ScheduledCommand Entry(string repeat, DateTime at, DateTime? lastRun = null, int grace = 15) => new()
    {
        Id = "abc123",
        Command = "/re help",
        At = at,
        Repeat = repeat,
        GraceMinutes = grace,
        LastRun = lastRun,
    };

    [Test]
    public void OnceIsNotDueBeforeItsTime()
    {
        var entry = Entry("once", SixAm);
        Assert.That(ScheduleRules.IsDue(entry, SixAm.AddSeconds(-1)), Is.False);
        Assert.That(ScheduleRules.NextRun(entry, SixAm.AddMinutes(-10)), Is.EqualTo(SixAm));
        Assert.That(ScheduleRules.Status(entry, SixAm.AddMinutes(-10)), Is.EqualTo("waiting"));
    }

    [Test]
    public void OnceIsDueAtItsTimeAndWithinGrace()
    {
        var entry = Entry("once", SixAm);
        Assert.That(ScheduleRules.IsDue(entry, SixAm), Is.True);
        Assert.That(ScheduleRules.IsDue(entry, SixAm.AddMinutes(15)), Is.True);
    }

    [Test]
    public void OnceIsMissedPastGrace()
    {
        var entry = Entry("once", SixAm);
        var now = SixAm.AddMinutes(15).AddSeconds(1);
        Assert.That(ScheduleRules.IsDue(entry, now), Is.False);
        Assert.That(ScheduleRules.NextRun(entry, now), Is.Null);
        Assert.That(ScheduleRules.Status(entry, now), Is.EqualTo("missed"));
    }

    [Test]
    public void OnceIsDoneAfterItRan()
    {
        var entry = Entry("once", SixAm, lastRun: SixAm.AddSeconds(3));
        Assert.That(ScheduleRules.IsDue(entry, SixAm.AddMinutes(1)), Is.False);
        Assert.That(ScheduleRules.Status(entry, SixAm.AddMinutes(1)), Is.EqualTo("done"));
    }

    [Test]
    public void DisabledAndInvalidEntriesAreNeverDue()
    {
        var disabled = Entry("once", SixAm);
        disabled.Enabled = false;
        Assert.That(ScheduleRules.IsDue(disabled, SixAm), Is.False);
        Assert.That(ScheduleRules.IsDue(Entry("weekly", SixAm), SixAm), Is.False);
    }

    [Test]
    public void DailyRunsEachDayAtTheSameTime()
    {
        var entry = Entry("daily", SixAm, lastRun: SixAm.AddSeconds(2));
        var nextDay = SixAm.AddDays(1);

        Assert.That(ScheduleRules.IsDue(entry, SixAm.AddHours(12)), Is.False);
        Assert.That(ScheduleRules.NextRun(entry, SixAm.AddHours(12)), Is.EqualTo(nextDay));
        Assert.That(ScheduleRules.IsDue(entry, nextDay.AddSeconds(-1)), Is.False);
        Assert.That(ScheduleRules.IsDue(entry, nextDay), Is.True);
    }

    [Test]
    public void DailyIsNotDueTwiceTheSameDay()
    {
        var entry = Entry("daily", SixAm.AddDays(-5), lastRun: SixAm.AddMinutes(1));
        Assert.That(ScheduleRules.IsDue(entry, SixAm.AddMinutes(5)), Is.False);
    }

    [Test]
    public void DailyAcrossMidnightUsesYesterdaysOccurrence()
    {
        var lateNight = new DateTime(2026, 11, 9, 23, 55, 0);
        var entry = Entry("daily", lateNight.AddDays(-3), lastRun: lateNight.AddDays(-1));

        Assert.That(ScheduleRules.IsDue(entry, lateNight.AddMinutes(10)), Is.True);
        Assert.That(ScheduleRules.LatestOccurrence(entry, lateNight.AddMinutes(10)), Is.EqualTo(lateNight));
    }

    [Test]
    public void DailyMissedPastGraceWaitsForTheNextDay()
    {
        var entry = Entry("daily", SixAm.AddDays(-2), lastRun: SixAm.AddDays(-2));
        var now = SixAm.AddHours(1);

        Assert.That(ScheduleRules.IsDue(entry, now), Is.False);
        Assert.That(ScheduleRules.NextRun(entry, now), Is.EqualTo(SixAm.AddDays(1)));
        Assert.That(ScheduleRules.Status(entry, now), Is.EqualTo("waiting"));
    }

    [Test]
    public void DailyDoesNotRunBeforeItsFirstDate()
    {
        var entry = Entry("daily", SixAm.AddDays(2));
        Assert.That(ScheduleRules.IsDue(entry, SixAm.AddMinutes(1)), Is.False);
        Assert.That(ScheduleRules.NextRun(entry, SixAm), Is.EqualTo(SixAm.AddDays(2)));
    }

    [Test]
    public void FilePathUsesTheCharacterName()
    {
        Assert.That(ScheduleRules.FilePath(@"C:\RE", "Some Char"), Is.EqualTo(Path.Combine(@"C:\RE", "ScheduledCommands", "Some Char.json")));
    }

    [Test]
    public void SaveThenLoadRoundTrips()
    {
        var path = Path.Combine(this.TestTemporaryDirectory, "ScheduledCommands", "Char.json");
        var data = new ScheduleFileData();
        data.Entries.Add(Entry("daily", SixAm, lastRun: DateTime.Now));
        data.Entries[0].LastRun = new DateTime(2026, 11, 9, 6, 0, 2, DateTimeKind.Local);

        ScheduleFile.Save(path, data);
        ScheduleFile.Save(path, data);

        var text = File.ReadAllText(path);
        Assert.That(text, Does.Contain("\"at\": \"2026-11-09T06:00:00\""));
        Assert.That(text, Does.Contain("\"lastRun\": \"2026-11-09T06:00:02\""));

        var loaded = ScheduleFile.Load(path);
        Assert.That(loaded.Entries, Has.Count.EqualTo(1));
        Assert.That(loaded.Entries[0].At, Is.EqualTo(SixAm));
        Assert.That(loaded.Entries[0].Repeat, Is.EqualTo("daily"));
        Assert.That(loaded.Entries[0].LastRun, Is.EqualTo(new DateTime(2026, 11, 9, 6, 0, 2)));
        Assert.That(File.Exists(path + ".tmp"), Is.False);
    }

    [Test]
    public void LoadOfAMissingFileIsEmpty()
    {
        Assert.That(ScheduleFile.Load(Path.Combine(this.TestTemporaryDirectory, "none.json")).Entries, Is.Empty);
    }
}
