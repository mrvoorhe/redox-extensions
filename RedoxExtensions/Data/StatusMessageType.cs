using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data
{
    public enum StatusMessageType
    {
        /// <summary>
        /// Happens if you try and use something and fail to reach it
        /// </summary>
        UnableToMoveObject = 57,

        Busy = 29,

        /// <summary>
        /// Ex:  Try and summon Bellas portal when not flagged
        /// </summary>
        SummonPortalFailed = 1140,

        /// <summary>
        /// Happens when you try to give something to someone, but it fails due to them being busy
        /// </summary>
        UnableToGiveItemBusy = 30,

        /// <summary>
        /// Happens when you try to give something to someone, but it fails due to them not being able to carry anymore
        /// </summary>
        UnableToGiveItemCannotCarryAnymore = 43
    }
}
