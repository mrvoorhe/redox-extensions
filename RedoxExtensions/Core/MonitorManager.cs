using System;
using System.Collections.Generic;
using System.Text;
using RedoxExtensions.Core;
using RedoxExtensions.Listeners.Monitors;

namespace RedoxExtensions.Core;

public class MonitorManager : IDisposable
{
    private CharacterState _characterState;
    private CopyCatMaster _copyCatMaster;
    private FellowshipMonitor _fellowshipMonitor;
    private WorldMonitor _worldMonitor;
    private BankingTracker _bankingTracker;
    private StuckDetector _stuckDetector;
    private RouteRecorder _routeRecorder;
    private CommandScheduler _scheduler;

    public MonitorManager(IREEventsFireCallbacks rtEventsFireCallbacks)
    {
        this._characterState = new CharacterState(rtEventsFireCallbacks);
        this._copyCatMaster = new CopyCatMaster();
        this._fellowshipMonitor = new FellowshipMonitor();
        this._worldMonitor = new WorldMonitor();
        this._bankingTracker = new BankingTracker();
        this._stuckDetector = new StuckDetector();
        this._routeRecorder = new RouteRecorder();
        this._scheduler = new CommandScheduler();
    }

    public CopyCatMaster CopyCatMaster => this._copyCatMaster;

    public CharacterState CharacterState => this._characterState;

    public FellowshipMonitor Fellowship => this._fellowshipMonitor;

    public WorldMonitor WorldMonitor => this._worldMonitor;

    public BankingTracker Banking => this._bankingTracker;

    public StuckDetector StuckDetector => this._stuckDetector;

    public RouteRecorder RouteRecorder => this._routeRecorder;

    public CommandScheduler Scheduler => this._scheduler;

    #region IDisposable Members

    public void Dispose()
    {
        this._characterState.Dispose();
        this._copyCatMaster.Dispose();
        this._fellowshipMonitor.Dispose();
        this._bankingTracker.Dispose();
        this._stuckDetector.Dispose();
        this._routeRecorder.Dispose();
        this._scheduler.Dispose();
    }

    #endregion
}
