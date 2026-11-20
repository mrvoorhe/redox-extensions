using System;
using System.Collections.Generic;

using NUnit.Framework;

using RedoxExtensions.Commands;
using RedoxExtensions.Recording;

namespace RedoxExtensions.Tests.Recording;

[TestFixture]
public class RecorderCommandTests
{
    private sealed class FakeRecorder : IRecorderControl
    {
        public readonly List<string> Calls = new();

        public void Start(TimeSpan timeout) => this.Calls.Add("start " + timeout);

        public void Stop(TimeSpan? delay) => this.Calls.Add("stop " + (delay.HasValue ? delay.Value.ToString() : "now"));

        public void Status() => this.Calls.Add("status");
    }

    private readonly FakeRecorder _recorder = new();
    private readonly List<string> _chat = new();

    [SetUp]
    public void SetUp()
    {
        this._recorder.Calls.Clear();
        this._chat.Clear();
    }

    private void Run(string text)
    {
        Assert.That(CommandHelpers.TryParse(text, out var command), Is.True, text);
        RecorderCommand.Handle(command.Arguments, this._recorder, this._chat.Add);
    }

    [TestCase("/re rec start", "start 05:00:00")]
    [TestCase("/re recorder start 2h", "start 02:00:00")]
    [TestCase("/re rec start 4.5h", "start 04:30:00")]
    [TestCase("/re rec stop", "stop now")]
    [TestCase("/re rec stop 15m", "stop 00:15:00")]
    [TestCase("/re rec STOP 30s", "stop 00:00:30")]
    [TestCase("/re rec status", "status")]
    [TestCase("/re rec", "status")]
    public void ValidCommands_ReachTheRecorder(string text, string expected)
    {
        this.Run(text);

        Assert.That(this._recorder.Calls, Is.EqualTo(new[] { expected }));
        Assert.That(this._chat, Is.Empty);
    }

    [TestCase("/re rec start 5")]
    [TestCase("/re rec start 0m")]
    [TestCase("/re rec start 5m extra")]
    [TestCase("/re rec stop 2d")]
    [TestCase("/re rec stop 5m 5m")]
    [TestCase("/re rec status now")]
    [TestCase("/re rec begin")]
    public void BadArguments_PrintUsageAndDoNothing(string text)
    {
        this.Run(text);

        Assert.That(this._recorder.Calls, Is.Empty);
        Assert.That(this._chat, Is.EqualTo(new[] { RecorderCommand.Usage }));
    }
}
