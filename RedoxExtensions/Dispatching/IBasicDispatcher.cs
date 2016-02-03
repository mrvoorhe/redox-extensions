using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Dispatching
{
    public interface IBasicDispatcher
    {
        void EnqueueAction(Action action);
    }
}
