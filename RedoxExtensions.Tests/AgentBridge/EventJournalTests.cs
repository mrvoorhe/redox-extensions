using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class EventJournalTests
{
    [Test]
    public void GetSinceReturnsEventsInOrderAndFiltersByKind()
    {
        var journal = new EventJournal(10);
        journal.Publish("chat", "one");
        journal.Publish("debug", "two");
        journal.Publish("chat", "three");

        Assert.That(journal.GetSince(0, null, 100).Select(e => e.Text), Is.EqualTo(new[] { "one", "two", "three" }));
        Assert.That(journal.GetSince(0, new[] { "chat" }, 100).Select(e => e.Text), Is.EqualTo(new[] { "one", "three" }));
        Assert.That(journal.GetSince(3, null, 100).Select(e => e.Text), Is.EqualTo(new[] { "three" }));
        Assert.That(journal.GetSince(0, null, 2).Select(e => e.Text), Is.EqualTo(new[] { "one", "two" }));
    }

    [Test]
    public void OldEventsAreOverwrittenWhenFull()
    {
        var journal = new EventJournal(3);
        for (var i = 1; i <= 5; i++)
            journal.Publish("chat", i.ToString());

        Assert.That(journal.GetSince(0, null, 100).Select(e => e.Seq), Is.EqualTo(new long[] { 3, 4, 5 }));
        Assert.That(journal.NextSeq, Is.EqualTo(6));
    }

    [Test]
    public void WaitForReturnsAlreadyPublishedMatch()
    {
        var journal = new EventJournal(10);
        var since = journal.NextSeq;
        journal.Publish("chat", "Route saved to foo.nav");

        var evt = journal.WaitFor(since, null, new Regex("route saved", RegexOptions.IgnoreCase), TimeSpan.Zero);

        Assert.That(evt, Is.Not.Null);
        Assert.That(evt.Text, Is.EqualTo("Route saved to foo.nav"));
    }

    [Test]
    public void WaitForWakesWhenMatchIsPublished()
    {
        var journal = new EventJournal(10);
        var since = journal.NextSeq;

        var publisher = new Thread(() =>
        {
            Thread.Sleep(50);
            journal.Publish("chat", "not it");
            Thread.Sleep(50);
            journal.Publish("chat", "Reload Complete");
        });
        publisher.Start();

        var evt = journal.WaitFor(since, new[] { "chat" }, new Regex("reload complete", RegexOptions.IgnoreCase), TimeSpan.FromSeconds(5));
        publisher.Join();

        Assert.That(evt?.Text, Is.EqualTo("Reload Complete"));
    }

    [Test]
    public void WaitForTimesOutWithoutMatch()
    {
        var journal = new EventJournal(10);
        journal.Publish("chat", "something else");

        var evt = journal.WaitFor(0, null, new Regex("never"), TimeSpan.FromMilliseconds(50));

        Assert.That(evt, Is.Null);
    }

    [Test]
    public void WaitForIgnoresEventsBeforeSince()
    {
        var journal = new EventJournal(10);
        journal.Publish("chat", "match");
        var since = journal.NextSeq;

        Assert.That(journal.WaitFor(since, null, new Regex("match"), TimeSpan.FromMilliseconds(20)), Is.Null);
    }
}
