using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.VirindiInterop.Events
{
    public class SpellCastAttemptingEventArgs : EventArgs
    {
        public SpellCastAttemptingEventArgs(int spellId, int target, int skill)
        {
            this.SpellId = spellId;
            this.Target = target;
            this.Skill = skill;
        }

        public int SpellId { get; private set; }
        public int Target { get; private set; }
        public int Skill { get; private set; }
    }
}
