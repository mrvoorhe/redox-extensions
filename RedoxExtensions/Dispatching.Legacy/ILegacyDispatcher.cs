using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RedoxExtensions.Dispatching.Legacy
{
    public interface ILegacyDispatcher
    {
        int QueueCount { get; }

        void QueueAction(DispatchedActionDelegate action);

        void QueueAction(IDispatchedAction action);

        void QueueDelayedAction(DispatchedActionDelegate action, int millisecondsDelay);

        void QueueDelayedAction(IDispatchedAction action, int millisecondsDelay);

        IActionAsyncResult<TResult> QueueCompletableAction<TResult>(DispatchedFuncDelegate<TResult> func, AutoResetEvent reusedCompletionEvent);
        IActionAsyncResult<TResult> QueueCompletableAction<TResult>(DispatchedFuncDelegate<TResult> func, ManualResetEvent reusedCompletionEvent);

        IStandaloneActionAsyncResult<TResult> QueueCompletableAction<TResult>(DispatchedFuncDelegate<TResult> func);
        IStandaloneActionAsyncResult<TResult> QueueCompletableAction<TResult>(IDispatchedActionFunc<TResult> func);
    }
}
