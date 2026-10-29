using System;

using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class AgentPolicyTests
{
    [TestCase("on", AgentMode.ReadOnly)]
    [TestCase("ro", AgentMode.ReadOnly)]
    [TestCase("RW", AgentMode.ReadWrite)]
    [TestCase(" full ", AgentMode.Full)]
    [TestCase("off", AgentMode.Off)]
    public void ParsesModes(string text, AgentMode expected)
    {
        Assert.That(AgentPolicy.TryParseMode(text, out var mode), Is.True);
        Assert.That(mode, Is.EqualTo(expected));
    }

    [Test]
    public void RejectsUnknownMode()
    {
        Assert.That(AgentPolicy.TryParseMode("sure", out _), Is.False);
    }

    [Test]
    public void OffDeniesEverything()
    {
        var policy = new AgentPolicy { Mode = AgentMode.Off };

        Assert.That(policy.CanRead(out _), Is.False);
        Assert.That(policy.CanExec("/vt start", out _), Is.False);
        Assert.That(policy.CanReload(out _), Is.False);
    }

    [Test]
    public void ReadOnlyAllowsReadsOnly()
    {
        var policy = new AgentPolicy { Mode = AgentMode.ReadOnly };

        Assert.That(policy.CanRead(out _), Is.True);
        Assert.That(policy.CanExec("/vt start", out var error), Is.False);
        Assert.That(error, Does.Contain("read-only"));
        Assert.That(policy.CanReload(out _), Is.False);
    }

    [Test]
    public void ReadWriteAllowsNormalCommandsAndReload()
    {
        var policy = new AgentPolicy { Mode = AgentMode.ReadWrite };

        Assert.That(policy.CanExec("/vt start", out _), Is.True);
        Assert.That(policy.CanExec("/re recordroute start", out _), Is.True);
        Assert.That(policy.CanReload(out _), Is.True);
    }

    [TestCase("/ub give Hero Token to Zero Cool")]
    [TestCase("/UB   GIVEP 10 Prismatic to Zero")]
    [TestCase("/ub ig muledItems.utl to Zero Cool")]
    [TestCase("/mt drop Pyreal")]
    [TestCase("/mt trade accept")]
    [TestCase("/b t p 1m \"MyAlt\"")]
    [TestCase("/b w p 250m")]
    [TestCase("/re give Bob Pyreal")]
    [TestCase("/enl")]
    [TestCase("/ub quit")]
    [TestCase("/ub delay 5000 /ub give Hero Token to Zero Cool")]
    public void ReadWriteDeniesUnsafeCommands(string command)
    {
        var policy = new AgentPolicy { Mode = AgentMode.ReadWrite };

        Assert.That(policy.CanExec(command, out var error), Is.False);
        Assert.That(error, Does.Contain("full"));
    }

    [Test]
    public void FullAllowsUnsafeCommands()
    {
        var policy = new AgentPolicy { Mode = AgentMode.Full };

        Assert.That(policy.CanExec("/ub give Hero Token to Zero Cool", out _), Is.True);
    }

    [TestCase("/ub delay 5000 /say hi")]
    [TestCase("/b")]
    [TestCase("/top bank")]
    [TestCase("/mt face 180")]
    public void SafeCommandsAreNotFlagged(string command)
    {
        Assert.That(AgentPolicy.IsUnsafeCommand(command), Is.False);
    }

    [TestCase("/re reload")]
    [TestCase("/re unload")]
    [TestCase("/rew load")]
    [TestCase("/re agent full")]
    [TestCase("/ub delay 100 /re reload")]
    public void LifecycleCommandsAreDeniedEvenInFullMode(string command)
    {
        var policy = new AgentPolicy { Mode = AgentMode.Full };

        Assert.That(policy.CanExec(command, out var error), Is.False);
        Assert.That(error, Does.Contain("reload request"));
    }

    [Test]
    public void LifecycleCheckDoesNotMatchLongerVerbs()
    {
        Assert.That(AgentPolicy.IsPluginLifecycleCommand("/re reloadsettings"), Is.False);
    }

    [Test]
    public void RateLimitsExecsWithinWindow()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var policy = new AgentPolicy(() => now, maxExecsPerWindow: 2, rateWindow: TimeSpan.FromSeconds(10)) { Mode = AgentMode.ReadWrite };

        Assert.That(policy.CanExec("/vt start", out _), Is.True);
        Assert.That(policy.CanExec("/vt stop", out _), Is.True);
        Assert.That(policy.CanExec("/vt start", out var error), Is.False);
        Assert.That(error, Does.Contain("Rate limited"));

        now = now.AddSeconds(10);
        Assert.That(policy.CanExec("/vt start", out _), Is.True);
    }

    [Test]
    public void DeniedExecsDoNotConsumeRate()
    {
        var policy = new AgentPolicy(maxExecsPerWindow: 1) { Mode = AgentMode.ReadWrite };

        Assert.That(policy.CanExec("/ub quit", out _), Is.False);
        Assert.That(policy.CanExec("/vt start", out _), Is.True);
    }
}
