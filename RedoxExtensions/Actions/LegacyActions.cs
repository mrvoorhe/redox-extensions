using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Data;
using RedoxExtensions.Dispatching.Legacy;
using RedoxExtensions.Listeners;
using RedoxExtensions.Mine;
using RedoxExtensions.VirindiInterop;
using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Actions
{
    /// <summary>
    /// A dumping ground for old actions I wrote that I no longer use, but may want to reference at some point
    /// </summary>
    internal static class LegacyActions
    {
        internal static void ProcessUsePortalCommand(ICommand command)
        {
            // Note - Need to turn VT Nav off while using. If it's a portal, the leader can go in and then VT nav
            // can mess up the use.
            var scope = VTRunScope.EnterStopped();

            SimpleActions.CombatStatePeace();

            var complexValue = command.Arguments;
            var objectId = int.Parse(complexValue[0]);

            // TODO : Need to spam use until we get confirmation of use (or some how wait until we are idle)
            // If you are in combat and you get a use command it still takes a few seconds after turning VT off to be
            // ready to use a portal.
            // A sleep might be enough.
            // Either way, this method is going to get more complicated since I'll have to defer it to the BG dispatcher
            // and then invoke actions on the game thread.

            if (complexValue.Count == 1)
            {
                REPlugin.Instance.PluginHost.Actions.UseItem(objectId, 0);
            }
            else if (complexValue.Count == 2)
            {
                REPlugin.Instance.PluginHost.Actions.UseItem(objectId, int.Parse(complexValue[1]));
            }
            else if (complexValue.Count == 3)
            {
                REPlugin.Instance.PluginHost.Actions.UseItem(objectId, int.Parse(complexValue[1]), int.Parse(complexValue[2]));
            }

            Continuations.PortalSpaceContinuation.ContinueOnEnterPortalSpace(
                (scopeObj) =>
                {
                    if (!command.FromSelf)
                    {
                        TellActions.TellFellow("{0}, right behind ya!", command.SourceCharacter);
                    }

                    var castedScope = (VTRunScope)scopeObj;
                    castedScope.Dispose();

                    // Always disable nav on the character that issued the command.  If nav
                    // was enabled prior to using a portal, it will be very common that the nav route
                    // that was loaded won't be valid for the new location and your character is going to end up
                    // running off into the distance as soon as it leaves portal space.
                    if (command.FromSelf)
                    {
                        VTActions.DisableNav();
                    }

                },
                (scopeObj) =>
                {
                    // Timeout
                    TellActions.TellFellow("{0}, I was left behind!", command.SourceCharacter);

                    var castedScope = (VTRunScope)scopeObj;
                    castedScope.Dispose();

                    // If a character fails to use the portal, we should disable nav so that the character stays put.
                    // And also avoids VT freaking out when the character you were following disappears.
                    VTActions.DisableNav();
                },
                10000,
                scope);
        }

        internal static void ProcessUseNpcCommand(ICommand command)
        {
            var complexValue = command.Arguments;
            var objectId = int.Parse(complexValue[0]);

            int waitBeforeUseDelayInMilliseconds;
            if (complexValue.Count == 1)
            {
                waitBeforeUseDelayInMilliseconds = MyUtilities.GetNpcSleepDelayForCurrentAccount();
            }
            else
            {
                waitBeforeUseDelayInMilliseconds = MyUtilities.GetNpcSleepDelayForCurrentAccount(int.Parse(complexValue[1]));
            }

            // Disable VT now so that it doesn't kick in while we are waiting or something
            var scope = VTRunScope.EnterStopped();

            Continuations.DelayedContinuation.ContinueAfterDelayOnGameThread(
                (stateObj) =>
                {
                    SimpleActions.CombatStatePeace();

                    REPlugin.Instance.PluginHost.Actions.UseItem(objectId, 0);
                    scope.Dispose();
                },
                waitBeforeUseDelayInMilliseconds,
                null);
        }

        internal static void ProcessUseNpc2Command(ICommand command)
        {
            // Disable VT now so that it doesn't kick in while we are waiting or something
            var scope = VTRunScope.EnterStopped();
            var objectId = int.Parse(command.Arguments[0]);

            // TODO: command.Value is the NPC ID, look up the npc's name some how.  Check mag-tools use command?
            var npcName = string.Empty;

            var tellListener = TellListener.Begin(npcName, true);

            TryCompleteActionDelegate tryFinish = () =>
            {
                if (tellListener.TellReceived.WaitOne(1000))
                {
                    // We have confirmation that we used the npc.  We are done.
                    scope.Dispose();
                    tellListener.Dispose();
                    return true;
                }

                return false;
            };

            DispatchedActionDelegate useItemAction = () =>
            {
                REPlugin.Instance.PluginHost.Actions.UseItem(objectId, 0);
            };

            SafeAction retriedFailed = () =>
            {
                TellActions.TellFellow(string.Format("{0}, I failed to speak with : {1}", command.SourceCharacter, npcName));
            };

            REPlugin.Instance.Dispatch.LegacyGameThread.QueueRetriedAction(useItemAction, tryFinish, 20, retriedFailed);
        }
    }
}
