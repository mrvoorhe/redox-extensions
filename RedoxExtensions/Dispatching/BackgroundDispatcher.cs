using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Dispatching.Legacy;

namespace RedoxExtensions.Dispatching;

public class BackgroundDispatcher : ILegacyDispatcher, IBasicDispatcher, IDisposable
{
    /// <summary>
    /// How long Stop() and Dispose wait for the dispatcher thread.  Dispose runs on the game thread (CloseDown), and
    /// the thread may be inside a long action (a pipeline action's WaitForOutcome), so it must not wait forever.
    /// </summary>
    internal const int DefaultStopTimeoutInMilliseconds = 2000;

    private readonly Thread _dispatcherThread;

    private readonly AutoResetEvent _dispatchEvent = new(false);
    private readonly ManualResetEvent _stopDispatcherEvent = new(false);

    private readonly WaitHandle[] _dispatcherWakeEvents;

    private readonly Queue<IDispatchedAction> _actionQueue;

    // Both are guarded by _actionQueue.  The dispatcher thread also reads _stopRequested without the lock.
    private volatile bool _stopRequested;
    private bool _disposed;

    public BackgroundDispatcher(ThreadPriority threadPriority)
    {
        // Stop comes first: WaitAny returns the lowest signaled index, so a stop wins over queued work.
        this._dispatcherWakeEvents = new WaitHandle[] { this._stopDispatcherEvent, this._dispatchEvent };
        this._actionQueue = new Queue<IDispatchedAction>();

        this._dispatcherThread = new Thread(this.DispatcherThread);
        this._dispatcherThread.Name = "RedoxExtensions.BackgroundDispatcher";
        this._dispatcherThread.Priority = threadPriority;
        // A dispatcher that is never stopped, or doesn't stop in time, must not keep the process alive.
        this._dispatcherThread.IsBackground = true;
        this._dispatcherThread.Start();
    }

    public BackgroundDispatcher()
        : this(ThreadPriority.Normal)
    {
    }

    public int QueueCount
    {
        get
        {
            lock (this._actionQueue)
            {
                return this._actionQueue.Count;
            }
        }
    }

    /// <summary>
    /// True once the dispatcher thread has exited.  For unit testing.
    /// </summary>
    internal bool IsThreadExited => !this._dispatcherThread.IsAlive;

    #region ILegacyDistpatcher

    public void QueueAction(DispatchedActionDelegate action)
    {
        this.QueueAction(new GenericDispatchedAction(action));
    }

    public void QueueAction(IDispatchedAction action)
    {
        lock (this._actionQueue)
        {
            // Work queued during or after shutdown (e.g. by a game event racing CloseDown) is dropped.  The plugin
            // instance it would run against is going away.
            if (this._stopRequested)
            {
                return;
            }

            this._actionQueue.Enqueue(action);

            // Set the event so that the dispatcher thread knows there is work to do.  Inside the lock so that
            // Dispose can't close the event in between.
            this._dispatchEvent.Set();
        }
    }

    public void QueueDelayedAction(DispatchedActionDelegate action, int millisecondsDelay)
    {
        this.QueueDelayedAction(new GenericDispatchedAction(action), millisecondsDelay);
    }

    public void QueueDelayedAction(IDispatchedAction action, int millisecondsDelay)
    {
        throw new NotImplementedException();
    }

    public IActionAsyncResult<TResult> QueueCompletableAction<TResult>(DispatchedFuncDelegate<TResult> func, AutoResetEvent reusedCompletionEvent)
    {
        throw new NotImplementedException();
    }

    public IActionAsyncResult<TResult> QueueCompletableAction<TResult>(DispatchedFuncDelegate<TResult> func, ManualResetEvent reusedCompletionEvent)
    {
        throw new NotImplementedException();
    }

    public IStandaloneActionAsyncResult<TResult> QueueCompletableAction<TResult>(DispatchedFuncDelegate<TResult> func)
    {
        throw new NotImplementedException();
    }

    public IStandaloneActionAsyncResult<TResult> QueueCompletableAction<TResult>(IDispatchedActionFunc<TResult> func)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region IBasicDispatcher

    public void EnqueueAction(Action action)
    {
        this.QueueAction(new GenericDispatchedAction2(action));
    }

    #endregion

    /// <summary>
    /// Asks the dispatcher thread to exit, drops queued work and waits up to <paramref name="millisecondsTimeout"/>
    /// for the thread.  Returns true if the thread has exited.  Safe to call more than once, and from the
    /// dispatcher thread itself (it then doesn't wait).
    /// </summary>
    public bool Stop(int millisecondsTimeout)
    {
        lock (this._actionQueue)
        {
            if (!this._stopRequested)
            {
                this._stopRequested = true;
                this._actionQueue.Clear();

                if (!this._disposed)
                {
                    this._stopDispatcherEvent.Set();
                }
            }
        }

        if (Thread.CurrentThread == this._dispatcherThread)
        {
            return false;
        }

        return this._dispatcherThread.Join(millisecondsTimeout);
    }

    public bool Stop() => this.Stop(DefaultStopTimeoutInMilliseconds);

    public void Dispose()
    {
        bool stopped = this.Stop();

        lock (this._actionQueue)
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;

            if (!stopped)
            {
                // The thread is still inside an action.  It exits when that returns (the stop event is set), so leave
                // the events open for it rather than close them under it.  It's a background thread, so it can't
                // hold up the process either way.
                TryDebugWriteLine("BackgroundDispatcher did not stop in time.  It will exit when its current action ends.");
                return;
            }

            this._dispatchEvent.Close();
            this._stopDispatcherEvent.Close();
        }
    }

    #region Private Methods

    private void DispatcherThread()
    {
        // Nothing may escape this method: an unhandled exception on this thread terminates the whole process (the
        // game client).  Everything here, the logging included, must work after REPlugin has shut down (Instance is
        // null) and in unit tests (no REPlugin at all).
        try
        {
            while (true)
            {
                int eventIndex = WaitHandle.WaitAny(this._dispatcherWakeEvents);

                if (this._dispatcherWakeEvents[eventIndex] == this._stopDispatcherEvent)
                {
                    return;
                }

                // Drain the queue.  The dispatch event is auto reset, so one wake can stand for several actions.
                while (!this._stopRequested)
                {
                    IDispatchedAction action;
                    lock (this._actionQueue)
                    {
                        action = this.CheckForWork();
                    }

                    if (action == null)
                    {
                        break;
                    }

                    InvokeWork(action);
                }
            }
        }
        catch (Exception e)
        {
            TryDebugWriteLine("WARNING - BackgroundDispatcher has died");
            LogUnexpectedException(e);
        }
        finally
        {
            TryDebugWriteLine("BackgroundDispatcher has exited");
        }
    }

    private IDispatchedAction CheckForWork() => this._actionQueue.Count > 0 ? this._actionQueue.Dequeue() : null;

    /// <summary>
    /// Runs one action.  An action that throws is logged and the dispatcher goes on to the next one.  Does what
    /// REPlugin.InvokeOperationSafely does, without needing REPlugin.Instance, which is null once the plugin has
    /// shut down (and in unit tests).
    /// </summary>
    private static void InvokeWork(IDispatchedAction action)
    {
        try
        {
            action.Invoke();
        }
        catch (DisplayToUserException e)
        {
            try
            {
                if (REPlugin.Instance != null)
                {
                    ExceptionUtilities.HandleDisplayToUserException(e);
                }
                else
                {
                    LogUnexpectedException(e);
                }
            }
            catch (Exception displayException)
            {
                LogUnexpectedException(e);
                LogUnexpectedException(displayException);
            }
        }
        catch (Exception e)
        {
            LogUnexpectedException(e);
        }
    }

    private static void LogUnexpectedException(Exception e)
    {
        try
        {
            // REPlugin's ErrorLogger, or the static log when the plugin is gone.
            ExceptionUtilities.HandleUnexpectedException(e);
        }
        catch
        {
            // The log itself failed (e.g. no log folder).  There is nothing left to report it with, and throwing
            // would take down the client.
        }
    }

    private static void TryDebugWriteLine(string message)
    {
        try
        {
            REPlugin.Instance?.Debug?.WriteLine(message);
        }
        catch
        {
            // See LogUnexpectedException
        }
    }

    #endregion

    #region Inner Class

    private class GenericDispatchedAction2 : IDispatchedAction
    {
        private readonly Action _action;

        public GenericDispatchedAction2(Action action)
        {
            this._action = action;
        }

        public void Invoke()
        {
            this._action();
        }
    }

    #endregion
}
