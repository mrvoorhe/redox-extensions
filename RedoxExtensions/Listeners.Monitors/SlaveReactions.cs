using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Listeners.Monitors
{
    public class SlaveReactions : MasterOrSlaveReactions
    {
        protected override void OnSelfLifestoneRecall()
        {
            throw new NotImplementedException();
        }
    }
}
