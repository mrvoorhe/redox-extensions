using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Actions;
using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.General.Utilities;
using RedoxExtensions.Data;
using RedoxExtensions.VirindiInterop;
using RedoxExtensions.Diagnostics;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched.Internal
{
    public class GiveItemsByName : AbstractPipelineAction
    {
        private readonly ReadOnlyCollection<GiveItemSearchData> _giveSearchData;
        private readonly bool _stopOnTargetFull;
        private readonly Dictionary<int, WorldObject> _foundTargets = new Dictionary<int, WorldObject>();

        private readonly object _stateLock = new object();

        private ReadOnlyCollection<ReadOnlyCollection<GiveItemData>> _foundItemsToGive;
        private List<int> _givenCount;

        private int _currentGiveIndex = 0;

        private GiveItemOutcome _lastOutcome = GiveItemOutcome.Undefined;
        private int _successfulGives = 0;

        public GiveItemsByName(ISupportFeedback requestor, IEnumerable<GiveItemSearchData> giveSearchData, bool stopOnTargetFull)
            : base(requestor)
        {
            this._giveSearchData = giveSearchData.ToList().AsReadOnly();
            this._stopOnTargetFull = stopOnTargetFull;
        }

        #region Static Methods

        public static IAction Create(ICommand command)
        {
            if (command.Channel == CommandChannel.DirectEntry)
            {
                string itemName = command.Arguments[0];

                int targetId = REPlugin.Instance.Actions.CurrentSelection;

                if (command.Arguments.Count >= 2)
                {
                    targetId = int.Parse(command.Arguments[1]);
                }

                return Create(command, itemName, 1, targetId, true);
            }
            else
            {
                // Value Formats :
                // <item name> | <target id> | [give count]

                if (command.Arguments.Count < 2)
                {
                    throw new DisplayToUserException(string.Format("Missing required arguments : {0}", command.RawValue), command);
                }
                if (command.Arguments.Count > 3)
                {
                    throw new DisplayToUserException(string.Format("Too many arguments : {0}", command.RawValue), command);
                }

                string itemName = command.Arguments[0];
                int targetId = int.Parse(command.Arguments[1]);
                int giveCount = command.Arguments.Count > 2 ? int.Parse(command.Arguments[2]) : int.MaxValue;

                return Create(command, itemName, giveCount, targetId, true);
            }
        }

        public static IAction Create(ISupportFeedback requestor, string itemName, int giveCount, string targetName, bool stopOnTargetFull, bool partialTargetMatch)
        {
            var closetTarget = Mag.Shared.Util.GetClosestObject(targetName, partialTargetMatch);
            return Create(requestor, new GiveItemSearchData(w => w.Name == itemName, giveCount, closetTarget.Id), partialTargetMatch);
        }

        public static IAction Create(ISupportFeedback requestor, string itemName, int giveCount, int targetId, bool stopOnTargetFull)
        {
            return Create(requestor, new GiveItemSearchData(w => w.Name == itemName, giveCount, targetId), stopOnTargetFull);
        }

        public static IAction Create(ISupportFeedback requestor, GiveItemSearchData giveItemSearchData, bool stopOnTargetFull)
        {
            return new GiveItemsByName(requestor, ListOperations.Create(giveItemSearchData), stopOnTargetFull);
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

        public override VTRunState DesiredVTRunState
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
                return 10;
            }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get
            {
                // Could have a little bit of a walk to a target, so give this a couple of seconds before timing out
                return 3000;
            }
        }

        #endregion

        #region Methods

        protected override void DoPeform()
        {
            var giveData = this._foundItemsToGive[this._currentGiveIndex][this._givenCount[this._currentGiveIndex]];

            giveData.Item.Give(giveData.TargetId);
        }

        protected override void InitializeData()
        {
            var targets = this._giveSearchData.Select(d => d.TargetId.ToWorldObject()).ToList();

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    this._foundTargets[this._giveSearchData[i].TargetId] = targets[i];
                }
            }

            var inventory = REPlugin.Instance.WorldFilter.GetInventory();
            var tmpGiveData = new List<ReadOnlyCollection<GiveItemData>>();
            this._givenCount = new List<int>();

            foreach (var searchData in this._giveSearchData)
            {
                var matchData = searchData.GetMatches(inventory).ToList();
                if (matchData.Count == 0)
                {
                    // TODO : Report skipping due to no matches
                    Debug.WriteLineToMain("Skipping, no matches found");
                    continue;
                }

                if (!this._foundTargets.ContainsKey(matchData.First().TargetId))
                {
                    // The target was not found for this give, we shouldn't try and give these things.
                    // TODO : Report skipping
                    Debug.WriteLineToMain("Skipping, target not found : {0}", matchData.First().TargetId);
                    continue;
                }

                // If the number of matches exceeds the requested number of gives, chop down the list
                if (matchData.Count > searchData.GiveCount)
                {
                    matchData = matchData.Take(searchData.GiveCount).ToList();
                }

                tmpGiveData.Add(matchData.AsReadOnly());
                this._givenCount.Add(0);
            }

            this._foundItemsToGive = tmpGiveData.AsReadOnly();
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    if (!this.Requestor.FromSelf)
                    {
                        this.Requestor.GiveFeedback(FeedbackType.Successful, "{0}, Gave {1} items to {2}, targets", this.Requestor.SourceCharacter, this._successfulGives, this._foundTargets.Count);
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
                if (outcome == WaitForCompleteOutcome.Success)
                {
                    this._givenCount[this._currentGiveIndex]++;
                    if (this._givenCount[this._currentGiveIndex] >= this._foundItemsToGive[this._currentGiveIndex].Count)
                    {
                        this._currentGiveIndex++;
                    }

                    // If we've reached the end of the items to give, then no more retries.
                    if (this._currentGiveIndex >= this._foundItemsToGive.Count)
                    {
                        Debug.WriteLineToMain("All items given");
                        return false;
                    }

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
                        this._successfulGives++;
                        this.Successful.Set();
                        break;
                    case GiveItemOutcome.FailedBusy:

                        // TODO : Don't don't this the same as other failures.  Should  do more retries than normal
                        Debug.WriteLineToMain("Give Failed - Target Busy");
                        this.Failed.Set();
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
