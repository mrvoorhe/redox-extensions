using System;
using System.IO;

using NUnit.Framework;

using RedoxExtensions.Core;
using RedoxExtensions.Tests.TestUtilities;

namespace RedoxExtensions.Tests;

[TestFixture]
public class SwitchHandoffTests : TemporaryDirectoryPerTestTestSuite
{
    private DateTime _now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    private SwitchHandoff Create() => new(this.TestTemporaryDirectory, () => this._now);

    [Test]
    public void SavedCommandIsTakenOnce()
    {
        var handoff = this.Create();
        handoff.Save("Server", "acct", "Bob Smith", "/re startmeta daily");

        var first = handoff.TryTake("Server", "acct", "bob smith", SwitchHandoff.DefaultMaxAge, out var command);
        var second = handoff.TryTake("Server", "acct", "Bob Smith", SwitchHandoff.DefaultMaxAge, out _);

        Assert.That(first, Is.True);
        Assert.That(command, Is.EqualTo("/re startmeta daily"));
        Assert.That(second, Is.False);
    }

    [Test]
    public void OtherCharacterGetsNothing()
    {
        var handoff = this.Create();
        handoff.Save("Server", "acct", "Bob", "/re help");

        Assert.That(handoff.TryTake("Server", "acct", "Woodford", SwitchHandoff.DefaultMaxAge, out _), Is.False);
        Assert.That(handoff.TryTake("Server", "acct", "Bob", SwitchHandoff.DefaultMaxAge, out _), Is.True);
    }

    [Test]
    public void OldCommandIsDiscarded()
    {
        var handoff = this.Create();
        handoff.Save("Server", "acct", "Bob", "/re help");
        this._now += TimeSpan.FromHours(1);

        Assert.That(handoff.TryTake("Server", "acct", "Bob", SwitchHandoff.DefaultMaxAge, out var command), Is.False);
        Assert.That(command, Is.Null);
        Assert.That(File.Exists(handoff.FilePathFor("Server", "acct", "Bob")), Is.False);
    }

    [Test]
    public void ClearRemovesASavedCommand()
    {
        var handoff = this.Create();
        handoff.Save("Server", "acct", "Bob", "/re help");
        handoff.Clear("Server", "acct", "Bob");

        Assert.That(handoff.TryTake("Server", "acct", "Bob", SwitchHandoff.DefaultMaxAge, out _), Is.False);
    }

    [Test]
    public void InvalidFileNameCharactersAreReplaced()
    {
        var path = this.Create().FilePathFor("Server", "acct", "Bob:Smith");

        Assert.That(Path.GetFileName(path), Is.EqualTo("server-acct-bob_smith.txt"));
    }
}
