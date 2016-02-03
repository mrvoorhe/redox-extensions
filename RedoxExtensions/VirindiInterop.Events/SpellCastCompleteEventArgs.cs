using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.VirindiInterop.Events
{
    public class SpellCastCompleteEventArgs : EventArgs
    {
        public SpellCastCompleteEventArgs(int spellId, int target, int duration)
        {
            this.SpellId = spellId;
            this.Target = target;
            this.Duration = duration;
        }

        public int SpellId { get; private set; }
        public int Target { get; private set; }
        public int Duration { get; private set; }
    }
}
