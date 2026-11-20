using System;
using System.IO;

using NUnit.Framework;

using RedoxExtensions.Recording;
using RedoxExtensions.Tests.TestUtilities;

namespace RedoxExtensions.Tests.Recording;

[TestFixture]
public class RecorderFileTests : TemporaryDirectoryPerTestTestSuite
{
    private string StatePath => RecorderRules.StatePath(TestTemporaryDirectory, "Cowhead");
    private string ConfigPath => RecorderRules.ConfigPath(TestTemporaryDirectory);

    private static RecorderState Sample() => new()
    {
        File = @"E:\Rec\2026-11-21\Cowhead.jsonl",
        StartedAt = new DateTime(2026, 11, 21, 1, 0, 12),
        TimeoutAt = new DateTime(2026, 11, 21, 6, 0, 12),
        StopAt = new DateTime(2026, 11, 21, 4, 22, 40),
        JournalId = "18244-2026-11-21T00:58:03",
        LastSeq = 41230,
    };

    [Test]
    public void StateRoundTrip()
    {
        RecorderFile.SaveState(StatePath, Sample());
        var loaded = RecorderFile.LoadState(StatePath, out var warning);

        Assert.That(warning, Is.Null);
        Assert.That(loaded.File, Is.EqualTo(Sample().File));
        Assert.That(loaded.StartedAt, Is.EqualTo(Sample().StartedAt));
        Assert.That(loaded.TimeoutAt, Is.EqualTo(Sample().TimeoutAt));
        Assert.That(loaded.StopAt, Is.EqualTo(Sample().StopAt));
        Assert.That(loaded.JournalId, Is.EqualTo(Sample().JournalId));
        Assert.That(loaded.LastSeq, Is.EqualTo(41230));
    }

    [Test]
    public void StateIsWrittenWithLocalTimesAndCamelCase()
    {
        RecorderFile.SaveState(StatePath, Sample());
        var text = File.ReadAllText(StatePath);
        Assert.That(text, Does.Contain("\"startedAt\": \"2026-11-21T01:00:12\""));
        Assert.That(text, Does.Contain("\"lastSeq\": 41230"));
    }

    [Test]
    public void NoStopAtRoundTripsAsNull()
    {
        var state = Sample();
        state.StopAt = null;
        RecorderFile.SaveState(StatePath, state);
        Assert.That(RecorderFile.LoadState(StatePath, out _).StopAt, Is.Null);
    }

    [Test]
    public void SaveLeavesNoTempFileAndOverwrites()
    {
        RecorderFile.SaveState(StatePath, Sample());
        var second = Sample();
        second.LastSeq = 7;
        RecorderFile.SaveState(StatePath, second);

        Assert.That(Directory.GetFiles(Path.GetDirectoryName(StatePath)), Is.EqualTo(new[] { StatePath }));
        Assert.That(RecorderFile.LoadState(StatePath, out _).LastSeq, Is.EqualTo(7));
    }

    [Test]
    public void DeleteRemovesStateAndIgnoresMissing()
    {
        RecorderFile.SaveState(StatePath, Sample());
        RecorderFile.DeleteState(StatePath);
        Assert.That(File.Exists(StatePath), Is.False);
        Assert.DoesNotThrow(() => RecorderFile.DeleteState(StatePath));
    }

    [Test]
    public void MissingStateIsNullWithoutWarning()
    {
        Assert.That(RecorderFile.LoadState(StatePath, out var warning), Is.Null);
        Assert.That(warning, Is.Null);
    }

    [TestCase("")]
    [TestCase("   \n")]
    [TestCase("{ not json")]
    [TestCase("null")]
    [TestCase("{\"startedAt\": \"yesterday\"}")]
    public void EmptyOrCorruptStateIsIgnoredWithWarning(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
        File.WriteAllText(StatePath, content);

        RecorderState loaded = null;
        Assert.DoesNotThrow(() => loaded = RecorderFile.LoadState(StatePath, out _));
        Assert.That(loaded, Is.Null);
        RecorderFile.LoadState(StatePath, out var warning);
        Assert.That(warning, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ConfigMissingFallsBackWithWarning()
    {
        var result = RecorderFile.LoadConfig(ConfigPath, TestTemporaryDirectory);
        Assert.That(result.UsedFallback, Is.True);
        Assert.That(result.Warning, Is.Not.Null);
        Assert.That(result.Config.OutputRoot, Is.EqualTo(Path.Combine(TestTemporaryDirectory, "Recordings")));
        Assert.That(result.Config.AfterStop, Is.Null);
    }

    [TestCase("{}")]
    [TestCase("{\"outputRoot\": \"\"}")]
    [TestCase("{\"outputRoot\": \"  \"}")]
    [TestCase("")]
    [TestCase("{ broken")]
    public void ConfigEmptyRootOrBadFileFallsBackWithWarning(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
        File.WriteAllText(ConfigPath, content);

        var result = RecorderFile.LoadConfig(ConfigPath, TestTemporaryDirectory);
        Assert.That(result.UsedFallback, Is.True);
        Assert.That(result.Warning, Is.Not.Null);
        Assert.That(result.Config.OutputRoot, Is.EqualTo(Path.Combine(TestTemporaryDirectory, "Recordings")));
    }

    [Test]
    public void ConfigLoadsRootAndAfterStop()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
        File.WriteAllText(ConfigPath, "{\"outputRoot\": \"E:\\\\Meta Analysis\", \"afterStop\": {\"program\": \"py\", \"args\": \"a.py {file}\"}}");

        var result = RecorderFile.LoadConfig(ConfigPath, TestTemporaryDirectory);
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(result.Warning, Is.Null);
        Assert.That(result.Config.OutputRoot, Is.EqualTo(@"E:\Meta Analysis"));
        Assert.That(result.Config.AfterStop.Program, Is.EqualTo("py"));
        Assert.That(result.Config.AfterStop.Args, Is.EqualTo("a.py {file}"));
    }

    [TestCase("{\"outputRoot\": \"X\"}")]
    [TestCase("{\"outputRoot\": \"X\", \"afterStop\": {}}")]
    [TestCase("{\"outputRoot\": \"X\", \"afterStop\": {\"program\": \"\", \"args\": \"a\"}}")]
    public void ConfigAfterStopAbsentMeansNull(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
        File.WriteAllText(ConfigPath, content);

        var result = RecorderFile.LoadConfig(ConfigPath, TestTemporaryDirectory);
        Assert.That(result.UsedFallback, Is.False);
        Assert.That(result.Config.AfterStop, Is.Null);
    }
}
