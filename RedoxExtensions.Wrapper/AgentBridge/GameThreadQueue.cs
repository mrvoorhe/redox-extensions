using System;
using System.Collections.Generic;
using System.Threading;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// Lets connection threads run work on the game thread and wait for the result.  Drain is called from RenderFrame.
/// </summary>
public class GameThreadQueue
{
    private class WorkItem
    {
        public Func<object> Func;
        public object Result;
        public Exception Error;
        public bool Abandoned;
        public readonly ManualResetEvent Done = new(false);
    }

    private readonly object _lock = new();
    private readonly Queue<WorkItem> _queue = new();
    private readonly TimeSpan _timeout;

    public GameThreadQueue(TimeSpan timeout)
    {
        this._timeout = timeout;
    }

    public int Count
    {
        get
        {
            lock (this._lock)
                return this._queue.Count;
        }
    }

    public object Invoke(Func<object> func)
    {
        var item = new WorkItem { Func = func };
        lock (this._lock)
            this._queue.Enqueue(item);

        try
        {
            if (!item.Done.WaitOne(this._timeout, false))
            {
                lock (this._lock)
                {
                    if (!item.Done.WaitOne(0, false))
                    {
                        item.Abandoned = true;
                        throw new TimeoutException($"Game thread did not run the request within {this._timeout.TotalSeconds}s");
                    }
                }
            }

            if (item.Error != null)
                throw new InvalidOperationException(item.Error.GetType().Name + ": " + item.Error.Message, item.Error);
            return item.Result;
        }
        finally
        {
            lock (this._lock)
            {
                if (!item.Abandoned)
                    item.Done.Close();
            }
        }
    }

    public void Drain()
    {
        while (true)
        {
            WorkItem item;
            lock (this._lock)
            {
                if (this._queue.Count == 0)
                    return;
                item = this._queue.Dequeue();
                if (item.Abandoned)
                {
                    item.Done.Close();
                    continue;
                }
            }

            try
            {
                item.Result = item.Func();
            }
            catch (Exception e)
            {
                item.Error = e;
            }

            lock (this._lock)
            {
                if (item.Abandoned)
                    item.Done.Close();
                else
                    item.Done.Set();
            }
        }
    }

    /// <summary>
    /// Fails anything still queued, e.g. when the bridge is turned off.
    /// </summary>
    public void FailAll(string reason)
    {
        lock (this._lock)
        {
            while (this._queue.Count > 0)
            {
                var item = this._queue.Dequeue();
                if (item.Abandoned)
                {
                    item.Done.Close();
                    continue;
                }
                item.Error = new InvalidOperationException(reason);
                item.Done.Set();
            }
        }
    }
}
