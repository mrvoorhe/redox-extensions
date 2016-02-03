using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RedoxExtensions.Dispatching.Legacy
{
    public class StandaloneActionAsyncResult<TResult> : IStandaloneActionAsyncResult<TResult>, IActionAsyncResultCompleter
    {
        private readonly ManualResetEvent _completeEvent = new ManualResetEvent(false);
        private TResult _actionResult;

        public System.Threading.WaitHandle WaitHandle
        {
            get
            {
                return this._completeEvent;
            }
        }

        public TResult Wait()
        {
            this._completeEvent.WaitOne();
            return this._actionResult;
        }

        public void Dispose()
        {
            this._completeEvent.Close();
        }

        public void SetResult(object result)
        {
            this._actionResult = (TResult)result;
            this._completeEvent.Set();
        }
    }
}
