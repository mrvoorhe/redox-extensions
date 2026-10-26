using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Actions;

namespace RedoxExtensions.Listeners.Monitors
{
    /// <summary>
    /// Detects when the character has not moved, attacked, or cast a spell for a while and runs unstuck
    /// </summary>
    public class StuckDetector : IDisposable
    {
        public const double DefaultThresholdMinutes = 1;

        private const int GameEventMessageType = 0xF7B0;
        private const int AttackCompletedEvent = 0x01A7;
        private const int InflictMeleeDamageEvent = 0x01B1;
        private const int OtherMeleeEvadeEvent = 0x01B3;

        private const double MovementEpsilon = 0.5;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

        private bool _enabled;
        private bool _inPortalSpace;
        private TimeSpan _threshold = TimeSpan.FromMinutes(DefaultThresholdMinutes);
        private DateTime _lastCheckAt;
        private DateTime _lastMovedAt;
        private DateTime _lastActivityAt;
        private Location.Location _anchor;

        public StuckDetector()
        {
            REPlugin.Instance.Events.Decal.RenderFrame += Decal_RenderFrame;
            REPlugin.Instance.Events.Decal.ServerDispatch += Decal_ServerDispatch;
            REPlugin.Instance.Events.Decal.SpellCast += Decal_SpellCast;
            REPlugin.Instance.Events.Decal.ChangePortalMode += Decal_ChangePortalMode;
        }

        public bool Enabled => this._enabled;

        public double ThresholdMinutes => this._threshold.TotalMinutes;

        public void Enable(double? minutes)
        {
            if (minutes.HasValue)
            {
                this._threshold = TimeSpan.FromMinutes(minutes.Value);
            }

            this.Reset();
            this._enabled = true;
        }

        public void Disable()
        {
            this._enabled = false;
        }

        public void Reset()
        {
            var now = DateTime.Now;
            this._lastMovedAt = now;
            this._lastActivityAt = now;
            this._anchor = Location.Location.CaptureCurrent();
        }

        public void Dispose()
        {
            REPlugin.Instance.Events.Decal.RenderFrame -= Decal_RenderFrame;
            REPlugin.Instance.Events.Decal.ServerDispatch -= Decal_ServerDispatch;
            REPlugin.Instance.Events.Decal.SpellCast -= Decal_SpellCast;
            REPlugin.Instance.Events.Decal.ChangePortalMode -= Decal_ChangePortalMode;
        }

        void Decal_RenderFrame(object sender, EventArgs e)
        {
            if (!this._enabled)
            {
                return;
            }

            var now = DateTime.Now;
            if (now - this._lastCheckAt < CheckInterval)
            {
                return;
            }

            this._lastCheckAt = now;

            var current = Location.Location.CaptureCurrent();
            if (HasMoved(this._anchor, current))
            {
                this._anchor = current;
                this._lastMovedAt = now;
            }

            if (this._inPortalSpace || UnstuckActions.InProgress || !REPlugin.Instance.MonitorManager.CharacterState.VTRunning.WaitOne(0))
            {
                return;
            }

            if (now - this._lastMovedAt >= this._threshold && now - this._lastActivityAt >= this._threshold)
            {
                REPlugin.Instance.Chat.WriteLine("[RE] Stuck detected, running unstuck");
                this.Reset();
                UnstuckActions.Unstuck(this.Reset);
            }
        }

        void Decal_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            if (e.Message.Type != GameEventMessageType)
            {
                return;
            }

            switch (Convert.ToInt32(e.Message["event"]))
            {
                case AttackCompletedEvent:
                case InflictMeleeDamageEvent:
                case OtherMeleeEvadeEvent:
                    this._lastActivityAt = DateTime.Now;
                    break;
            }
        }

        void Decal_SpellCast(object sender, SpellCastEventArgs e)
        {
            this._lastActivityAt = DateTime.Now;
        }

        void Decal_ChangePortalMode(object sender, ChangePortalModeEventArgs e)
        {
            this._inPortalSpace = e.Type == PortalEventType.EnterPortal;
        }

        private static bool HasMoved(Location.Location anchor, Location.Location current)
        {
            if (anchor == null || anchor.LandBlock != current.LandBlock)
            {
                return true;
            }

            return Math.Abs(anchor.X - current.X) > MovementEpsilon
                || Math.Abs(anchor.Y - current.Y) > MovementEpsilon
                || Math.Abs(anchor.Z - current.Z) > MovementEpsilon;
        }
    }
}
