using RedoxExtensions.Wrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Mine;

namespace RedoxExtensions.VirindiInterop
{
    public static class VTUtilities
    {
        private static readonly string MyDefaultVTankDirectory = Path.Combine(Path.Combine(WrapperUtilities.RedoxExtensionsBinDirectory, ".."), "VirindiPlugins\\VirindiTank");

        private const string VTOptionPrefix = "[VTank] Option";

        private const string VTStartText = "[VTank] Macro started.";
        private const string VTStopText = "[VTank] Macro stopped.";

        static VTUtilities()
        {
            VTankDirectory = MyDefaultVTankDirectory;
        }

        public static string VTankDirectory { get; set; }

        #region Option Helpers

        public static Dictionary<string, string> GetVTOptions(IEnumerable<string> optionNames)
        {
            using (var captureContext = new VTCaptureContext())
            {
                captureContext.QueryOptions(optionNames);

                return captureContext.CapturedSettings;
            }
        }

        public static void SetOption(KeyValuePair<string, string> keyValue)
        {
            SetOption(keyValue.Key, keyValue.Value);
        }

        public static void SetOption(string optionName, string optionValue)
        {
            ACUtilities.ProcessArbitraryCommand(string.Format("/vt opt set {0} {1}", optionName, optionValue));
        }

        public static void SetOption(string optionName, bool enabledValue)
        {
            SetOption(optionName, enabledValue ? "True" : "False");
        }

        public static void SetOptions(IEnumerable<KeyValuePair<string, string>> optionsAndValues)
        {
            foreach (var keyValue in optionsAndValues)
            {
                SetOption(keyValue);
            }
        }

        public static void SetRangeOptionWhenHumanValue(KeyValuePair<string, string> keyValue)
        {
            SetRangeOptionWhenHumanValue(keyValue.Key, keyValue.Value);
        }

        public static void SetRangeOptionWhenHumanValue(string optionName, string rangeValue)
        {
            SetRangeOption(optionName, HumanRangeToVTRange(rangeValue));
        }

        public static void SetRangeOptionWhenHumanValue(string optionName, int rangeValue)
        {
            SetRangeOption(optionName, HumanRangeToVTRange(rangeValue));
        }

        public static void SetRangeOption(string optionName, double rangeValue)
        {
            ACUtilities.ProcessArbitraryCommand(string.Format("/vt opt set {0} {1}", optionName, rangeValue));
        }


        public static bool TryParseOptionOutput(string anyOutput, out KeyValuePair<string, string> keyValue)
        {
            if (anyOutput.StartsWith(VTOptionPrefix))
            {
                keyValue = ParseOptionOutput(anyOutput);
                return true;
            }

            keyValue = new KeyValuePair<string, string>(null, null);
            return false;
        }

        public static KeyValuePair<string, string> ParseOptionOutput(string optionOutput)
        {
            // Ex: [VTank] Option EnableLooting = False
            var splitKeyValue = optionOutput.Substring(VTOptionPrefix.Length + 1).Split('=');
            return new KeyValuePair<string, string>(splitKeyValue[0].Trim(), splitKeyValue[1].Trim());
        }

        public static double HumanRangeToVTRange(string humanRange)
        {
            return HumanRangeToVTRange(int.Parse(humanRange));
        }

        public static double HumanRangeToVTRange(int humanRange)
        {
            return ((double)humanRange) / 240;
        }

        #endregion

        #region VT Output Parsing

        public static bool IsVTStartNotification(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.StartsWith(VTStartText);
        }

        public static bool IsVTStopNotification(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.StartsWith(VTStopText);
        }

        #endregion

        #region Navigation Helpers

        public static string GetFollowNavForCharacter(string characterName)
        {
            string possibleFileName = string.Format("Follow{0}.nav", characterName);

            if (File.Exists(Path.Combine(VTankDirectory, possibleFileName)))
            {
                return possibleFileName;
            }

            // Next try the charactername, spaces removed.
            if (characterName.Contains(" "))
            {
                possibleFileName = string.Format("Follow{0}.nav", characterName.Replace(" ", ""));

                if (File.Exists(Path.Combine(VTankDirectory, possibleFileName)))
                {
                    return possibleFileName;
                }
            }

            // Next each individual word in the charaters name

            var partialNames = characterName.Split(' ');

            if (partialNames.Length > 1)
            {
                foreach (string partialName in partialNames)
                {
                    possibleFileName = string.Format("Follow{0}.nav", partialName);

                    if (File.Exists(Path.Combine(VTankDirectory, possibleFileName)))
                    {
                        return possibleFileName;
                    }
                }
            }

            return string.Empty;
        }

        #endregion

        #region Profile Helpers

        public static string GetCharacterSpecificProfileFullFilePath(string profileName)
        {
            string currentCharacter = REPlugin.Instance.CoreManager.CharacterFilter.Name;
            string currentServer = REPlugin.Instance.CoreManager.CharacterFilter.Server;
            return GetCharacterSpecificProfileFullFilePath(profileName, currentCharacter, currentServer);
        }

        public static string GetCharacterSpecificProfileFullFilePath(string profileName, string characterName, string server)
        {
            return Path.Combine(VTankDirectory, string.Format("--{0}_{1}_{2}.usd", characterName, server, profileName));
        }

        public static string GetAnyProfileFullFilePath(string profileName)
        {
            return Path.Combine(VTankDirectory, string.Format("{0}.usd", profileName));
        }

        #endregion
    }
}
