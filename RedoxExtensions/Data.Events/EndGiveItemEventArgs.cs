using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data.Events
{
    public class EndGiveItemEventArgs : EventArgs
    {
        public EndGiveItemEventArgs(IWorldObject itemGiven, string targetName, GiveItemOutcome outcome)
        {
            this.ItemGiven = itemGiven;
            this.TargetName = targetName;
            this.Outcome = outcome;
        }

        public IWorldObject ItemGiven { get; private set; }
        public string TargetName { get; private set; }
        public GiveItemOutcome Outcome { get; private set; }
    }
}
