﻿using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

using RedoxExtensions.MagToolsInterop;
using RedoxExtensions.Settings;

namespace RedoxExtensions.Mine
{
    /// <summary>
    /// Hacks specific to my accounts
    /// </summary>
    public static class MyUtilities
    {
        public const int NpcSleepDelayInMilliseconds = 1000;

        private static readonly HashSet<string> _whitelist = new HashSet<string>();
        private static readonly Dictionary<string, int> _sleepDelayFactorByCharacter = new Dictionary<string, int>();


        internal static void Shutdown()
        {
            _whitelist.Clear();
            _sleepDelayFactorByCharacter.Clear();
        }

        public static void Init()
        {
            Shutdown();

            int counter = 0;
            foreach (var characters in ActiveSettings.Instance.CharactersGroupedByAccount)
            {
                foreach (var character in characters)
                {
                    _whitelist.Add(character);
                    _sleepDelayFactorByCharacter.Add(character, counter);
                }

                counter++;
            }
        }

        public static bool IsCharacterOnMasterWhiteList(string characterName)
        {
            // you is always okay.
            if (characterName.ToLower() == "you")
                return true;

            return _whitelist.Contains(characterName);
        }

        public static int GetNpcSleepDelayForCurrentAccount()
        {
            return GetNpcSleepDelayForCurrentAccount(1000);
        }

        private static int GetNpcSleepDelayFactorForCurrentAccount()
        {
            return GetNpcSleepDelayFactor(REPlugin.Instance.CoreManager.CharacterFilter.Name);
        }

        public static int GetNpcSleepDelayFactor(string characterName)
        {
            int factor;
            if (_sleepDelayFactorByCharacter.TryGetValue(characterName, out factor))
            {
                return factor;
            }

            throw new InvalidOperationException(string.Format("Unknown character : {0}", characterName));
        }

        public static int GetNpcSleepDelayForCurrentAccount(int baseDelayBetweenAccounts)
        {
            return GetNpcSleepDelayFactorForCurrentAccount() * baseDelayBetweenAccounts;
        }

        public static string LookupProfileName(MyMainProfiles requestedProfile)
        {
            //if (requestedProfile == MyMainProfiles.CharacterSpecificDefault)
            //{
            //    switch (REPlugin.Instance.CoreManager.CharacterFilter.Name)
            //    {
            //        case "char3-1":
            //            return MyMainProfiles.Support.ToString();
            //        default:
            //            return MyMainProfiles.Normal.ToString();
            //    }
            //}

            //return requestedProfile.ToString();

            throw new NotImplementedException("TODO : Reimplement loading from settings file");
        }

        public static int GetFormationRangeForCurrentCharacter(string formation)
        {
            return GetFormationRange(REPlugin.Instance.CoreManager.CharacterFilter.Name, formation);
        }

        public static int GetFormationRange(string characterName, string formationName)
        {
            Formation formation;
            if (ActiveSettings.Instance.Formations.TryGetValue(formationName.ToLower(), out formation))
            {
                int range;
                if (formation.RangeTable.TryGetValue(characterName, out range))
                {
                    return range;
                }

                return formation.RangeDefault;
            }

            throw new InvalidOperationException(string.Format("Unknown formation : {0}", formationName));
        }

        public static bool EnableLootForFormationForCurrentCharacter(string formation)
        {
            //var charName = REPlugin.Instance.CoreManager.CharacterFilter.Name.ToLower();
            //switch (formation)
            //{
            //    case "outside":
            //        switch (charName)
            //        {
            //            case "char0-1":
            //            case "char1-1":
            //            case "char1-2":
            //                return true;
            //            default:
            //                return false;
            //        }
            //    default:
            //        throw new InvalidOperationException(string.Format("Unknown formation : {0}", formation));
            //}

            throw new NotImplementedException("TODO : Reimplement loading from settings file");
        }

        public static void RecruitUsualSuspects()
        {
            foreach (var character in _whitelist)
            {
                MTActions.RecruitFellow(character);
            }
        }
    }
}
