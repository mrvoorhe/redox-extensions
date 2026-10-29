using System;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class AgentRequestProcessorTests
{
    private const string Token = "secret";

    private FakeHost _host;
    private AgentPolicy _policy;
    private EventJournal _journal;
    private AgentHandlerRegistry _handlers;
    private AgentRequestProcessor _processor;

    [SetUp]
    public void SetUp()
    {
        this._host = new FakeHost();
        this._policy = new AgentPolicy { Mode = AgentMode.ReadWrite };
        this._journal = new EventJournal(100);
        this._handlers = new AgentHandlerRegistry();
        this._processor = new AgentRequestProcessor(this._host, this._policy, this._journal, this._handlers, Token);
    }

    [Test]
    public void RejectsBadToken()
    {
        var response = this.Send("{\"token\":\"wrong\",\"cmd\":\"ping\"}");

        Assert.That(response["ok"], Is.False);
        Assert.That(response["error"], Is.EqualTo("Bad token"));
    }

    [Test]
    public void RejectsMalformedJson()
    {
        var response = this.Send("{not json");

        Assert.That(response["ok"], Is.False);
        Assert.That((string)response["error"], Does.StartWith("Bad JSON"));
    }

    [Test]
    public void RejectsEverythingWhenOff()
    {
        this._policy.Mode = AgentMode.Off;

        var response = this.Request("ping");

        Assert.That(response["ok"], Is.False);
        Assert.That(response["error"], Is.EqualTo("Agent bridge is off"));
    }

    [Test]
    public void PingDescribesInstanceAndEchoesId()
    {
        this._handlers.Register("state", _ => null);

        var response = this.Send("{\"token\":\"secret\",\"cmd\":\"ping\",\"id\":7}");
        var result = (Dictionary<string, object>)response["result"];

        Assert.That(response["ok"], Is.True);
        Assert.That(response["id"], Is.EqualTo(7.0));
        Assert.That(result["character"], Is.EqualTo("Tester"));
        Assert.That(result["mode"], Is.EqualTo("rw"));
        Assert.That((List<object>)result["handlers"], Is.EqualTo(new object[] { "state" }));
    }

    [Test]
    public void ExecDispatchesOnGameThreadAndReturnsSeq()
    {
        this._journal.Publish("chat", "earlier");

        var response = this.Request("exec", "\"text\":\"/vt start\"");
        var result = (Dictionary<string, object>)response["result"];

        Assert.That(response["ok"], Is.True);
        Assert.That(this._host.Executed, Is.EqualTo(new[] { "/vt start" }));
        Assert.That(this._host.GameThreadCalls, Is.EqualTo(1));
        Assert.That(result["seq"], Is.EqualTo(2.0));
        Assert.That(this._journal.GetSince(2, new[] { EventJournal.KindCommand }, 10)[0].Text, Is.EqualTo("/vt start"));
    }

    [Test]
    public void ExecIsDeniedByPolicy()
    {
        var response = this.Request("exec", "\"text\":\"/ub quit\"");

        Assert.That(response["ok"], Is.False);
        Assert.That(this._host.Executed, Is.Empty);
    }

    [Test]
    public void ReloadCallsHost()
    {
        var response = this.Request("reload");

        Assert.That(response["ok"], Is.True);
        Assert.That(this._host.ReloadCount, Is.EqualTo(1));
    }

    [Test]
    public void EventsReturnsPublishedEventsAndNextCursor()
    {
        this._journal.Publish("chat", "one");
        this._journal.Publish("debug", "two");

        var result = (Dictionary<string, object>)this.Request("events", "\"since\":0,\"kinds\":[\"chat\"]")["result"];
        var events = (List<object>)result["events"];

        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(((Dictionary<string, object>)events[0])["text"], Is.EqualTo("one"));
        Assert.That(result["next"], Is.EqualTo(2.0));
        Assert.That(result["truncated"], Is.False);
    }

    [Test]
    public void EventsReportsTruncationWhenCursorFellOffBuffer()
    {
        var journal = new EventJournal(2);
        var processor = new AgentRequestProcessor(this._host, this._policy, journal, this._handlers, Token);
        for (var i = 0; i < 5; i++)
            journal.Publish("chat", i.ToString());

        var response = (Dictionary<string, object>)MiniJson.Parse(processor.Process("{\"token\":\"secret\",\"cmd\":\"events\",\"since\":1}"));

        Assert.That(((Dictionary<string, object>)response["result"])["truncated"], Is.True);
    }

    [Test]
    public void WaitMatchesEventPublishedAfterExec()
    {
        var seq = (double)((Dictionary<string, object>)this.Request("exec", "\"text\":\"/re help\"")["result"])["seq"];

        var publisher = new Thread(() =>
        {
            Thread.Sleep(50);
            this._journal.Publish("chat", "[RT] Commands: ...");
        });
        publisher.Start();

        var result = (Dictionary<string, object>)this.Request("wait", $"\"since\":{seq},\"kinds\":[\"chat\"],\"regex\":\"commands:\",\"timeoutMs\":5000")["result"];
        publisher.Join();

        Assert.That(result["matched"], Is.True);
        Assert.That(((Dictionary<string, object>)result["event"])["text"], Is.EqualTo("[RT] Commands: ..."));
    }

    [Test]
    public void WaitReportsNoMatchOnTimeout()
    {
        var result = (Dictionary<string, object>)this.Request("wait", "\"regex\":\"never\",\"timeoutMs\":20")["result"];

        Assert.That(result["matched"], Is.False);
    }

    [Test]
    public void WaitRejectsBadRegex()
    {
        var response = this.Request("wait", "\"regex\":\"(\",\"timeoutMs\":20");

        Assert.That(response["ok"], Is.False);
    }

    [Test]
    public void DispatchesRegisteredHandlerOnGameThread()
    {
        this._handlers.Register("state", request => new Dictionary<string, object> { ["echo"] = MiniJson.GetString(request, "arg") });

        var response = this.Request("STATE", "\"arg\":\"hi\"");

        Assert.That(response["ok"], Is.True);
        Assert.That(((Dictionary<string, object>)response["result"])["echo"], Is.EqualTo("hi"));
        Assert.That(this._host.GameThreadCalls, Is.EqualTo(1));
    }

    [Test]
    public void UnknownCommandListsAvailableCommands()
    {
        var response = this.Request("state");

        Assert.That(response["ok"], Is.False);
        Assert.That((string)response["error"], Does.Contain("Unknown cmd 'state'").And.Contain("plugin is unloaded"));
    }

    [Test]
    public void HandlerExceptionsBecomeErrors()
    {
        this._handlers.Register("boom", _ => throw new InvalidOperationException("kaboom"));

        var response = this.Request("boom");

        Assert.That(response["ok"], Is.False);
        Assert.That((string)response["error"], Does.Contain("kaboom"));
    }

    private Dictionary<string, object> Request(string cmd, string extraFields = null)
    {
        var extra = string.IsNullOrEmpty(extraFields) ? string.Empty : "," + extraFields;
        return this.Send($"{{\"token\":\"{Token}\",\"cmd\":\"{cmd}\"{extra}}}");
    }

    private Dictionary<string, object> Send(string line) => (Dictionary<string, object>)MiniJson.Parse(this._processor.Process(line));

    private class FakeHost : IAgentBridgeHost
    {
        public readonly List<string> Executed = new();
        public int ReloadCount;
        public int GameThreadCalls;

        public object InvokeOnGameThread(Func<object> func)
        {
            this.GameThreadCalls++;
            return func();
        }

        public void Exec(string commandText) => this.Executed.Add(commandText);

        public void Reload() => this.ReloadCount++;

        public Dictionary<string, object> Describe() => new() { ["pid"] = 1234, ["character"] = "Tester" };
    }
}
