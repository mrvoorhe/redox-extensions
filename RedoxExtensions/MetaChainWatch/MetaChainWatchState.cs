using System;

namespace RedoxExtensions.MetaChainWatch;

public enum MetaChainWatchPhase
{
    /// <summary>The watch is off.  Meta loads are still tracked.</summary>
    Off,

    /// <summary>Timing the current meta.</summary>
    Timing,

    /// <summary>
    /// The meta timed out and a Recall was emitted.  Waiting for <see cref="MetaChainWatchState.OnRecallStarted"/> and
    /// then for a <see cref="MetaChainWatchLocation"/> at the recall spot.  Ends with a Timeout.
    /// </summary>
    Recalling,

    /// <summary>A Timeout was emitted.  Waiting for the caller to call <see cref="MetaChainWatchState.OnChainStateResult"/>.</summary>
    ResolvingChainState,

    /// <summary>The meta was sent to its chain state.  Waiting for a meta load before the fallback deadline.</summary>
    AwaitingNextMeta,
}

public enum MetaChainWatchActionKind
{
    None,

    /// <summary>
    /// The current meta ran past its limit (try 1) or the gem didn't teleport him (try 2).  Stop VT, clear the nav,
    /// go to peace mode, then use the gem unless he is already at the recall spot, and call OnRecallStarted.
    /// </summary>
    Recall,

    /// <summary>The current meta ran past its limit and the recall (if any) is over.  Resolve its chain state and call OnChainStateResult.</summary>
    Timeout,

    /// <summary>Run <see cref="MetaChainWatchAction.Command"/>.</summary>
    RunFallback,
}

/// <summary>How the recall before a chain state jump ended.</summary>
public enum MetaChainWatchRecallOutcome
{
    /// <summary>No recall: it is turned off in the config.</summary>
    None,

    /// <summary>The gem took him to the recall spot.</summary>
    Arrived,

    /// <summary>He was already at the recall spot, so the gem wasn't used.</summary>
    AlreadyThere,

    /// <summary>The gem isn't in his inventory.</summary>
    GemMissing,

    /// <summary>No arrival within <see cref="MetaChainWatchState.RecallWait"/> on any of the <see cref="MetaChainWatchState.RecallTries"/> tries.</summary>
    TimedOut,
}

/// <summary>What the caller did with a Recall action.</summary>
public enum MetaChainWatchRecallStart
{
    /// <summary>The gem was used.</summary>
    Used,

    /// <summary>He was already at the recall spot; the gem wasn't used.</summary>
    AlreadyThere,

    /// <summary>No item with the gem's exact name in inventory.</summary>
    GemMissing,
}

/// <summary>Where the character was, read on the game thread.</summary>
public class MetaChainWatchLocation
{
    public int Landcell { get; set; }
    public bool InPortalSpace { get; set; }

    /// <summary>When it was read.  Only reads after the gem use count.</summary>
    public DateTime ReadAt { get; set; }
}

/// <summary>
/// Something the caller should do.  Which fields are set depends on <see cref="Kind"/>.
/// </summary>
public class MetaChainWatchAction
{
    public static readonly MetaChainWatchAction None = new() { Kind = MetaChainWatchActionKind.None };

    public const string ReasonNoChainState = "no chain state";
    public const string ReasonNoNextMeta = "no next meta";

    public MetaChainWatchActionKind Kind { get; set; }

    /// <summary>The meta that timed out.  Null when no meta load has been seen.</summary>
    public string MetaName { get; set; }

    /// <summary>Timeout and Recall.</summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>Timeout and Recall.</summary>
    public TimeSpan Limit { get; set; }

    /// <summary>Timeout and Recall.  True when a per meta override set the limit.</summary>
    public bool IsOverride { get; set; }

    /// <summary>Timeout only.  The chain state from the meta's override entry, or null to scan the .met file.</summary>
    public string ConfiguredChainState { get; set; }

    /// <summary>Recall only.  The exact inventory name of the gem.</summary>
    public string RecallGem { get; set; }

    /// <summary>Recall, and Timeout after a recall.  The landblock the gem takes him to.</summary>
    public int RecallLandblock { get; set; }

    /// <summary>Recall only.  1 for the first try, 2 for the retry.</summary>
    public int RecallAttempt { get; set; }

    /// <summary>Recall only.  Pass it back to OnRecallStarted and IsRecallCurrent.</summary>
    public int RecallId { get; set; }

    /// <summary>Timeout only.  How the recall before it ended.</summary>
    public MetaChainWatchRecallOutcome RecallOutcome { get; set; }

    /// <summary>Timeout only, for <see cref="MetaChainWatchRecallOutcome.Arrived"/>.  Where he landed.</summary>
    public int? RecallLandcell { get; set; }

    /// <summary>RunFallback only.  The command to run.</summary>
    public string Command { get; set; }

    /// <summary>RunFallback only.  <see cref="ReasonNoChainState"/> or <see cref="ReasonNoNextMeta"/>.</summary>
    public string Reason { get; set; }

    /// <summary>RunFallback only, for <see cref="ReasonNoNextMeta"/>.  The chain state the meta was sent to.</summary>
    public string ChainState { get; set; }
}

public class TurnOnResult
{
    /// <summary>True when the watch was off and is now on.  False when it was already on and only the timeout changed.</summary>
    public bool Started { get; set; }

    /// <summary>The main timeout before the call, when it was already on.</summary>
    public double PreviousMinutes { get; set; }
}

public class TurnOffResult
{
    /// <summary>False when it was already off.</summary>
    public bool WasOn { get; set; }
}

public class MetaChainWatchStatus
{
    public bool IsOn { get; set; }
    public double MainMinutes { get; set; }
    public MetaChainWatchPhase Phase { get; set; }

    /// <summary>The last loaded meta, or null when none has been seen.</summary>
    public string CurrentMeta { get; set; }

    public DateTime? LoadedAt { get; set; }

    /// <summary>Time since the current meta loaded.  Null when none has been seen.</summary>
    public TimeSpan? SinceLoad { get; set; }

    /// <summary>Time counted toward the limit.  Null when off.</summary>
    public TimeSpan? Elapsed { get; set; }

    /// <summary>When the fallback runs if no meta loads.  Set only in <see cref="MetaChainWatchPhase.AwaitingNextMeta"/>.</summary>
    public DateTime? FallbackDeadline { get; set; }

    /// <summary>Set only in <see cref="MetaChainWatchPhase.Recalling"/>.</summary>
    public string RecallGem { get; set; }

    /// <summary>Set only in <see cref="MetaChainWatchPhase.Recalling"/>.  1 or 2.</summary>
    public int RecallAttempt { get; set; }

    /// <summary>Set only in <see cref="MetaChainWatchPhase.Recalling"/>.  When the current try gives up.</summary>
    public DateTime? RecallDeadline { get; set; }
}

/// <summary>
/// What is saved so the watch survives a crash, relog or plugin reload.  Only saved while the watch is on.
/// </summary>
public class MetaChainWatchSnapshot
{
    public bool IsOn { get; set; }
    public double MainMinutes { get; set; }

    /// <summary>The last loaded meta, or null when none had been seen.</summary>
    public string CurrentMeta { get; set; }

    /// <summary>Minutes since the current meta loaded, at <see cref="SavedAt"/>.  Null when none had been seen.</summary>
    public double? SinceLoadMinutes { get; set; }

    /// <summary>Minutes counted toward the current meta's limit, at <see cref="SavedAt"/>.</summary>
    public double ElapsedMinutes { get; set; }

    /// <summary>Local time of the save.  The plugin saves at least every 30 s while on, so this is about when the client went down.</summary>
    public DateTime SavedAt { get; set; }
}

public class ResumeResult
{
    public const string ReasonNoSave = "no saved state";
    public const string ReasonStale = "too old";
    public const string ReasonInvalid = "invalid";
    public const string ReasonAlreadyOn = "already on";

    public bool Resumed { get; set; }

    /// <summary>Why it didn't resume.  Null when it did.</summary>
    public string Reason { get; set; }

    /// <summary>How long ago the state was saved, i.e. roughly how long the client was down.  Zero when there was no save.</summary>
    public TimeSpan Age { get; set; }
}

/// <summary>
/// The meta chain watch logic, with no Decal or clock dependencies.  Not thread safe; the caller serializes calls.
/// <para>
/// Timing rules:
/// <list type="bullet">
/// <item>Meta loads are tracked even while off.</item>
/// <item>Turning on starts timing the current meta from the time it was turned on, not from when it loaded,
///       i.e. timing starts at max(LoadedAt, TurnedOnAt).  Turning on again while on only changes the timeout.</item>
/// <item>Every meta load restarts timing, including a reload of the same meta, and cancels a pending recall or
///       fallback.</item>
/// <item>With the recall on (the default), a meta past its limit first gets a Recall: the caller uses the gem and
///       the watch waits up to <see cref="RecallWait"/> for a location at the recall spot, then tries once more.
///       Arrival, the gem missing, already being there, or both tries failing all end the recall with a Timeout.</item>
/// <item>A Timeout is emitted once; ticks then return None until <see cref="OnChainStateResult"/> is called.</item>
/// <item>After a RunFallback, timing restarts from the fallback time for the current meta, so the timeout can fire
///       again later if the fallback gets stuck too.</item>
/// <item>When no meta load has been seen the watch still times out, with a null meta name.  The caller should
///       report no chain state, which leads to the fallback.</item>
/// <item>A <see cref="Resume"/> carries the saved run time over but not the time the client was down, and never
///       times out within <see cref="ResumeGrace"/> of resuming, so the game and VT can settle after a login.  The
///       phase isn't saved, so a crash mid recall resumes as Timing past its limit and recalls again.</item>
/// </list>
/// </para>
/// </summary>
public class MetaChainWatchState
{
    public static readonly TimeSpan NextMetaWait = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan ResumeGrace = TimeSpan.FromMinutes(1);

    /// <summary>How long each recall try waits for him to arrive, from the Recall action (and again from the gem use).</summary>
    public static readonly TimeSpan RecallWait = TimeSpan.FromSeconds(30);

    /// <summary>
    /// A try only gives up on a location read after its deadline (a slow portal may land just before it), or once
    /// this much past the deadline if no reads come at all.
    /// </summary>
    public static readonly TimeSpan RecallReadGrace = TimeSpan.FromSeconds(15);

    /// <summary>After a portal space exit during a try, it waits at least this long for a read of where he landed.</summary>
    public static readonly TimeSpan RecallLandingWait = TimeSpan.FromSeconds(10);

    /// <summary>The first gem use plus one retry.</summary>
    public const int RecallTries = 2;

    /// <summary>A save from the future by more than this (clock change) is treated as invalid.</summary>
    private static readonly TimeSpan ClockSkewAllowance = TimeSpan.FromMinutes(5);

    private DateTime _timingStart;
    private DateTime _fallbackDeadline;
    private DateTime? _graceUntil;
    private string _pendingChainState;

    // Recalling
    private MetaChainWatchAction _timedOut;
    private int _recallId;
    private int _recallAttempt;
    private string _recallGem;
    private int _recallLandblock;
    private DateTime? _recallUsedAt;
    private DateTime _recallDeadline;
    private MetaChainWatchRecallOutcome? _recallFinished;
    private bool _inPortalSpace;

    public bool IsOn { get; private set; }
    public double MainMinutes { get; private set; }
    public MetaChainWatchPhase Phase { get; private set; } = MetaChainWatchPhase.Off;
    public string CurrentMeta { get; private set; }
    public DateTime? LoadedAt { get; private set; }

    /// <summary>Goes up whenever something a <see cref="GetSnapshot"/> holds changes, so the caller knows when to save.</summary>
    public int Revision { get; private set; }

    public void OnMetaLoaded(string metaName, DateTime now)
    {
        CurrentMeta = MetaChainWatchRules.NormalizeName(metaName);
        LoadedAt = now;
        Revision++;

        if (IsOn)
            StartTiming(now);
    }

    /// <summary>What to save, or null when the watch is off (the caller then removes the saved state).</summary>
    public MetaChainWatchSnapshot GetSnapshot(DateTime now)
    {
        if (!IsOn)
            return null;

        return new MetaChainWatchSnapshot
        {
            IsOn = true,
            MainMinutes = MainMinutes,
            CurrentMeta = CurrentMeta,
            SinceLoadMinutes = LoadedAt.HasValue ? (now - LoadedAt.Value).TotalMinutes : null,
            ElapsedMinutes = (now - _timingStart).TotalMinutes,
            SavedAt = now,
        };
    }

    /// <summary>
    /// Turns the watch back on from a save, e.g. after a crash.  The meta is timed from where it was when saved, so a
    /// meta that keeps crashing the client still times out, but the time the client was down doesn't count.  A save
    /// older than <paramref name="maxAge"/> is ignored.  A phase other than Timing isn't restored: a meta that was
    /// already past its limit (recalling or waiting for the next meta) times out again once <see cref="ResumeGrace"/>
    /// is over, and gets a fresh recall.  Call it before turning on.  When a meta load was seen before the call, that
    /// newer meta is kept and timed from its load.
    /// </summary>
    public ResumeResult Resume(MetaChainWatchSnapshot saved, DateTime now, TimeSpan maxAge)
    {
        if (saved == null || !saved.IsOn)
            return new ResumeResult { Reason = ResumeResult.ReasonNoSave };

        var age = now - saved.SavedAt;
        if (IsOn)
            return new ResumeResult { Reason = ResumeResult.ReasonAlreadyOn, Age = age };

        if (!(saved.MainMinutes > 0) || !(saved.ElapsedMinutes >= 0) || age < -ClockSkewAllowance
            || (saved.SinceLoadMinutes.HasValue && !(saved.SinceLoadMinutes.Value >= 0)))
            return new ResumeResult { Reason = ResumeResult.ReasonInvalid, Age = age };

        if (age > maxAge)
            return new ResumeResult { Reason = ResumeResult.ReasonStale, Age = age };

        IsOn = true;
        MainMinutes = saved.MainMinutes;

        if (LoadedAt.HasValue)
        {
            StartTiming(LoadedAt.Value > now ? now : LoadedAt.Value);
        }
        else
        {
            CurrentMeta = string.IsNullOrEmpty(saved.CurrentMeta) ? null : MetaChainWatchRules.NormalizeName(saved.CurrentMeta);
            LoadedAt = saved.SinceLoadMinutes.HasValue ? now - TimeSpan.FromMinutes(saved.SinceLoadMinutes.Value) : null;
            StartTiming(now - TimeSpan.FromMinutes(saved.ElapsedMinutes));
        }

        _graceUntil = now + ResumeGrace;
        return new ResumeResult { Resumed = true, Age = age };
    }

    public TurnOnResult TurnOn(double minutes, DateTime now)
    {
        if (IsOn)
        {
            var previous = MainMinutes;
            MainMinutes = minutes;
            Revision++;
            return new TurnOnResult { Started = false, PreviousMinutes = previous };
        }

        IsOn = true;
        MainMinutes = minutes;
        StartTiming(LoadedAt.HasValue && LoadedAt.Value > now ? LoadedAt.Value : now);
        return new TurnOnResult { Started = true };
    }

    public TurnOffResult TurnOff()
    {
        var wasOn = IsOn;
        IsOn = false;
        Phase = MetaChainWatchPhase.Off;
        _pendingChainState = null;
        _graceUntil = null;
        ClearRecall();
        Revision++;
        return new TurnOffResult { WasOn = wasOn };
    }

    /// <summary>
    /// <paramref name="location"/> is the latest read of where he is (null when none), used only while Recalling.
    /// </summary>
    public MetaChainWatchAction Tick(DateTime now, MergedMetaChainWatchConfig merged, MetaChainWatchLocation location = null)
    {
        switch (Phase)
        {
            case MetaChainWatchPhase.Timing:
                var limit = MetaChainWatchRules.EffectiveLimit(merged, CurrentMeta, MainMinutes);
                var elapsed = now - _timingStart;
                if (elapsed < limit.Limit || (_graceUntil.HasValue && now < _graceUntil.Value))
                    return MetaChainWatchAction.None;

                var timeout = new MetaChainWatchAction
                {
                    Kind = MetaChainWatchActionKind.Timeout,
                    MetaName = CurrentMeta,
                    Elapsed = elapsed,
                    Limit = limit.Limit,
                    IsOverride = limit.IsOverride,
                    ConfiguredChainState = MetaChainWatchRules.FindTimeout(merged, CurrentMeta)?.ChainState,
                    RecallOutcome = MetaChainWatchRecallOutcome.None,
                };

                var recall = merged?.Recall;
                if (recall == null || !recall.Enabled || string.IsNullOrEmpty(recall.Gem))
                {
                    Phase = MetaChainWatchPhase.ResolvingChainState;
                    return timeout;
                }

                Phase = MetaChainWatchPhase.Recalling;
                _timedOut = timeout;
                _recallGem = recall.Gem;
                _recallLandblock = recall.Landblock;
                _recallFinished = null;
                return StartRecallTry(1, now);

            case MetaChainWatchPhase.Recalling:
                return TickRecalling(now, location);

            case MetaChainWatchPhase.AwaitingNextMeta:
                if (now < _fallbackDeadline)
                    return MetaChainWatchAction.None;
                return Fallback(now, merged, MetaChainWatchAction.ReasonNoNextMeta, _pendingChainState);

            default:
                return MetaChainWatchAction.None;
        }
    }

    /// <summary>
    /// True while the Recall with this id is the one in progress.  The caller checks it right before using the gem,
    /// so a recall that a meta load, an off or a newer try replaced never uses one.
    /// </summary>
    public bool IsRecallCurrent(int recallId) => Phase == MetaChainWatchPhase.Recalling && recallId == _recallId;

    /// <summary>
    /// The caller's answer to a Recall.  Used starts the wait for arrival; AlreadyThere and GemMissing end the recall,
    /// and the next tick emits the Timeout.  Returns false (and does nothing) when the recall isn't current.
    /// </summary>
    public bool OnRecallStarted(int recallId, MetaChainWatchRecallStart start, DateTime now)
    {
        if (!IsRecallCurrent(recallId))
            return false;

        switch (start)
        {
            case MetaChainWatchRecallStart.Used:
                _recallUsedAt = now;
                _recallDeadline = now + RecallWait;
                break;
            case MetaChainWatchRecallStart.AlreadyThere:
                _recallFinished = MetaChainWatchRecallOutcome.AlreadyThere;
                break;
            default:
                _recallFinished = MetaChainWatchRecallOutcome.GemMissing;
                break;
        }

        return true;
    }

    /// <summary>
    /// Portal space entered or left.  A recall try never gives up while he is in portal space, and after he leaves it
    /// the try waits at least <see cref="RecallLandingWait"/> so a read of where he landed comes in first.
    /// </summary>
    public void OnPortalSpace(bool entered, DateTime now)
    {
        _inPortalSpace = entered;
        if (!entered && Phase == MetaChainWatchPhase.Recalling && now + RecallLandingWait > _recallDeadline)
            _recallDeadline = now + RecallLandingWait;
    }

    /// <summary>
    /// The caller's answer to a Timeout.  When a state was found (and sent to VT) the watch waits
    /// <see cref="NextMetaWait"/> for a meta load, else it returns RunFallback right away.  Ignored (returns None)
    /// unless a Timeout is outstanding, e.g. when a meta loaded or the watch was turned off in the meantime.
    /// </summary>
    public MetaChainWatchAction OnChainStateResult(bool found, string stateName, DateTime now, MergedMetaChainWatchConfig merged)
    {
        if (Phase != MetaChainWatchPhase.ResolvingChainState)
            return MetaChainWatchAction.None;

        if (!found)
            return Fallback(now, merged, MetaChainWatchAction.ReasonNoChainState, null);

        Phase = MetaChainWatchPhase.AwaitingNextMeta;
        _pendingChainState = stateName;
        _fallbackDeadline = now + NextMetaWait;
        return MetaChainWatchAction.None;
    }

    public MetaChainWatchStatus GetStatus(DateTime now)
    {
        var recalling = Phase == MetaChainWatchPhase.Recalling;
        return new MetaChainWatchStatus
        {
            IsOn = IsOn,
            MainMinutes = MainMinutes,
            Phase = Phase,
            CurrentMeta = CurrentMeta,
            LoadedAt = LoadedAt,
            SinceLoad = LoadedAt.HasValue ? now - LoadedAt.Value : null,
            Elapsed = IsOn ? now - _timingStart : null,
            FallbackDeadline = Phase == MetaChainWatchPhase.AwaitingNextMeta ? _fallbackDeadline : null,
            RecallGem = recalling ? _recallGem : null,
            RecallAttempt = recalling ? _recallAttempt : 0,
            RecallDeadline = recalling ? _recallDeadline : null,
        };
    }

    private MetaChainWatchAction TickRecalling(DateTime now, MetaChainWatchLocation location)
    {
        if (_recallFinished.HasValue)
            return FinishRecall(_recallFinished.Value, null);

        // Only a read taken after the gem use, outside portal space, says where the gem took him.
        var fresh = location != null && _recallUsedAt.HasValue && location.ReadAt >= _recallUsedAt.Value;
        if (fresh && !location.InPortalSpace && MetaChainWatchRules.IsAtRecallSpot(location.Landcell, _recallLandblock))
            return FinishRecall(MetaChainWatchRecallOutcome.Arrived, location.Landcell);

        if (_inPortalSpace || now < _recallDeadline)
            return MetaChainWatchAction.None;

        // Past the deadline.  Judge on a read from after it (so a slow teleport that lands right at the deadline is
        // seen), and never while that read says he is in portal space, unless the reads stopped coming.
        var readAfterDeadline = location != null && location.ReadAt >= _recallDeadline;
        var noReads = now >= _recallDeadline + RecallReadGrace;
        if (!noReads && (!readAfterDeadline || location.InPortalSpace))
            return MetaChainWatchAction.None;

        if (_recallAttempt < RecallTries)
            return StartRecallTry(_recallAttempt + 1, now);

        return FinishRecall(MetaChainWatchRecallOutcome.TimedOut, null);
    }

    private MetaChainWatchAction StartRecallTry(int attempt, DateTime now)
    {
        _recallAttempt = attempt;
        _recallId++;
        _recallUsedAt = null;
        _recallDeadline = now + RecallWait;

        return new MetaChainWatchAction
        {
            Kind = MetaChainWatchActionKind.Recall,
            MetaName = _timedOut.MetaName,
            Elapsed = _timedOut.Elapsed,
            Limit = _timedOut.Limit,
            IsOverride = _timedOut.IsOverride,
            RecallGem = _recallGem,
            RecallLandblock = _recallLandblock,
            RecallAttempt = attempt,
            RecallId = _recallId,
        };
    }

    private MetaChainWatchAction FinishRecall(MetaChainWatchRecallOutcome outcome, int? landcell)
    {
        var timeout = _timedOut;
        timeout.RecallOutcome = outcome;
        timeout.RecallLandcell = landcell;
        timeout.RecallLandblock = _recallLandblock;
        ClearRecall();
        Phase = MetaChainWatchPhase.ResolvingChainState;
        return timeout;
    }

    private void ClearRecall()
    {
        // _recallId is never reset, so an old id can't match a later recall.
        _timedOut = null;
        _recallAttempt = 0;
        _recallGem = null;
        _recallUsedAt = null;
        _recallFinished = null;
    }

    private MetaChainWatchAction Fallback(DateTime now, MergedMetaChainWatchConfig merged, string reason, string chainState)
    {
        StartTiming(now);
        return new MetaChainWatchAction
        {
            Kind = MetaChainWatchActionKind.RunFallback,
            MetaName = CurrentMeta,
            Command = merged?.FallbackCommand ?? MetaChainWatchRules.DefaultFallbackCommand,
            Reason = reason,
            ChainState = chainState,
        };
    }

    private void StartTiming(DateTime start)
    {
        _timingStart = start;
        _pendingChainState = null;
        _graceUntil = null;
        ClearRecall();
        Phase = MetaChainWatchPhase.Timing;
        Revision++;
    }
}
