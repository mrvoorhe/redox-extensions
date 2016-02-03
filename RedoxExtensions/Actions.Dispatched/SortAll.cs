using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched
{
    /// <summary>
    /// Uses mudsort to sort your main inventory and all packs
    /// </summary>
    public class SortAll : AbstractPipelineAction
    {
        public SortAll(ISupportFeedback requestor)
            : base(requestor)
        {
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
            get { throw new NotImplementedException(); }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get { throw new NotImplementedException(); }
        }

        protected override void DoPeform()
        {
            throw new NotImplementedException();
        }

        protected override void InitializeData()
        {
            throw new NotImplementedException();
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
