using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Dispatching;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Actions.Dispatched
{
    public class Jump : AbstractPipelineAction
    {
        private readonly string _jumpCommand;

        public Jump(ISupportFeedback requestor, string jumpCommand)
            : base(requestor)
        {
            this._jumpCommand = jumpCommand;
        }

        #region Static Methods

        public static IAction Create(ISupportFeedback requestor, string mtJumpOption, int millisecondsHold)
        {
            return Create(requestor, string.Format("/mt {0} {1}", mtJumpOption, millisecondsHold));
        }

        public static IAction Create(ISupportFeedback requestor, string jumpCommand)
        {
            return new Jump(requestor, jumpCommand);
        }

        public static IAction CreateShiftHop()
        {
            return CreateShiftHop(CommandHelpers.Self);
        }

        public static IAction CreateShiftHop(ISupportFeedback requestor)
        {
            return Create(requestor, "sjumpw", 1);
        }

        public static IAction CreateHop()
        {
            return CreateHop(CommandHelpers.Self);
        }

        public static IAction CreateHop(ISupportFeedback requestor)
        {
            return Create(requestor, "jumpw", 1);
        }

        public static IAction CreateHopFull()
        {
            return CreateHopFull(CommandHelpers.Self);
        }

        public static IAction CreateHopFull(ISupportFeedback requestor)
        {
            return Create(requestor, "jumpw", 1000);
        }

        public static IAction CreateShiftHopFull()
        {
            return CreateShiftHopFull(CommandHelpers.Self);
        }

        public static IAction CreateShiftHopFull(ISupportFeedback requestor)
        {
            return Create(requestor, "sjumpw", 1000);
        }

        #endregion

        #region Properties

        public override bool RequireIdleToPerform
        {
            get
            {
                return true;
            }
        }

        public override VirindiInterop.VTRunState DesiredVTRunState
        {
            get
            {
                return VTRunState.Off;
            }
        }

        protected override int MaxTries
        {
            get
            {
                // Don't bother retrying a jump.  If the first one wasn't successful, we don't know what state or direction we are facing
                // trying to jump again would be pointless.
                return 0;
            }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get
            {
                // If it's a long fall, it could take awhile for the jump to complete
                return 7000;
            }
        }

        #endregion

        #region Methods

        protected override void DoPeform()
        {
            Core.Utilities.ACUtilities.ProcessArbitraryCommand(this._jumpCommand);
        }

        protected override void InitializeData()
        {
            // Nothing to initialize
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    if (!this.Requestor.FromSelf)
                    {
                        this.Requestor.GiveFeedback(FeedbackType.Successful, "{0}, Jump Complete", this.Requestor.SourceCharacter);
                    }
                    break;
                default:
                    this.Requestor.GiveFeedback(FeedbackType.Failed, "{0}, I FAILED to Jump", this.Requestor.SourceCharacter);
                    break;
            }
        }

        protected override void HookEvents()
        {
            REPlugin.Instance.Events.RE.SelfJumpCompleted += RT_SelfJumpCompleted;
        }

        protected override void UnhookEvents()
        {
            REPlugin.Instance.Events.RE.SelfJumpCompleted -= RT_SelfJumpCompleted;
        }

        void RT_SelfJumpCompleted(object sender, EventArgs e)
        {
            this.Successful.Set();
        }

        #endregion
    }
}
