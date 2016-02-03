using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.MagToolsInterop
{
    /// <summary>
    /// Helper class for running MagTools commands
    /// </summary>
    internal static class MTActions
    {
        internal static void ProcessCommand(string mtCommand)
        {
            MagTools.PluginCore.Current.ProcessMTCommand(mtCommand);
        }

        internal static void CreateFellow(string name)
        {
            MagTools.PluginCore.Current.ProcessMTCommand(string.Format("/mt fellow create {0}", name));
        }

        internal static void RecruitFellow(string characterName)
        {
            MagTools.PluginCore.Current.ProcessMTCommand(string.Format("/mt fellow recruit {0}", characterName));
        }

        #region Click

        internal static void ClickOkay()
        {
            MagTools.PluginCore.Current.ProcessMTCommand("/mt ok");
        }

        internal static void ClickYes()
        {
            MagTools.PluginCore.Current.ProcessMTCommand("/mt yes");
        }

        internal static void ClickNo()
        {
            MagTools.PluginCore.Current.ProcessMTCommand("/mt no");
        }

        #endregion
    }
}
