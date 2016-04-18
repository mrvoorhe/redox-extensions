using RedoxExtensions.Dispatching;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests.Fakes
{
    public class FakePipelineAction : IPipelineAction
    {
        private readonly ManualResetEvent _complete = new ManualResetEvent(false);
        private volatile bool _retry;
        private bool _postDisposeCachedCompleteValue;
        private bool _disposed;

        public FakePipelineAction()
            :this(true)
        {
        }

        public FakePipelineAction(bool intiallyReady)
        {
            ForTesting_Ready = intiallyReady;
        }

        public int BeginInvokeCallCount { get; private set; }
        public int EndInvokeCallCount { get; private set; }
        public int InitCallCount { get; private set; }
        public int ResetForRetryCallCount { get; private set; }
        public int RetryCallCount { get; private set; }
        public int DisposeCallCount { get; private set; }
        public int ReadyCallCount { get; private set; }

        public int BeginInvokeThreadId { get; private set; }
        public int EndInvokeThreadId { get; private set; }
        public int InitThreadId { get; private set; }
        public int ResetForRetryThreadId { get; private set; }

        public int WaitForCompleteThreadId { get; private set; }

        public bool ForTesting_Ready { get; set; }

        public bool IsComplete
        {
            get
            {
                if(this.DisposeCallCount > 0)
                {
                    return _postDisposeCachedCompleteValue;
                }

                return this._complete.WaitOne(0);
            }
        }

        public bool Retry
        {
            get
            {
                this.RetryCallCount++;
                return this._retry;
            }
        }

        public bool RequireIdleToPerform
        {
            get
            {
                return false;
            }
        }

        public bool RequiresExplicitVTState
        {
            get
            {
                // For now, always disable this.  Makes unit testing easier since I don't have to mock out the VTStateScope stuff
                return false;
            }
        }

        public VTRunState DesiredVTRunState
        {
            get
            {
                return VTRunState.Off;
            }
        }

        public void Init()
        {
            this.InitThreadId = Thread.CurrentThread.ManagedThreadId;
            this.InitCallCount++;
        }

        public bool Ready()
        {
            ReadyCallCount++;
            return ForTesting_Ready;
        }

        public void Perform()
        {
            this.BeginInvokeThreadId = Thread.CurrentThread.ManagedThreadId;
            this.BeginInvokeCallCount++;
        }

        public void WaitForOutcome()
        {
            this.WaitForCompleteThreadId = Thread.CurrentThread.ManagedThreadId;
            this._complete.WaitOne();
        }

        public void End()
        {
            this.EndInvokeThreadId = Thread.CurrentThread.ManagedThreadId;
            this.EndInvokeCallCount++;
        }

        public void ResetForRetry()
        {
            this.ResetForRetryThreadId = Thread.CurrentThread.ManagedThreadId;
            this.ResetForRetryCallCount++;

            // Need to do some actual reset logic to reset this class as well so that it behaves correctly.
            this._complete.Reset();
            this._retry = false;
        }

        public void Dispose()
        {
            this.DisposeCallCount++;

            if (!this._disposed)
            {
                this._postDisposeCachedCompleteValue = this._complete.WaitOne(0);
                this._complete.Close();
            }

            this._disposed = true;
        }

        public void ForTesting_SetComplete(bool retry)
        {
            this._retry = retry;
            this._complete.Set();
        }
    }
}
