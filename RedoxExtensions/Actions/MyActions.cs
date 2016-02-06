using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Core;
using RedoxExtensions.MagToolsInterop;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Actions
{
    /// <summary>
    /// Hacky actions specific to my account(s)
    /// </summary>
    public static class MyActions
    {
        private const int MillisecondsDelayBeforeRecruitingFellowMembers = 2000;

        public static void FellowUsualSuspects(bool forceBuff)
        {
            // If there is already a fellow, this will harmlessly fail, no big deal.
            MTActions.CreateFellow("X");

            // We need to delay to give the fellowship a chance to be created before
            // recruiting members
            Continuations.DelayedContinuation.ContinueAfterDelayOnGameThread((stateObj) =>
            {

                // Should invite all the usual suspects into the fellow.
                Mine.MyUtilities.RecruitUsualSuspects();

                // TODO : Need to delay until all members have been recruited before issuing these commands.
                // Otherwise some of the fellows may not receive the message
                // Init everyones state.  This will almost always be desired after using the fellow up command
                //Utilities.ProcessArbitraryCommand("/rf init");

                //if (forceBuff)
                //{
                //    Utilities.ProcessArbitraryCommand("/rf buff");
                //}
            },
            MillisecondsDelayBeforeRecruitingFellowMembers,
            null);
        }

        public static void LoadMyDefaultLootProfile()
        {
            VTActions.LoadLootProfile(Settings.ActiveSettings.Instance.VTProfiles.Loot.Default);
        }
    }
}
