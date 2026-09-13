using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

using RedoxExtensions.Commands;
using RedoxExtensions.Core;
using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Data;
using RedoxExtensions.Data.Events;
using RedoxExtensions.Dispatching;
using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Actions.Dispatched
{
    public class CastSpell : AbstractPipelineAction
    {
        /// <summary>
        /// How long to wait for a wand to finish wielding before giving up and failing the action.
        /// </summary>
        private static readonly TimeSpan EquipTimeout = TimeSpan.FromMilliseconds(3000);

        private readonly int _spellId;
        private readonly int _targetId;
        private readonly int _wandId;

        private volatile bool _wandEquipped;
        private bool _magicModeRequested;
        private int _lastWieldAttempt = -1;
        private DateTime _wieldIssuedAt;

        public CastSpell(ISupportFeedback requestor, int spellId, int targetId, int wandId, bool wandAlreadyEquipped)
            : base(requestor)
        {
            this._spellId = spellId;
            this._targetId = targetId;
            this._wandId = wandId;
            this._wandEquipped = wandAlreadyEquipped;
        }

        #region Static Methods

        public static IAction Create(ISupportFeedback requestor, string spellName, int targetId)
        {
            var spellId = RedoxLib.SpellUtilities.LookUpSpellIdByName(spellName);
            if (spellId == 0)
            {
                throw new DisplayToUserException(string.Format("Unable to lookup spell id for : {0}", spellName), requestor);
            }

            bool wandAlreadyEquipped;
            var wandId = ResolveWand(out wandAlreadyEquipped);
            if (wandId == 0)
            {
                throw new DisplayToUserException("I have no wand/staff/orb to cast with", requestor);
            }

            return new CastSpell(requestor, spellId, targetId, wandId, wandAlreadyEquipped);
        }

        /// <summary>
        /// Selects a casting item using the order : already equipped -> known casting item -> any in inventory.
        /// </summary>
        /// <returns>The world object id of the wand to use, or 0 if none could be found.</returns>
        private static int ResolveWand(out bool alreadyEquipped)
        {
            alreadyEquipped = false;

            var magicWeapons = REPlugin.Instance.CoreManager.WorldFilter.GetInventory().GetMagicWeapons();

            var equipped = magicWeapons.FirstOrDefault(w => w.IsEquippedByMe());
            if (equipped != null)
            {
                alreadyEquipped = true;
                return equipped.Id;
            }

            foreach (var knownId in REPlugin.Instance.MonitorManager.CharacterState.KnownCastingItems)
            {
                var known = REPlugin.Instance.CoreManager.WorldFilter[knownId];
                if (known != null && known.InMyPossession())
                {
                    return known.Id;
                }
            }

            var anyWand = magicWeapons.FirstOrDefault();
            return anyWand != null ? anyWand.Id : 0;
        }

        #endregion

        #region Properties

        public override bool RequireIdleToPerform
        {
            get { return true; }
        }

        public override VTRunState DesiredVTRunState
        {
            get { return VTRunState.Off; }
        }

        protected override int MaxTries
        {
            get { return 2; }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            // A cast has windup, give it time to complete before timing out.
            get { return 5000; }
        }

        #endregion

        #region Methods

        protected override bool DoReady(int attemptsThusFar)
        {
            if (this.IsWandEquipped())
            {
                // Wand is on, make sure we are in magic mode before we try to cast.
                if (!this._magicModeRequested && REPlugin.Instance.Actions.CombatMode != CombatState.Magic)
                {
                    CoreManager.Current.Actions.SetCombatMode(CombatState.Magic);
                    this._magicModeRequested = true;
                }

                return true;
            }

            // Wand is not equipped yet.  Kick off a wield (once per attempt) and wait for it to complete.
            if (attemptsThusFar != this._lastWieldAttempt)
            {
                REPlugin.Instance.Debug.WriteLineDiagnostic($"{attemptsThusFar} Attempt to wield {_wandId.ToWorldObject().ToShortSummary()}");
                REPlugin.Instance.Actions.AutoWield(this._wandId);
                this._lastWieldAttempt = attemptsThusFar;
                this._wieldIssuedAt = DateTime.Now;
                return false;
            }

            // If the wield never lands, don't hang forever.  Let DoPeform fail the action.
            if (DateTime.Now - this._wieldIssuedAt > EquipTimeout)
            {
                REPlugin.Instance.Debug.WriteLineDiagnostic("Failed to equip magic item");
                return true;
            }

            return false;
        }

        protected override void DoPeform()
        {
            if (!this.IsWandEquipped())
            {
                // The wield timed out (see DoReady).  Fail rather than casting bare handed.
                this.Failed.Set();
                return;
            }

            REPlugin.Instance.Actions.CastSpell(this._spellId, this._targetId);
        }

        protected override void InitializeData()
        {
            // Nothing to initialize - the wand and spell were resolved in Create.
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            switch (finalOutcome)
            {
                case WaitForCompleteOutcome.Success:
                    if (!this.Requestor.FromSelf)
                    {
                        this.Requestor.GiveFeedback(FeedbackType.Successful, "{0}, Cast Complete", this.Requestor.SourceCharacter);
                    }
                    break;
                default:
                    this.Requestor.GiveFeedback(FeedbackType.Failed, "{0}, I FAILED to cast", this.Requestor.SourceCharacter);
                    break;
            }
        }

        protected override void HookEvents()
        {
            REPlugin.Instance.Events.RE.EndEquipItem += RT_EndEquipItem;
            REPlugin.Instance.Events.Decal.SpellCast += Decal_SpellCast;
        }

        protected override void UnhookEvents()
        {
            REPlugin.Instance.Events.RE.EndEquipItem -= RT_EndEquipItem;
            REPlugin.Instance.Events.Decal.SpellCast -= Decal_SpellCast;
        }

        void RT_EndEquipItem(object sender, ObjectIdEventArgs e)
        {
            if (e.ObjectId == this._wandId)
            {
                this._wandEquipped = true;
            }
        }

        void Decal_SpellCast(object sender, SpellCastEventArgs e)
        {
            if (e.SpellId == this._spellId)
            {
                this.Successful.Set();
            }
        }

        private bool IsWandEquipped()
        {
            if (this._wandEquipped)
            {
                return true;
            }

            var wand = REPlugin.Instance.CoreManager.WorldFilter[this._wandId];
            return wand != null && wand.IsEquippedByMe();
        }

        #endregion
    }
}
