using RedoxExtensions.Data.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Core
{
    public class DecalEventsProxy : IDecalEventsProxy, IDisposable
    {
        public DecalEventsProxy()
        {
            REPlugin.Instance.CoreManager.CommandLineText += CoreManager_CommandLineText;
            REPlugin.Instance.CoreManager.ChatBoxMessage += CoreManager_ChatBoxMessage;
            REPlugin.Instance.CoreManager.RenderFrame += CoreManager_RenderFrame;
            REPlugin.Instance.CoreManager.ItemSelected += CoreManager_ItemSelected;

            REPlugin.Instance.CharacterFilter.ActionComplete += CharacterFilter_ActionComplete;
            REPlugin.Instance.CharacterFilter.ChangePortalMode += CharacterFilter_ChangePortalMode;
            REPlugin.Instance.CharacterFilter.ChangeVital += CharacterFilter_ChangeVital;
            REPlugin.Instance.CharacterFilter.Death += CharacterFilter_Death;
            REPlugin.Instance.CharacterFilter.SpellCast += CharacterFilter_SpellCast;
            REPlugin.Instance.CharacterFilter.StatusMessage += CharacterFilter_StatusMessage;
            REPlugin.Instance.CharacterFilter.ChangeFellowship += CharacterFilter_ChangeFellowship;

            REPlugin.Instance.CoreManager.WorldFilter.ApproachVendor += WorldFilter_ApproachVendor;
            REPlugin.Instance.CoreManager.WorldFilter.ChangeObject += WorldFilter_ChangeObject;
            REPlugin.Instance.CoreManager.WorldFilter.CreateObject += WorldFilter_CreateObject;
            REPlugin.Instance.CoreManager.WorldFilter.MoveObject += WorldFilter_MoveObject;

            REPlugin.Instance.PluginHost.Underlying.Hooks.StatusTextIntercept += new Decal.Interop.Core.IACHooksEvents_StatusTextInterceptEventHandler(Hooks_StatusTextIntercept);

            REPlugin.Instance.PluginBaseEvents.ClientDispatch += PluginBaseEvents_ClientDispatch;
            REPlugin.Instance.PluginBaseEvents.ServerDispatch += PluginBaseEvents_ServerDispatch;
        }

        public event EventHandler<Decal.Adapter.ChatParserInterceptEventArgs> CommandLineText;

        public event EventHandler<Decal.Adapter.ChatTextInterceptEventArgs> ChatBoxMessage;

        public event EventHandler<Decal.Adapter.ItemSelectedEventArgs> ItemSelected;

        public event EventHandler<EventArgs> RenderFrame;

        public event EventHandler ActionComplete;

        public event EventHandler<Decal.Adapter.Wrappers.ChangePortalModeEventArgs> ChangePortalMode;

        public event EventHandler<Decal.Adapter.Wrappers.ChangeVitalEventArgs> ChangeVital;

        public event EventHandler<Decal.Adapter.Wrappers.DeathEventArgs> Death;

        public event EventHandler<Decal.Adapter.Wrappers.SpellCastEventArgs> SpellCast;

        public event EventHandler<Decal.Adapter.Wrappers.StatusMessageEventArgs> StatusMessage;

        public event EventHandler<Decal.Adapter.Wrappers.ChangeFellowshipEventArgs> ChangeFellowship;

        public event EventHandler<Decal.Adapter.Wrappers.ApproachVendorEventArgs> ApproachingVendor;

        public event EventHandler<Decal.Adapter.Wrappers.CreateObjectEventArgs> CreateObject;

        public event EventHandler<Decal.Adapter.Wrappers.ChangeObjectEventArgs> ChangeObject;

        public event EventHandler<Decal.Adapter.Wrappers.MoveObjectEventArgs> MoveObject;

        public event EventHandler<Data.Events.StatusTextInterceptEventArgs> StatusTextIntercept;

        #region IPluginBaseEvents

        public event EventHandler<Decal.Adapter.NetworkMessageEventArgs> ClientDispatch;

        public event EventHandler<Decal.Adapter.NetworkMessageEventArgs> ServerDispatch;

        #endregion

        public void Dispose()
        {
            REPlugin.Instance.CoreManager.CommandLineText -= CoreManager_CommandLineText;
            REPlugin.Instance.CoreManager.ChatBoxMessage -= CoreManager_ChatBoxMessage;
            REPlugin.Instance.CoreManager.RenderFrame -= CoreManager_RenderFrame;
            REPlugin.Instance.CoreManager.ItemSelected -= CoreManager_ItemSelected;

            REPlugin.Instance.CharacterFilter.ActionComplete -= CharacterFilter_ActionComplete;
            REPlugin.Instance.CharacterFilter.ChangePortalMode -= CharacterFilter_ChangePortalMode;
            REPlugin.Instance.CharacterFilter.ChangeVital -= CharacterFilter_ChangeVital;
            REPlugin.Instance.CharacterFilter.Death -= CharacterFilter_Death;
            REPlugin.Instance.CharacterFilter.SpellCast -= CharacterFilter_SpellCast;
            REPlugin.Instance.CharacterFilter.StatusMessage -= CharacterFilter_StatusMessage;
            REPlugin.Instance.CharacterFilter.ChangeFellowship -= CharacterFilter_ChangeFellowship;

            REPlugin.Instance.CoreManager.WorldFilter.ApproachVendor -= WorldFilter_ApproachVendor;
            REPlugin.Instance.CoreManager.WorldFilter.ChangeObject -= WorldFilter_ChangeObject;
            REPlugin.Instance.CoreManager.WorldFilter.CreateObject -= WorldFilter_CreateObject;
            REPlugin.Instance.CoreManager.WorldFilter.MoveObject -= WorldFilter_MoveObject;

            REPlugin.Instance.PluginHost.Underlying.Hooks.StatusTextIntercept -= new Decal.Interop.Core.IACHooksEvents_StatusTextInterceptEventHandler(Hooks_StatusTextIntercept);

            REPlugin.Instance.PluginBaseEvents.ClientDispatch -= PluginBaseEvents_ClientDispatch;
            REPlugin.Instance.PluginBaseEvents.ServerDispatch -= PluginBaseEvents_ServerDispatch;
        }

        private void CoreManager_CommandLineText(object sender, Decal.Adapter.ChatParserInterceptEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                REPlugin.Instance.Debug.WriteObject(e);

                if (this.CommandLineText != null)
                {
                    this.CommandLineText(sender, e);
                }
            });
        }

        private void CoreManager_ChatBoxMessage(object sender, Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                REPlugin.Instance.Debug.WriteObject(e);

                if (this.ChatBoxMessage != null)
                {
                    this.ChatBoxMessage(sender, e);
                }
            });
        }

        private void CoreManager_RenderFrame(object sender, EventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                if (this.RenderFrame != null)
                {
                    this.RenderFrame(sender, e);
                }
            });
        }

        void CoreManager_ItemSelected(object sender, Decal.Adapter.ItemSelectedEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                REPlugin.Instance.Debug.WriteObject(e);

                if (this.ItemSelected != null)
                {
                    this.ItemSelected(sender, e);
                }
            });
        }

        void CharacterFilter_ActionComplete(object sender, EventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                REPlugin.Instance.Debug.WriteLine("CharacterState.ActionComplete");

                if (this.ActionComplete != null)
                {
                    this.ActionComplete(sender, e);
                }
            });
        }

        private void CharacterFilter_ChangePortalMode(object sender, Decal.Adapter.Wrappers.ChangePortalModeEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                //RTPlugin.Instance.DebugWriter.WriteObject(e);

                if (this.ChangePortalMode != null)
                {
                    this.ChangePortalMode(sender, e);
                }
            });
        }

        void CharacterFilter_Death(object sender, Decal.Adapter.Wrappers.DeathEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                //RTPlugin.Instance.DebugWriter.WriteObject(e);

                if (this.Death != null)
                {
                    this.Death(sender, e);
                }
            });
        }

        void CharacterFilter_ChangeVital(object sender, Decal.Adapter.Wrappers.ChangeVitalEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                //RTPlugin.Instance.Debug.WriteObject(e);

                if (this.ChangeVital != null)
                {
                    this.ChangeVital(sender, e);
                }
            });
        }

        void CharacterFilter_SpellCast(object sender, Decal.Adapter.Wrappers.SpellCastEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                REPlugin.Instance.Debug.WriteObject(e);

                if (this.SpellCast != null)
                {
                    this.SpellCast(sender, e);
                }
            });
        }

        void CharacterFilter_StatusMessage(object sender, Decal.Adapter.Wrappers.StatusMessageEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                REPlugin.Instance.Debug.WriteObject(e);

                if (this.StatusMessage != null)
                {
                    this.StatusMessage(sender, e);
                }
            });
        }

        void CharacterFilter_ChangeFellowship(object sender, Decal.Adapter.Wrappers.ChangeFellowshipEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                //RTPlugin.Instance.Debug.WriteObject(e);

                if (this.ChangeFellowship != null)
                {
                    this.ChangeFellowship(sender, e);
                }
            });
        }

        void WorldFilter_ApproachVendor(object sender, Decal.Adapter.Wrappers.ApproachVendorEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                REPlugin.Instance.Debug.WriteObject(e);

                if (this.ApproachingVendor != null)
                {
                    this.ApproachingVendor(sender, e);
                }
            });
        }

        void WorldFilter_CreateObject(object sender, Decal.Adapter.Wrappers.CreateObjectEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                REPlugin.Instance.Debug.WriteObject(e);

                if (this.CreateObject != null)
                {
                    this.CreateObject(sender, e);
                }
            });
        }

        void WorldFilter_ChangeObject(object sender, Decal.Adapter.Wrappers.ChangeObjectEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                // Don't print if it's a useless spammy one
                if (e.Change != Decal.Adapter.Wrappers.WorldChangeType.ManaChange && e.Change != Decal.Adapter.Wrappers.WorldChangeType.IdentReceived)
                {
                    REPlugin.Instance.Debug.WriteObject(e);
                }

                if (this.ChangeObject != null)
                {
                    this.ChangeObject(sender, e);
                }
            });
        }

        void WorldFilter_MoveObject(object sender, Decal.Adapter.Wrappers.MoveObjectEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                //RTPlugin.Instance.Debug.WriteObject(e);

                if (this.MoveObject != null)
                {
                    this.MoveObject(sender, e);
                }
            });
        }

        void Hooks_StatusTextIntercept(string bstrText, ref bool bEat)
        {
            // Can't set bEat inside an annonymous method.  So need to duplicate the
            // try/catch logic from InvokeOperationSafely
            try
            {
                var eventArgs = new StatusTextInterceptEventArgs(bstrText);

                REPlugin.Instance.Debug.WriteObject(eventArgs);

                if (this.StatusTextIntercept != null)
                {
                    this.StatusTextIntercept(this, eventArgs);

                    bEat = eventArgs.Eat;
                }
            }
            catch (Exception e)
            {
                REPlugin.Instance.Error.LogError(e);
            }
        }

        void PluginBaseEvents_ServerDispatch(object sender, Decal.Adapter.NetworkMessageEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                //RTPlugin.Instance.Debug.WriteObject(e, "ServerDispatch");

                if (this.ServerDispatch != null)
                {
                    this.ServerDispatch(sender, e);
                }
            });
        }

        void PluginBaseEvents_ClientDispatch(object sender, Decal.Adapter.NetworkMessageEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                //RTPlugin.Instance.Debug.WriteObject(e, "ClientDispatch");

                if (this.ClientDispatch != null)
                {
                    this.ClientDispatch(sender, e);
                }
            });
        }
    }
}
