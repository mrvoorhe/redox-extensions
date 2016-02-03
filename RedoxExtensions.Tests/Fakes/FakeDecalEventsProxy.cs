using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

using RedoxExtensions.Core;

namespace RedoxExtensions.Tests.Fakes
{
    public class FakeDecalEventsProxy : IDecalEventsProxy
    {
        #region IDecalEventsProxy Events

        public event EventHandler<Decal.Adapter.ChatParserInterceptEventArgs> CommandLineText;

        public event EventHandler<Decal.Adapter.ChatTextInterceptEventArgs> ChatBoxMessage;

        public event EventHandler<Decal.Adapter.ItemSelectedEventArgs> ItemSelected;

        public event EventHandler<EventArgs> RenderFrame;

        public event EventHandler ActionComplete;

        public event EventHandler<Decal.Adapter.Wrappers.ChangePortalModeEventArgs> ChangePortalMode;

        public event EventHandler<Decal.Adapter.Wrappers.ChangeVitalEventArgs> ChangeVital;

        public event EventHandler<Decal.Adapter.Wrappers.DeathEventArgs> Death;

        public event EventHandler<Decal.Adapter.Wrappers.SpellCastEventArgs> SpellCast;

        public event EventHandler<Decal.Adapter.Wrappers.ChangeFellowshipEventArgs> ChangeFellowship;

        public event EventHandler<Decal.Adapter.Wrappers.StatusMessageEventArgs> StatusMessage;

        public event EventHandler<Decal.Adapter.Wrappers.ApproachVendorEventArgs> ApproachingVendor;

        public event EventHandler<Data.Events.StatusTextInterceptEventArgs> StatusTextIntercept;

        public event EventHandler<Decal.Adapter.Wrappers.CreateObjectEventArgs> CreateObject;

        public event EventHandler<Decal.Adapter.Wrappers.ChangeObjectEventArgs> ChangeObject;

        public event EventHandler<Decal.Adapter.Wrappers.MoveObjectEventArgs> MoveObject;

        public event EventHandler<Decal.Adapter.NetworkMessageEventArgs> ClientDispatch;

        public event EventHandler<Decal.Adapter.NetworkMessageEventArgs> ServerDispatch;

        public event EventHandler GraphicsReset;

        #endregion

        public void FireRenderFrame(EventArgs e)
        {
            if(this.RenderFrame != null)
            {
                this.RenderFrame(this, e);
            }
        }
    }
}
