using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;
using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Data;
using RedoxExtensions.Dispatching;
using RedoxLib.Objects;

namespace RedoxExtensions.Actions.Dispatched.Internal
{
    public class GiveItemsByKeyword : AbstractPipelineAction
    {
        private readonly ReadOnlyCollection<WorldObject> _itemsToGive;
        private readonly int _targetId;

        private readonly object _stateLock = new object();

        private IWorldObject _targetWorldObj;

        private int _currentGiveIndex = 0;
        private GiveItemOutcome _lastOutcome = GiveItemOutcome.Undefined;
        private int _successfulGives = 0;

        public GiveItemsByKeyword(ISupportFeedback requestor, ReadOnlyCollection<WorldObject> itemsToGive, int targetId)
            : base(requestor)
        {
            this._itemsToGive = itemsToGive;
            this._targetId = targetId;
        }

        #region Static Methods

        public static IAction Create(ICommand command)
        {
            // Value Formats :
            // <keyword>

            string keyword = command.Arguments[0];
            string secondOption = command.Arguments.Count > 1 ? command.Arguments[1] : string.Empty;

            ReadOnlyCollection<WorldObject> itemsToGive = ItemUtilities.TryGetInventoryItemsForKeyword(keyword, secondOption);

            if (itemsToGive == null)
            {
                throw new DisplayToUserException(string.Format("Unknown keyword : {0}", keyword), command);
            }

            return Create(command, itemsToGive);
        }

        public static IAction Create(ICommand command, ReadOnlyCollection<WorldObject> alreadyMatchedItems)
        {
            if (alreadyMatchedItems == null)
            {
                throw new ArgumentNullException("alreadyMatchedItems");
            }

            // Value Formats :
            // <keyword>
            // <target>

            int targetId = REPlugin.Instance.Actions.CurrentSelection;

            if (command.Arguments.Count >= 2)
            {
                targetId = int.Parse(command.Arguments[1]);
            }

            ReadOnlyCollection<WorldObject> itemsToGive = alreadyMatchedItems;

            if (itemsToGive.Count == 0)
            {
                throw new DisplayToUserException("No items to give", command);
            }

            var action = new GiveItemsByKeyword(command, itemsToGive, targetId);
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
                return 3;
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
            this._itemsToGive[this._currentGiveIndex].Give(this._targetId);
        }

        protected override void InitializeData()
        {
            this._targetWorldObj = this._targetId.ToWorldObject();

            if (this._targetWorldObj == null)
            {
                throw new DisplayToUserException(string.Format("Could not find target : {0}", this._targetId), this.Requestor);
            }

            REPlugin.Instance.Debug.WriteLine(string.Format("Number of Items to Give = {0}", this._itemsToGive.Count));
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    if (!this.Requestor.FromSelf)
                    {
                        this.Requestor.GiveFeedback(FeedbackType.Successful, "{0}, Gave {1} items to {2}", this.Requestor.SourceCharacter, this._successfulGives, this._targetWorldObj.Name);
                    }
                    break;
                default:
                    throw new DisplayToUserException(string.Format("Give failed.  Target Busy.  Items Given = {0}, Cause = {1}", this._currentGiveIndex, this._lastOutcome), this.Requestor);
            }
        }

        protected override void HookEvents()
        {
            REPlugin.Instance.Events.RE.EndGiveItem += RT_EndGiveItem;
        }

        protected override void UnhookEvents()
        {
            REPlugin.Instance.Events.RE.EndGiveItem -= RT_EndGiveItem;
        }

        protected override void DoResetForRetry()
        {
            lock (this._stateLock)
            {
                this._lastOutcome = GiveItemOutcome.Undefined;
            }
        }

        /// <summary>
        /// Called on the game thread.  Called AFTER ShouldRetry
        /// </summary>
        /// <param name="outcome"></param>
        /// <returns></returns>
        protected override bool ShouldCountRetry(WaitForCompleteOutcome outcome)
        {
            lock (this._stateLock)
            {
                if (this._lastOutcome == GiveItemOutcome.Successful)
                {
                    // Don't count successful gives.  Piggyback on the retry mechanism to give multiple items.
                    return false;
                }

                // Only count failed gives
                return true;
            }
        }

        /// <summary>
        /// Called on a background thread.  Called before ShouldCountRetry
        /// </summary>
        /// <param name="outcome"></param>
        /// <returns></returns>
        protected override bool ShouldRetry(WaitForCompleteOutcome outcome)
        {
            lock (this._stateLock)
            {
                this._currentGiveIndex++;

                // If we've reached the end of the items to give, then no more retries.
                if (this._currentGiveIndex >= this._itemsToGive.Count)
                {
                    return false;
                }

                if (outcome == WaitForCompleteOutcome.Success)
                {
                    return true;
                }

                return base.ShouldRetry(outcome);
            }
        }

        void RT_EndGiveItem(object sender, Data.Events.EndGiveItemEventArgs e)
        {
            lock (this._stateLock)
            {
                this._lastOutcome = e.Outcome;
                switch (e.Outcome)
                {
                    case GiveItemOutcome.Successful:
                        this.Successful.Set();
                        this._successfulGives++;
                        break;
                    default:
                        this.Failed.Set();
                        break;
                }
            }
        }

        #endregion
    }
}
