using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Core;

namespace RedoxExtensions.Listeners.Monitors
{
    public abstract class MasterOrSlaveReactions : IDisposable
    {

        protected MasterOrSlaveReactions()
        {
            REPlugin.Instance.Events.Decal.SpellCast += _decalEvents_SpellCast;
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.Decal.SpellCast -= _decalEvents_SpellCast;
        }

        protected abstract void OnSelfLifestoneRecall();

        void _decalEvents_SpellCast(object sender, Decal.Adapter.Wrappers.SpellCastEventArgs e)
        {
            switch (e.SpellId)
            {
                case Data.SpellIds.LifestoneRecall:
                    this.OnSelfLifestoneRecall();
                    break;
            }
        }
    }
}
