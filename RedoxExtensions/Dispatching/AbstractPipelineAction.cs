using RedoxExtensions.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Commands;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Dispatching
{
    public abstract class AbstractPipelineAction : IPipelineAction, IAction
    {
        #region Instance Data

        private readonly ManualResetEvent _successful = new ManualResetEvent(false);
        private readonly ManualResetEvent _failed = new ManualResetEvent(false);
        private readonly ManualResetEvent _busy = new ManualResetEvent(false);

        private readonly ManualResetEvent _complete = new ManualResetEvent(false);

        private readonly ISupportFeedback _requestor;

        private readonly WaitHandle[] _completionCausingHandles;

        private WaitForCompleteOutcome _outcome = WaitForCompleteOutcome.Undefined;

        private volatile bool _retryPlease;
        private int _retryCounter;

        private bool _disposed;

        #endregion

        #region Constructors

        protected AbstractPipelineAction(ISupportFeedback requestor)
        {
            this._requestor = requestor;

            // TODO : Allow derived type to extend what we put in here?
            this._completionCausingHandles = new[] { this._successful, this._failed, this._busy };
        }

        #endregion

        #region Public Properties

        public bool IsComplete
        {
            get
            {
                return this._complete.WaitOne(0);
            }
        }

        public bool Retry
        {
            get
            {
                return this._retryPlease && this._retryCounter < this.MaxTries;
            }
        }

        #region Abstract

        public abstract bool RequireIdleToPerform { get; }

        public abstract VTRunState DesiredVTRunState { get; }

        #endregion

        #region Virtual

        public virtual bool RequiresExplicitVTState
        {
            get
            {
                return true;
            }
        }

        #endregion

        #endregion

        #region Protected Properties

        #region Abstract

        /// <summary>
        /// Gets the maximum tries.
        /// </summary>
        /// <value>
        /// The maximum tries.
        /// </value>
        protected abstract int MaxTries { get; }

        /// <summary>
        /// How long to wait for one of the events to trigger before timing out
        /// </summary>
        /// <value>
        /// The wait timeout in milliseconds.
        /// </value>
        protected abstract int WaitTimeoutInMilliseconds { get; }

        #endregion

        protected ManualResetEvent Successful
        {
            get
            {
                return this._successful;
            }
        }

        protected ManualResetEvent Failed
        {
            get
            {
                return this._failed;
            }
        }

        protected ManualResetEvent Busy
        {
            get
            {
                return this._busy;
            }
        }

        protected ISupportFeedback Requestor
        {
            get
            {
                return this._requestor;
            }
        }

        #endregion

        #region Public Methods

        public void Init()
        {
            this.InitializeData();

            REPlugin.Instance.Events.RE.YourTooBusy += RTEvents_YourTooBusy;

            this.HookEvents();
        }

        public virtual bool Ready()
        {
            return true;
        }

        public void Perform()
        {
            this.DoPeform();
        }

        public void WaitForOutcome()
        {
            try
            {
                var index = WaitHandle.WaitAny(this._completionCausingHandles, this.WaitTimeoutInMilliseconds);

                if (index == WaitHandle.WaitTimeout)
                {
                    this._outcome = WaitForCompleteOutcome.Timeout;
                }
                else if (this._completionCausingHandles[index] == this._successful)
                {
                    this._outcome = WaitForCompleteOutcome.Success;
                }
                else if (this._completionCausingHandles[index] == this._failed)
                {
                    this._outcome = WaitForCompleteOutcome.Failed;
                }
                else if (this._completionCausingHandles[index] == this._busy)
                {
                    this._outcome = WaitForCompleteOutcome.TooBusy;
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Unhandled WaitAny result : {0}", index));
                }

                this._retryPlease = this.ShouldRetry(this._outcome);
            }
            finally
            {
                this._complete.Set();
            }
        }

        public void End()
        {
            // Unsubscribe from events asap to avoid noise
            this.UnhookEvents();

            REPlugin.Instance.Events.RE.YourTooBusy -= RTEvents_YourTooBusy;

            this.DoEnd(this._outcome);
        }

        public void ResetForRetry()
        {
            // Note : Do not reset the successful flag.  It's possible the action completed while we were resetting.  If it did,
            // we want to keep that information so that WaitForCompletion will immediately end successfully.

            if (this.ShouldCountRetry(this._outcome))
            {
                this._retryCounter++;
            }

            this._retryPlease = false;
            this._complete.Reset();
            this._successful.Reset();
            this._failed.Reset();
            this._busy.Reset();

            this._outcome = WaitForCompleteOutcome.Undefined;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        #region Abstract

        protected abstract void DoPeform();

        protected abstract void InitializeData();

        protected abstract void DoEnd(WaitForCompleteOutcome finalOutcome);

        protected abstract void HookEvents();

        protected abstract void UnhookEvents();

        #endregion

        #region Virtual

        protected virtual void DoResetForRetry()
        {
            // Nothing to do by default
        }

        protected virtual bool ShouldRetry(WaitForCompleteOutcome outcome)
        {
            switch(outcome)
            {
                case WaitForCompleteOutcome.Success:
                    return false;
                default:
                    return true;
            }
        }

        protected virtual bool ShouldCountRetry(WaitForCompleteOutcome outcome)
        {
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing & !this._disposed)
            {
                this._successful.Close();
                this._failed.Close();
                this._busy.Close();
                this._complete.Close();
            }

            this._disposed = true;
        }

        #endregion

        #endregion

        #region Private Methods

        private void RTEvents_YourTooBusy(object sender, Data.Events.YourTooBusyEventArgs e)
        {
            this._busy.Set();
        }

        #endregion
    }
}
