using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Core
{
    /// <summary>
    /// Events provided by Redox Tools
    /// </summary>
    public interface IREEvents
    {
        IFellowshipEvents Fellowship { get; }

        event EventHandler<UsingObjectEventArgs> UsingObject;

        event EventHandler<UsingPortalEventArgs> UsingPortal;

        event EventHandler<ApproachingObjectEventArgs> ApproachingObject;

        event EventHandler<UsingPortalCompleteEventArgs> UsingPortalComplete;

        event EventHandler<YourTooBusyEventArgs> YourTooBusy;

        event EventHandler<BeginBusyEventArgs> BeginBusy;

        event EventHandler<BeginIdleEventArgs> BeginIdle;

        event EventHandler<BeginNonZeroBusyStateEventArgs> BeginNonZeroBusyState;

        event EventHandler<EndNonZeroBusyStateEventArgs> EndNonZeroBusyState;

        event EventHandler<BeginGiveItemEventArgs> BeginGiveItem;

        event EventHandler<EndGiveItemEventArgs> EndGiveItem;

        event EventHandler<EventArgs> VTStarted;

        event EventHandler<EventArgs> VTStopped;

        event EventHandler<ObjectIdEventArgs> BeginEquipItem;
        event EventHandler<ObjectIdEventArgs> EndEquipItem;

        event EventHandler<ObjectIdEventArgs> BeginUnequipItem;
        event EventHandler<ObjectIdEventArgs> EndUnequipItem;

        event EventHandler<JumpEventArgs> SelfJump;

        /// <summary>
        /// Fired when you land after jumping
        /// </summary>
        event EventHandler<SelfJumpCompleteEventArgs> SelfJumpCompleted;


        // TODO:
        //event EventHandler<MovementEventArgs> BeginMovement;
        //event EventHandler<MovementEventArgs> EndMovement;

        // TODO : Use MoveObject + Scan over GetInventory to verify it's in our inventory (this is what mag tools does)
        //event EventHandler<ItemReceivedEventArgs> ItemReceived;

        // TODO : Support "You must wait"
        // Ex: You must wait 17h 50m 40s to be rewarded again for destroying Frozen Crystals.
    }
}
