using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;

namespace RedoxExtensions.Actions
{
    /// <summary>
    /// Actions related to telling
    /// </summary>
    public static class TellActions
    {
        private const string MyVirindiFellowName = "Redoxrules";

        public static void TellFellow(string text)
        {
            // TODO : Find a reliable way to determine if we are in a fellow or not.
            TellNativeFellow(text);
            //if (RTPlugin.Instance.MonitorManager.CharacterState.InFellowship.WaitOne(0))
            //{
            //    TellNativeFellow(text);
            //}
            //else
            //{
            //    // Auto fallback on Virindi Fellow
            //    TellVirindiFellow(MyVirindiFellowName, text);
            //}
        }

        public static void TellFellow(string text, params object[] args)
        {
            TellFellow(string.Format(text, args));
        }

        public static void TellNativeFellow(string text)
        {
            ACUtilities.ProcessNativeCommand(string.Format("/f {0}", text));
        }

        public static void TellNativeFellow(string text, params object[] args)
        {
            TellNativeFellow(string.Format(text, args));
        }

        public static void TellVirindiFellow(string viFellowName, string text)
        {
            ACUtilities.ProcessArbitraryCommand(string.Format("/vim {0} {1}", viFellowName, text));
        }

        public static void TellVirindiFellow(string viFellowName, string text, params object[] args)
        {
            TellVirindiFellow(viFellowName, string.Format(text, args));
        }

        public static void TellPlayer(string player, string text)
        {
            ACUtilities.ProcessNativeCommand(string.Format("/t {0}, {1}", player, text));
        }

        public static void TellPlayer(string player, string text, params object[] args)
        {
            TellPlayer(player, string.Format(text, args));
        }
    }
}
