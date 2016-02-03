using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Dispatching.Legacy
{
    public interface IActionAsyncResultCompleter
    {
        void SetResult(object result);
    }
}
