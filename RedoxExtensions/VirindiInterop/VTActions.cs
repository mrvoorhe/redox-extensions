using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Mine;

namespace RedoxExtensions.VirindiInterop
{
    /// <summary>
    /// Helper class for running Virindi Tank Commands
    /// </summary>
    internal static class VTActions
    {
        #region VT State Actions

        internal static void StartVT()
        {
            ACUtilities.ProcessArbitraryCommand("/vt start");
        }

        internal static void StopVT()
        {
            ACUtilities.ProcessArbitraryCommand("/vt stop");
        }

        internal static void EnableAllVTStates()
        {
            foreach (var state in VTOptionNames.AllStateOptions)
            {
                VTUtilities.SetOption(state, "True");
            }
        }

        internal static void DisableAllVTStates()
        {
            foreach (var state in VTOptionNames.AllStateOptions)
            {
                VTUtilities.SetOption(state, "False");
            }
        }

        internal static void EnableNav()
        {
            VTUtilities.SetOption(VTOptionNames.EnableNav, "True");
        }

        internal static void EnableBuffing()
        {
            VTUtilities.SetOption(VTOptionNames.EnableBuffing, "True");
        }

        internal static void EnableCombat()
        {
            EnableCombat(false);
        }

        internal static void EnableCombat(bool enableSummon)
        {
            VTUtilities.SetOption(VTOptionNames.EnableCombat, "True");
            if (enableSummon)
            {
                EnableSummon();
            }
        }

        internal static void EnableLooting()
        {
            VTUtilities.SetOption(VTOptionNames.EnableLooting, "True");
        }

        internal static void EnableMeta()
        {
            VTUtilities.SetOption(VTOptionNames.EnableMeta, "True");
        }

        internal static void DisableNav()
        {
            VTUtilities.SetOption(VTOptionNames.EnableNav, "False");
        }

        internal static void DisableBuffing()
        {
            VTUtilities.SetOption(VTOptionNames.EnableBuffing, "False");
        }

        internal static void DisableCombat()
        {
            DisableCombat(false);
        }

        internal static void DisableCombat(bool disableSummon)
        {
            VTUtilities.SetOption(VTOptionNames.EnableCombat, "False");
            if (disableSummon)
            {
                DisableSummon();
            }
        }

        internal static void DisableLooting()
        {
            VTUtilities.SetOption(VTOptionNames.EnableLooting, "False");
        }

        internal static void DisableMeta()
        {
            VTUtilities.SetOption(VTOptionNames.EnableMeta, "False");
        }

        #endregion

        internal static void ForceBuff()
        {
            ACUtilities.ProcessArbitraryCommand("/vt forcebuff");
        }

        internal static void SetBoostNavPriority(bool enabled)
        {
            VTUtilities.SetOption(VTOptionNames.NavPriorityBoost, enabled);
        }

        internal static void SetAttackRange(string rangeValue)
        {
            SetAttackRange(int.Parse(rangeValue));
        }

        internal static void SetAttackRange(int rangeValue)
        {
            VTUtilities.SetRangeOptionWhenHumanValue(VTOptionNames.AttackDistance, rangeValue);
        }

        internal static void SetNavDistance(int rangeValue)
        {
            VTUtilities.SetRangeOptionWhenHumanValue(VTOptionNames.NavCloseStopRange, rangeValue);
        }

        internal static void SetUsePortalDistance(int rangeValue)
        {
            VTUtilities.SetRangeOptionWhenHumanValue(VTOptionNames.UsePortalDistance, rangeValue);
        }

        internal static void LoadNav(string navFile)
        {
            // VT will automatically append the .nav extension, so remove the extesion if it's defined
            var modifiedNavFile = navFile.EndsWith(".nav") ? System.IO.Path.GetFileNameWithoutExtension(navFile) : navFile;
            ACUtilities.ProcessArbitraryCommand(string.Format("/vt nav load {0}", modifiedNavFile));
        }

        internal static void FollowCharacter(string characterName, bool boostNav, int followDistance)
        {
            // Just in case, disable nav while we are busy changing the nav file
            using (var scope = VTStateScope.EnterNavDisabled())
            {
                string navFile = VTUtilities.GetFollowNavForCharacter(characterName);

                if (string.IsNullOrEmpty(navFile))
                {
                    REPlugin.Instance.Debug.WriteLine(string.Format("Could not locate nav file to follow : {0}", characterName));
                    return;
                }

                LoadNav(navFile);
            }

            SetBoostNavPriority(boostNav);

            SetNavDistance(followDistance);

            EnableNav();

            StartVT();
        }

        /// <summary>
        /// Attempts to figure out if a profile is name character specific or global.  Character specific will be picked first
        /// </summary>
        /// <param name="profileName"></param>
        /// <returns></returns>
        internal static bool LoadSmartSettingsProfile(string profileName)
        {
            if (LoadCharacterSettingsProfile(profileName))
            {
                return true;
            }

            if (LoadAnySettingsProfile(profileName))
            {
                return true;
            }

            return false;
        }

        internal static bool LoadAnySettingsProfile(string profileName)
        {
            if (File.Exists(VTUtilities.GetAnyProfileFullFilePath(profileName)))
            {
                ACUtilities.ProcessArbitraryCommand(string.Format("/vt settings load {0}", profileName));
                return true;
            }

            return false;
        }

        internal static bool LoadCharacterSettingsProfile(string profileName)
        {
            if (File.Exists(VTUtilities.GetCharacterSpecificProfileFullFilePath(profileName)))
            {
                ACUtilities.ProcessArbitraryCommand(string.Format("/vt settings loadchar {0}", profileName));
                return true;
            }

            return false;
        }

        internal static void LoadLootProfile(string profileName)
        {
            // VT wants loot profiles to include the utl file extension
            var tweakedProfileNmae = profileName.EndsWith(".utl") ? profileName : string.Format("{0}.utl", profileName);
            ACUtilities.ProcessArbitraryCommand(string.Format("/vt loot load {0}", tweakedProfileNmae));
        }

        internal static void LoadMetaProfile(string metaName)
        {
            ACUtilities.ProcessArbitraryCommand(string.Format("/vt meta load {0}", metaName));
        }

        internal static void SetMetaState(string stateName)
        {
            ACUtilities.ProcessArbitraryCommand(string.Format("/vt setmetastate {0}", stateName));
        }

        internal static void EnableAutoCram()
        {
            VTUtilities.SetOption(VTOptionNames.AutoCram, "True");
        }

        internal static void DisableAutoCram()
        {
            VTUtilities.SetOption(VTOptionNames.AutoCram, "False");
        }

        #region Summoning

        internal static void EnableSummon()
        {
            VTUtilities.SetOption(VTOptionNames.SummonPets, "True");
        }

        internal static void DisableSummon()
        {
            VTUtilities.SetOption(VTOptionNames.SummonPets, "False");
        }

        internal static void EnableCustomPetRange()
        {
            // PetRangeMode [1/2]: Determines what monster range is used when summoning a combat pet. 2 = AttackDistance, 1 = PetCustomRange
            VTUtilities.SetOption(VTOptionNames.PetRangeMode, "1");
        }

        internal static void DisableCustomPetRange()
        {
            // PetRangeMode [1/2]: Determines what monster range is used when summoning a combat pet. 2 = AttackDistance, 1 = PetCustomRange
            VTUtilities.SetOption(VTOptionNames.PetRangeMode, "2");
        }

        internal static void SetPetRange(int rangeValue)
        {
            // If custom pet range is used, automatically enable custom pet range
            EnableCustomPetRange();
            VTUtilities.SetRangeOptionWhenHumanValue(VTOptionNames.PetCustomRange, rangeValue);
        }

        internal static void SetPetMonsterDensity(int monsterDensity)
        {
            VTUtilities.SetOption(VTOptionNames.PetMonsterDensity, monsterDensity.ToString());
        }

        internal static void SetPetRefillCountIdle(int value)
        {
            VTUtilities.SetOption(VTOptionNames.PetRefillCountIdle, value.ToString());
        }

        internal static void PetRefillCountNormal(int value)
        {
            VTUtilities.SetOption(VTOptionNames.PetRefillCountNormal, value.ToString());
        }

        #endregion

        #region Spell Combat

        internal static void SetDebuffEachFirstMode(DebuffEachFirstMode mode)
        {
            VTUtilities.SetOption(VTOptionNames.DebuffEachFirst, ((int)mode).ToString());
        }

        #endregion
    }
}
