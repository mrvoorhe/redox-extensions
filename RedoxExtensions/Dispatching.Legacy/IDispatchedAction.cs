using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Dispatching.Legacy
{
    public delegate bool TryCompleteActionDelegate();

    public delegate void DispatchedActionDelegate();

    public delegate TResult DispatchedFuncDelegate<TResult>();

    public interface IDispatchedAction
    {
        void Invoke();
    }

    public interface IDispatchedActionFunc<TResult> : IDispatchedActionFuncGeneric
    {
        TResult Invoke();
    }

    public interface IDispatchedActionFuncGeneric
    {
        object InvokeGeneric();
    }
}
