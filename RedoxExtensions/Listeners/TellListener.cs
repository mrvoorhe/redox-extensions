using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Listeners
{
    public class TellListener : IDisposable
    {
        private readonly string _expectedSourceName;
        private readonly ManualResetEvent _tellReceivedWaitHandle = new ManualResetEvent(false);
        private readonly bool _unhookOnMatch;

        private bool _unhooked;

        private TellListener(string expectedSourceName, bool unhookOnMatch)
        {
            this._expectedSourceName = expectedSourceName;
            this._unhookOnMatch = unhookOnMatch;
            REPlugin.Instance.CoreManager.CommandLineText += CoreManager_CommandLineText;
        }

        public static TellListener Begin(string expectedSourceName, bool unhookOnMatch)
        {
            return new TellListener(expectedSourceName, unhookOnMatch);
        }

        public WaitHandle TellReceived
        {
            get
            {
                return this._tellReceivedWaitHandle;
            }
        }

        public void Reset()
        {
            if (this._unhookOnMatch)
            {
                throw new InvalidOperationException("Reset can't be used when unhookOnMatch is true");
            }

            this._tellReceivedWaitHandle.Reset();
        }

        public void Dispose()
        {
            if (!this._unhooked)
            {
                REPlugin.Instance.CoreManager.CommandLineText -= CoreManager_CommandLineText;
            }

            this._tellReceivedWaitHandle.Close();
        }

        private void CoreManager_CommandLineText(object sender, Decal.Adapter.ChatParserInterceptEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                ChatEvent chatEvent;
                if (ChatEvent.TryParse(e.Text, out chatEvent))
                {
                    if (chatEvent.SourceName.Equals(this._expectedSourceName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        this._tellReceivedWaitHandle.Set();

                        if (this._unhookOnMatch)
                        {
                            this._unhooked = true;
                            REPlugin.Instance.CoreManager.CommandLineText -= CoreManager_CommandLineText;
                        }
                    }
                }
            });
        }
    }
}
