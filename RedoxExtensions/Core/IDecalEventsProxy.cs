using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using RedoxExtensions.Data.Events;
using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Core
{
    /// <summary>
    /// An interface that acts as a proxy to decal events.
    /// </summary>
    public interface IDecalEventsProxy : IPluginBaseEvents
    {
        #region Events from CoreManager

        event EventHandler<ChatParserInterceptEventArgs> CommandLineText;

        event EventHandler<ChatTextInterceptEventArgs> ChatBoxMessage;

        event EventHandler<ItemSelectedEventArgs> ItemSelected;

        event EventHandler<EventArgs> RenderFrame;

        #endregion

        #region Events from CharacterFilter

        /// <summary>
        /// Caused by:
        /// -Spell Cast Complete
        /// -Use NPC Complete
        /// -After using a portal (but before exiting the portal)
        /// -After using a portal gem
        /// 
        /// 
        /// NOT CAUSED BY:
        /// - Changing combat stance
        /// - Equipting or unequiping an item.
        /// - At any pointing during a recall command (ex: /hom)
        /// </summary>
        event EventHandler ActionComplete;

        event EventHandler<ChangePortalModeEventArgs> ChangePortalMode;

        event EventHandler<ChangeVitalEventArgs> ChangeVital;

        event EventHandler<DeathEventArgs> Death;

        event EventHandler<SpellCastEventArgs> SpellCast;

        event EventHandler<StatusMessageEventArgs> StatusMessage;

        event EventHandler<ChangeFellowshipEventArgs> ChangeFellowship;

        #endregion

        #region From WorldFilter

        event EventHandler<ApproachVendorEventArgs> ApproachingVendor;

        event EventHandler<CreateObjectEventArgs> CreateObject;

        event EventHandler<ChangeObjectEventArgs> ChangeObject;

        event EventHandler<MoveObjectEventArgs> MoveObject;

        #endregion

        #region From Underlying Hooks

        event EventHandler<StatusTextInterceptEventArgs> StatusTextIntercept;

        #endregion
    }
}
