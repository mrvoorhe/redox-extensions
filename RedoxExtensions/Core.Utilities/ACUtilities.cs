using Decal.Filters;
using RedoxExtensions.VirindiInterop;
using RedoxExtensions.Wrapper.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RedoxExtensions.Core.Utilities
{
    public static class ACUtilities
    {
        public static void ProcessArbitraryCommand(string command, params object[] args)
        {
            ProcessArbitraryCommand(string.Format(command, args));
        }

        public static void ProcessArbitraryCommand(string command)
        {
            if (command.StartsWith("/mt"))
            {
                MagToolsInterop.MTActions.ProcessCommand(command);
            }
            else
            {
                DecalProxy.DispatchChatToBoxWithPluginIntercept(command);
            }
        }

        public static void ProcessNativeCommand(string command, params object[] args)
        {
            ProcessNativeCommand(string.Format(command, args));
        }

        /// <summary>
        /// Processes a native AC command.  This is more efficient than giving all the plugins
        /// a chance to intercept
        /// </summary>
        /// <param name="command"></param>
        public static void ProcessNativeCommand(string command)
        {
            REPlugin.Instance.PluginHost.Actions.InvokeChatParser(command);
        }
    }
}
