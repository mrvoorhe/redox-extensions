using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RedoxExtensions.Dispatching.Legacy
{
    public interface IActionAsyncResult<TResult>
    {
        TResult Wait();
    }
}
