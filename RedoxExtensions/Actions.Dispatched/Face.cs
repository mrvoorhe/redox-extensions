using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;
using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Dispatching;
using RedoxExtensions.VirindiInterop;
using RedoxLib.Objects;
using RedoxLib.Objects.Extensions;

namespace RedoxExtensions.Actions.Dispatched
{
    public class Face : AbstractPipelineAction
    {
        private readonly double _heading;

        private volatile bool _unhooked;

        public Face(ISupportFeedback requestor, double heading)
            : base(requestor)
        {
            _heading = heading;
        }

        #region Static Methods

        public static IAction Create(ICommand command)
        {

            if (command.Arguments.Count == 0)
            {
                return Create(command, REPlugin.Instance.Actions.CurrentSelection);
            }

            if (command.Arguments.Count == 2)
            {
                // It must be coords to face
                double ns = double.Parse(command.Arguments[0]);
                double ew = double.Parse(command.Arguments[1]);
                return Create(command, new CoordsObject(ns, ew));
            }

            if (command.Arguments.Count == 1)
            {
                if (command.Arguments[0].Contains("."))
                {
                    // Treat the argument like a heading
                    return Create(command, double.Parse(command.Arguments[0]));
                }

                int woId;
                if (int.TryParse(command.Arguments[0], out woId))
                {
                    // It must be a world object id
                    return Create(command, woId);
                }

                // Lastly, treat it as the name of an object
                return Create(command, command.Arguments[0]);
            }

            throw new DisplayToUserException(string.Format("Invalid argument count of : {0}", command.Arguments.Count), command);
        }

        public static IAction Create(ISupportFeedback requestor, string objectName)
        {
            return Create(requestor, WorldUtilities.GetFirstLandscapeObjectByName(objectName).Wrap());
        }

        public static IAction Create(ISupportFeedback requestor, int woId)
        {
            return Create(requestor, woId.ToWorldObject(REPlugin.Instance).Wrap());
        }

        public static IAction Create(ISupportFeedback requestor, IHaveCoordsObject obj)
        {
            return Create(requestor, obj.Coords);
        }

        public static IAction Create(ISupportFeedback requestor, CoordsObject point)
        {
            var selfCoords = Location.CaptureCurrent().Coords;
            var heading = selfCoords.AngleToCoords(point);
            return Create(requestor, heading);
        }

        public static IAction Create(ISupportFeedback requestor, double heading)
        {
            return new Face(requestor, heading);
        }

        #endregion

        #region AbstractPipelineAction

        public override bool RequireIdleToPerform
        {
            get { return true; }
        }

        public override VTRunState DesiredVTRunState
        {
            // TODO : Technically we don't care.  Add another property to say "Don't change it"
            get { return VTRunState.Off;}
        }

        protected override int MaxTries
        {
            get { return 0; }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get { return 3000; }
        }

        protected override void DoPeform()
        {
            REPlugin.Instance.Actions.FaceHeading(_heading, true);
        }

        protected override void InitializeData()
        {
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    if (!this.Requestor.FromSelf)
                    {
                        this.Requestor.GiveFeedback(FeedbackType.Successful, "{0}, I'm facing the desired heading", this.Requestor.SourceCharacter);
                    }
                    break;
                default:
                    this.Requestor.GiveFeedback(FeedbackType.Failed, "{0}, I FAILED.  My current heading is : {1} and I was told to face {2}", this.Requestor.SourceCharacter, Location.CurrentHeading, _heading);
                    break;
            }
        }

        protected override void HookEvents()
        {
            REPlugin.Instance.Events.Decal.RenderFrame += CoreManager_RenderFrame;
        }

        protected override void UnhookEvents()
        {
            _unhooked = true;
            REPlugin.Instance.Events.Decal.RenderFrame -= CoreManager_RenderFrame;
        }

        #endregion

        private void CoreManager_RenderFrame(object sender, EventArgs e)
        {
            // This fires a lot and it seems it can fire after we've disposed
            // so let's used a flag to prevent issues accessing a disposed object
            if (_unhooked)
                return;

            if (Location.HeadingMatches(_heading))
                Successful.Set();
        }
    }
}
