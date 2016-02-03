using RedoxExtensions.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Data;
using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Listeners.Monitors
{
    /// <summary>
    /// A class for providing more advanced information regarding the current state of
    /// your character
    /// </summary>
    public class CharacterState : IDisposable
    {
        /// <summary>
        /// The maximum number of casting items to keep id's cached for
        /// </summary>
        private const int MaxCachedCastingItems = 5;

        private readonly ManualResetEvent _idle = new ManualResetEvent(true);
        private readonly ManualResetEvent _busy = new ManualResetEvent(false);
        private readonly ManualResetEvent _airBorne = new ManualResetEvent(false);
        private readonly ManualResetEvent _grounded = new ManualResetEvent(true);

        private readonly ManualResetEvent _vtRunning = new ManualResetEvent(false);

        private readonly IREEventsFireCallbacks _fireCallbacks;

        private readonly List<int> _knownCastingItems = new List<int>();

        public CharacterState(IREEventsFireCallbacks fireCallbacks)
        {
            this._fireCallbacks = fireCallbacks;

            REPlugin.Instance.Events.Decal.ActionComplete += DecalEventsProxy_ActionComplete;
            REPlugin.Instance.Events.Decal.CommandLineText += DecalEventsProxy_CommandLineText;
            REPlugin.Instance.Events.Decal.ChangePortalMode += DecalEventsProxy_ChangePortalMode;
            REPlugin.Instance.Events.Decal.StatusMessage += DecalEventsProxy_StatusMessage;
            REPlugin.Instance.Events.Decal.SpellCast += DecalEventsProxy_SpellCast;

            REPlugin.Instance.Events.RE.UsingPortal += RTEvents_UsingPortal;
            REPlugin.Instance.Events.RE.UsingObject += RTEvents_UsingObject;
            REPlugin.Instance.Events.RE.ApproachingObject += RTEvents_ApproachingObject;
            REPlugin.Instance.Events.RE.BeginNonZeroBusyState += RTEvents_BeginNonZeroBusyState;
            REPlugin.Instance.Events.RE.EndNonZeroBusyState += RTEvents_EndNonZeroBusyState;
            REPlugin.Instance.Events.RE.EndEquipItem += RT_EndEquipItem;

            REPlugin.Instance.Events.RE.SelfJump += RT_Jump;
            REPlugin.Instance.Events.RE.SelfJumpCompleted += RT_SelfJumpCompleted;

            REPlugin.Instance.Events.RE.VTStarted += RTEvents_VTStarted;
            REPlugin.Instance.Events.RE.VTStopped += RTEvents_VTStopped;

            // TODO : Hook VT Spell cast events once I have VTEventProxy Init'ing correctly
        }

        public WaitHandle Idle
        {
            get
            {
                return this._idle;
            }
        }

        public WaitHandle Busy
        {
            get
            {
                return this._busy;
            }
        }

        public WaitHandle NoFellowship
        {
            get
            {
                return REPlugin.Instance.MonitorManager.Fellowship.NoFellowship;
            }
        }

        public WaitHandle InFellowship
        {
            get
            {
                return REPlugin.Instance.MonitorManager.Fellowship.InFellowship;
            }
        }

        public WaitHandle AirBorne
        {
            get
            {
                return this._airBorne;
            }
        }

        public WaitHandle Grounded
        {
            get
            {
                return this._grounded;
            }
        }

        public WaitHandle VTRunning
        {
            get
            {
                return this._vtRunning;
            }
        }

        public ReadOnlyCollection<int> KnownCastingItems
        {
            get
            {
                return this._knownCastingItems.AsReadOnly();
            }
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.Decal.ActionComplete -= DecalEventsProxy_ActionComplete;
            REPlugin.Instance.Events.Decal.CommandLineText -= DecalEventsProxy_CommandLineText;
            REPlugin.Instance.Events.Decal.StatusMessage -= DecalEventsProxy_StatusMessage;
            REPlugin.Instance.Events.Decal.SpellCast -= DecalEventsProxy_SpellCast;

            REPlugin.Instance.Events.RE.UsingPortal -= RTEvents_UsingPortal;
            REPlugin.Instance.Events.RE.UsingObject -= RTEvents_UsingObject;
            REPlugin.Instance.Events.RE.ApproachingObject -= RTEvents_ApproachingObject;
            REPlugin.Instance.Events.RE.BeginNonZeroBusyState -= RTEvents_BeginNonZeroBusyState;
            REPlugin.Instance.Events.RE.EndNonZeroBusyState -= RTEvents_EndNonZeroBusyState;
            REPlugin.Instance.Events.RE.EndEquipItem -= RT_EndEquipItem;

            REPlugin.Instance.Events.RE.SelfJump -= RT_Jump;
            REPlugin.Instance.Events.RE.SelfJumpCompleted -= RT_SelfJumpCompleted;

            REPlugin.Instance.Events.RE.VTStarted -= RTEvents_VTStarted;
            REPlugin.Instance.Events.RE.VTStopped -= RTEvents_VTStopped;

            this._knownCastingItems.Clear();

            this._idle.Close();
            this._busy.Close();
            this._airBorne.Close();
            this._grounded.Close();
            this._vtRunning.Close();
        }

        void DecalEventsProxy_ActionComplete(object sender, EventArgs e)
        {
            this.TransitionToIdle(sender);
        }

        void DecalEventsProxy_CommandLineText(object sender, Decal.Adapter.ChatParserInterceptEventArgs e)
        {
            // The only way to capture busy invoking actions like recall to mansion, home, ls, etc
            // is to see the command come in
            if (e.Text.StartsWith("/ah") || e.Text.StartsWith("/ls") || e.Text.StartsWith("/hom"))
            {
                this.TransitionToBusy(sender);
            }
        }

        void DecalEventsProxy_ChangePortalMode(object sender, Decal.Adapter.Wrappers.ChangePortalModeEventArgs e)
        {
            if (e.Type == Decal.Adapter.Wrappers.PortalEventType.EnterPortal)
            {
                this.TransitionToBusy(sender);
            }
            else
            {
                this.TransitionToIdle(sender);
            }
        }

        void DecalEventsProxy_StatusMessage(object sender, Decal.Adapter.Wrappers.StatusMessageEventArgs e)
        {
            if (e.Type == (int)StatusMessageType.UnableToMoveObject)
            {
                this.TransitionToIdle(sender);
            }
        }

        void DecalEventsProxy_SpellCast(object sender, Decal.Adapter.Wrappers.SpellCastEventArgs e)
        {
            this.TransitionToBusy(sender);
        }

        void RTEvents_UsingObject(object sender, UsingObjectEventArgs e)
        {
            this.TransitionToBusy(sender);
        }

        void RTEvents_UsingPortal(object sender, Data.Events.UsingPortalEventArgs e)
        {
            this.TransitionToBusy(sender);
        }

        void RTEvents_ApproachingObject(object sender, Data.Events.ApproachingObjectEventArgs e)
        {
            this.TransitionToBusy(sender);
        }

        void RTEvents_EndNonZeroBusyState(object sender, EndNonZeroBusyStateEventArgs e)
        {
            this.TransitionToIdle(sender);
        }

        void RTEvents_BeginNonZeroBusyState(object sender, BeginNonZeroBusyStateEventArgs e)
        {
            this.TransitionToBusy(sender);
        }

        void RT_EndEquipItem(object sender, ObjectIdEventArgs e)
        {
            var worldObj = e.ObjectId.ToWorldObject();

            if (worldObj == null)
            {
                return;
            }

            // Code to support caching of casting items so that we can equip a wand/staff/orb
            if (this._knownCastingItems.Count < MaxCachedCastingItems && worldObj.IsWandStaffOrb())
            {
                worldObj.WriteToDebug("Caching Known Casting Item");
                this._knownCastingItems.Add(worldObj.Id);
            }
        }

        void RTEvents_VTStopped(object sender, EventArgs e)
        {
            this._vtRunning.Reset();
        }

        void RTEvents_VTStarted(object sender, EventArgs e)
        {
            this._vtRunning.Set();
        }


        void RT_Jump(object sender, JumpEventArgs e)
        {
            this._airBorne.Set();
            this._grounded.Reset();
        }

        void RT_SelfJumpCompleted(object sender, EventArgs e)
        {
            this._grounded.Set();
            this._airBorne.Set();
        }

        private void TransitionToIdle(object sender)
        {
            this._idle.Set();
            this._busy.Reset();

            this._fireCallbacks.FireBeginIdle(sender, new BeginIdleEventArgs());
        }

        private void TransitionToBusy(object sender)
        {
            this._idle.Reset();
            this._busy.Set();
            this._fireCallbacks.FireBeginBusy(sender, new BeginBusyEventArgs());
        }
    }
}
