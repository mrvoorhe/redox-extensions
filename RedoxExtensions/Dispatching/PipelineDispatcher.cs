using RedoxExtensions.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using RedoxExtensions.Actions.Dispatched.Internal;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Dispatching.Legacy;
using RedoxExtensions.VirindiInterop;
using RedoxExtensions.Diagnostics;
using RedoxLib.General;

namespace RedoxExtensions.Dispatching
{
    /// <summary>
    /// An action that will not fail (or if it does, you don't care)
    /// </summary>
    public delegate void NonFailableAction();

    public class PipelineDispatcher : IDisposable
    {
        private const int ProcessQueueTimerIntervalInMilliseconds = 20;

        private delegate void PendingBackgroundWaitCall();

        private readonly object _queueLock = new object();
        private readonly Queue<IPipelineAction> _actionQueue = new Queue<IPipelineAction>();

        private readonly IDecalEventsProxy _decalEventsProxy;
        private readonly Timer _processQueueTimer;

        private readonly IBasicDispatcher _backgroundDispatcher;

        private bool _pendingActionNeedsBegun;
        private IPipelineAction _pendingAction = null;
        private PendingBackgroundWaitCall _backgroundAction;
        private bool _hooked;
        private VTRunScope _cachedVTRunScope;

        private DateTime _lastDebugPrint = DateTime.Now;

        public PipelineDispatcher(IDecalEventsProxy decalEventsProxy)
        {
            this._decalEventsProxy = decalEventsProxy;
            this._processQueueTimer = new Timer();
            this._processQueueTimer.Interval = ProcessQueueTimerIntervalInMilliseconds;
            this._processQueueTimer.Tick += _processQueueTimer_Tick;

            this._backgroundDispatcher = new BackgroundDispatcher();
        }

        public int QueueCount
        {
            get
            {
                lock (this._queueLock)
                {
                    return this._actionQueue.Count;
                }
            }
        }

        public bool HasPendingAction
        {
            get
            {
                return this._pendingAction != null;
            }
        }

        /// <summary>
        /// For unit testing only
        /// </summary>
        public bool IsHooked
        {
            get
            {
                return this._hooked;
            }
        }

        public void EnqueueAction(IAction action)
        {
            this.EnqueueAction((IPipelineAction)action);
        }

        public void EnqueueAction(IPipelineAction action)
        {
            lock (this._queueLock)
            {
                this._actionQueue.Enqueue(action);

                this.Start();
            }
        }

        public void EnqueueNonFailableAction(string command)
        {
            this.EnqueueNonFailableAction(ListOperations.Create(command));
        }

        public void EnqueueNonFailableAction(IEnumerable<string> commandSequence)
        {
            this.EnqueueNonFailableAction(() =>
            {
                foreach(var command in commandSequence)
                {
                    ACUtilities.ProcessArbitraryCommand(command);
                }
            });
        }

        public void EnqueueNonFailableAction(NonFailableAction immediateAction)
        {
            throw new NotImplementedException("Implement if a need arises");
        }

        public void Dispose()
        {
            this.Stop();

            this._processQueueTimer.Dispose();
        }

        public void Clear()
        {
            lock (this._queueLock)
            {
                try
                {
                    // Nothing to do if the queue is empty
                    if (this.QueueCount == 0)
                    {
                        return;
                    }

                    this._actionQueue.Clear();

                    if (this._pendingAction != null && !this._pendingActionNeedsBegun)
                    {
                        // There is a pending action, let's try and clean it up
                        try
                        {
                            this._pendingAction.End();
                        }
                        finally
                        {
                            this._pendingAction.Dispose();
                            this._pendingAction = null;
                            this._backgroundAction = null;
                        }
                    }
                }
                finally
                {
                    this.DisposeOfVTScopeIfEmptyQueue(true);
                }
            }
        }

        private void Start()
        {
            if (this._hooked)
            {
                return;
            }

            this._decalEventsProxy.RenderFrame += CoreManager_RenderFrame;
            this._processQueueTimer.Start();

            this._hooked = true;
        }

        private void Stop()
        {
            if (!this._hooked)
            {
                return;
            }

            this._decalEventsProxy.RenderFrame -= CoreManager_RenderFrame;
            this._processQueueTimer.Stop();

            this._hooked = false;
        }

        private void LogState()
        {
            var currentTime = DateTime.Now;

            // Limit logging so that we don't spam
            if ((currentTime - this._lastDebugPrint).TotalSeconds > 10)
            {
                REPlugin.Instance.Debug.WriteLine("---PipelineDispatcher---");
                REPlugin.Instance.Debug.WriteLine("  IsHooked = {0}", this.IsHooked);
                REPlugin.Instance.Debug.WriteLine("  QueueCount = {0}", this.QueueCount);
                REPlugin.Instance.Debug.WriteLine("  HasPendingAction = {0}", this.HasPendingAction);
                REPlugin.Instance.Debug.WriteLine("  CurrentThreadId = {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
                REPlugin.Instance.Debug.WriteLine("  CharacterState.Busy = {0}", REPlugin.Instance.MonitorManager.CharacterState.Busy.WaitOne(0));
                REPlugin.Instance.Debug.WriteLine("  CharacterState.Idle = {0}", REPlugin.Instance.MonitorManager.CharacterState.Idle.WaitOne(0));
                REPlugin.Instance.Debug.WriteLine("  _pendingAction.IsComplete = {0}", this._pendingAction != null ? this._pendingAction.IsComplete.ToString() : "N/A");
                REPlugin.Instance.Debug.WriteLine("  _pendingActionNeedsBegun = {0}", this._pendingActionNeedsBegun);
                REPlugin.Instance.Debug.WriteLine("");

                this._lastDebugPrint = currentTime;
            }
        }

        private void ProcessQueue()
        {
            this.LogState();

            // Only allow 1 action at a time.  If an action is still in progress, we have to wait for it to complete
            // before moving onto the next one
            if (this.CheckForReadyToEndOrRetry())
            {
                // there is nothing more we can do.  Return.
                return;
            }

            // Next check if there are any new actions we can begin.
            if (this.CheckForNewActionToInit())
            {
                // there is nothing more we can do.  Return.
                return;
            }

            if (this.CheckForActionToBegin())
            {
                // there is nothing more we can do.  Return.
                // Note, this is kinda pointless.  Keep it for now for consistency.
                return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns true if the current processing stage should break</returns>
        private bool CheckForReadyToEndOrRetry()
        {
            if (this._pendingAction != null && !this._pendingActionNeedsBegun)
            {
                if (!this._pendingAction.IsComplete)
                {
                    // nothing to do yet, we are still waiting on the action to complete.  Return
                    return true;
                }

                if (this._pendingAction.Retry)
                {
                    Debug.WriteLine("[PDispatcher] - Resetting Action for Retry");

                    try
                    {
                        // The action as requested that we retry it.
                        this._pendingAction.ResetForRetry();

                        this._pendingActionNeedsBegun = true;

                        // We don't want to process it on this cycle, so return true to signal a break
                        return true;
                    }
                    catch
                    {
                        // If there are any exceptions we need to reset our state
                        this._pendingAction.Dispose();
                        this._pendingAction = null;
                        this._backgroundAction = null;

                        this.DisposeOfVTScopeIfEmptyQueue(false);
                        throw;
                    }
                }

                // The pending action has completed.  We need to finalize it
                try
                {
                    this._pendingAction.End();

                    // Need to deal VT Scope.  Only restore if the queue is empty.
                    // if the queue is not empty, we will deal with it when we go to invoke the next
                    // action.
                    this.DisposeOfVTScopeIfEmptyQueue(false);

                    return true;
                }
                finally
                {
                    this._pendingAction.Dispose();
                    this._pendingAction = null;
                    this._backgroundAction = null;
                }
            }

            // There are no pending actions.
            return false;
        }

        private void DisposeOfVTScopeIfEmptyQueue(bool forceDispose)
        {
            if (this._cachedVTRunScope != null)
            {
                if (forceDispose || this.QueueCount == 0)
                {
                    this._cachedVTRunScope.Dispose();
                    this._cachedVTRunScope = null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns true if the current processing stage should break</returns>
        private bool CheckForNewActionToInit()
        {
            if (this._pendingAction == null)
            {
                IPipelineAction actionToBegin = null;
                // Don't hold the lock any longer than necessary.
                lock (this._queueLock)
                {
                    if (this._actionQueue.Count == 0)
                    {
                        this.Stop();
                        return true;
                    }

                    actionToBegin = this._actionQueue.Dequeue();
                }

                if (actionToBegin.RequiresExplicitVTState)
                {
                    // Support VT on/off toggling.  The cached scope will not be null when rolling over from a previous action.
                    // In that case, if multiple actions were queued up and both want VT disabled, don't bother turning it back on in between.
                    if (this._cachedVTRunScope != null)
                    {
                        // The next action wants a different VTState, so we need to dispose of the current one
                        if (this._cachedVTRunScope.DesiredState != actionToBegin.DesiredVTRunState)
                        {
                            this._cachedVTRunScope.Dispose();
                            this._cachedVTRunScope = null;
                        }
                    }

                    // If we get here and the sached VT scope is null, it means we need to initialize it
                    if (this._cachedVTRunScope == null)
                    {
                        this._cachedVTRunScope = VTRunScope.Enter(actionToBegin.DesiredVTRunState);
                    }
                }

                actionToBegin.Init();

                this._pendingActionNeedsBegun = true;

                // If we get this far without any exceptions, then store the action as the pending action.
                // If any exceptions happen before this, we want to make sure we are not stuck in some state where we are waiting
                // on a bugged action to finish
                this._pendingAction = actionToBegin;

                // Don't Begin invoke on the same cycle as init
                return true;
            }

            // We didn't do anything
            return false;
        }

        private bool CheckForActionToBegin()
        {
            if (this._pendingAction != null && this._pendingActionNeedsBegun)
            {
                if (this._pendingAction.RequireIdleToPerform && REPlugin.Instance.MonitorManager.CharacterState.Busy.WaitOne(0))
                {
                    // Can't begin invoke yet because this action wants us to wait until we are idle to invoke
                    return true;
                }

                try
                {
                    // First begin invoke the action
                    this._pendingAction.Perform();

                    this._backgroundDispatcher.EnqueueAction(this._pendingAction.WaitForOutcome);
                    return true;
                }
                catch
                {
                    // If we hit an exception, we don't want to leave VT in an undesired state.  That could lead to a bot getting killed or something
                    if (this._cachedVTRunScope != null)
                    {
                        this._cachedVTRunScope.Dispose();
                        this._cachedVTRunScope = null;
                    }

                    // If any exceptions, need to reset our state.
                    this._pendingAction = null;
                    this._backgroundAction = null;
                    throw;
                }
                finally
                {
                    this._pendingActionNeedsBegun = false;
                }
            }

            // We didn't do anything
            return false;
        }

        private void CoreManager_RenderFrame(object sender, EventArgs e)
        {
            this.ProcessQueue();
        }

        void _processQueueTimer_Tick(object sender, EventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(this.ProcessQueue);
        }
    }
}
