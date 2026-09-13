using System;
using System.Collections.Generic;
using System.Text;
using RedoxExtensions.Core;
using RedoxExtensions.Listeners.Monitors;

namespace RedoxExtensions.Core
{
    public class MonitorManager : IDisposable
    {
        private CharacterState _characterState;
        private CopyCatMaster _copyCatMaster;
        private FellowshipMonitor _fellowshipMonitor;
        private WorldMonitor _worldMonitor;
        private BankStatsTracker _bankStatsTracker;

        public MonitorManager(IREEventsFireCallbacks rtEventsFireCallbacks)
        {
            this._characterState = new CharacterState(rtEventsFireCallbacks);
            this._copyCatMaster = new CopyCatMaster();
            this._fellowshipMonitor = new FellowshipMonitor();
            this._worldMonitor = new WorldMonitor();
            this._bankStatsTracker = new BankStatsTracker();
        }

        public CopyCatMaster CopyCatMaster
        {
            get
            {
                return this._copyCatMaster;
            }
        }

        public CharacterState CharacterState
        {
            get
            {
                return this._characterState;
            }
        }

        public FellowshipMonitor Fellowship
        {
            get
            {
                return this._fellowshipMonitor;
            }
        }

        public WorldMonitor WorldMonitor
        {
            get
            {
                return this._worldMonitor;
            }
        }

        public BankStatsTracker BankStats
        {
            get
            {
                return this._bankStatsTracker;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            this._characterState.Dispose();
            this._copyCatMaster.Dispose();
            this._fellowshipMonitor.Dispose();
            this._bankStatsTracker.Dispose();
        }

        #endregion
    }
}
