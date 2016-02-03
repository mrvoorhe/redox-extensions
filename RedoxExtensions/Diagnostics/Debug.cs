using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Wrapper;
using RedoxExtensions.Wrapper.Diagnostics;

namespace RedoxExtensions.Diagnostics
{
    /// <summary>
    /// Shortcut to writing debug messages
    /// </summary>
    public static class Debug
    {
        private const string DefaultDebugMessagePrefix = "[RT-DEBUG]";

        private const int DEFAULT_MESSAGE_COLOR = 5;

        private static IWriteToChat _temporaryWriter = null;

        private static IWriteToChat Writer
        {
            get
            {
                if (_temporaryWriter != null)
                {
                    return _temporaryWriter;
                }

                return REPlugin.Instance;
            }
        }

        public static IDisposable CreateTemporaryWriterScope(IWriteToChat writer)
        {
            SetTemporaryWriter(writer);
            return new RestoreWriterScope();
        }

        public static void SetTemporaryWriter(IWriteToChat writer)
        {
            _temporaryWriter = writer;
        }

        public static void RestoreDefaultWriter()
        {
            _temporaryWriter = null;
        }

        public static void WriteLine(string text)
        {
            WriteLine(text, WrapperDebugWriter.DEBUG_CHAT);
        }

        public static void WriteLine(string text, params object[] args)
        {
            WriteLine(string.Format(text, args));
        }

        public static void WriteLineToMain(string text)
        {
            WriteLine(text, WrapperChatWriter.MAIN_CHAT);
        }

        public static void WriteLineToMain(string text, params object[] args)
        {
            WriteLineToMain(string.Format(text, args));
        }

        private static void WriteLine(string text, int chatWindow)
        {
            if (Writer == null)
            {
                throw new InvalidOperationException("These debug helpers cannot be used until the Plugin instance has been created");
            }

            Writer.WriteToChat(string.Format("{0} {1}", DefaultDebugMessagePrefix, text), DEFAULT_MESSAGE_COLOR, chatWindow);
        }

        #region Inner Class

        private class RestoreWriterScope : IDisposable
        {
            public RestoreWriterScope()
            {
            }

            public void Dispose()
            {
                Debug.RestoreDefaultWriter();
            }
        }

        #endregion
    }
}
