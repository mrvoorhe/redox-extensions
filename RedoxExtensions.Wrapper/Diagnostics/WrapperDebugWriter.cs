using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RedoxExtensions.Wrapper.Diagnostics
{
    public class WrapperDebugWriter
    {
        private const int DEFAULT_MESSAGE_COLOR = 5;
        public const int DEBUG_CHAT = 4;

        private const string StaticDebugFileName = "RTDebugStatic.txt";

        protected readonly string _currentPath;
        protected readonly object _writeLock = new object();
        private readonly string _messagePrefix;
        private readonly IPluginServices _pluginServices;

        public WrapperDebugWriter(IPluginServices pluginServices, string logNamePrefix, string logNamePostFix, string messagePrefix)
        {
            var logFileName = string.Format("{0}_Debug_{1}.txt", logNamePrefix, logNamePostFix);
            this._currentPath = Path.Combine(WrapperUtilities.LogDirectory, logFileName);
            this._messagePrefix = messagePrefix;
            this._pluginServices = pluginServices;

            this.LogLine("============================================================================");
            this.LogLine(DateTime.Now.ToString());
            this.LogLine("============================================================================");
        }

        public WrapperDebugWriter(IPluginServices pluginServices, string logNamePrefix, string messagePrefix)
            : this(pluginServices, logNamePrefix, pluginServices.CoreManager.CharacterFilter.Name, messagePrefix)
        {
        }

        #region Static Methods

        public static void WriteToStaticLog(string source, string text)
        {
            using (StreamWriter stream = new StreamWriter(Path.Combine(WrapperUtilities.LogDirectory, StaticDebugFileName), true))
            {
                stream.WriteLine(string.Format("[{0}] [{1}] [{3}] - {2}", source, DateTime.Now.ToString(), text, System.Threading.Thread.CurrentThread.ManagedThreadId));
            }
        }

        #endregion

        #region Public Methods

        #region WriteObject

        //public void WriteObject(ChatTextInterceptEventArgs obj)
        //{
        //    this.WriteObject(obj, false);
        //}

        //public void LogObject(ChatTextInterceptEventArgs obj)
        //{
        //    this.WriteObject(obj, true);
        //}

        //public void WriteObject(ChatTextInterceptEventArgs obj, bool toLogFileOnly)
        //{
        //    lock (_writeLock)
        //    {
        //        using (StreamWriter stream = new StreamWriter(this._currentPath, true))
        //        {
        //            this.LogRawMessage(this.FormatWithPrefix("ChatTextInterupt"), stream, toLogFileOnly);
        //            this.LogRawMessage(string.Format("  Target = {0}", obj.Target), stream, toLogFileOnly);
        //            this.LogRawMessage(string.Format("  Text = {0}", obj.Text), stream, toLogFileOnly);

        //            //this.WriteCurrentStateStuff(stream, toLogFileOnly);
        //        }
        //    }
        //}

        #endregion

        #region WriteLines

        public void WriteLine(string text)
        {
            this._pluginServices.WriteToChat(this.FormatWithPrefix(text), DEFAULT_MESSAGE_COLOR, DEBUG_CHAT);

            // Log to file as well
            this.LogLine(text);
        }

        public void WriteLine(string text, params object[] args)
        {
            this.WriteLine(string.Format(text, args));
        }

        #endregion

        #endregion

        #region Private Methods

        protected void WriteCurrentStateStuff(StreamWriter echoStream, bool toLogFileOnly)
        {
            this.LogRawMessage("  ---State Data---", echoStream, toLogFileOnly);
            this.LogRawMessage(string.Format("  ThreadId = {0}", System.Threading.Thread.CurrentThread.ManagedThreadId), echoStream, toLogFileOnly);
            this.LogRawMessage("", echoStream, toLogFileOnly);
        }

        protected string FormatWithPrefix(string text)
        {
            return string.Format("[{0}-DEBUG] {1}", this._messagePrefix, text);
        }

        protected void LogRawMessage(string text, StreamWriter echoStream)
        {
            this.LogRawMessage(text, echoStream, false);
        }

        protected void LogRawMessage(string text, StreamWriter echoStream, bool toLogFileOnly)
        {
#if DEBUG
            if (!toLogFileOnly)
            {
                this._pluginServices.WriteToChat(text, DEFAULT_MESSAGE_COLOR, DEBUG_CHAT);
            }
#endif

            echoStream.WriteLine((string.Format("[{0}] [{1}] [{3}] - {2}", this._messagePrefix , DateTime.Now.ToString(), text, System.Threading.Thread.CurrentThread.ManagedThreadId)));
        }

        private void LogLine(string text)
        {
            lock (_writeLock)
            {
                using (StreamWriter stream = new StreamWriter(this._currentPath, true))
                {
                    stream.WriteLine(text);
                }
            }
        }

        #endregion
    }
}
