using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;
using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Dispatching;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Actions.Dispatched.Internal
{
    public class ListItemsByKeyword : AbstractPipelineAction
    {
        private readonly ReadOnlyCollection<WorldObject> _itemsToGive;

        public ListItemsByKeyword(ISupportFeedback requestor, ReadOnlyCollection<WorldObject> itemsToGive)
            : base(requestor)
        {
            _itemsToGive = itemsToGive;
        }

        #region Static Methods

        public static IAction Create(ICommand command, ReadOnlyCollection<WorldObject> alreadyMatchedItems)
        {
            if (alreadyMatchedItems == null)
            {
                throw new ArgumentNullException("alreadyMatchedItems");
            }

            // Value Formats :
            // <keyword>

            ReadOnlyCollection<WorldObject> itemsToGive = alreadyMatchedItems;

            if (itemsToGive.Count == 0)
            {
                throw new DisplayToUserException("No items found", command);
            }

            var action = new ListItemsByKeyword(command, itemsToGive);
            return action;
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
                return VirindiInterop.VTRunState.Off;
            }
        }


        protected override int MaxTries
        {
            get
            {
                return 1;
            }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get
            {
                // Could have a little bit of a walk to a target, so give this a couple of seconds before timing out
                return 2000;
            }
        }

        #endregion

        #region Methods

        protected override void DoPeform()
        {
            foreach (var item in _itemsToGive)
            {
                this.Requestor.GiveFeedback(FeedbackType.Successful, "Match : {0} = {1} ", item.Name, item.Id);
            }
        }

        protected override void InitializeData()
        {
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    this.Requestor.GiveFeedback(FeedbackType.Successful, "Listing complete");
                    break;
                default:
                    throw new DisplayToUserException("Listing failed", this.Requestor);
            }
        }

        protected override void HookEvents()
        {
        }

        protected override void UnhookEvents()
        {
        }

        #endregion
    }
}
