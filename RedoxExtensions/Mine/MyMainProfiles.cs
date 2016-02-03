using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Mine
{
    public enum MyMainProfiles
    {
        /// <summary>
        /// Looks up a character's default profile
        /// </summary>
        CharacterSpecificDefault,

        /// <summary>
        /// Loads a characters normal combat profile
        /// </summary>
        Normal,

        /// <summary>
        /// Loads a characters support profile
        /// </summary>
        Support,

        /// <summary>
        /// Loads a characters light profile.  Used for when full buffing is not required
        /// </summary>
        Light
    }
}
