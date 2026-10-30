using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RedoxExtensions.Continuations;

public class DelayedContinuation
{
    // A System.Threading.Timer that nothing references can be garbage collected before it fires.
    // Callers usually discard the returned continuation, so keep pending ones alive here until they run.
    private static readonly HashSet<DelayedContinuation> _pending = new();
    private static readonly object _pendingLock = new();

    private readonly Action<object> _onEventAction;
    private readonly Timer _timeoutTimer;
    private readonly object _stateTag;
    private readonly bool _invokeActioOnGameThread;

    public DelayedContinuation(Action<object> onEventAction, int millisecondsDelay, object stateTag, bool invokeActioOnGameThread)
    {
        this._onEventAction = onEventAction;
        this._stateTag = stateTag;
        this._invokeActioOnGameThread = invokeActioOnGameThread;

        lock (_pendingLock)
        {
            _pending.Add(this);
        }

        this._timeoutTimer = new Timer(this.TimerCallback, stateTag, millisecondsDelay, Timeout.Infinite);
    }

    public static DelayedContinuation ContinueAfterDelayOnGameThread(
        Action<object> onEventAction,
        int millisecondsDelay,
        object stateTag)
    {
        return new DelayedContinuation(onEventAction, millisecondsDelay, stateTag, true);
    }

    public static DelayedContinuation ContinueAfterDelayOnBackgroundThread(
        Action<object> onEventAction,
        int millisecondsDelay,
        object stateTag)
    {
        return new DelayedContinuation(onEventAction, millisecondsDelay, stateTag, false);
    }

    private void TimerCallback(object state)
    {
        lock (_pendingLock)
        {
            _pending.Remove(this);
        }

        REPlugin.Instance.InvokeOperationSafely(() =>
        {
            if (this._invokeActioOnGameThread)
            {
                // We are on a time thread, we need to get back on the game thread if we want to do anything meaningful
                // so queue up the action so that it is ran on the next render frame
                REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() => this._onEventAction(state));
            }
            else
            {
                this._onEventAction(state);
            }

            this._timeoutTimer.Dispose();
        });
    }
}
