using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Core;

namespace RedoxExtensions.Listeners.Monitors
{
    public class MasterReactions : MasterOrSlaveReactions
    {
        protected override void OnSelfLifestoneRecall()
        {
            // TODO : If slaves were following, tell them to wait.
            // Maybe set flag saying I abandonded them so that I can automatically tell them to follow me again
            // the next time I see them?
            throw new NotImplementedException();
        }
    }
}
