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
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Diagnostics;
using RedoxExtensions.Dispatching;
using RedoxLib.Utilities;

namespace RedoxExtensions.Actions.Dispatched.Internal
{
    public class UseNpc : AbstractPipelineAction
    {
        private readonly int _npcId;

        private FilteredChatBoxMessageEventProvider _npcResponseListener;
        private string _npcName;

        private VTRunScope _originalVTScope;
        private WorldObjectMutex _woMutex;

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

        protected override bool DoReady(int attemptsThusFar)
        {
            //  The first couple times, let's just spam use the npc.  Some of them you can use quickly and there is no need
            //  to get bogged down syncing up with a global lock
            if (attemptsThusFar < 10)
                return true;

            if (_woMutex == null)
                _woMutex = WorldObjectMutex.Create(_npcId.ToWorldObject());

            if (_woMutex.TryObtain(0))
                return true;

            return false;
        }

        protected override void DoPeform()
        {
            REPlugin.Instance.PluginHost.Actions.SelectItem(this._npcId);

            using (var mutex = WorldObjectMutex.Obtain(_npcId.ToWorldObject()))
            {
                REPlugin.Instance.PluginHost.Actions.UseItem(this._npcId, 0);
            }
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
            if (_woMutex != null)
                _woMutex.Dispose();

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
            REPlugin.Instance.Events.Decal.ChatBoxMessage += Decal_ChatBoxMessage;
        }

        private void Decal_ChatBoxMessage(object sender, Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            // Sometimes the npc won't say anything, all you get is a message about needing to wait.  When this happens, we need to count this
            // as a successful use as well.
            Debug.WriteLine("UseNpc : ChatBox Message. Text = {2}, Color = {0}, Target = {1}", e.Color, e.Target, e.Text);

            // NOTE : Some special NPC's don't send you a tell, they just result in a chatbox message.  Since the list of possible Text is endless,
            // let's treat any chatbox message as success.
            // This could potentially result in false positives. But I think the odds are low and favoring success over failed will normally give a slightly better behavior
            // since failing means retry over and over
            this.Successful.Set();
        }

        protected override void UnhookEvents()
        {
            this._npcResponseListener.FilteredChatBoxMessage -= _npcResponseListener_FilteredChatBoxMessage;
            REPlugin.Instance.Events.Decal.ChatBoxMessage -= Decal_ChatBoxMessage;
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
