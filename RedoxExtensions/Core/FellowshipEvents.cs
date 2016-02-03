using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Core
{
    public class FellowshipEvents : IFellowshipEventsDisposable
    {
        private readonly IDecalEventsProxy _decalEventsProxy;

        public FellowshipEvents(IDecalEventsProxy decalEventsProxy)
        {
            this._decalEventsProxy = decalEventsProxy;

        }

        public event EventHandler<Data.Events.FellowshipMemberDiedEventArgs> MemberDied;

        public event EventHandler<Data.Events.FellowshipCreatedEventArgs> Created;

        public event EventHandler<Data.Events.FellowshipDisbandEventArgs> Disband;

        public event EventHandler<FellowshipDismissedYouEventArgs> DismissedYou;

        public event EventHandler<Data.Events.FellowshipJoinedOtherEventArgs> JoinedOther;

        public event EventHandler<Data.Events.FellowshipJoinedYouEventArgs> JoinedYou;

        public event EventHandler<Data.Events.FellowshipQuitOtherEventArgs> QuitOther;

        public event EventHandler<Data.Events.FellowshipQuitYouEventArgs> QuitYou;

        public void Dispose()
        {
            this._decalEventsProxy.ChatBoxMessage -= _decalEventsProxy_ChatBoxMessage;
        }

        void _decalEventsProxy_ChatBoxMessage(object sender, Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            string tmpValue;
            string tmpValue2;

            // TODO : Support MemberDied

            if (e.IsFellowshipCreated(out tmpValue))
            {
                var createdEventArgs = new FellowshipCreatedEventArgs(tmpValue);

                REPlugin.Instance.Debug.WriteObject(createdEventArgs);

                if (this.Created != null)
                {
                    this.Created(this, createdEventArgs);
                }
            }
            else if (e.IsFellowshipDisbanded(out tmpValue))
            {
                var disbandEventArgs = new FellowshipDisbandEventArgs(tmpValue);

                REPlugin.Instance.Debug.WriteObject(disbandEventArgs);

                if (this.Disband != null)
                {
                    this.Disband(sender, disbandEventArgs);
                }
            }
            else if (e.IsYouHaveBeenDismissedFromFellowship(out tmpValue))
            {
                var dismissedYouEventArgs = new FellowshipDismissedYouEventArgs(tmpValue);

                REPlugin.Instance.Debug.WriteObject(dismissedYouEventArgs);

                if (this.DismissedYou != null)
                {
                    this.DismissedYou(sender, dismissedYouEventArgs);
                }
            }
            else if (e.IsPlayerJoinedFellowship(out tmpValue))
            {
                var joinedEventArgs = new FellowshipJoinedOtherEventArgs(tmpValue);

                REPlugin.Instance.Debug.WriteObject(joinedEventArgs);

                if (this.JoinedOther != null)
                {
                    this.JoinedOther(this, joinedEventArgs);
                }
            }
            else if (e.IsYouHaveBeenRecruitedToFellowship(out tmpValue, out tmpValue2))
            {
                var recruitedEventArgs = new FellowshipJoinedYouEventArgs(tmpValue, tmpValue2);

                REPlugin.Instance.Debug.WriteObject(recruitedEventArgs);

                if (this.JoinedYou != null)
                {
                    this.JoinedYou(this, recruitedEventArgs);
                }
            }
            else if (e.IsPlayerLeftFellowship(out tmpValue))
            {
                var leftEventArgs = new FellowshipQuitOtherEventArgs(tmpValue);

                REPlugin.Instance.Debug.WriteObject(leftEventArgs);

                if (this.QuitOther != null)
                {
                    this.QuitOther(this, leftEventArgs);
                }
            }

            else if (e.IsYouLeftFellowship(out tmpValue))
            {
                var quitYouEventArgs = new FellowshipQuitYouEventArgs(tmpValue);

                REPlugin.Instance.Debug.WriteObject(quitYouEventArgs);

                if (this.QuitYou != null)
                {
                    this.QuitYou(this, quitYouEventArgs);
                }
            }
        }
    }
}
