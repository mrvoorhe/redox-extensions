using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Core;
using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Dispatching.Legacy
{
    public class GameThreadDispatcher : ILegacyDispatcher, IDisposable
    {
        #region Private Struct

        private struct ActionContainer
        {
            public readonly IDispatchedAction DispatchedAction;
            public readonly IDispatchedActionFuncGeneric DispatchedActionFunc;
            public readonly IActionAsyncResultCompleter Completer;

            public ActionContainer(IDispatchedAction action)
            {
                this.DispatchedAction = action;
                this.DispatchedActionFunc = null;
                this.Completer = null;
            }

            public ActionContainer(IDispatchedActionFuncGeneric action, IActionAsyncResultCompleter completer)
            {
                this.DispatchedAction = null;
                this.DispatchedActionFunc = action;
                this.Completer = completer;
            }
        }

        #endregion

        private readonly object _classLock = new object();
        private readonly Queue<ActionContainer> _actionQueue;
        private readonly IDecalEventsProxy _decalEventsProxy;

        private bool _hooked;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="decalEventsProxy">Passed in rather than use singleton to enable unit testing</param>
        public GameThreadDispatcher(IDecalEventsProxy decalEventsProxy)
        {
            this._actionQueue = new Queue<ActionContainer>();
            this._decalEventsProxy = decalEventsProxy;
        }

        public int QueueCount
        {
            get
            {
                lock (this._classLock)
                {
                    return this._actionQueue.Count;
                }
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

        public void QueueAction(DispatchedActionDelegate action)
        {
            this.QueueAction(new GenericDispatchedAction(action));
        }

        public void QueueAction(IDispatchedAction action)
        {
            var actionContainer = new ActionContainer(action);
            this.QueueContainer(actionContainer);
        }

        public void QueueDelayedAction(DispatchedActionDelegate action, int millisecondsDelay)
        {
            this.QueueDelayedAction(new GenericDispatchedAction(action), millisecondsDelay);
        }

        public void QueueDelayedAction(IDispatchedAction action, int millisecondsDelay)
        {
            REPlugin.Instance.Dispatch.Background.QueueAction(() =>
            {
                Thread.Sleep(millisecondsDelay);
                QueueAction(action);
            });
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
            return this.QueueCompletableAction(new GenericDispatchedActionFunc<TResult>(func));
        }

        public IStandaloneActionAsyncResult<TResult> QueueCompletableAction<TResult>(IDispatchedActionFunc<TResult> func)
        {
            StandaloneActionAsyncResult<TResult> asyncResult = new StandaloneActionAsyncResult<TResult>();
            ActionContainer container = new ActionContainer(func, asyncResult);
            this.QueueContainer(container);
            return asyncResult;
        }

        public void QueueRetriedAction(DispatchedActionDelegate action, TryCompleteActionDelegate tryCompleteAction, int millisecondsRetryDelay, int maxTries)
        {
            throw new NotImplementedException();
        }

        public void QueueRetriedAction(DispatchedActionDelegate action, TryCompleteActionDelegate tryCompleteAction, int maxTries, SafeAction exceedMaxRetriesAction)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.UnhookRenderFrame();
        }

        private void QueueContainer(ActionContainer actionContainer)
        {
            lock (this._classLock)
            {
                this._actionQueue.Enqueue(actionContainer);

                this.HookRenderFrame();
            }
        }

        private void HookRenderFrame()
        {
            if (this._hooked)
            {
                return;
            }

            this._decalEventsProxy.RenderFrame += CoreManager_RenderFrame;
            this._hooked = true;
        }

        private void UnhookRenderFrame()
        {
            if (!this._hooked)
            {
                return;
            }

            this._decalEventsProxy.RenderFrame -= CoreManager_RenderFrame;
            this._hooked = false;
        }

        private void CoreManager_RenderFrame(object sender, EventArgs e)
        {
            ActionContainer actionContainer;

            // Don't hold the lock any longer than necessary.  That could hold up queueing
            lock (this._classLock)
            {
                if (this._actionQueue.Count > 0)
                {
                    actionContainer = this._actionQueue.Dequeue();

                    // Unhook if we have no more actions
                    if (this._actionQueue.Count == 0)
                    {
                        this.UnhookRenderFrame();
                    }
                }
                else
                {
                    // No actions queued up.  Nothing else to do
                    return;
                }
            }


            if (actionContainer.DispatchedAction != null)
            {
                // It's a simple action with no result.
                actionContainer.DispatchedAction.Invoke();
            }
            else
            {
                // It's the more complexed action func
                object result = actionContainer.DispatchedActionFunc.InvokeGeneric();
                actionContainer.Completer.SetResult(result);
            }
        }
    }
}
