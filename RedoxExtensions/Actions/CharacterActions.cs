using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Mine;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Actions
{
    /// <summary>
    /// General purpose actions that a character can perform
    /// </summary>
    public static class CharacterActions
    {
        public static void PullLegendaryKeys()
        {
            VTActions.StopVT();
            VTActions.DisableAllVTStates();

            VTActions.LoadMetaProfile(Mine.MyMetaProfiles.PullLegendaryKeys);
            VTActions.LoadLootProfile(Mine.MyLootProfiles.LegendaryChestPulls);

            VTActions.SetMetaState("Check");

            VTActions.EnableMeta();
            VTActions.StartVT();
        }

        public static bool SetMainProfileRaw(string profileName)
        {
            return VTActions.LoadCharacterSettingsProfile(profileName);
        }

        public static bool SetMainProfile(string profileName)
        {
            try
            {
                return SetMainProfile((MyMainProfiles)Enum.Parse(typeof(MyMainProfiles), profileName, true));
            }
            catch (ArgumentException)
            {
                // It's not one of my profiles.  See if there is a match
                // for the name as is.
                return VTActions.LoadSmartSettingsProfile(profileName);
            }
        }

        public static bool SetMainProfile(MyMainProfiles myProfile)
        {
            return SetMainProfileRaw(MyUtilities.LookupProfileName(myProfile));
        }
    }
}
