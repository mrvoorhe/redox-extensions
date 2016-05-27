﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Commands;
using RedoxExtensions.Data;
using RedoxExtensions.Dispatching;
using RedoxExtensions.VirindiInterop;
using RedoxLib.Objects;

namespace RedoxExtensions.Actions.Dispatched.Internal
{
    public class GoToNearbyLocation : AbstractPipelineAction
    {
        private readonly CoordsObject _coords;
        private readonly double _finalHeadingInDegrees;

        private Location _startingLocation;

        public GoToNearbyLocation(ISupportFeedback requestor, CoordsObject coords, double finalHeadingInDegrees)
            : base(requestor)
        {
            this._coords = coords;
            this._finalHeadingInDegrees = finalHeadingInDegrees;
        }

        #region Static Methods

        public static IAction Create(ICommand command)
        {
            var eastWest = double.Parse(command.Arguments[0].Trim());
            var northSouth = double.Parse(command.Arguments[1].Trim());
            var heading = command.Arguments.Count > 2 ? double.Parse(command.Arguments[2].Trim()) : double.MinValue;

            return Create(command, eastWest, northSouth, heading);
        }

        public static IAction Create(ISupportFeedback requestor, double x, double y, int landblock, double headingInDegrees)
        {
            return Create(requestor, Mag.Shared.Util.GetCoords(landblock, x, y), headingInDegrees);
        }

        public static IAction Create(ISupportFeedback requestor, double eastWest, double northSouth, double headingInDegrees)
        {
            return Create(requestor, new CoordsObject(eastWest, northSouth), headingInDegrees);
        }

        public static IAction Create(ISupportFeedback requestor, Location location)
        {
            return new GoToNearbyLocation(requestor, location.Coords, location.HeadingInDegrees);
        }

        public static IAction Create(ISupportFeedback requestor, CoordsObject coords, double headingInDegrees)
        {
            return new GoToNearbyLocation(requestor, coords, headingInDegrees);
        }

        #endregion

        #region Properties

        public override bool RequireIdleToPerform
        {
            get { return true; }
        }

        public override VirindiInterop.VTRunState DesiredVTRunState
        {
            get { return VTRunState.Off; }
        }

        protected override int MaxTries
        {
            get
            {
                // Not sure what a good value would be.  May need tweaking
                return 3;
            }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get
            {
                // It could take a few seconds to run to the point, this may need tweaked
                // It's really getting stuck that is of most use for go to.  As long as we are still running
                // it's hard to know if we should timeout
                return 5000;
            }
        }

        #endregion

        #region Methods

        protected override void DoPeform()
        {
            // Implementation Ideas:
            // 1) Face the point we want to go to
            // 2) Hold down forward key until DoEnd called  (Or Call CoreManager.SetAutoRun(true)
            // 3) Each RenderFrame event, check current location, if destination, set successful & SetAutoRun(false)
            // 4) Set final heading once at location
            //var angleToDestionation = _startingLocation.Coords.AngleToCoords(_coords);

            //Requestor.GiveFeedback(FeedbackType.Information, "I'm going to face {0}", angleToDestionation);
            //REPlugin.Instance.Actions.FaceHeading(angleToDestionation, true);

            //REPlugin.Instance.CoreManager.Actions.SetAutorun(true);

            throw new NotImplementedException("This logic doesn't work entirely.  Need to use chained actions");
        }

        protected override void InitializeData()
        {
            _startingLocation = Location.CaptureCurrent();
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    if (!this.Requestor.FromSelf)
                    {
                        this.Requestor.GiveFeedback(FeedbackType.Successful, "{0}, I made it to my destination", this.Requestor.SourceCharacter);
                    }
                    break;
                default:
                    this.Requestor.GiveFeedback(FeedbackType.Failed, "{0}, I FAILED", this.Requestor.SourceCharacter);
                    break;
            }
        }

        protected override void HookEvents()
        {
            REPlugin.Instance.CoreManager.RenderFrame += CoreManager_RenderFrame;
        }

        protected override void UnhookEvents()
        {
            REPlugin.Instance.CoreManager.RenderFrame -= CoreManager_RenderFrame;
        }

        #endregion

        private void CoreManager_RenderFrame(object sender, EventArgs e)
        {
            //var newLocation = Location.CaptureCurrent();
            throw new NotImplementedException();
        }
    }
}
