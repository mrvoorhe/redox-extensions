using System;
using System.Diagnostics;
using System.Threading;

using NUnit.Framework;

using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Tests;

/// <summary>
/// REPlugin.Instance is null in these tests, as it is in game once the plugin has shut down (logoff, /re reload,
/// rectl dev).  Before the fix any action run there threw a NullReferenceException out of the dispatcher thread,
/// which terminated the process (the test host here, the game client in game).
/// </summary>
[TestFixture]
public class BackgroundDispatcherTests
{
    private const int WaitTimeoutInMilliseconds = 5000;

    [Test]
    public void RunsQueuedActionsWithoutAPluginInstance()
    {
        using var dispatcher = new BackgroundDispatcher();
        using var done = new CountdownEvent(3);

        for (int i = 0; i < 3; i++)
        {
            dispatcher.EnqueueAction(() => done.Signal());
        }

        Assert.That(done.Wait(WaitTimeoutInMilliseconds), Is.True, "all queued actions ran");
    }

    [Test]
    public void AnActionThatThrowsDoesNotKillTheDispatcher()
    {
        using var dispatcher = new BackgroundDispatcher();
        using var ranAfterThrow = new ManualResetEvent(false);

        dispatcher.EnqueueAction(() => throw new InvalidOperationException("test: action failed"));
        dispatcher.EnqueueAction(() => ranAfterThrow.Set());

        Assert.That(ranAfterThrow.WaitOne(WaitTimeoutInMilliseconds), Is.True, "the action after the failing one ran");
        Assert.That(dispatcher.IsThreadExited, Is.False);
    }

    [Test]
    public void DisposeStopsTheThreadAndCanBeCalledTwice()
    {
        var dispatcher = new BackgroundDispatcher();
        using var ran = new ManualResetEvent(false);
        dispatcher.EnqueueAction(() => ran.Set());
        Assert.That(ran.WaitOne(WaitTimeoutInMilliseconds), Is.True);

        dispatcher.Dispose();

        Assert.That(dispatcher.IsThreadExited, Is.True);
        Assert.That(() => dispatcher.Dispose(), Throws.Nothing);
        Assert.That(() => dispatcher.Stop(), Throws.Nothing);
    }

    [Test]
    public void QueueingAfterDisposeIsDropped()
    {
        var dispatcher = new BackgroundDispatcher();
        dispatcher.Dispose();

        bool ran = false;
        Assert.That(() => dispatcher.EnqueueAction(() => ran = true), Throws.Nothing);
        Assert.That(dispatcher.QueueCount, Is.EqualTo(0));
        Assert.That(ran, Is.False);
    }

    [Test]
    public void DisposeDoesNotWaitForeverOnABlockedActionAndDropsQueuedWork()
    {
        var dispatcher = new BackgroundDispatcher();
        using var started = new ManualResetEvent(false);
        using var release = new ManualResetEvent(false);
        bool queuedRan = false;

        dispatcher.EnqueueAction(() =>
        {
            started.Set();
            release.WaitOne(WaitTimeoutInMilliseconds);
        });
        dispatcher.EnqueueAction(() => queuedRan = true);
        Assert.That(started.WaitOne(WaitTimeoutInMilliseconds), Is.True);

        var stopwatch = Stopwatch.StartNew();
        dispatcher.Dispose();
        stopwatch.Stop();

        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(WaitTimeoutInMilliseconds), "Dispose gave up waiting");
        Assert.That(dispatcher.IsThreadExited, Is.False, "still inside the blocked action");

        // Once the action returns the thread sees the stop and exits without running the queued action.
        release.Set();
        Assert.That(SpinWait.SpinUntil(() => dispatcher.IsThreadExited, WaitTimeoutInMilliseconds), Is.True);
        Assert.That(queuedRan, Is.False);
    }

    [Test]
    public void StopFromInsideAnActionDoesNotDeadlock()
    {
        var dispatcher = new BackgroundDispatcher();
        using var stopReturned = new ManualResetEvent(false);

        dispatcher.EnqueueAction(() =>
        {
            dispatcher.Stop();
            stopReturned.Set();
        });

        Assert.That(stopReturned.WaitOne(WaitTimeoutInMilliseconds), Is.True);
        Assert.That(SpinWait.SpinUntil(() => dispatcher.IsThreadExited, WaitTimeoutInMilliseconds), Is.True);
        dispatcher.Dispose();
    }

    [Test]
    public void DisposeRacingQueueAndFailingActionsDoesNotCrash()
    {
        // Rough model of CloseDown racing game events: one thread keeps queueing (some actions throw) while the
        // dispatcher is disposed.  An exception escaping the dispatcher thread would abort the test run.
        for (int round = 0; round < 50; round++)
        {
            var dispatcher = new BackgroundDispatcher();
            using var go = new ManualResetEvent(false);
            int queued = 0;

            var producer = new Thread(() =>
            {
                go.WaitOne();
                for (int i = 0; i < 200; i++)
                {
                    int n = i;
                    dispatcher.EnqueueAction(() =>
                    {
                        if (n % 3 == 0)
                        {
                            throw new InvalidOperationException("test");
                        }
                    });
                    Interlocked.Increment(ref queued);
                }
            });
            producer.Start();

            go.Set();
            dispatcher.Dispose();

            Assert.That(producer.Join(WaitTimeoutInMilliseconds), Is.True);
            Assert.That(queued, Is.EqualTo(200), "QueueAction never threw");
            Assert.That(dispatcher.IsThreadExited, Is.True);
        }
    }
}
