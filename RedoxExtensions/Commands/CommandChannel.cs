using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Commands
{
    public enum CommandChannel
    {
        /// <summary>
        /// Ex: /re exit
        /// </summary>
        DirectEntry,

        /// <summary>
        /// A command received over local area chat
        /// </summary>
        AreaChat,

        /// <summary>
        /// Private tell
        /// </summary>
        Tell,

        /// <summary>
        /// Native fellowship
        /// </summary>
        Fellowship,

        VirindiFellowship
    }
}
