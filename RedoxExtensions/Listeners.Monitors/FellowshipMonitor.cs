using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Decal.Adapter.Wrappers;

namespace RedoxExtensions.Listeners.Monitors
{
    /// <summary>
    /// An object responsible for monitoring the status of the fellowship
    /// </summary>
    public class FellowshipMonitor : IDisposable
    {
        private readonly ManualResetEvent _noFellowship = new ManualResetEvent(true);
        private readonly ManualResetEvent _inFellowship = new ManualResetEvent(false);

        private string _fellowshipName = string.Empty;
        private string _leader = string.Empty;

        private readonly HashSet<string> _fellowshipMembers = new HashSet<string>();

        public FellowshipMonitor()
        {
            REPlugin.Instance.Events.RE.Fellowship.Created += Fellowship_Created;
            REPlugin.Instance.Events.RE.Fellowship.Disband += Fellowship_Disband;
            REPlugin.Instance.Events.RE.Fellowship.DismissedYou += Fellowship_DismissedYou;
            REPlugin.Instance.Events.RE.Fellowship.JoinedOther += Fellowship_JoinedOther;
            REPlugin.Instance.Events.RE.Fellowship.JoinedYou += Fellowship_JoinedYou;
            REPlugin.Instance.Events.RE.Fellowship.QuitOther += Fellowship_QuitOther;
            REPlugin.Instance.Events.RE.Fellowship.QuitYou += Fellowship_QuitYou;
        }

        #region Public Properties

        public WaitHandle NoFellowship
        {
            get
            {
                return this._noFellowship;
            }
        }

        public WaitHandle InFellowship
        {
            get
            {
                return this._inFellowship;
            }
        }

        public string FellowshipName
        {
            get
            {
                return this._fellowshipName;
            }
        }

        public string Leader
        {
            get
            {
                return this._leader;
            }
        }

        #endregion

        #region Public Methods

        public bool IsInFellowship(string characterName)
        {
            return this._fellowshipMembers.Contains(characterName);
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.RE.Fellowship.Created -= Fellowship_Created;
            REPlugin.Instance.Events.RE.Fellowship.Disband -= Fellowship_Disband;
            REPlugin.Instance.Events.RE.Fellowship.DismissedYou -= Fellowship_DismissedYou;
            REPlugin.Instance.Events.RE.Fellowship.JoinedOther -= Fellowship_JoinedOther;
            REPlugin.Instance.Events.RE.Fellowship.JoinedYou -= Fellowship_JoinedYou;
            REPlugin.Instance.Events.RE.Fellowship.QuitOther -= Fellowship_QuitOther;
            REPlugin.Instance.Events.RE.Fellowship.QuitYou -= Fellowship_QuitYou;

            this._fellowshipMembers.Clear();

            this._noFellowship.Close();
            this._inFellowship.Close();
        }

        #endregion

        void Fellowship_QuitYou(object sender, Data.Events.FellowshipQuitYouEventArgs e)
        {
            this.TransitionToNoFellowship();
        }

        void Fellowship_QuitOther(object sender, Data.Events.FellowshipQuitOtherEventArgs e)
        {
            this.RemoveMember(e.CharacterName);
        }

        void Fellowship_JoinedYou(object sender, Data.Events.FellowshipJoinedYouEventArgs e)
        {
            this.TransitionToInFellowship(e.FellowshipName, e.Leader);
        }

        void Fellowship_JoinedOther(object sender, Data.Events.FellowshipJoinedOtherEventArgs e)
        {
            this.AddMember(e.CharacterName);
        }

        void Fellowship_DismissedYou(object sender, Data.Events.FellowshipDismissedYouEventArgs e)
        {
            this.TransitionToNoFellowship();
        }

        void Fellowship_Disband(object sender, Data.Events.FellowshipDisbandEventArgs e)
        {
            this.TransitionToNoFellowship();
        }

        void Fellowship_Created(object sender, Data.Events.FellowshipCreatedEventArgs e)
        {
            this.TransitionToInFellowship(e.FellowshipName, "You");
        }

        private void TransitionToNoFellowship()
        {
            this._fellowshipName = string.Empty;
            this._leader = string.Empty;
            this._fellowshipMembers.Clear();

            this._noFellowship.Set();
            this._inFellowship.Reset();
        }

        private void TransitionToInFellowship(string fellowshipName, string leader)
        {
            this._fellowshipName = fellowshipName;
            this._leader = leader;
            this._inFellowship.Set();
            this._noFellowship.Reset();
        }

        private void AddMember(string originalCharacterName)
        {
            this._fellowshipMembers.Add(originalCharacterName);

            // Note (4/6/2015) : Not sure if I want to handle case incensitive contains checks or not.
            // going with not for now
            //var loweredName = originalCharacterName.ToLower();

            //if (loweredName != originalCharacterName)
            //{
            //    this._fellowshipMembers.Add(loweredName);
            //}
        }

        private void RemoveMember(string originalCharacterName)
        {
            this._fellowshipMembers.Remove(originalCharacterName);
        }
    }
}
