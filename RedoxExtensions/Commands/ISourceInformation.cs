using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Commands
{
    /// <summary>
    /// Interface containing information regarding the source of a command.
    /// 
    /// This contains everything needed to respond back to the source of the request if needed.
    /// </summary>
    public interface ISourceInformation
    {
        string SourceCharacter { get; }
        int SourceCharacterId { get; }
        bool FromSelf { get; }
        CommandChannel Channel { get; }

        bool UsesChannelTag { get; }

        /// <summary>
        /// May or may not be used.
        /// 
        /// For VirindiFellow, this is used to store the fellowship name
        /// </summary>
        object ChannelTag { get; }

        bool IsSourceIdAvailable { get; }
    }
}
