using Decal.Adapter.Wrappers;
using RedoxExtensions.Actions;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Events;
using RedoxExtensions.Data;
using RedoxExtensions.VirindiInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Commands;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched.Internal
{
    public class UseNpc : AbstractPipelineAction
    {
        private readonly int _npcId;

        private FilteredChatBoxMessageEventProvider _npcResponseListener;
        private string _npcName;

        private VTRunScope _originalVTScope;

        private bool _disposed;

        public UseNpc(ISupportFeedback requestor, int npcId)
            : base(requestor)
        {
            this._npcId = npcId;
        }

        #region Public Static Methods

        public static IAction Create(ICommand command)
        {
            var npcId = int.Parse(command.Arguments[0].Trim());
            return Create(command, npcId);
        }

        public static IAction Create(ISupportFeedback requestor, int npcId)
        {
            return new UseNpc(requestor, npcId);
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
            get { return 20; }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get { return 500; }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposed)
            {
                this._npcResponseListener.Dispose();
                this._disposed = true;
            }

            base.Dispose(disposing);
        }

        protected override void DoPeform()
        {
            REPlugin.Instance.PluginHost.Actions.SelectItem(this._npcId);
            REPlugin.Instance.PluginHost.Actions.UseItem(this._npcId, 0);
        }

        protected override void InitializeData()
        {
            this._npcResponseListener = new FilteredChatBoxMessageEventProvider(REPlugin.Instance.Events.Decal, Mag.Shared.Util.ChatFlags.NpcTellsYou, Mag.Shared.Util.ChatChannels.Tells);

            WorldObject npcObject = REPlugin.Instance.CoreManager.WorldFilter[this._npcId];

            this._npcName = npcObject.Name;

            REPlugin.Instance.Debug.WriteLine(string.Format("Will attempt to speak with : {0}", this._npcName));
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    if (!this.Requestor.FromSelf)
                    {
                        this.Requestor.GiveFeedback(FeedbackType.Successful, "{0}, I spoke to {1}", this.Requestor.SourceCharacter, this._npcName);
                    }
                    break;
                default:
                    this.Requestor.GiveFeedback(FeedbackType.Failed, "{0}, I FAILED to speak with {1}", this.Requestor.SourceCharacter, this._npcName);
                    break;
            }
        }

        protected override void HookEvents()
        {
            this._npcResponseListener.FilteredChatBoxMessage += _npcResponseListener_FilteredChatBoxMessage;
        }

        protected override void UnhookEvents()
        {
            this._npcResponseListener.FilteredChatBoxMessage -= _npcResponseListener_FilteredChatBoxMessage;
        }

        private void _npcResponseListener_FilteredChatBoxMessage(object sender, Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            string source = ChatParsingUtilities.GetSourceOfChat(e.Text);
            if (source == this._npcName)
            {
                REPlugin.Instance.Debug.WriteLine(string.Format("Received confirmation that I spoke with : {0}", this._npcName));
                this.Successful.Set();
            }
        }

        #endregion
    }
}
