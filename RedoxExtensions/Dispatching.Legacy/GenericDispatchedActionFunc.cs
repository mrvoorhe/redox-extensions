using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Dispatching.Legacy
{
    public class GenericDispatchedActionFunc<TResult> : IDispatchedActionFunc<TResult>
    {
        private readonly DispatchedFuncDelegate<TResult> _actionFunc;

        public GenericDispatchedActionFunc(DispatchedFuncDelegate<TResult> actionFunc)
        {
            this._actionFunc = actionFunc;
        }

        public TResult Invoke()
        {
            return this._actionFunc();
        }

        public object InvokeGeneric()
        {
            return this.Invoke();
        }
    }
}
