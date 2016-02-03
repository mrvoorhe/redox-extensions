using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Actions;
using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Data;

namespace RedoxExtensions.Listeners.Monitors
{
    /// <summary>
    /// Monitors for certain game events and then issues
    /// commands to slaves telling them to do the same thing.
    /// 
    /// For example, if the master uses an NPC, this class would tell
    /// the slaves to use the same NPC.
    /// </summary>
    public class CopyCatMaster : IDisposable
    {
        private bool _enabled;

        public CopyCatMaster()
        {
            REPlugin.Instance.Events.RE.UsingPortal += RTEvents_UsingPortal;
            REPlugin.Instance.Events.RE.ApproachingObject += RTEvents_ApproachingObject;
            REPlugin.Instance.Events.RE.EndGiveItem += RT_EndGiveItem;
        }

        public void Enable()
        {
            this._enabled = true;
            if (CurrentThreadContext.OnGameThread)
            {
                REPlugin.Instance.Chat.WriteLine("CopyCastMaster On");
            }
        }

        public void Disable()
        {
            this._enabled = false;
            if (CurrentThreadContext.OnGameThread)
            {
                REPlugin.Instance.Chat.WriteLine("CopyCastMaster Off");
            }
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.RE.UsingPortal -= RTEvents_UsingPortal;
            REPlugin.Instance.Events.RE.ApproachingObject -= RTEvents_ApproachingObject;
            REPlugin.Instance.Events.RE.EndGiveItem -= RT_EndGiveItem;
        }

        void RTEvents_ApproachingObject(object sender, Data.Events.ApproachingObjectEventArgs e)
        {
            if (this._enabled)
            {
                // TODO : Improve this to check if it's an NPC?  Or, make 'use' smart enough to figure out how to use anything
                TellActions.TellFellow("#use {0}", e.ObjectId);
            }
        }

        void RTEvents_UsingPortal(object sender, Data.Events.UsingPortalEventArgs e)
        {
            if (this._enabled)
            {
                // Note : Should probably verify usable before sending command.  That way if I wanted to slide and 'abort' the use portal.
                // the slaves wouldn't be full steam ahead into the portal
                TellActions.TellFellow("#use {0}", e.PortalId);
            }
        }

        void RT_EndGiveItem(object sender, Data.Events.EndGiveItemEventArgs e)
        {
            if (this._enabled)
            {
                if (e.Outcome == GiveItemOutcome.Successful)
                {
                    // If we just gave an item to someone, then they must be close by.  So MagTools get closest object is probably a good enough
                    // way to obtain the WorldObject for the target.
                    var targetWorldObj = Mag.Shared.Util.GetClosestObject(e.TargetName);

                    if (targetWorldObj == null)
                    {
                        throw new DisplayToUserException(string.Format("Expected to find a world object for target name : {0}", e.TargetName), CommandHelpers.Self);
                    }

                    // For now, limit this feature to NPC's.  It would be annoying to have it going off every time I mule something
                    if (targetWorldObj.ObjectClass != ObjectClass.Npc)
                    {
                        REPlugin.Instance.Debug.WriteLine("[Copycat] Ignoring Give.  Target is not an NPC : {0}", targetWorldObj.ToShortSummary());
                        return;
                    }

                    TellActions.TellFellow("#give {0}|{1}|1", e.ItemGiven.Name, targetWorldObj.Id);
                }
            }
        }
    }
}
