using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Wrapper.Diagnostics;

namespace RedoxExtensions.Wrapper
{
    public delegate void SafeAction();

    public interface IPluginServices : IRealPluginBase, IWriteToChat
    {
        WrapperDebugWriter Debug { get; }
        WrapperChatWriter Chat { get; }
        ErrorLogger Error { get; }

        void InvokeOperationSafely(SafeAction action);
        void InvokeOperationSafely(Action<object> action, object arg);
    }
}
