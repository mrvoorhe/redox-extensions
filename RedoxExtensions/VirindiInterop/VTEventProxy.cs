using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.VirindiInterop.Events;
using RedoxExtensions.Wrapper.Diagnostics;

namespace RedoxExtensions.VirindiInterop
{
    /// <summary>
    ///
    /// </summary>
    public class VTEventProxy : IVTEventsProxy, IDisposable
    {
        private readonly bool _enableEventDebugLogging;

        private Timer _hackyDelayedRetryInitTimer;
        private bool _hookedToVTEvents;

        public VTEventProxy(bool enableEventDebugLogging)
        {
            this._enableEventDebugLogging = enableEventDebugLogging;

            this.TrySubscribeToVTEvents();
        }

        public event uTank2.PluginCore.EmptyDelegate ProfileChanged;

        public event uTank2.PluginCore.NavRouteChangedDelegate NavRouteChanged;

        public event uTank2.PluginCore.EmptyDelegate MacroStateChanged;

        public event EventHandler<SpellCastAttemptingEventArgs> SpellCastAttempting;

        public event EventHandler<SpellCastCompleteEventArgs> SpellCastComplete;

        public void TrySubscribeToVTEvents()
        {
            try
            {
                // TODO : Do something harmless first that still checks to see if utank is loaded.  Going straight to the
                // events causes VT to log an error
                // Do something harmless first to test if the assembly is loaded.  Otherwise VT may log an error.
                //var tmp = new uTank2.MySortedList<object, object>(0);

                // Currently Bugged.  Exception is throw.  See class doc for details.
                //this._vtEventsProxy = new VTEventProxy(EnableDebugEventLogger);

                uTank2.PluginCore.PC.SpellCastComplete += PC_SpellCastComplete;
                uTank2.PluginCore.PC.SpellCastAttempting += PC_SpellCastAttempting;

                this._hookedToVTEvents = true;

                REPlugin.Instance.Chat.WriteLine("***Initialization Complete - VTEventProxy****");

                if (this._hackyDelayedRetryInitTimer != null)
                {
                    this._hackyDelayedRetryInitTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    this._hackyDelayedRetryInitTimer.Dispose();
                    this._hackyDelayedRetryInitTimer = null;
                }
            }
            catch (Exception ex)
            {
                // TODO : Only catch the exception type that is throw by VT.  I believe it is type load.  But need to double check
                WrapperDebugWriter.WriteToStaticLog("RT", "Warning : Failed to subscribe to VTEvents");
                WrapperDebugWriter.WriteToStaticLog("RT", ex.Message);

                WrapperDebugWriter.WriteToStaticLog("RT", "");
                WrapperDebugWriter.WriteToStaticLog("RT", "Starting the hacky retry timer");

                if (this._hackyDelayedRetryInitTimer == null)
                {
                    this._hackyDelayedRetryInitTimer = new Timer(this.TimerCallback_RetryTryInitializeExternalPluginDependencies, null, 1000, 1000);
                }
            }
        }

        public void Dispose()
        {
            if(this._hackyDelayedRetryInitTimer != null)
            {
                this._hackyDelayedRetryInitTimer.Change(Timeout.Infinite, Timeout.Infinite);
                this._hackyDelayedRetryInitTimer.Dispose();
            }

            if (this._hookedToVTEvents)
            {
                uTank2.PluginCore.PC.SpellCastComplete -= PC_SpellCastComplete;
                uTank2.PluginCore.PC.SpellCastAttempting -= PC_SpellCastAttempting;
            }
        }

        private void PC_SpellCastComplete(int spellID, int target, int duration)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                var eventArgs = new SpellCastCompleteEventArgs(spellID, target, duration);
                if (this._enableEventDebugLogging)
                {
                    REPlugin.Instance.Debug.WriteObject(eventArgs);
                }

                if (this.SpellCastComplete != null)
                {
                    this.SpellCastComplete(uTank2.PluginCore.PC, eventArgs);
                }
            });
        }

        private void PC_SpellCastAttempting(int spellID, int target, int skill)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                var eventArgs = new SpellCastAttemptingEventArgs(spellID, target, skill);
                if (this._enableEventDebugLogging)
                {
                    REPlugin.Instance.Debug.WriteObject(eventArgs);
                }

                if (this.SpellCastAttempting != null)
                {
                    this.SpellCastAttempting(uTank2.PluginCore.PC, eventArgs);
                }
            });
        }

        private void TimerCallback_RetryTryInitializeExternalPluginDependencies(object state)
        {
            REPlugin.Instance.InvokeOperationSafely(this.TrySubscribeToVTEvents);
        }
    }
}
