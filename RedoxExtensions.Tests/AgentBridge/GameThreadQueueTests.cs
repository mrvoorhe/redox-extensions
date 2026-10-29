using System;
using System.Threading;

using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class GameThreadQueueTests
{
    [Test]
    public void InvokeRunsOnDrainingThreadAndReturnsResult()
    {
        var queue = new GameThreadQueue(TimeSpan.FromSeconds(5));
        var gameThreadId = 0;
        var ranOnThreadId = -1;
        object result = null;

        var caller = new Thread(() => result = queue.Invoke(() =>
        {
            ranOnThreadId = Thread.CurrentThread.ManagedThreadId;
            return 42;
        }));
        caller.Start();

        var gameThread = new Thread(() =>
        {
            gameThreadId = Thread.CurrentThread.ManagedThreadId;
            while (caller.IsAlive)
            {
                queue.Drain();
                Thread.Sleep(5);
            }
        });
        gameThread.Start();

        caller.Join();
        gameThread.Join();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(ranOnThreadId, Is.EqualTo(gameThreadId));
    }

    [Test]
    public void InvokePropagatesExceptions()
    {
        var queue = new GameThreadQueue(TimeSpan.FromSeconds(5));
        Exception error = null;

        var caller = new Thread(() =>
        {
            try
            {
                queue.Invoke(() => throw new ArgumentException("bad"));
            }
            catch (Exception e)
            {
                error = e;
            }
        });
        caller.Start();
        while (caller.IsAlive)
        {
            queue.Drain();
            Thread.Sleep(5);
        }

        Assert.That(error, Is.TypeOf<InvalidOperationException>());
        Assert.That(error.Message, Does.Contain("bad"));
    }

    [Test]
    public void InvokeTimesOutAndAbandonedWorkIsSkipped()
    {
        var queue = new GameThreadQueue(TimeSpan.FromMilliseconds(20));
        var ran = false;

        Assert.That(() => queue.Invoke(() => ran = true), Throws.TypeOf<TimeoutException>());

        queue.Drain();
        Assert.That(ran, Is.False);
        Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void FailAllReleasesWaitingCallers()
    {
        var queue = new GameThreadQueue(TimeSpan.FromSeconds(5));
        Exception error = null;

        var caller = new Thread(() =>
        {
            try
            {
                queue.Invoke(() => null);
            }
            catch (Exception e)
            {
                error = e;
            }
        });
        caller.Start();
        while (queue.Count == 0)
            Thread.Sleep(5);

        queue.FailAll("bridge off");
        caller.Join();

        Assert.That(error?.Message, Does.Contain("bridge off"));
    }
}
