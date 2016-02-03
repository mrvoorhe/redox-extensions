using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Decal.Adapter.Wrappers;

namespace RedoxExtensions.Continuations
{
    public class PortalSpaceContinuation
    {
        private readonly PortalEventType _expectedPortalEventType;
        private readonly Action<object> _onEventAction;
        private readonly Action<object> _onTimeoutAction;
        private readonly Timer _timeoutTimer;
        private readonly object _stateTag;
        private readonly object _lock = new object();

        private bool _complete;

        public PortalSpaceContinuation(
            PortalEventType expectedPortalEventType,
            Action<object> onEventAction,
            Action<object> onTimeoutAction,
            int millisecondsTimeout,
            object stateTag)
        {
            this._expectedPortalEventType = expectedPortalEventType;
            this._onEventAction = onEventAction;
            this._onTimeoutAction = onTimeoutAction;
            this._stateTag = stateTag;

            this._timeoutTimer = new Timer(this.TimerCallback, stateTag, millisecondsTimeout, Timeout.Infinite);

            REPlugin.Instance.Events.Decal.ChangePortalMode += this.CharacterFilter_ChangePortalMode;
        }

        public static PortalSpaceContinuation ContinueOnEnterPortalSpace(
            Action<object> onEventAction,
            Action<object> onTimeoutAction,
            int millisecondsTimeout,
            object stateTag)
        {
            return new PortalSpaceContinuation(PortalEventType.EnterPortal, onEventAction, onTimeoutAction, millisecondsTimeout, stateTag);
        }

        public static PortalSpaceContinuation ContinueOnExitPortalSpace(
            Action<object> onEventAction,
            Action<object> onTimeoutAction,
            int millisecondsTimeout,
            object stateTag)
        {
            return new PortalSpaceContinuation(PortalEventType.ExitPortal, onEventAction, onTimeoutAction, millisecondsTimeout, stateTag);
        }

        private void CharacterFilter_ChangePortalMode(object sender, Decal.Adapter.Wrappers.ChangePortalModeEventArgs e)
        {
            lock (this._lock)
            {
                if (this._complete)
                {
                    return;
                }

                this._complete = true;

                if (e.Type == this._expectedPortalEventType)
                {
                    this._onEventAction(this._stateTag);

                    this.CleanUpAndComplete();
                }
            }
        }

        private void TimerCallback(object state)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                lock (this._lock)
                {
                    if (this._complete)
                    {
                        return;
                    }

                    this._complete = true;

                    this._onTimeoutAction(state);

                    this.CleanUpAndComplete();
                }
            });
        }

        private void CleanUpAndComplete()
        {
            this._complete = true;

            REPlugin.Instance.Events.Decal.ChangePortalMode -= this.CharacterFilter_ChangePortalMode;
            this._timeoutTimer.Dispose();
        }
    }
}
