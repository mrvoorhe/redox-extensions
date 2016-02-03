using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Wrapper
{
    public class WrapperChatWriter
    {
        #region Constants

        public const int DEFAULT_MESSAGE_COLOR = 5;
        public const int MAIN_CHAT = 1;
        public const int DEFAULT_CHAT = 0;

        #endregion

        private readonly string _messagePrefix;
        private readonly IPluginServices _pluginServices;

        public WrapperChatWriter(IPluginServices pluginServices, string messagePrefix)
        {
            this._pluginServices = pluginServices;
            this._messagePrefix = messagePrefix;
        }

        #region Internal Methods

        public void WriteLine(string text)
        {
            this._pluginServices.WriteToChat(string.Format("{0} {1}", this._messagePrefix, text), DEFAULT_MESSAGE_COLOR, DEFAULT_CHAT);
        }

        public void WriteLine(string text, params object[] args)
        {
            this._pluginServices.WriteToChat(string.Format("{0} {1}", this._messagePrefix, string.Format(text, args)), DEFAULT_MESSAGE_COLOR, DEFAULT_CHAT);
        }

        #endregion
    }
}
