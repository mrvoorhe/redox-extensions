using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;

using RedoxExtensions.Wrapper.Diagnostics;
using Decal.Adapter.Wrappers;

namespace RedoxExtensions.Wrapper
{
    /// <summary>
    /// A wrapper plugin that will dynamically load/unload Redox tools so that the dll can be updated
    /// and reloaded without having to restart the game
    /// </summary>
    [FriendlyName("RedoxExtensionsDev")]
    public class REWrapperPlugin : PluginBase, IPluginServices
    {
        private static REWrapperPlugin _instance;

        private IWrappedPlugin _wrappedPlugin;

        private PluginBaseEventsHelper _pluginBaseEventsHelper;

        private WrapperDebugWriter _wrapperDebug;
        private WrapperChatWriter _wrapperChat;
        private ErrorLogger _errorLogger;

        private WrapperCommandListener _wrapperCommandListener;

        #region Internal Static Properties

        internal static REWrapperPlugin Instance
        {
            get
            {
                return _instance;
            }
        }

        #endregion

        public CoreManager CoreManager
        {
            get
            {
                return this.Core;
            }
        }

        public PluginHost PluginHost
        {
            get
            {
                return this.Host;
            }
        }

        public WrapperChatWriter Chat
        {
            get
            {
                return this._wrapperChat;
            }
        }

        public WrapperDebugWriter Debug
        {
            get
            {
                return this._wrapperDebug;
            }
        }

        public ErrorLogger Error
        {
            get
            {
                return this._errorLogger;
            }
        }

        #region Public Methods

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
                this.Host.Actions.AddChatText(message, color, chatWindow);
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
            catch (Exception e)
            {
                this.Error.LogError(e);
            }
        }

        public void InvokeOperationSafely(SafeAction action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                this.Error.LogError(e);
            }
        }

        #endregion

        #region Internal Methods

        internal void UnloadWrappedPlugin()
        {
            if (this._wrappedPlugin == null)
            {
                this.Debug.WriteLine(string.Format("Ignoring Unload.  No Plugin Loaded"));
                return;
            }

            this._wrappedPlugin.WrappedShutdown();
            this._wrappedPlugin = null;
        }

        internal void LoadWrappedPlugin()
        {
            if (this._wrappedPlugin != null)
            {
                this.Debug.WriteLine(string.Format("Ignoring Load.  Plugin Already Loaded"));
                return;
            }

            this._wrappedPlugin = WrapperUtilities.CreateWrappedPluginInstance("RedoxExtensions");
            this._wrappedPlugin.WrappedStartup(this, this._pluginBaseEventsHelper);
        }

        #endregion

        protected override void Shutdown()
        {
            this.InvokeOperationSafely(() =>
            {
                this.Core.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
                this.Core.CharacterFilter.Logoff -= CharacterFilter_Logoff;
                this.CoreManager.PluginInitComplete -= CoreManager_PluginInitComplete;

                this.ClientDispatch -= RTWrapperPlugin_ClientDispatch;
                this.ServerDispatch -= RTWrapperPlugin_ServerDispatch;

                this._wrappedPlugin.WrappedShutdown();
                this._wrappedPlugin = null;
                this._pluginBaseEventsHelper = null;

                _instance = null;
            });
        }

        protected override void Startup()
        {
            this.InvokeOperationSafely(() =>
            {
                _instance = this;

                this.Core.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
                this.Core.CharacterFilter.Logoff += CharacterFilter_Logoff;
                this.CoreManager.PluginInitComplete += CoreManager_PluginInitComplete;
                //CoreManager.Current.PluginInitComplete += Current_PluginInitComplete;

                this._pluginBaseEventsHelper = new PluginBaseEventsHelper();

                this.ClientDispatch += RTWrapperPlugin_ClientDispatch;
                this.ServerDispatch += RTWrapperPlugin_ServerDispatch;

                this._wrappedPlugin = WrapperUtilities.CreateWrappedPluginInstance("RedoxExtensions");
                this._wrappedPlugin.WrappedStartup(this, this._pluginBaseEventsHelper);
            });
        }

        private void Initialize()
        {
            this._errorLogger = new ErrorLogger(this, "REWrapper");
            this._wrapperDebug = new WrapperDebugWriter(this, "REWrapper", "REW");
            this._wrapperChat = new WrapperChatWriter(this, "REW");

            this._wrapperCommandListener = new WrapperCommandListener();

            this.Chat.WriteLine("***Initialization Complete****");
        }

        private void CloseDown()
        {
            this.Chat.WriteLine("***Closing Down****");

            if (this._wrapperCommandListener != null)
            {
                this._wrapperCommandListener.Dispose();
            }

            this._wrapperCommandListener = null;

            this._wrapperChat = null;
            this._wrapperDebug = null;
            this._errorLogger = null;
        }


        void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            //DebugWriter.WriteToStaticLog("In CharacterFilter_LoginComplete");

            this.InvokeOperationSafely(this.Initialize);

            //this.DebugWriter.WriteLine("Leaving CharacterFilter_LoginComplete");
        }

        void CharacterFilter_Logoff(object sender, Decal.Adapter.Wrappers.LogoffEventArgs e)
        {
            this.InvokeOperationSafely(this.CloseDown);
        }

        void CoreManager_PluginInitComplete(object sender, EventArgs e)
        {
            //this.CoreManager.PluginInitComplete -= this.CoreManager_PluginInitComplete;

            this.InvokeOperationSafely(() =>
            {
                WrapperDebugWriter.WriteToStaticLog("REW", "In CoreManager_PluginInitComplete");
                this._wrappedPlugin.WrappedPluginInitComplete();
            });
        }

        void RTWrapperPlugin_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            this.InvokeOperationSafely(() => this._pluginBaseEventsHelper.FireServerDispatch(sender, e));
        }

        void RTWrapperPlugin_ClientDispatch(object sender, NetworkMessageEventArgs e)
        {
            this.InvokeOperationSafely(() => this._pluginBaseEventsHelper.FireClientDispatch(sender, e));
        }

        //void Current_PluginInitComplete(object sender, EventArgs e)
        //{
        //    this.InvokeOperationSafely(() =>
        //    {
        //        WrapperDebugWriter.WriteToStaticLog("RTW", "In Current_PluginInitComplete");
        //        this._wrappedPlugin.WrappedPluginInitComplete();
        //    });
        //}
    }
}
