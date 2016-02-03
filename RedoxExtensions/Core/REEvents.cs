using Decal.Adapter.Wrappers;
using RedoxExtensions.Data.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Data;
using RedoxExtensions.Diagnostics;

namespace RedoxExtensions.Core
{
    /// <summary>
    /// More complex events that are provided by Redox Tools
    /// </summary>
    public class REEvents : IREEvents, IREEventsFireCallbacks, IDisposable
    {
        private const int UsingPortalTimeoutInMilliseconds = 5000;

        private readonly ManualResetEvent _usingPortalPending = new ManualResetEvent(false);
        private readonly ManualResetEvent _nonZeroActionStatePending = new ManualResetEvent(false);
        private readonly ManualResetEvent _giveItemPending = new ManualResetEvent(false);

        private readonly IDecalEventsProxy _decalEventsProxy;
        private readonly IFellowshipEventsDisposable _fellowshipEvents;

        private int _pendingBusyState = int.MaxValue;
        private int _pendingBusyStateId = int.MaxValue;

        private string _pendingGiveItemTargetName = null;
        private GiveItemOutcome _pendingGiveItemOutcome = GiveItemOutcome.Undefined;
        private IWorldObject _pendingGiveItemCapturedWorldObject = null;

        private JumpRecorder _jumpRecorder;

        public REEvents(IDecalEventsProxy decalEventsProxy, IFellowshipEventsDisposable fellowshipEvents)
        {
            this._decalEventsProxy = decalEventsProxy;
            this._fellowshipEvents = fellowshipEvents;

            this._decalEventsProxy.StatusTextIntercept += DecalEventsProxy_StatusTextIntercept;
            this._decalEventsProxy.ChangePortalMode += DecalEventsProxy_ChangePortalMode;
            this._decalEventsProxy.RenderFrame += DecalEventsProxy_RenderFrame;
            this._decalEventsProxy.ChatBoxMessage += _decalEventsProxy_ChatBoxMessage;
            this._decalEventsProxy.StatusMessage += _decalEventsProxy_StatusMessage;

            this._decalEventsProxy.ServerDispatch += _decalEventsProxy_ServerDispatch;

            this._jumpRecorder = new JumpRecorder(this, this._decalEventsProxy, this.FireSelfJumpCompleted);
        }

        public REEvents(IDecalEventsProxy decalEventsProxy)
            : this(decalEventsProxy, new FellowshipEvents(decalEventsProxy))
        {
        }

        #region Public Properties

        public IFellowshipEvents Fellowship
        {
            get
            {
                return this._fellowshipEvents;
            }
        }

        #endregion

        public event EventHandler<FellowshipMemberDiedEventArgs> FellowshipMemberDied;

        public event EventHandler<FellowshipCreatedEventArgs> FellowshipCreated;

        public event EventHandler<FellowshipJoinedOtherEventArgs> FellowshipMemberJoined;

        public event EventHandler<FellowshipQuitOtherEventArgs> FellowshipMemberLeft;

        public event EventHandler<FellowshipJoinedYouEventArgs> FellowshipYouJoined;

        public event EventHandler<UsingObjectEventArgs> UsingObject;

        public event EventHandler<UsingPortalEventArgs> UsingPortal;

        public event EventHandler<UsingPortalCompleteEventArgs> UsingPortalComplete;

        public event EventHandler<ApproachingObjectEventArgs> ApproachingObject;

        public event EventHandler<YourTooBusyEventArgs> YourTooBusy;

        public event EventHandler<BeginBusyEventArgs> BeginBusy;

        public event EventHandler<BeginIdleEventArgs> BeginIdle;

        public event EventHandler<BeginNonZeroBusyStateEventArgs> BeginNonZeroBusyState;

        public event EventHandler<EndNonZeroBusyStateEventArgs> EndNonZeroBusyState;

        public event EventHandler<BeginGiveItemEventArgs> BeginGiveItem;

        public event EventHandler<EndGiveItemEventArgs> EndGiveItem;

        public event EventHandler<ObjectIdEventArgs> BeginEquipItem;

        public event EventHandler<ObjectIdEventArgs> EndEquipItem;

        public event EventHandler<ObjectIdEventArgs> BeginUnequipItem;

        public event EventHandler<ObjectIdEventArgs> EndUnequipItem;

        // TODO : Find a way to support these two events some how.  Or some way of knowing if VT is on or off.

        public event EventHandler<EventArgs> VTStarted;
        public event EventHandler<EventArgs> VTStopped;

        public event EventHandler<JumpEventArgs> SelfJump;

        public event EventHandler<SelfJumpCompleteEventArgs> SelfJumpCompleted;

        #region IRTEventsFireCallbacks

        public void FireBeginBusy(object sender, BeginBusyEventArgs e)
        {
            REPlugin.Instance.Debug.WriteObject(e);

            if (this.BeginBusy != null)
            {
                this.BeginBusy(sender, e);
            }
        }

        public void FireBeginIdle(object sender, BeginIdleEventArgs e)
        {
            REPlugin.Instance.Debug.WriteObject(e);

            if (this.BeginIdle != null)
            {
                this.BeginIdle(sender, e);
            }
        }

        #endregion

        public void Dispose()
        {
            this._jumpRecorder.Dispose();
            this._fellowshipEvents.Dispose();

            this._decalEventsProxy.StatusTextIntercept -= DecalEventsProxy_StatusTextIntercept;
            this._decalEventsProxy.ChangePortalMode -= DecalEventsProxy_ChangePortalMode;
            this._decalEventsProxy.RenderFrame -= DecalEventsProxy_RenderFrame;
            this._decalEventsProxy.ChatBoxMessage -= _decalEventsProxy_ChatBoxMessage;
            this._decalEventsProxy.StatusMessage -= _decalEventsProxy_StatusMessage;

            this._decalEventsProxy.ServerDispatch -= _decalEventsProxy_ServerDispatch;

            this._giveItemPending.Close();
            this._usingPortalPending.Close();
            this._nonZeroActionStatePending.Close();
        }

        private void CheckForAndFireMoreSpecificUsingEvents(object sender, string objectName, int objectId)
        {
            // TODO : Investigate why indexer didn't work.  From VTFree Example, indexing a monster should work..
            // Decal.Adapter.Wrappers.WorldObject sel = Core.WorldFilter[Host.Actions.CurrentSelection];
            foreach (var worldObject in REPlugin.Instance.CoreManager.WorldFilter.GetLandscape())
            {
                // We've found the object we are using
                if (worldObject.Id == objectId)
                {
                    switch (worldObject.ObjectClass)
                    {
                        case ObjectClass.Portal:
                            this.FireUsingPortal(sender, new UsingPortalEventArgs(objectName, objectId));
                            return;
                        default:
                            // It's some object class we don't care about providing event support for
                            return;
                    }
                }
            }
        }

        private void FireUsingPortal(object sender, UsingPortalEventArgs eventArgs)
        {
            this._usingPortalPending.Set();

            REPlugin.Instance.Debug.WriteObject(eventArgs);

            if (this.UsingPortal != null)
            {
                this.UsingPortal(sender, eventArgs);
            }

            // TODO : Test this out.  Will it be a problem that this sleep will block all other
            // background actions while we are waiting?  Might not be the best behavior for this use case.
            // May have to use  BeginInvoke somewhere (or some other thread pool based async invoke)
            REPlugin.Instance.Dispatch.Background.QueueAction(() =>
            {
                if (!this._usingPortalPending.WaitOne(UsingPortalTimeoutInMilliseconds))
                {
                    // Timed out trying to use the portal.
                    // Need to get back on the game thread
                    REPlugin.Instance.Dispatch.LegacyGameThread.QueueAction(() =>
                    {
                        this.FireUsingPortalComplete(sender, new UsingPortalCompleteEventArgs(false));
                    });
                }
            });
        }

        private void FireUsingPortalComplete(object sender, UsingPortalCompleteEventArgs eventArgs)
        {
            this._usingPortalPending.Reset();

            REPlugin.Instance.Debug.WriteObject(eventArgs);

            if (this.UsingPortalComplete != null)
            {
                this.UsingPortalComplete(sender, eventArgs);
            }
        }

        private void FireBeginNonZeroBusyState(object sender, BeginNonZeroBusyStateEventArgs eventArgs)
        {
            this._pendingBusyState = eventArgs.BusyState;
            this._pendingBusyStateId = eventArgs.BusyStateId;

            this._nonZeroActionStatePending.Set();

            REPlugin.Instance.Debug.WriteObject(eventArgs);

            if (this.BeginNonZeroBusyState != null)
            {
                this.BeginNonZeroBusyState(sender, eventArgs);
            }

            // Check for busy states that we fire events for.
            switch (this._pendingBusyState)
            {
                case BusyStateConstants.GivingItem:  // Give Object

                    // Cache the object name now, when giving to NPC's, the object will be gone by the time we handle the end event
                    // so we won't be able to get the name then.  Strangely, when giving to player, you can still get to the WorldObject
                    this._pendingGiveItemCapturedWorldObject = this._pendingBusyStateId.ToWorldObject().Capture();
                    this._giveItemPending.Set();

                    var beginGiveEventArgs = new BeginGiveItemEventArgs(this._pendingBusyStateId);

                    REPlugin.Instance.Debug.WriteObject(beginGiveEventArgs);

                    if (this.BeginGiveItem != null)
                    {
                        this.BeginGiveItem(sender, beginGiveEventArgs);
                    }
                    break;
                case BusyStateConstants.EquipingItem:
                    // TODO : Investigate how to detect failed equips.  Once found, introduce a manual reset event

                    var beginEquipEventArgs = new ObjectIdEventArgs(this._pendingBusyStateId);

                    REPlugin.Instance.Debug.WriteObject(beginEquipEventArgs);

                    if (this.BeginEquipItem != null)
                    {
                        this.BeginEquipItem(sender, beginEquipEventArgs);
                    }
                    break;

                case BusyStateConstants.UnequipingItem:
                    // TODO : Investigate how to detect failed equips.  Once found, introduce a manual reset event

                    var beginUnequipEventArgs = new ObjectIdEventArgs(this._pendingBusyStateId);

                    REPlugin.Instance.Debug.WriteObject(beginUnequipEventArgs);

                    if (this.BeginUnequipItem != null)
                    {
                        this.BeginUnequipItem(sender, beginUnequipEventArgs);
                    }
                    break;
            }
        }

        private void FireEndNonZeroBusyState(object sender, EndNonZeroBusyStateEventArgs eventArgs)
        {
            try
            {
                this._nonZeroActionStatePending.Reset();

                REPlugin.Instance.Debug.WriteObject(eventArgs);

                // Check for busy states that we fire events for.
                switch (this._pendingBusyState)
                {
                    case BusyStateConstants.GivingItem:  // Give Object
                        // If the pending give event is still set at this point, that means we were not able to
                        // verify a successful (or failed) give.  Every Begin event needs a correspond End event,
                        // so since no one else took care of it, we should fire a failed End now
                        if (this._giveItemPending.WaitOne(0))
                        {
                            var tmpEventArgs = new EndGiveItemEventArgs(null, string.Empty, GiveItemOutcome.FailedUnknown);
                            REPlugin.Instance.Debug.WriteObject(eventArgs);
                            if (this.EndGiveItem != null)
                            {
                                this.EndGiveItem(sender, tmpEventArgs);
                            }

                            this._giveItemPending.Reset();
                        }

                        break;

                    case BusyStateConstants.EquipingItem:
                        // TODO : Investigate how to detect failed equips.  Once found, introduce a manual reset event
                        if (this.EndEquipItem != null)
                        {
                            this.EndEquipItem(sender, new ObjectIdEventArgs(this._pendingBusyStateId));
                        }
                        break;

                    case BusyStateConstants.UnequipingItem:
                        // TODO : Investigate how to detect failed equips.  Once found, introduce a manual reset event
                        if (this.EndUnequipItem != null)
                        {
                            this.EndUnequipItem(sender, new ObjectIdEventArgs(this._pendingBusyStateId));
                        }
                        break;
                }

                if (this.EndNonZeroBusyState != null)
                {
                    this.EndNonZeroBusyState(sender, eventArgs);
                }
            }
            finally
            {
                this._pendingBusyState = int.MaxValue;
                this._pendingBusyStateId = int.MaxValue;
            }
        }

        private void FireSelfJumpCompleted(SelfJumpCompleteEventArgs eventArgs)
        {
            REPlugin.Instance.Debug.WriteObject(eventArgs);

            if (this.SelfJumpCompleted != null)
            {
                this.SelfJumpCompleted(this, eventArgs);
            }
        }

        private void DecalEventsProxy_RenderFrame(object sender, EventArgs e)
        {
            var currentBusyState = REPlugin.Instance.CoreManager.Actions.BusyState;

            bool nonZeroActionStatePending = this._nonZeroActionStatePending.WaitOne(0);

            if (!nonZeroActionStatePending && currentBusyState != 0)
            {
                var currentBusyStateId = REPlugin.Instance.CoreManager.Actions.BusyStateId;
                this.FireBeginNonZeroBusyState(sender, new BeginNonZeroBusyStateEventArgs(currentBusyState, currentBusyStateId));
            }
            else if(nonZeroActionStatePending && currentBusyState == 0)
            {
                // The busy state has ended
                this.FireEndNonZeroBusyState(sender, new EndNonZeroBusyStateEventArgs(this._pendingBusyState, this._pendingBusyStateId));
            }

            // TODO cache location to detect begin & end movement
        }

        private void DecalEventsProxy_StatusTextIntercept(object sender, Data.Events.StatusTextInterceptEventArgs e)
        {
            // We can provide support for a couple of additional events based on this one.

            string objectName = string.Empty;

            // Using Object Text Example(s) :
            //  Hooks_StatusTextIntercept : Text = Using the Portal to Town Network
            //  Hooks_StatusTextIntercept : Text = Using the Ong-Hau Village Portal
            //  Hooks_StatusTextIntercept : Text = Using the Corpse of <Blah...blah>

            if (e.IsUsingObject(out objectName))
            {
                // Note (3/5/2015) : So far this seems reliable.  When this message comes in, you pretty much have to have
                // the item selected.  So grab the id from the current selection.
                var currentSelectionId = REPlugin.Instance.PluginHost.Actions.CurrentSelection;

                var eventArgs = new UsingObjectEventArgs(objectName, currentSelectionId);

                REPlugin.Instance.Debug.WriteObject(eventArgs);

                if (this.UsingObject != null)
                {
                    this.UsingObject(sender, eventArgs);
                }

                // Once we fire the generic event, check and see if there is a more specific event we can fire
                this.CheckForAndFireMoreSpecificUsingEvents(sender, objectName, currentSelectionId);

                return;
            }

            // Object Test Example(s) :
            //  Hooks_StatusTextIntercept : Text = Approaching Small Creepy Statue
            //  Hooks_StatusTextIntercept : Text = Approaching Hisham al-Evv
            //  Hooks_StatusTextIntercept : Text = Approaching Umbral Guard
            if (e.IsApproachingObject(out objectName))
            {
                // Note (3/5/2015) : So far this seems reliable.  When this message comes in, you pretty much have to have
                // the item selected.  So grab the id from the current selection.
                var currentSelectionId = REPlugin.Instance.PluginHost.Actions.CurrentSelection;

                var eventArgs = new ApproachingObjectEventArgs(objectName, currentSelectionId);

                REPlugin.Instance.Debug.WriteObject(eventArgs);
                if (this.ApproachingObject != null)
                {
                    this.ApproachingObject(sender, eventArgs);
                }

                return;
            }

            // Too Busy Example :
            //  Text = You're too busy!
            if (e.IsYoureTooBusy())
            {
                var eventArgs = new YourTooBusyEventArgs();

                REPlugin.Instance.Debug.WriteObject(eventArgs);

                if (this.YourTooBusy != null)
                {
                    this.YourTooBusy(sender, eventArgs);
                }
            }

            if (this._giveItemPending.WaitOne(0) && e.IsCantBeGiven())
            {
                if (string.IsNullOrEmpty(this._pendingGiveItemTargetName))
                {
                    throw new InvalidOperationException("Expected to have a target name at this point, but we do not.");
                }

                if (this._pendingGiveItemOutcome == GiveItemOutcome.Undefined)
                {
                    throw new InvalidOperationException("Expected to know the outcome of the give at this point");
                }

                var eventArgs = new EndGiveItemEventArgs(this._pendingBusyStateId.ToWorldObject().Capture(), this._pendingGiveItemTargetName, this._pendingGiveItemOutcome);

                REPlugin.Instance.Debug.WriteObject(eventArgs);

                if (this.EndGiveItem != null)
                {
                    this.EndGiveItem(sender, eventArgs);
                }

                this._giveItemPending.Reset();
                this._pendingGiveItemOutcome = GiveItemOutcome.Undefined;
            }
        }

        private void DecalEventsProxy_ChangePortalMode(object sender, Decal.Adapter.Wrappers.ChangePortalModeEventArgs e)
        {
            // TODO : Hook up failure case.  Unable to Move object event
            if (e.Type == Decal.Adapter.Wrappers.PortalEventType.EnterPortal && this._usingPortalPending.WaitOne(0))
            {
                var eventArgs = new UsingPortalCompleteEventArgs(true);

                this.FireUsingPortalComplete(sender, eventArgs);
            }
        }

        private void _decalEventsProxy_ChatBoxMessage(object sender, Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            if (this._giveItemPending.WaitOne(0))
            {
                // Ex: You give Kreap Invitation Ithaenc Cathedral.
                // Ex: You give Kreap Velvet Baggy Pants.
                //         WorldObject Name will = Baggy Pants
                // Ex: You give Vassari 4 Quills of Infliction
                // Ex: You give Rockdown Guy 50 Silver Peas.
                if (e.IsYouGive())
                {
                    var worldObj = this._pendingGiveItemCapturedWorldObject;

                    var textMinusPrefix = e.Text.Substring(9);

                    var itemNameStartIndex = textMinusPrefix.IndexOf(worldObj.Name);

                    var targetName = textMinusPrefix.Substring(0, itemNameStartIndex).Trim();

                    // If the item we are giving has a material, it's going to be in the text and we need to trim it out
                    var materialValue = worldObj.Values(LongValueKey.Material);
                    if (materialValue > 0)
                    {
                        string materialName;
                        if(Mag.Shared.Constants.Dictionaries.MaterialInfo.TryGetValue(materialValue, out materialName))
                        {
                            targetName = targetName.Substring(0, targetName.Length - (materialName.Length + 1));
                        }
                    }

                    var stackCount = worldObj.Values(LongValueKey.StackCount);
                    if (stackCount > 1)
                    {
                        // When stack count > 0, what we will be looking at is :
                        //      targetName = Rockdown Guy 50 S
                        // The single character is left over from the item name being plural in the give message
                        var endingCharsToIgnore = 2 + (stackCount.ToString()).Length;
                        targetName = targetName.Substring(0, targetName.Length - endingCharsToIgnore).Trim();
                    }

                    var eventArgs = new EndGiveItemEventArgs(worldObj, targetName, GiveItemOutcome.Successful);
                    REPlugin.Instance.Debug.WriteObject(eventArgs);

                    if (this.EndGiveItem != null)
                    {
                        this.EndGiveItem(sender, eventArgs);
                    }

                    this._giveItemPending.Reset();
                    this._pendingGiveItemCapturedWorldObject = null;
                }
            }
        }

        private void _decalEventsProxy_StatusMessage(object sender, StatusMessageEventArgs e)
        {
            if (this._giveItemPending.WaitOne(0))
            {
                if (e.Type == (int)StatusMessageType.UnableToGiveItemBusy)
                {
                    // Ex:
                    //[RT-DEBUG] StatusMessageEventArgs
                    //    Text = Town Crier
                    //    Type = 30
                    this._pendingGiveItemTargetName = e.Text;
                    this._pendingGiveItemOutcome = GiveItemOutcome.FailedBusy;
                }
                else if (e.Type == (int)StatusMessageType.UnableToGiveItemCannotCarryAnymore)
                {
                    // Ex:
                    //[RT-DEBUG] StatusMessageEventArgs
                    //    Text = Trophy Cache
                    //    Type = 43
                    this._pendingGiveItemTargetName = e.Text;
                    this._pendingGiveItemOutcome = GiveItemOutcome.FailedCannotCarryAnymore;
                }
            }
        }

        private void _decalEventsProxy_ServerDispatch(object sender, Decal.Adapter.NetworkMessageEventArgs e)
        {
            if (e.Message.Type == 0xF74E) // Jumping
            {
                //Debug.WriteLineToMain("[ServerDispatch] - Jump detected!!");

                int characterId = Convert.ToInt32(e.Message["object"]);
                double heading = Convert.ToDouble(e.Message["heading"]);
                double height = Convert.ToDouble(e.Message["height"]);
                short numLogins = Convert.ToInt16(e.Message["logins"]);
                short totalJumps = Convert.ToInt16(e.Message["sequence"]);

                // I don't know if this could ever happen, so if check for it until I know.
                // TODO : Remove or support once I know if it happens for other characters
                // ANSWER : YES - Does pick up other character jumps
                if(characterId != REPlugin.Instance.CharacterFilter.Id)
                {
                    // Yes, it happens.  Commenting out since it is very spammy
                    //Debug.WriteLineToMain("Jump detected for character other than self!! - {0}", characterId);
                    //Debug.WriteLineToMain("Character was - {0}", characterId.ToWorldObject().Name);
                    return;
                }

                var jumpData = new JumpData(Location.CaptureCurrent(), heading, height);
                var jumpEventArgs = new JumpEventArgs(characterId, jumpData, numLogins, totalJumps);

                REPlugin.Instance.Debug.WriteObject(jumpEventArgs);

                if (this.SelfJump != null)
                {
                    this.SelfJump(sender, jumpEventArgs);
                }
            }
            else if (e.Message.Type == 0x019E) // Player Killed
            {
                Debug.WriteLineToMain("[ServerDispatch] - Player Killed detected!!");
            }
            else if(e.Message.Type == 0xF748) // Set Position & Motion
            {
                //Set position - the server pathologically sends these after every actions - sometimes more than once. If has options for setting a fixed velocity or an arc for thrown weapons and arrows.
                //    ObjectID    object  The object with the position changing.
                //    Position    position    The current or starting location.
                //    WORD    logins          logins
                //    WORD    sequence    A sequence number of some sort
                //    WORD    portals     number of portals
                //    WORD    adjustments     Adjustments to position

                // TODO : Could this be useful in my effort to implement auto-slave-jump?
            }
            else if (e.Message.Type == 0xF749) // Wield Object
            {
                //Multipurpose message. So far object wielding has been decoded. Lots of unknowns		
                //    ObjectID	owner	id of the owner of this object
                //    ObjectID	object	id of the object
                //    DWORD	unknown1	Unknown, always 1 in investigations
                //    DWORD	unknown2	Unknown, always 1 in investigations
                //    DWORD	unknown3	Unknown, Some sort of an equip counter, formula used to generate it: 0x00254 | (0x40000 + 0x30000*((ConnUser[UserParsing].EquipCount - 1) / 2))

                // Note by Mike : Happens a lot.  Happens for other characters.  Might even happen for another characters arrow draw.
                // TODO : How can I utilize this?
                // Commenting for now.  Spammy
                //Debug.WriteLineToMain("[ServerDispatch] - Wield Object detected!!");
            }
            else if (e.Message.Type == 0xF65A) // Move object into inventory
            {
                //ObjectID	object
                //WORD	unknown	unknown, was 0335 during testing
                //WORD	unknown1	unknown, appears to be a sequence number of some kind

                Debug.WriteLineToMain("[ServerDispatch] - Move object into inventory detected!!");
            }
            else if (e.Message.Type == 0x0052) // Close Container
            {
                // See web documentation for details.
                Debug.WriteLineToMain("[ServerDispatch] - Close Container detected!!");
            }
            else if (e.Message.Type == 0x00A0) // Failure to give item
            {
                // See web documentation for details.
                Debug.WriteLineToMain("[ServerDispatch] - Failure to Give Item detected!!");
            }
            else if (e.Message.Type == 0x01C7) // Ready. Previous action complete
            {
                // TODO : Does does this relate to CharacterFilter.ActionComplete?
                Debug.WriteLine("[ServerDispatch] - Ready. Previous action complete detected!!");
            }
            else if (e.Message.Type == 0x02BE) // Create Fellowhsip
            {
                Debug.WriteLineToMain("[ServerDispatch] - Create Fellowship detected!!");
            }
            else if (e.Message.Type == 0x02BF) // Disband Fellowhsip
            {
                Debug.WriteLineToMain("[ServerDispatch] - Disband Fellowship detected!!");
            }
            else if (e.Message.Type == 0x02C0) // Add Fellowhsip Member
            {
                Debug.WriteLineToMain("[ServerDispatch] - Add Fellowhsip Member detected!!");
            }
        }
    }
}
