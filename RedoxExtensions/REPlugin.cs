using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Core;
using RedoxExtensions.Data;
using RedoxExtensions.Diagnostics;
using RedoxExtensions.Wrapper;
using RedoxExtensions.Wrapper.Diagnostics;
using System.Threading;

using RedoxLib;
using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions
{
    [FriendlyName("RedoxExtensions")]
    public class REPlugin : PluginBase, IWrappedPlugin, IPluginServices, IDecalPluginProvider
    {
        #region Constants

        #endregion

        private static REPlugin _instance;

        #region Instance Data

        private readonly object _classLock = new object();

        private bool _startupComplete;
        private bool _shutdownComplete;
        private bool _initializeComplete;
        private bool _closeDownComplete;

        private IRealPluginBase _realPlugin;
        private IPluginBaseEvents _realPluginBaseEvents;

        private PluginBaseEventsHelper _pluginBaseEventsHelper;

        private DebugWriter _debugWriter;
        private ChatWriter _chatWriter;
        private ErrorLogger _errorLogger;

        private EventsManager _eventsManager;
        private DispatchManager _dispatchManager;
        private MonitorManager _monitorManager;
        private CommandListenerManager _commandListenerManager;

        private RTSettings _settings;

        #endregion

        #region Internal Static Properties

        internal static REPlugin Instance
        {
            get
            {
                return _instance;
            }
        }

        #endregion

        #region Internal Properties

        #region My Objects

        public ChatWriter Chat
        {
            get
            {
                return this._chatWriter;
            }
        }

        public DebugWriter Debug
        {
            get
            {
                return this._debugWriter;
            }
        }

        public ErrorLogger Error
        {
            get
            {
                return this._errorLogger;
            }
        }

        internal RTSettings Settings
        {
            get
            {
                return this._settings;
            }
        }

        internal EventsManager Events
        {
            get
            {
                return this._eventsManager;
            }
        }

        internal DispatchManager Dispatch
        {
            get
            {
                return this._dispatchManager;
            }
        }

        internal MonitorManager MonitorManager
        {
            get
            {
                return this._monitorManager;
            }
        }

        internal IPluginBaseEvents PluginBaseEvents
        {
            get
            {
                if (this._realPluginBaseEvents != null)
                {
                    return this._realPluginBaseEvents;
                }

                return this._pluginBaseEventsHelper;
            }
        }

        #endregion

        #region Decal Objects

        public CoreManager CoreManager
        {
            get
            {
                // Check if using wrapper
                if (this._realPlugin != null)
                {
                    return this._realPlugin.CoreManager;
                }

                return this.Core;
            }
        }

        public PluginHost PluginHost
        {
            get
            {
                // Check if using wrapper
                if (this._realPlugin != null)
                {
                    return this._realPlugin.PluginHost;
                }

                return this.Host;
            }
        }

        public CharacterFilter CharacterFilter
        {
            get
            {
                return this.CoreManager.CharacterFilter;
            }
        }

        public WorldFilter WorldFilter
        {
            get
            {
                return this.CoreManager.WorldFilter;
            }
        }

        public HooksWrapper Actions
        {
            get
            {
                return this.PluginHost.Actions;
            }
        }

        #endregion

        #endregion

        #region Public Methods

        public void WrappedStartup(IRealPluginBase realPlugin, IPluginBaseEvents realPluginBaseEvents)
        {
            this._realPlugin = realPlugin;
            this._realPluginBaseEvents = realPluginBaseEvents;
            this.Startup();

            if (this.CoreManager.CharacterFilter.LoginStatus == 3)
            {
                this.Initialize();

                this.TryInitializeExternalPluginDependencies();

                this.Chat.WriteLine("***Initialized After Reload****");
            }
        }

        public void WrappedShutdown()
        {
            this.CloseDown();
            this.Shutdown();
        }


        public void WrappedPluginInitComplete()
        {
            this.TryInitializeExternalPluginDependencies(false, true);
        }

        public void WriteToChat(string message, int color, ChatMessageWindow chatWindow)
        {
            this.WriteToChat(message, color, (int)chatWindow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        /// <param name="chatWindow">0=Default, 1=Main,2-5=1-4 Windows</param>
        public void WriteToChat(string message, int color, int chatWindow)
        {
            try
            {
                this.Actions.AddChatText(message, color, chatWindow);
            }
            catch (Exception e)
            {
                this.Error.LogError(e);
            }
        }

        public void InvokeOperationSafely(Action<object> action, object arg)
        {
            try
            {
                action(arg);
            }
            catch (DisplayToUserException e)
            {
                ExceptionUtilities.HandleDisplayToUserException(e);
            }
            catch (Exception e)
            {
                ExceptionUtilities.HandleUnexpectedException(e);
            }
        }

        public void InvokeOperationSafely(SafeAction action)
        {
            try
            {
                action();
            }
            catch(DisplayToUserException e)
            {
                ExceptionUtilities.HandleDisplayToUserException(e);
            }
            catch (Exception e)
            {
                ExceptionUtilities.HandleUnexpectedException(e);
            }
        }

        #endregion

        #region Protected Methods

        protected override void Shutdown()
        {
            lock (this._classLock)
            {
                // If we've already shutdown, then return
                if (this._shutdownComplete)
                {
                    WrapperDebugWriter.WriteToStaticLog("RT", "Skipping Shutdown.  Already Complete");
                    return;
                }

                //WrapperDebugWriter.WriteToStaticLog("RT", "In Shutdown");

                //this.CoreManager.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
                //this.CoreManager.CharacterFilter.Logoff -= CharacterFilter_Logoff;
                this.CoreManager.PluginInitComplete -= CoreManager_PluginInitComplete;

                try
                {
                    this.InvokeOperationSafely(() =>
                    {
                        // Attempt to fix relog bug : try and CloseDown again.  Maybe we missed the logoff event?
                        this.CloseDown();
                    });
                }
                catch (Exception)
                {
                    // If invoke failed to log an error, there is not much we could do here aside from let the game crash.
                }

                this._shutdownComplete = true;

                // Reset the start up flag
                this._startupComplete = false;

                _instance = null;
            }
        }

        protected override void Startup()
        {
            lock (this._classLock)
            {
                // Make sure startup hasn't been called on a second plugin instance
                if (_instance != null && _instance != this)
                {
                    WrapperDebugWriter.WriteToStaticLog("RT", "");
                    WrapperDebugWriter.WriteToStaticLog("RT", "*****************WARNING********************");
                    WrapperDebugWriter.WriteToStaticLog("RT", "Startup has been called on an RTPlugin instance, while a different instance is currently the active singleton instance");
                    WrapperDebugWriter.WriteToStaticLog("RT", "Startup of this instance will return immediately and NOT attached to game events");
                    WrapperDebugWriter.WriteToStaticLog("RT", "*****************WARNING********************");
                    WrapperDebugWriter.WriteToStaticLog("RT", "");
                    return;
                }

                _instance = this;

                // My attempt to try and resolve the mutiple active instances after reloggging issue
                // Start up was called aready, but at least this instance is already the singleton.  So it should be okay to ignore this
                if (this._startupComplete)
                {
                    WrapperDebugWriter.WriteToStaticLog("RT", "Skipping start up.  Already Complete");
                    return;
                }

                this.CoreManager.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
                this.CoreManager.PluginInitComplete += CoreManager_PluginInitComplete;
                this.CoreManager.CharacterFilter.Logoff += CharacterFilter_Logoff;

                Mine.MyUtilities.Init();

                this._startupComplete = true;

                // Reset the shutdown flag
                this._shutdownComplete = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when a character logs in
        /// </summary>
        private void Initialize()
        {
            lock (this._classLock)
            {
                // Some how initialize was called multiple times.  Ignore it
                if (this._initializeComplete)
                {
                    WrapperDebugWriter.WriteToStaticLog("RT", "Skipping Initialize.  Already Complete");
                    return;
                }

                try
                {
                    // Cache ourself with the F# layer so that the library code can access decal
                    // singletons
                    PluginProvider.Set(this);

                    this._settings = RTSettings.Resume(this.CharacterFilter.Name);

                    this._errorLogger = new ErrorLogger(this, "RT");
                    this._debugWriter = new DebugWriter(this, "RT", "RT");
                    this._chatWriter = new ChatWriter(this, "RT");

                    // If not using wrapper, we have to hook up the logic to support the plugin base events
                    if (this._realPluginBaseEvents == null)
                    {
                        this._pluginBaseEventsHelper = new PluginBaseEventsHelper();
                        this.ClientDispatch += RTPlugin_ClientDispatch;
                        this.ServerDispatch += RTPlugin_ServerDispatch;
                    }

                    //this.TryInitializeExternalPluginDependencies();

                    this._eventsManager = new EventsManager();

                    this._dispatchManager = new DispatchManager(this.Events.Decal);

                    this._monitorManager = new MonitorManager((IREEventsFireCallbacks)this.Events.RE);

                    this._commandListenerManager = new CommandListenerManager(this.Events.Decal);

                    this.Chat.WriteLine("***Initialization Complete****");
                }
                finally
                {
                    this._initializeComplete = true;

                    // Reset close down flag
                    this._closeDownComplete = false;
                }
            }
        }

        //private void TimerCallback_RetryTryInitializeExternalPluginDependencies(object state)
        //{
        //    this.InvokeOperationSafely(this.TryInitializeExternalPluginDependencies);
        //}

        private void TryInitializeExternalPluginDependencies()
        {
            this.TryInitializeExternalPluginDependencies(false, false);
        }

        private void TryInitializeExternalPluginDependencies(bool calledFromTimer, bool calledFromWrapperPluginInit)
        {
            //WrapperDebugWriter.WriteToStaticLog("RT", "In TryInitializeExternalPluginDependencies");
            //WrapperDebugWriter.WriteToStaticLog("RT", string.Format("   Called From Timer = {0}", calledFromTimer));
            //WrapperDebugWriter.WriteToStaticLog("RT", string.Format("   Called From Wrapped Plugin Init = {0}", calledFromWrapperPluginInit));

            //this._vtEventsProxy.TrySubscribeToVTEvents();

            // This is going to get called multiple times.  If VT isn't loaded yet, this will fail.  That's fine.

            //if (this._vtEventsProxy == null)
            //{
            //    try
            //    {
            //        // TODO : Do something harmless first that still checks to see if utank is loaded.  Going straight to the
            //        // events causes VT to log an error
            //        // Do something harmless first to test if the assembly is loaded.  Otherwise VT may log an error.
            //        //var tmp = new uTank2.MySortedList<object, object>(0);

            //        // Currently Bugged.  Exception is throw.  See class doc for details.
            //        this._vtEventsProxy = new VTEventProxy(EnableDebugEventLogger);

            //        this.ChatWriter.WriteLine("***Initialization Complete - VTEventProxy****");

            //        if (this._hackyDelayedRetryInitTimer != null)
            //        {
            //            this._hackyDelayedRetryInitTimer.Change(Timeout.Infinite, Timeout.Infinite);
            //            this._hackyDelayedRetryInitTimer.Dispose();
            //            this._hackyDelayedRetryInitTimer = null;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        WrapperDebugWriter.WriteToStaticLog("RT", "Warning : Failed to create VTEventProxy");
            //        WrapperDebugWriter.WriteToStaticLog("RT", ex.Message);

            //        WrapperDebugWriter.WriteToStaticLog("RT", "");
            //        WrapperDebugWriter.WriteToStaticLog("RT", "Starting the hacky retry timer");

            //        if (this._hackyDelayedRetryInitTimer == null)
            //        {
            //            this._hackyDelayedRetryInitTimer = new Timer(this.TimerCallback_RetryTryInitializeExternalPluginDependencies, null, 1000, 1000);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Called when a character logs off
        /// </summary>
        private void CloseDown()
        {
            lock (this._classLock)
            {
                try
                {
                    this.Chat.WriteLine("***Closing Down****");

                    //if (this._hackyDelayedRetryInitTimer != null)
                    //{
                    //    this._hackyDelayedRetryInitTimer.Dispose();
                    //    this._hackyDelayedRetryInitTimer = null;
                    //}

                    if (this._commandListenerManager != null)
                    {
                        this._commandListenerManager.Dispose();
                        this._commandListenerManager = null;
                    }

                    if (this._settings != null)
                    {
                        this._settings.Save();
                        this._settings = null;
                    }

                    if (this._monitorManager != null)
                    {
                        this._monitorManager.Dispose();
                        this._monitorManager = null;
                    }

                    if (this._dispatchManager != null)
                    {
                        this._dispatchManager.Dispose();
                        this._dispatchManager = null;
                    }

                    if (this._eventsManager != null)
                    {
                        this._eventsManager.Dispose();
                        this._eventsManager = null;
                    }

                    // If not using wrapper, we have to unhook the dispatch events we hooked up too
                    if (this._realPluginBaseEvents == null)
                    {
                        this.ClientDispatch -= RTPlugin_ClientDispatch;
                        this.ServerDispatch -= RTPlugin_ServerDispatch;
                        this._realPluginBaseEvents = null;
                    }

                    PluginProvider.Clear();

                    this._chatWriter = null;
                    this._debugWriter = null;
                    this._errorLogger = null;
                }
                finally
                {
                    this._closeDownComplete = true;

                    // Reset initialize flag
                    this._initializeComplete = false;
                }
            }
        }

        #region Event Handlers

        void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            WrapperDebugWriter.WriteToStaticLog("RT", "In CharacterFilter_LoginComplete");

            // Unhook from login immediately to help avoid duplicate initialization.
            this.CoreManager.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;

            this.InvokeOperationSafely(this.Initialize);

            this.Debug.WriteLine("Leaving CharacterFilter_LoginComplete");
        }

        void CharacterFilter_Logoff(object sender, Decal.Adapter.Wrappers.LogoffEventArgs e)
        {
            // Unhook once we done with the event
            this.CoreManager.CharacterFilter.Logoff -= CharacterFilter_Logoff;

            this.InvokeOperationSafely(this.CloseDown);
        }

        void CoreManager_PluginInitComplete(object sender, EventArgs e)
        {
            this.InvokeOperationSafely(this.TryInitializeExternalPluginDependencies);
        }

        void RTPlugin_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            this.InvokeOperationSafely(() => this._pluginBaseEventsHelper.FireServerDispatch(sender, e));
        }

        void RTPlugin_ClientDispatch(object sender, NetworkMessageEventArgs e)
        {
            this.InvokeOperationSafely(() => this._pluginBaseEventsHelper.FireClientDispatch(sender, e));
        }

        #endregion

        #endregion

        #region Explicit IPluginServices Implementation

        WrapperDebugWriter IPluginServices.Debug
        {
            get
            {
                return this.Debug;
            }
        }

        WrapperChatWriter IPluginServices.Chat
        {
            get
            {
                return this.Chat;
            }
        }

        ErrorLogger IPluginServices.Error
        {
            get
            {
                return this.Error;
            }
        }

        void IPluginServices.InvokeOperationSafely(SafeAction action)
        {
            this.InvokeOperationSafely(action);
        }

        void IPluginServices.InvokeOperationSafely(Action<object> action, object arg)
        {
            this.InvokeOperationSafely(action, arg);
        }

        CoreManager IRealPluginBase.CoreManager
        {
            get
            {
                return this.CoreManager;
            }
        }

        PluginHost IRealPluginBase.PluginHost
        {
            get
            {
                return this.PluginHost;
            }
        }

        #endregion
    }
}
