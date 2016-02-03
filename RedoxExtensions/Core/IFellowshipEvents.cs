using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Data.Events;

namespace RedoxExtensions.Core
{
    public interface IFellowshipEventsDisposable : IFellowshipEvents, IDisposable
    {
    }

    public interface IFellowshipEvents
    {
        // TODO : Need to support initializing fellow members when you are recruited

        // TODO : Implement support for
        event EventHandler<FellowshipMemberDiedEventArgs> MemberDied;

        event EventHandler<FellowshipCreatedEventArgs> Created;

        event EventHandler<FellowshipDisbandEventArgs> Disband;

        // TODO : Is there a special message when someone else is dimisseD?  Or is it the same as them quiting?
        event EventHandler<FellowshipDismissedYouEventArgs> DismissedYou;

        event EventHandler<FellowshipJoinedOtherEventArgs> JoinedOther;

        event EventHandler<FellowshipJoinedYouEventArgs> JoinedYou;

        event EventHandler<FellowshipQuitOtherEventArgs> QuitOther;

        event EventHandler<FellowshipQuitYouEventArgs> QuitYou;
    }
}
