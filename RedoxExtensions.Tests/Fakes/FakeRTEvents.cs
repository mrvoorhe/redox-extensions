using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RedoxExtensions.Core;
using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Tests.Fakes
{
    public class FakeRTEvents : IREEvents
    {
        public IFellowshipEvents Fellowship
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler<ApproachingObjectEventArgs> ApproachingObject;
        public event EventHandler<BeginBusyEventArgs> BeginBusy;
        public event EventHandler<ObjectIdEventArgs> BeginEquipItem;
        public event EventHandler<BeginGiveItemEventArgs> BeginGiveItem;
        public event EventHandler<BeginIdleEventArgs> BeginIdle;
        public event EventHandler<BeginNonZeroBusyStateEventArgs> BeginNonZeroBusyState;
        public event EventHandler<ObjectIdEventArgs> BeginUnequipItem;
        public event EventHandler<ObjectIdEventArgs> EndEquipItem;
        public event EventHandler<EndGiveItemEventArgs> EndGiveItem;
        public event EventHandler<EndNonZeroBusyStateEventArgs> EndNonZeroBusyState;
        public event EventHandler<ObjectIdEventArgs> EndUnequipItem;
        public event EventHandler<JumpEventArgs> SelfJump;
        public event EventHandler<SelfJumpCompleteEventArgs> SelfJumpCompleted;
        public event EventHandler<UsingObjectEventArgs> UsingObject;
        public event EventHandler<UsingPortalEventArgs> UsingPortal;
        public event EventHandler<UsingPortalCompleteEventArgs> UsingPortalComplete;
        public event EventHandler<EventArgs> VTStarted;
        public event EventHandler<EventArgs> VTStopped;
        public event EventHandler<YourTooBusyEventArgs> YourTooBusy;

        public void FireSelfJump(JumpEventArgs eventArgs)
        {
            if(this.SelfJump != null)
            {
                this.SelfJump(this, eventArgs);
            }
        }
    }
}
