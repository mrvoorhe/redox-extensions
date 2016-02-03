using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched
{
    public class LootItem : AbstractPipelineAction
    {
        public LootItem(ISupportFeedback requestor, int containerId, string itemName)
            : base(requestor)
        {
            // TODO
        }

        public override bool RequireIdleToPerform
        {
            get { throw new NotImplementedException(); }
        }

        public override VirindiInterop.VTRunState DesiredVTRunState
        {
            get { throw new NotImplementedException(); }
        }

        protected override int MaxTries
        {
            get { return 3; }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get { return 500; }
        }

        protected override void DoPeform()
        {
            throw new NotImplementedException();
        }

        protected override void InitializeData()
        {
            // Disable auto loot by changing loot profile
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            throw new NotImplementedException();
        }

        protected override void HookEvents()
        {
            throw new NotImplementedException();
        }

        protected override void UnhookEvents()
        {
            throw new NotImplementedException();
        }
    }
}
