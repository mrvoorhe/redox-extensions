using System;

namespace RedoxExtensions.MetaChainWatch;

public enum MetaChainWatchPhase
{
    /// <summary>The watch is off.  Meta loads are still tracked.</summary>
    Off,

    /// <summary>Timing the current meta.</summary>
    Timing,

    /// <summary>A Timeout was emitted.  Waiting for the caller to call <see cref="MetaChainWatchState.OnChainStateResult"/>.</summary>
    ResolvingChainState,

    /// <summary>The meta was sent to its chain state.  Waiting for a meta load before the fallback deadline.</summary>
    AwaitingNextMeta,
}

public enum MetaChainWatchActionKind
{
    None,

    /// <summary>The current meta ran past its limit.  Resolve its chain state and call OnChainStateResult.</summary>
    Timeout,

    /// <summary>Run <see cref="MetaChainWatchAction.Command"/>.</summary>
    RunFallback,
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

    /// <summary>Timeout only.</summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>Timeout only.</summary>
    public TimeSpan Limit { get; set; }

    /// <summary>Timeout only.  True when a per meta override set the limit.</summary>
    public bool IsOverride { get; set; }

    /// <summary>Timeout only.  The chain state from the meta's override entry, or null to scan the .met file.</summary>
    public string ConfiguredChainState { get; set; }

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
}

/// <summary>
/// The meta chain watch logic, with no Decal or clock dependencies.  Not thread safe; the caller serializes calls.
/// <para>
/// Timing rules:
/// <list type="bullet">
/// <item>Meta loads are tracked even while off.</item>
/// <item>Turning on starts timing the current meta from the time it was turned on, not from when it loaded,
///       i.e. timing starts at max(LoadedAt, TurnedOnAt).  Turning on again while on only changes the timeout.</item>
/// <item>Every meta load restarts timing, including a reload of the same meta, and cancels a pending fallback.</item>
/// <item>A Timeout is emitted once; ticks then return None until <see cref="OnChainStateResult"/> is called.</item>
/// <item>After a RunFallback, timing restarts from the fallback time for the current meta, so the timeout can fire
///       again later if the fallback gets stuck too.</item>
/// <item>When no meta load has been seen the watch still times out, with a null meta name.  The caller should
///       report no chain state, which leads to the fallback.</item>
/// </list>
/// </para>
/// </summary>
public class MetaChainWatchState
{
    public static readonly TimeSpan NextMetaWait = TimeSpan.FromMinutes(1);

    private DateTime _timingStart;
    private DateTime _fallbackDeadline;
    private string _pendingChainState;

    public bool IsOn { get; private set; }
    public double MainMinutes { get; private set; }
    public MetaChainWatchPhase Phase { get; private set; } = MetaChainWatchPhase.Off;
    public string CurrentMeta { get; private set; }
    public DateTime? LoadedAt { get; private set; }

    public void OnMetaLoaded(string metaName, DateTime now)
    {
        CurrentMeta = MetaChainWatchRules.NormalizeName(metaName);
        LoadedAt = now;

        if (IsOn)
            StartTiming(now);
    }

    public TurnOnResult TurnOn(double minutes, DateTime now)
    {
        if (IsOn)
        {
            var previous = MainMinutes;
            MainMinutes = minutes;
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
        return new TurnOffResult { WasOn = wasOn };
    }

    public MetaChainWatchAction Tick(DateTime now, MergedMetaChainWatchConfig merged)
    {
        switch (Phase)
        {
            case MetaChainWatchPhase.Timing:
                var limit = MetaChainWatchRules.EffectiveLimit(merged, CurrentMeta, MainMinutes);
                var elapsed = now - _timingStart;
                if (elapsed < limit.Limit)
                    return MetaChainWatchAction.None;

                Phase = MetaChainWatchPhase.ResolvingChainState;
                return new MetaChainWatchAction
                {
                    Kind = MetaChainWatchActionKind.Timeout,
                    MetaName = CurrentMeta,
                    Elapsed = elapsed,
                    Limit = limit.Limit,
                    IsOverride = limit.IsOverride,
                    ConfiguredChainState = MetaChainWatchRules.FindTimeout(merged, CurrentMeta)?.ChainState,
                };

            case MetaChainWatchPhase.AwaitingNextMeta:
                if (now < _fallbackDeadline)
                    return MetaChainWatchAction.None;
                return Fallback(now, merged, MetaChainWatchAction.ReasonNoNextMeta, _pendingChainState);

            default:
                return MetaChainWatchAction.None;
        }
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
        };
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
        Phase = MetaChainWatchPhase.Timing;
    }
}
