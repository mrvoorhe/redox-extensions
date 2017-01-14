using RedoxExtensions.Actions;
using RedoxExtensions.Data;
using RedoxExtensions.VirindiInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Commands;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched.Internal
{
    public class UsePortal : AbstractPipelineAction
    {
        private readonly int _portalId;

        private VTRunScope _originalVTScope;

        public UsePortal(ISupportFeedback requestor, int portalId)
            : base(requestor)
        {
            this._portalId = portalId;
        }

        #region Public Static Methods

        public static IAction Create(ICommand command)
        {
            var portalId = int.Parse(command.Arguments[0].Trim());
            return Create(command, portalId);
        }

        public static IAction Create(ISupportFeedback requestor, int portalId)
        {
            return new UsePortal(requestor, portalId);
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

        #endregion

        #region Methods

        protected override void DoPeform()
        {
            // Select the portal first so that RTEvents portal using tracking will work correctly.
            REPlugin.Instance.PluginHost.Actions.SelectItem(this._portalId);
            REPlugin.Instance.PluginHost.Actions.UseItem(this._portalId, 0);
        }

        protected override void InitializeData()
        {
            // Nothing to do
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    if (!this.Requestor.FromSelf)
                    {
                        this.Requestor.GiveFeedback(FeedbackType.Successful, "{0}, right behind ya!", this.Requestor.SourceCharacter);
                    }
                    break;
                default:
                    this.Requestor.GiveFeedback(FeedbackType.Failed, "{0}, I was left behind!", this.Requestor.SourceCharacter);

                    // If a character fails to use the portal, we should disable nav so that the character stays put.
                    // And also avoids VT freaking out when the character you were following disappears.
                    VTActions.DisableNav();
                    break;
            }
        }

        protected override void HookEvents()
        {
            REPlugin.Instance.Events.RE.UsingPortalComplete += RTEvents_UsingPortalComplete;
            REPlugin.Instance.Events.Decal.ChangePortalMode += Decal_ChangePortalMode;
        }

        private void Decal_ChangePortalMode(object sender, Decal.Adapter.Wrappers.ChangePortalModeEventArgs e)
        {
            //REPlugin.Instance.Debug.WriteLine("Portal mode changes to : {0}", e.Type);
            // Consder enter or exist as successful
            this.Successful.Set();
        }

        protected override void UnhookEvents()
        {
            REPlugin.Instance.Events.RE.UsingPortalComplete -= RTEvents_UsingPortalComplete;
            REPlugin.Instance.Events.Decal.ChangePortalMode -= Decal_ChangePortalMode;
        }

        private void RTEvents_UsingPortalComplete(object sender, Data.Events.UsingPortalCompleteEventArgs e)
        {
            //REPlugin.Instance.Debug.WriteLine("Using portal complete");
            if (e.Successful)
            {
                this.Successful.Set();
            }
            else
            {
                this.Failed.Set();
            }
        }

        #endregion

        protected override int MaxTries
        {
            get { return 3; }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get
            {
                // Could have a long walk to the portal
                return 3000;
            }
        }
    }
}
