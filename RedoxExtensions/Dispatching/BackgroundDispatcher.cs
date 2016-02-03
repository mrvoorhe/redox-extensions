using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Dispatching.Legacy;

namespace RedoxExtensions.Dispatching
{
    public class BackgroundDispatcher : ILegacyDispatcher, IBasicDispatcher, IDisposable
    {
        //private const int PulseIntervalInMilliseconds = 1000;

        private readonly Thread _dispatcherThread;

        //private readonly Timer _dispatcherTimer;

        private readonly AutoResetEvent _dispatchEvent = new AutoResetEvent(false);
        private readonly ManualResetEvent _stopDispatcherEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent _dispatcherStoppedEvent = new ManualResetEvent(false);

        private readonly WaitHandle[] _dispatcherWakeEvents;

        private readonly Queue<IDispatchedAction> _actionQueue; 

        public BackgroundDispatcher(ThreadPriority threadPriority)
        {
            this._dispatcherWakeEvents = new WaitHandle[] { this._dispatchEvent, this._stopDispatcherEvent };
            this._actionQueue = new Queue<IDispatchedAction>();

            this._dispatcherThread = new Thread(this.DispatcherThread);
            this._dispatcherThread.Priority = threadPriority;
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

        #region ILegacyDistpatcher

        public void QueueAction(DispatchedActionDelegate action)
        {
            this.QueueAction(new GenericDispatchedAction(action));
        }

        public void QueueAction(IDispatchedAction action)
        {
            lock (this._actionQueue)
            {
                this._actionQueue.Enqueue(action);
            }

            // Set the event so that the dispatcher thread knows there is work to do
            this._dispatchEvent.Set();
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

        public void Stop()
        {
            this._stopDispatcherEvent.Set();

            this._dispatcherStoppedEvent.WaitOne();

            this._dispatcherThread.Join();
        }

        public void Dispose()
        {
            this.Stop();

            this._dispatchEvent.Close();
            this._stopDispatcherEvent.Close();
            this._dispatcherStoppedEvent.Close();
        }

        #region Private Methods

        private void DispatcherThread()
        {
            try
            {
                while (true)
                {
                    int eventIndex = WaitHandle.WaitAny(this._dispatcherWakeEvents);

                    WaitHandle firedEvent = this._dispatcherWakeEvents[eventIndex];

                    if (firedEvent == _stopDispatcherEvent)
                    {
                        return;
                    }

                    IDispatchedAction action;
                    lock (this._actionQueue)
                    {
                        action = this.CheckForWork();
                    }

                    if (action != null)
                    {
                        try
                        {
                            this.InvokeWork(action);
                        }
                        finally
                        {
                            // Set the event so that if another item is in the queue we process it on the next time around
                            // in the while loop.
                            this._dispatchEvent.Set();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                REPlugin.Instance.Debug.WriteLine("WARNING - ActionDispatcher has died");
                REPlugin.Instance.Error.LogError(e);
            }
            finally
            {
                REPlugin.Instance.Debug.WriteLine("ActionDispatcher has exited");
                this._dispatcherStoppedEvent.Set();
            }
        }

        private IDispatchedAction CheckForWork()
        {
            return this._actionQueue.Count > 0 ? this._actionQueue.Dequeue() : null;
        }

        private void InvokeWork(IDispatchedAction action)
        {
            REPlugin.Instance.InvokeOperationSafely(action.Invoke);
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
}
