using System;
using System.Collections.Generic;

using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
[NonParallelizable]
public class EventLogSplitTests
{
    private const string Token = "secret";

    [TearDown]
    public void TearDown() => AgentBridgeService.SetJournal(null);

    [Test]
    public void BridgeServesEventsPublishedBeforeItExisted()
    {
        var journal = new EventJournal(100);
        journal.Publish(EventJournal.KindChat, "before one");
        journal.Publish(EventJournal.KindPlugin, "before two");

        var processor = new AgentRequestProcessor(new Host(), new AgentPolicy { Mode = AgentMode.ReadOnly }, journal, new AgentHandlerRegistry(), Token);
        var response = (Dictionary<string, object>)MiniJson.Parse(processor.Process($"{{\"token\":\"{Token}\",\"cmd\":\"events\",\"since\":0}}"));
        var events = (List<object>)((Dictionary<string, object>)response["result"])["events"];

        Assert.That(events.Count, Is.EqualTo(2));
        Assert.That(((Dictionary<string, object>)events[0])["text"], Is.EqualTo("before one"));
        Assert.That(((Dictionary<string, object>)events[1])["text"], Is.EqualTo("before two"));
    }

    [Test]
    public void TurningTheBridgeOffLeavesTheJournal()
    {
        var journal = new EventJournal(100, "42-2026-11-20T06:00:00");
        AgentBridgeService.SetJournal(journal);
        journal.Publish(EventJournal.KindChat, "kept");
        var policy = new AgentPolicy { Mode = AgentMode.ReadOnly };
        var processor = new AgentRequestProcessor(new Host(), policy, journal, new AgentHandlerRegistry(), Token);

        policy.Mode = AgentMode.Off;
        var response = (Dictionary<string, object>)MiniJson.Parse(processor.Process($"{{\"token\":\"{Token}\",\"cmd\":\"ping\"}}"));

        Assert.That(response["ok"], Is.False);
        Assert.That(AgentBridgeService.IsEnabled, Is.True);
        Assert.That(AgentBridgeService.Journal, Is.SameAs(journal));
        Assert.That(AgentBridgeService.JournalId, Is.EqualTo("42-2026-11-20T06:00:00"));
        Assert.That(journal.GetSince(0, null, 10)[0].Text, Is.EqualTo("kept"));
    }

    private class Host : IAgentBridgeHost
    {
        public object InvokeOnGameThread(Func<object> func) => func();

        public void Exec(string commandText)
        {
        }

        public void Reload()
        {
        }

        public Dictionary<string, object> Describe() => new();
    }
}
