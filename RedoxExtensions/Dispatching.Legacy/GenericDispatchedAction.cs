using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Dispatching.Legacy
{
    public class GenericDispatchedAction : IDispatchedAction
    {
        private readonly DispatchedActionDelegate _action;

        public GenericDispatchedAction(DispatchedActionDelegate action)
        {
            this._action = action;
        }

        public void Invoke()
        {
            this._action();
        }
    }
}
