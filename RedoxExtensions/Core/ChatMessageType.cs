using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Core
{
    public enum ChatMessageType
    {
        Unknown,

        PlayerSaysLocal,
        PlayerSaysChannel,
        YouSay,

        PlayerTellsYou,
        YouTell,

        NpcSays,
        NpcTellsYou
    }
}
