using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RedoxExtensions.Dispatching.Legacy
{
    /// <summary>
    /// A standalone action async result that doesn't need any resources passed in.
    /// However, this comes at the expense of the object needing to be disposable
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IStandaloneActionAsyncResult<TResult> : IActionAsyncResult<TResult>, IDisposable
    {
        WaitHandle WaitHandle { get; }
    }
}
