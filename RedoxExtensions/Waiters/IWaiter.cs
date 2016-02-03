using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Waiters
{
    public interface IWaiter : IDisposable
    {
        //void Reset();

        void Wait();

        bool Wait(int millisecondsTimeout);
    }
}
