using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

using RedoxExtensions.Core;
using RedoxExtensions.MagToolsInterop;

namespace RedoxExtensions.Mine
{
    /// <summary>
    /// Hacks specific to my accounts
    /// </summary>
    public static class MyUtilities
    {
        public static bool IsCharacterOnMasterWhiteList(string characterName)
        {
            //switch(characterName.ToLower())
            //{
            //    // you is always okay.
            //    case "you":

            //    case "char0-1":
            //    case "char0-2":
            //    case "char0-3":

            //    case "char1-1":
            //    case "char1-2":
            //    case "char1-3":

            //    case "char2-1":

            //    case "char3-1":

            //    case "char4-1":

            //    case "char5-1":
            //        return true;
            //}

            //return false;

            throw new NotImplementedException("TODO : Reimplement loading from settings file");
        }

        public static int GetNpcSleepDelayForCurrentAccount()
        {
            return GetNpcSleepDelayForCurrentAccount(1000);
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

        private static int GetNpcSleepDelayFactorForCurrentAccount()
        {
            //switch (REPlugin.Instance.CoreManager.CharacterFilter.Name.ToLower())
            //{
            //    case "char0-1":
            //    case "char0-2":
            //    case "char0-3":
            //        return 0;
            //    case "char1-1":
            //    case "char1-2":
            //    case "char1-3":
            //        return 1;
            //    case "char2-1":
            //        return 2;
            //    case "char3-1":
            //        return 3;
            //    case "char4-1":
            //        return 4;
            //    case "char5-1":
            //        return 5;
            //    default:
            //        REPlugin.Instance.Chat.WriteLine("WARNING - Unknown delay for this character");
            //        return 0;
            //}

            throw new NotImplementedException("TODO : Reimplement loading from settings file");
        }

        public static int GetFormationRangeForCurrentCharacter(string formation)
        {
            //var charName = REPlugin.Instance.CoreManager.CharacterFilter.Name.ToLower();
            //switch (formation)
            //{
            //    case "outside":
            //        switch (charName)
            //        {
            //            case "char0-1":
            //                // char0-1 is the leader, so keep him low so that he
            //                // doesn't get stuck wasting time trying to hit an out-of-range
            //                // target
            //                return 12;
            //            case "char1-1":
            //                // char1-1 is a looter, so keep him lower so that he has time to loot
            //                return 20;
            //            case "char3-1":
            //                // char3-1 lures, so set his range high
            //                return 60;
            //            case "char4-1":
            //            case "char2-1":
            //                // Set archers higher, to get a head start.
            //                return 40;
            //            case "char5-1":
            //                // Position him as a kill focused mage
            //                return 30;
            //            case "char1-2":
            //            case "char0-2":
            //                // melees, keep close by
            //                return 10;
            //            default:
            //                return 15;
            //        }
            //    default:
            //        throw new InvalidOperationException(string.Format("Unknown formation : {0}", formation));
            //}

            throw new NotImplementedException("TODO : Reimplement loading from settings file");
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
            //MTActions.RecruitFellow("Char 1 Case Exact");

            //MTActions.RecruitFellow("Char 2 Case Exact");

            //MTActions.RecruitFellow("Char 3 Case Exact");

            //MTActions.RecruitFellow("Char 4 Case Exact");

            //MTActions.RecruitFellow("Char 5 Case Exact");

            //MTActions.RecruitFellow("Char 6 Case Exact");

            throw new NotImplementedException("TODO : Reimplement loading from settings file");
        }
    }
}
