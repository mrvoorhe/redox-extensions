using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;

namespace RedoxExtensions.Wrapper
{
    /// <summary>
    /// Class responsible for listening for, and handling of, global commands.
    /// 
    /// Ex:  /rt do something
    /// </summary>
    internal class WrapperCommandListener : IDisposable
    {
        private const string CommandPrefix = "/re";
        private const string CommandPrefix_Legacy = "/rew";

        internal WrapperCommandListener()
        {
            REWrapperPlugin.Instance.CoreManager.CommandLineText += DecalCore_CommandLineText;
        }

        public void Dispose()
        {
            REWrapperPlugin.Instance.CoreManager.CommandLineText -= DecalCore_CommandLineText;
        }

        #region Private Methods


        private bool HandleNormalCommand(string command, bool explicitWrapperCommand)
        {
            REWrapperPlugin.Instance.Debug.WriteLine(string.Format("Processing Command : {0}", command));

            switch (command.ToLower())
            {
                case "unload":
                    REWrapperPlugin.Instance.UnloadWrappedPlugin();
                    return true;
                case "load":
                    REWrapperPlugin.Instance.LoadWrappedPlugin();
                    return true;
                case "reload":
                    REWrapperPlugin.Instance.Debug.WriteLine("Reloading...");

                    REWrapperPlugin.Instance.UnloadWrappedPlugin();
                    REWrapperPlugin.Instance.LoadWrappedPlugin();

                    REWrapperPlugin.Instance.Debug.WriteLine("Reload Complete");
                    return true;
                default:
                    if (explicitWrapperCommand)
                    {
                        REWrapperPlugin.Instance.Chat.WriteLine("Unknown Command : {0}", command);
                        return true;
                    }

                    return false;
            }
        }

        #endregion

        #region Event Handlers

        private void DecalCore_CommandLineText(object sender, Decal.Adapter.ChatParserInterceptEventArgs e)
        {
            REWrapperPlugin.Instance.InvokeOperationSafely(() =>
            {
                if (e.Text.StartsWith(CommandPrefix_Legacy))
                {
                    var command = e.Text.Substring(CommandPrefix_Legacy.Length + 1).Trim();

                    e.Eat = this.HandleNormalCommand(command, true);
                }
                else if (e.Text.StartsWith(CommandPrefix))
                {
                    var command = e.Text.Substring(CommandPrefix.Length + 1).Trim();

                    e.Eat = this.HandleNormalCommand(command, false);
                }
            });
        }

        #endregion
    }
}
