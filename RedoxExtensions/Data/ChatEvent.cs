using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Dispatching
{
    public class ChatEvent
    {
        public string SourceName { get; set; }
        public int SourceId { get; set; }

        public static bool TryParse(string text, out ChatEvent chatEvent)
        {

            //[RT-DEBUG] ChatTextInterupt
            //  Target = 0
            //  Text = Royal Guard tells you, "Kill 25 of the shadows to fight back this corruption. Killing the shadows on the perimeter will do us no good, make sure to strike at the heart of their forces."

            // TODO : Implement.  Use my code from FellowCommand?  Or use helpers in Mag.Shared.Util??
            throw new NotImplementedException();
        }
    }
}
