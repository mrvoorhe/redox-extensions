using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Actions;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Mine;
using RedoxExtensions.Settings;
using RedoxExtensions.VirindiInterop;
using RedoxLib;

namespace RedoxExtensions.Commands.Handlers
{
    internal static class ForeignHandler
    {
        internal static void HandleCommand(ICommand command)
        {
            REPlugin.Instance.Debug.WriteObject(command);

            // If a command is for slaves only and the command is from ourself,
            // then we must be the Master and therefore we should ignore slave only commands
            if (command.IsSlaveOnlyAndFromSelf())
            {
                return;
            }

            // make sure someone isn't trying to pull a fast one on my guys
            if (!MyUtilities.IsCharacterOnMasterWhiteList(command.SourceCharacter))
            {
                return;
            }

            switch (command.Name.ToLower())
            {
                #region Generic Stuff & Shortcuts to Plugin Commands

                case "cmd":
                case "c":
                    ACUtilities.ProcessArbitraryCommand(command.RebuildArgumentsWithSpaceSeparator());
                    break;

                // short cut for running vt commands
                case "vt":
                    ACUtilities.ProcessArbitraryCommand(string.Format("/vt {0}", command.RebuildArgumentsWithSpaceSeparator()));
                    break;

                // Shortcut for running mt commands
                case "mt":
                    ACUtilities.ProcessArbitraryCommand(string.Format("/mt {0}", command.RebuildArgumentsWithSpaceSeparator()));
                    break;

                // Shortcut for local redox extensions commands
                case "re":
                    ACUtilities.ProcessArbitraryCommand(string.Format("/re {0}", command.RebuildArgumentsWithSpaceSeparator()));
                    break;

                case "rew":
                    ACUtilities.ProcessArbitraryCommand(string.Format("/rew {0}", command.RebuildArgumentsWithSpaceSeparator()));
                    break;

                #endregion

                #region Simple Shortcuts to built in commands

                // Shortcuts for simple commands.
                case "ls":
                    ACUtilities.ProcessArbitraryCommand("/ls");
                    break;
                case "ah":
                    ACUtilities.ProcessArbitraryCommand("/ah");
                    break;
                case "hom":
                    ACUtilities.ProcessArbitraryCommand("/hom");
                    break;
                case "tn":
                    ACUtilities.ProcessArbitraryCommand("/tn");
                    break;

                #endregion

                #region Login & Exit

                case "logout":
                case "logoff":
                    SimpleActions.Logout();
                    break;
                case "exit":
                    SimpleActions.ExitGame();
                    break;

                #endregion

                #region Using

                case "use":
                    Actions.Dispatched.UseObject.Create(command).Enqueue();
                    break;

                #endregion

                #region Give

                case "give":
                    Actions.Dispatched.GiveItems.Create(command).Enqueue();
                    break;

                #endregion

                #region List

                case "list":
                    Actions.Dispatched.ListItems.Create(command).Enqueue();
                    break;

                #endregion

                #region Cram

                case "cram":
                    Actions.Dispatched.CramItems.Create(command).Enqueue();
                    break;

                #endregion

                #region Jumping

                case "hop":
                    if(!SourceIsWithinJumpRange(command))
                    {
                        command.GiveFeedback(FeedbackType.Ignored, "You are too far away");
                        return;
                    }

                    Actions.Dispatched.Jump.CreateHop(command).Enqueue();
                    break;
                case "shifthop":
                case "shop":
                    if (!SourceIsWithinJumpRange(command))
                    {
                        command.GiveFeedback(FeedbackType.Ignored, "You are too far away");
                        return;
                    }

                    Actions.Dispatched.Jump.CreateShiftHop(command).Enqueue();
                    break;
                case "hopfull":
                case "hopf":
                    if (!SourceIsWithinJumpRange(command))
                    {
                        command.GiveFeedback(FeedbackType.Ignored, "You are too far away");
                        return;
                    }

                    Actions.Dispatched.Jump.CreateHopFull(command).Enqueue();
                    break;
                case "shifthopfull":
                case "shopf":
                    if (!SourceIsWithinJumpRange(command))
                    {
                        command.GiveFeedback(FeedbackType.Ignored, "You are too far away");
                        return;
                    }

                    Actions.Dispatched.Jump.CreateShiftHopFull(command).Enqueue();
                    break;

                #endregion

                #region Recalling - Spell Casting

                case "recall":
                case "r":
                    SimpleActions.CastSelfSpell("Portal Recall");
                    break;
                case "rprimary":
                case "rp":
                    // TODO
                    throw new NotImplementedException();

                case "rsecondary":
                case "rs":
                    // TODO
                    throw new NotImplementedException();

                case "rls":
                case "rl":
                    // TODO
                    throw new NotImplementedException();

                case "regroup":
                    // TODO : Will regroup the fellow at a known location.  So, probably just do portal recall.
                    // wil also turn off combat (before recalling), rebuff, etc.  This will be used after someone in the fellow dies.
                    throw new NotImplementedException();

                #endregion

                #region Plugin Management

                // Plugin Management Options
                case "reunload":
                    ACUtilities.ProcessArbitraryCommand("/rew unload");
                    break;
                case "rereload":
                    ACUtilities.ProcessArbitraryCommand("/rew reload");
                    break;

                #endregion

                #region Diagnostic / Troubleshooting

                case "clearqueue":
                    // Clears the dispatch pipeline queue
                    REPlugin.Instance.Dispatch.Pipeline.Clear();
                    break;

                case "distancecheck":
                case "distcheck":
                    var distanceFromSelf = WorldUtilities.GetDistanceFromSelf(command);
                    command.GiveFeedback(FeedbackType.Successful, "Distance from you = {0}", distanceFromSelf);
                    break;

                #endregion

                case "init":
                    SimpleActions.InitState(command);
                    break;
                case "profile":
                    // No Args = default
                    // Standard Values :
                    //     normal
                    //     support
                    //     light
                    SimpleActions.SetMainProfile(command);
                    break;
                case "followme":
                case "come":
                    SimpleActions.FollowMe(command);
                    break;
                case "wait":
                    SimpleActions.Wait(command);
                    break;
                case "waitnice":
                case "waitn":
                    SimpleActions.WaitNice(command);
                    break;

                #region Range

                case "range":
                    SimpleActions.SetAttackRange(command);
                    break;

                case "petrange":
                case "prange":
                    SimpleActions.SetPetRange(command);
                    break;

                #region Mage

                case "magerange":
                case "mrange":
                    if (ActiveSettings.Instance.Mage.Contains(CurrentCharacter.Name))
                        SimpleActions.SetAttackRange(command);
                    break;

                case "attackmagerange":
                case "amrange":
                    if (ActiveSettings.Instance.AttackMage.Contains(CurrentCharacter.Name))
                        SimpleActions.SetAttackRange(command);
                    break;

                case "supportmagerange":
                case "smrange":
                    if (ActiveSettings.Instance.SupportMage.Contains(CurrentCharacter.Name))
                        SimpleActions.SetAttackRange(command);
                    break;

                #endregion

                #region Support

                case "supportrange":
                case "srange":
                    if (ActiveSettings.Instance.Support.Contains(CurrentCharacter.Name))
                        SimpleActions.SetAttackRange(command);
                    break;

                #endregion

                #region Archer

                case "archerrange":
                case "arange":
                    if (ActiveSettings.Instance.Archer.Contains(CurrentCharacter.Name))
                        SimpleActions.SetAttackRange(command);
                    break;

                case "attackarcherrange":
                case "aarange":
                    if (ActiveSettings.Instance.AttackArcher.Contains(CurrentCharacter.Name))
                        SimpleActions.SetAttackRange(command);
                    break;

                case "supportarcherrange":
                case "sarange":
                    if (ActiveSettings.Instance.SupportArcher.Contains(CurrentCharacter.Name))
                        SimpleActions.SetAttackRange(command);
                    break;

                #endregion

                #region Melee

                case "meleerange":
                    if (ActiveSettings.Instance.Melee.Contains(CurrentCharacter.Name))
                        SimpleActions.SetAttackRange(command);
                    break;

                #endregion

                #endregion

                case "fight":
                    SimpleActions.Fight();
                    break;

                case "loot":
                    SimpleActions.Loot();
                    break;

                case "buff":
                    SimpleActions.Buff();
                    break;
                case "fbuff":
                    SimpleActions.ForceBuff();
                    break;

                case "peace":
                    SimpleActions.Peace();
                    break;

                case "camp":
                    SimpleActions.Camp(command);
                    break;

                // nav controls
                case "nav":
                    SimpleActions.ProcessNavCommand(command);
                    break;
                case "navt":
                    SimpleActions.NavDistanceTight(command);
                    break;
                case "navn":
                    SimpleActions.NavDistanceNormal(command);
                    break;
                case "navl":
                    SimpleActions.NavDistanceLoose(command);
                    break;

                case "formation":
                    SimpleActions.SetFormation(command);
                    break;

                // Pets
                case "pets":
                    SimpleActions.ProcessPetsCommand(command);
                    break;

                case "pmin":
                case "petdensity":
                case "petmin":
                    VTActions.SetPetMonsterDensity(int.Parse(command.Arguments[0]));
                    break;

                // VT State short cuts
                case "buffon":
                    VTActions.EnableBuffing();
                    break;

                default:
                    CommandResponseHandler.TellSource(command, "Unknown Command : {0} , with value of : {1}", command.Name, command.RebuildArgumentString());
                    break;
            }
        }

        private static bool SourceIsWithinJumpRange(ISourceInformation requestor)
        {
            // For jumping, the slaves should be pretty close to the master, otherwise jumping would be pointless
            return Core.Utilities.WorldUtilities.WithinRangeOfSelf(requestor, 4);
        }
    }
}
