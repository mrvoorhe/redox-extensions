using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Core
{
    /// <summary>
    /// Here in case enhancements need to be made while AC is running.  Because in that case
    /// the wrapper version of this class would be modifiable.
    /// </summary>
    public class ChatWriter : Wrapper.WrapperChatWriter
    {
        public ChatWriter(IPluginServices pluginServices, string messagePrefix)
            : base(pluginServices, messagePrefix)
        {
        }
    }
}
