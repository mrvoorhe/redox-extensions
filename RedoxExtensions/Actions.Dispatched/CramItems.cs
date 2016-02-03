using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Data;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched
{
    public class CramItems : AbstractPipelineAction
    {
        public CramItems(ISupportFeedback requestor)
            : base(requestor)
        {
            throw new NotImplementedException();
        }

        #region Static Methods

        public static IAction Create(ICommand command)
        {
            // Note : Reuse ItemUtilities.TryGetInventoryItemsForKeyword

            throw new NotImplementedException();
        }

        #endregion

        #region Properties

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
            get { return 100; }
        }

        #endregion

        #region Methods

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

        #endregion
    }
}
