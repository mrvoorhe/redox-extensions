using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Dispatching;
using RedoxExtensions.Listeners;
using RedoxExtensions.VirindiInterop;
using RedoxExtensions.Wrapper;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Data;
using RedoxExtensions.Dispatching.Legacy;
using RedoxExtensions.Mine;
using RedoxExtensions.PhatACInterop;
using RedoxLib.General;
using RedoxLib.Location;

namespace RedoxExtensions.Actions
{
    internal static class SimpleActions
    {
        private const int DefaultNormalNavDistance = 3;
        private const int UsePortalToleranceOverNavDistance = 5;
        private const int DefaultPetRange = 4;
        private const int DefaultPetDensity = 4;

        private const int MillisecondsDelayBeforeKillProcessOnExit = 10000;

        #region Movement Related

        internal static void ProcessNavCommand(ICommand command)
        {
            // always ignore self nav commands.  Nav commands are always for controlling slave nav behavior
            if (command.FromSelf)
            {
                return;
            }

            switch (command.Arguments[0].ToLower().Trim())
            {
                case "tight":
                    NavDistanceTight();
                    break;
                case "normal":
                    NavDistanceNormal();
                    break;
                case "loose":
                    NavDistanceLoose();
                    break;
                default:
                    REPlugin.Instance.Chat.WriteLine(string.Format("Unknown nav value : {0}", command.Arguments[0]));
                    break;
            }
        }

        internal static void FollowMe(ISourceInformation command)
        {
            // Always ignore if the source character was yourself.  It would never make sense to follow yourself
            if (command.FromSelf)
            {
                return;
            }

            VTActions.FollowCharacter(command.SourceCharacter, true, 3);
        }

        internal static void NavDistanceTight(ISourceInformation command)
        {
            // always ignore self nav commands.  Nav commands are always for controlling slave nav behavior
            if (command.FromSelf)
            {
                return;
            }

            NavDistanceTight();
        }

        internal static void NavDistanceTight()
        {
            // Set the use portal distance slightly higher than the follow distance to ensure that
            // we can always use the portal
            VTActions.SetUsePortalDistance(1 + UsePortalToleranceOverNavDistance);

            VTActions.SetNavDistance(1);
        }

        internal static void NavDistanceNormal(ISourceInformation command)
        {
            // always ignore self nav commands.  Nav commands are always for controlling slave nav behavior
            if (command.FromSelf)
            {
                return;
            }

            // Set the use portal distance slightly higher than the follow distance to ensure that
            // we can always use the portal
            VTActions.SetUsePortalDistance(DefaultNormalNavDistance + UsePortalToleranceOverNavDistance);

            NavDistanceNormal();
        }

        internal static void NavDistanceNormal()
        {
            VTActions.SetNavDistance(DefaultNormalNavDistance);
        }

        internal static void NavDistanceLoose(ISourceInformation command)
        {
            // always ignore self nav commands.  Nav commands are always for controlling slave nav behavior
            if (command.FromSelf)
            {
                return;
            }

            NavDistanceLoose();
        }

        internal static void NavDistanceLoose()
        {
            VTActions.SetNavDistance(10);
        }

        internal static void FaceObject(ICommand command)
        {
            // Support both woId as the argument or object name
            int targetId;
            if(int.TryParse(command.Arguments[0], out targetId))
            {
                WorldUtilities.Face(targetId);
            }
            else
            {
                WorldUtilities.Face(command.Arguments[0]);
            }
        }

        internal static void FaceHeading(ISourceInformation command, double degrees, bool skipSelf)
        {
            if(command.FromSelf && skipSelf)
            {
                return;
            }

            FaceHeading(degrees);
        }

        internal static void FaceHeading(double degrees)
        {
            REPlugin.Instance.Actions.FaceHeading(degrees, true);
        }

        #endregion

        #region Combat Related

        internal static void SetAttackRange(ICommand command)
        {
            int rangeValue = int.Parse(command.Arguments[0]);

            if (command.FromSelf)
            {
                // If we are the master, set our range to be range - default nav distace.
                // This helps avoid a situation where the slaves are all trailing just a little bit behind
                // master.  If a monster is right at the edge of our attack distance, master can get caught in
                // a situation where he is fighting alone due to slaves viewing the monster as out of range.

                // Be smart about this, don't set the range to something stupid over this (i.e. less than 0)
                int possibleMasterRange = rangeValue - DefaultNormalNavDistance;

                // Don't make the adjustment if the result is less than 2, a range of 2 or less or pretty use less.
                if (possibleMasterRange > 2)
                {
                    rangeValue = possibleMasterRange;
                }
            }

            VTActions.SetAttackRange(rangeValue);
        }

        internal static void SetPetRange(ICommand command)
        {
            int rangeValue = int.Parse(command.Arguments[0]);

            VTActions.SetPetRange(rangeValue);
        }

        #endregion

        #region State Related

        /// <summary>
        /// Initializes VT to a harmless, and standardized state across all fellow members
        /// </summary>
        internal static void InitState(ICommand command)
        {
            if (command.Arguments.Count == 0)
            {
                InitDefaultState(command);
            }
            else
            {
                switch (command.Arguments[0].ToLower())
                {
                    case "mule":
                        InitMuleState(command);
                        break;
                    default:
                        throw new DisplayToUserException(string.Format("Unknown state : {0}", command.Arguments[0].ToLower()), command);
                }
            }
        }

        internal static void InitMuleState(ISourceInformation command)
        {
            if (command.FromSelf)
            {
                return;
            }

            VTActions.DisableAllVTStates();
            VTActions.EnableAutoCram();
            VTActions.StartVT();
        }

        internal static void InitDefaultState(ISourceInformation command)
        {
            // Init needs to put everybod into a consistent state, day in and day out.
            // So we also need to initialize our main VT profile.
            SetMainProfile(MyMainProfiles.CharacterSpecificDefault.ToString(), command.SourceCharacter);

            VTActions.DisableAllVTStates();

            MyActions.LoadMyDefaultLootProfile();

            NavDistanceNormal(command);

            VTActions.DisableSummon();
            VTActions.SetPetRange(DefaultPetRange);
            VTActions.SetPetMonsterDensity(DefaultPetDensity);

            // Note : I think it would be nice to follow immediately as well.  We'll see how this goes.
            FollowMe(command);

            VTActions.StartVT();

            // If we are the master, turn on copy cat on init
            if (command.FromSelf)
            {
                REPlugin.Instance.MonitorManager.CopyCatMaster.Enable();
            }
        }

        internal static void Peace()
        {
            VTActions.DisableCombat();
            CombatStatePeace();
        }

        internal static void CombatStatePeace()
        {
            CoreManager.Current.Actions.SetCombatMode(CombatState.Peace);
        }

        /// <summary>
        /// Waits 'nicely'.  No Nav, No Combt, no Buff
        /// </summary>
        /// <param name="command"></param>
        internal static void WaitNice(ISourceInformation command)
        {
            // Always ignore if the source character was yourself.
            if (command.FromSelf)
            {
                return;
            }

            VTActions.DisableNav();
            VTActions.DisableCombat();
            VTActions.DisableBuffing();
        }

        /// <summary>
        /// Waits, but will still buff and fight (if already enabled)
        /// </summary>
        /// <param name="command"></param>
        internal static void Wait(ISourceInformation command)
        {
            // Always ignore if the source character was yourself.  It would never make sense to wait on yourself
            if (command.FromSelf)
            {
                return;
            }

            VTActions.DisableNav();
        }

        internal static void Fight()
        {
            // Will always want buffing on when fighting
            VTActions.EnableBuffing();
            VTActions.EnableCombat();
        }

        internal static void Loot()
        {
            VTActions.EnableLooting();
        }

        internal static void Buff()
        {
            VTActions.EnableBuffing();
        }

        /// <summary>
        /// A state optimized for camping a given location
        /// </summary>
        internal static void Camp(ICommand command)
        {
            // Optional argument to camp is attack range
            if (command.Arguments.Count > 0)
            {
                int rangeValue = int.Parse(command.Arguments[0]);
                VTActions.SetAttackRange(rangeValue);
            }

            Camp();
        }

        /// <summary>
        /// A state optimized for camping a given location
        /// </summary>
        internal static void Camp()
        {
            VTActions.DisableNav();
            Fight();
            Loot();
        }

        #endregion

        internal static void SetMainProfile(ICommand command)
        {
            var profile = command.Arguments.Count == 0 ? MyMainProfiles.CharacterSpecificDefault.ToString() : command.Arguments[0];
            SetMainProfile(profile, command.SourceCharacter);
        }

        internal static void SetMainProfile(string profile, string requestingCharacter)
        {
            bool success = CharacterActions.SetMainProfile(profile);

            if (!success)
            {
                TellActions.TellFellow(string.Format("{0}, I couldn't set my profile", requestingCharacter));
            }
        }

        internal static void SetFormation(ICommand command)
        {
            // Note : Could update the 'formation' concept to include stuff beyond range.
            // since it's going to be kind of a "hunting formation" concept.  So some characters might get
            // loot enabled, summon, on/off etc.
            // TODO : Switch to json file that is loaded which contains a dictionary of <strig (char name), FormationSettings>
            // that will load up and apply each characters formation specific settings
            var formationName = command.Arguments[0].ToLower();

            var range = Mine.MyUtilities.GetFormationRangeForCurrentCharacter(formationName);

            VTActions.SetAttackRange(range);

            if (Mine.MyUtilities.EnableLootForFormationForCurrentCharacter(formationName))
            {
                VTActions.EnableLooting();
            }

            VTActions.SetPetRange(10);
        }

        internal static void ProcessPetsCommand(ICommand command)
        {
            // If no args, assume on
            if (command.Arguments.Count == 0)
            {
                VTActions.EnableSummon();
                return;
            }

            var optionalVal = command.Arguments[0].ToLower();
            if (optionalVal == "on")
            {
                VTActions.EnableSummon();
                return;
            }

            // It's a simple on/off option, but who cares about typos, if it's not on, assume off
            VTActions.DisableSummon();
        }

        internal static void ForceBuff()
        {
            VTActions.EnableBuffing();
            VTActions.ForceBuff();
            VTActions.StartVT();
        }

        internal static void CastSelfSpell(string spellName)
        {
            var spellId = RedoxLib.SpellUtilities.LookUpSpellIdByName(spellName);

            if (spellId == 0)
            {
                REPlugin.Instance.Chat.WriteLine(string.Format("Unable to lookup spell id for : {0}", spellName));
                return;
            }

            // Turn off VT while we do this so that it doesn't mess with our cast.
            using (var scope = VTRunScope.EnterStopped())
            {

                var currentCombatMode = REPlugin.Instance.Actions.CombatMode;

                if (currentCombatMode == Decal.Adapter.Wrappers.CombatState.Peace)
                {
                    // TODO : Need to equip a wand and enter combat mode.
                    REPlugin.Instance.Chat.WriteLine("TODO : Need to implement casting from peace mode.");
                    return;
                }
                else if(currentCombatMode == Decal.Adapter.Wrappers.CombatState.Missile || currentCombatMode == Decal.Adapter.Wrappers.CombatState.Melee)
                {
                    // TODO : Need to equip a wand and enter combat mode.
                    REPlugin.Instance.Chat.WriteLine("TODO : Need to implement casting from melee or missile mode.");
                    return;
                }

                REPlugin.Instance.Actions.CastSpell(spellId, REPlugin.Instance.CharacterFilter.Id);
            }
        }

        internal static void Logout()
        {
            // TODO : Add random delay so that all slaves do not log out at the same time
            // many plugins have concurrency bugs and they can be tripped when lots of clients
            // log out at once
            REPlugin.Instance.PluginHost.Actions.Logout();
        }

        internal static void ExitGame()
        {
            // First logout.
            Logout();

            // Now give the logout a chance to complete gracefully before we kill the current process.
            Continuations.DelayedContinuation.ContinueAfterDelayOnBackgroundThread(
                (stateObj) =>
                {
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                },
                MillisecondsDelayBeforeKillProcessOnExit,
                null);
        }

        internal static bool Teleport(ICommand command)
        {
            return Teleport(command.Arguments.AggregateWithSpace());
        }

        internal static bool Teleport(string args)
        {
            UserFacingLocation location;
            if (UserFacingLocation.TryParse(args, out location))
            {
                PhatACActions.TeleTo(location);
                return true;
            }

            Town town;
            if (Town.TryParse(args, out town))
            {
                PhatACActions.TeleTown(town.Name);
                return true;
            }

            Dungeon dungeon;
            if (Dungeon.TryParse(args, out dungeon))
            {
                PhatACActions.TeleTo(dungeon.Location);
                return true;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Teleports into a named dungeon
        /// </summary>
        /// <param name="command"></param>
        internal static void TeleportInto(ICommand command)
        {
            throw new NotImplementedException();
        }
    }
}
