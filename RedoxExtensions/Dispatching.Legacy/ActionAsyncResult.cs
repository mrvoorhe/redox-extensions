using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RedoxExtensions.Dispatching.Legacy
{
    public class ActionAsyncResult<TResult> : IActionAsyncResult<TResult>, IActionAsyncResultCompleter
    {
        private readonly AutoResetEvent _sharedAutoCompleteEvent;
        private readonly ManualResetEvent _sharedManualCompleteEvent;

        private TResult _actionResult;

        public ActionAsyncResult(AutoResetEvent autoResetEvent)
        {
            this._sharedAutoCompleteEvent = autoResetEvent;
            this._sharedManualCompleteEvent = null;
        }

        public ActionAsyncResult(ManualResetEvent manualResetEvent)
        {
            this._sharedManualCompleteEvent = manualResetEvent;
            this._sharedAutoCompleteEvent = null;
        }

        public TResult Wait()
        {
            if (this._sharedManualCompleteEvent != null)
            {
                this._sharedManualCompleteEvent.WaitOne();
            }
            else
            {
                this._sharedAutoCompleteEvent.WaitOne();
            }

            return this._actionResult;
        }

        public void SetResult(object result)
        {
            this._actionResult = (TResult)result;

            if (this._sharedManualCompleteEvent != null)
            {
                this._sharedManualCompleteEvent.Set();
            }
            else
            {
                this._sharedAutoCompleteEvent.Set();
            }
        }
    }
}
