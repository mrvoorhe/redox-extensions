using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RedoxExtensions.Commands;
using RedoxExtensions.Data;
using RedoxExtensions.Dispatching;

namespace RedoxExtensions.Actions.Dispatched
{
    public class CastSpell : AbstractPipelineAction
    {
        private readonly string _spellName;
        private readonly int _targetId;

        public CastSpell(ISupportFeedback requestor, string spellName, int targetId)
            : base(requestor)
        {
            this._spellName = spellName;
            this._targetId = targetId;
        }

        #region Static Methods

        public static IAction Create(ICommand command)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Properties

        public override bool RequireIdleToPerform
        {
            get { throw new NotImplementedException(); }
        }

        public override VirindiInterop.VTRunState DesiredVTRunState
        {
            get { throw new NotImplementedException(); }
        }

        protected override int MaxTries
        {
            get { return 20; }
        }

        protected override int WaitTimeoutInMilliseconds
        {
            get { return 1000; }
        }

        #endregion

        #region Methods

        protected override void DoPeform()
        {
            throw new NotImplementedException();
        }

        protected override void InitializeData()
        {
            // TODO : Check if WandSpellOrb is equipped.  If not, go to CharacterState.KnownCastingItems.  Make sure to filter list for what is in the list
            throw new NotImplementedException();
        }

        protected override void DoEnd(WaitForCompleteOutcome finalOutcome)
        {
            throw new NotImplementedException();
        }

        protected override void HookEvents()
        {
            throw new NotImplementedException();
        }

        protected override void UnhookEvents()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
