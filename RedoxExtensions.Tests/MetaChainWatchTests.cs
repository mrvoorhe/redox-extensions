using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using RedoxExtensions.MetaChainWatch;
using RedoxExtensions.Tests.TestUtilities;

namespace RedoxExtensions.Tests;

[TestFixture]
public class MetaChainWatchTests : TemporaryDirectoryPerTestTestSuite
{
    private static readonly DateTime T0 = new(2026, 11, 12, 6, 0, 0);

    private static MergedMetaChainWatchConfig Config(params (string name, double minutes, string chainState)[] timeouts)
    {
        var global = new MetaChainWatchFileData { FallbackCommand = "/re sm aFarm" };
        foreach (var t in timeouts)
            global.Timeouts[t.name] = new MetaTimeoutEntry { Minutes = t.minutes, ChainState = t.chainState };
        return MetaChainWatchRules.Merge(global, null);
    }

    // --- NormalizeName ---

    [TestCase("Daily", "Daily")]
    [TestCase("  Daily  ", "Daily")]
    [TestCase("Daily.met", "Daily")]
    [TestCase("Daily.MET", "Daily")]
    [TestCase(" Attributes4.0.met ", "Attributes4.0")]
    [TestCase("Attributes4.0", "Attributes4.0")]
    [TestCase(null, "")]
    public void NormalizeName(string input, string expected)
    {
        Assert.That(MetaChainWatchRules.NormalizeName(input), Is.EqualTo(expected));
    }

    // --- Paths ---

    [Test]
    public void FilePaths()
    {
        Assert.That(MetaChainWatchRules.GlobalFilePath(@"C:\re"), Is.EqualTo(Path.Combine(@"C:\re", "MetaChainWatch", "Global.json")));
        Assert.That(MetaChainWatchRules.CharacterFilePath(@"C:\re", "Bob"), Is.EqualTo(Path.Combine(@"C:\re", "MetaChainWatch", "Bob.json")));
    }

    // --- Merge ---

    [Test]
    public void MergeWithNoFilesUsesDefaults()
    {
        var merged = MetaChainWatchRules.Merge(null, null);
        Assert.That(merged.FallbackCommand, Is.EqualTo("/re sm aFarm"));
        Assert.That(merged.ChainStates, Is.EqualTo(new[] { "** Chain **", "* Chain *", "TT-chain", "Chain" }));
        Assert.That(merged.Timeouts, Is.Empty);
    }

    [Test]
    public void MergeCharacterOverridesGlobalOverridesDefaults()
    {
        var global = new MetaChainWatchFileData
        {
            FallbackCommand = "/re sm gFarm",
            ChainStates = new List<string> { "G" },
        };
        var character = new MetaChainWatchFileData
        {
            FallbackCommand = "/vt meta load zzMCW_End",
            ChainStates = new List<string> { "C1", "C2" },
        };

        var both = MetaChainWatchRules.Merge(global, character);
        Assert.That(both.FallbackCommand, Is.EqualTo("/vt meta load zzMCW_End"));
        Assert.That(both.ChainStates, Is.EqualTo(new[] { "C1", "C2" }));

        var globalOnly = MetaChainWatchRules.Merge(global, new MetaChainWatchFileData { ChainStates = new List<string>() });
        Assert.That(globalOnly.FallbackCommand, Is.EqualTo("/re sm gFarm"));
        Assert.That(globalOnly.ChainStates, Is.EqualTo(new[] { "G" }), "an empty character list doesn't count");

        var blank = MetaChainWatchRules.Merge(new MetaChainWatchFileData { FallbackCommand = "  " }, null);
        Assert.That(blank.FallbackCommand, Is.EqualTo("/re sm aFarm"));
    }

    [Test]
    public void MergeTimeoutsCharacterReplacesGlobalCaseInsensitively()
    {
        var global = new MetaChainWatchFileData();
        global.Timeouts["Attributes4.0"] = new MetaTimeoutEntry { Minutes = 10 };
        global.Timeouts["aKTTouTou"] = new MetaTimeoutEntry { Minutes = 30, ChainState = "TT-chain" };

        var character = new MetaChainWatchFileData();
        character.Timeouts["akttoutou.met"] = new MetaTimeoutEntry { Minutes = 20 };
        character.Timeouts["Howard"] = new MetaTimeoutEntry { Minutes = 10 };

        var merged = MetaChainWatchRules.Merge(global, character);

        Assert.That(merged.Timeouts.Count, Is.EqualTo(3));

        var attributes = merged.Timeouts["ATTRIBUTES4.0"];
        Assert.That(attributes.Minutes, Is.EqualTo(10));
        Assert.That(attributes.Source, Is.EqualTo("global"));

        var touTou = MetaChainWatchRules.FindTimeout(merged, "aKTTouTou.met");
        Assert.That(touTou.Minutes, Is.EqualTo(20));
        Assert.That(touTou.ChainState, Is.Null, "the character entry replaces the whole global entry");
        Assert.That(touTou.Source, Is.EqualTo("character"));

        Assert.That(merged.Timeouts["howard"].Source, Is.EqualTo("character"));
    }

    // --- EffectiveLimit ---

    [Test]
    public void EffectiveLimitUsesShorterOverride()
    {
        var limit = MetaChainWatchRules.EffectiveLimit(Config(("Howard", 10, null)), "howard", 60);
        Assert.That(limit.Limit, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(limit.IsOverride, Is.True);
    }

    [Test]
    public void EffectiveLimitIgnoresOverrideNotShorterThanMain()
    {
        var merged = Config(("Kinoko", 90, null), ("Same", 60, null), ("Zero", 0, null));
        foreach (var name in new[] { "Kinoko", "Same", "Zero", "NoOverride" })
        {
            var limit = MetaChainWatchRules.EffectiveLimit(merged, name, 60);
            Assert.That(limit.Limit, Is.EqualTo(TimeSpan.FromMinutes(60)), name);
            Assert.That(limit.IsOverride, Is.False, name);
        }
    }

    [Test]
    public void EffectiveLimitAllowsDecimalMinutes()
    {
        var limit = MetaChainWatchRules.EffectiveLimit(Config(("zzMCW_Stall", 0.5, null)), "zzMCW_Stall", 2);
        Assert.That(limit.Limit, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(limit.IsOverride, Is.True);
    }

    // --- FindChainState ---

    [Test]
    public void FindChainStateReturnsFirstCandidateInCandidateOrder()
    {
        var lines = new[] { "Default\r", "Chain\r", "* Chain *\r", "other" };
        Assert.That(MetaChainWatchRules.FindChainState(lines, MetaChainWatchRules.DefaultChainStates), Is.EqualTo("* Chain *"));
    }

    [Test]
    public void FindChainStateNeedsAnExactWholeLine()
    {
        var lines = new[] { " Chain", "chain", "Chain ", "TT-chain2", "x** Chain **" };
        Assert.That(MetaChainWatchRules.FindChainState(lines, MetaChainWatchRules.DefaultChainStates), Is.Null);
        Assert.That(MetaChainWatchRules.FindChainState(new[] { "TT-chain" }, MetaChainWatchRules.DefaultChainStates), Is.EqualTo("TT-chain"));
    }

    // --- State machine ---

    [Test]
    public void OffNeverActsButTracksLoads()
    {
        var state = new MetaChainWatchState();
        state.OnMetaLoaded("Daily.met", T0);

        Assert.That(state.Tick(T0.AddHours(5), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        var status = state.GetStatus(T0.AddMinutes(3));
        Assert.That(status.IsOn, Is.False);
        Assert.That(status.Phase, Is.EqualTo(MetaChainWatchPhase.Off));
        Assert.That(status.CurrentMeta, Is.EqualTo("Daily"));
        Assert.That(status.SinceLoad, Is.EqualTo(TimeSpan.FromMinutes(3)));
        Assert.That(status.Elapsed, Is.Null);
    }

    [Test]
    public void TimingStartsWhenTurnedOnForAMetaLoadedEarlier()
    {
        var state = new MetaChainWatchState();
        state.OnMetaLoaded("Daily", T0);
        var on = state.TurnOn(60, T0.AddMinutes(10));
        Assert.That(on.Started, Is.True);

        Assert.That(state.Tick(T0.AddMinutes(69), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        Assert.That(state.GetStatus(T0.AddMinutes(69)).Elapsed, Is.EqualTo(TimeSpan.FromMinutes(59)));

        var action = state.Tick(T0.AddMinutes(70), Config());
        Assert.That(action.Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        Assert.That(action.MetaName, Is.EqualTo("Daily"));
        Assert.That(action.Elapsed, Is.EqualTo(TimeSpan.FromMinutes(60)));
        Assert.That(action.Limit, Is.EqualTo(TimeSpan.FromMinutes(60)));
        Assert.That(action.IsOverride, Is.False);
    }

    [Test]
    public void TimeoutIsNotRepeatedOnConsecutiveTicks()
    {
        var state = new MetaChainWatchState();
        state.TurnOn(60, T0);
        state.OnMetaLoaded("Howard", T0);
        var config = Config(("Howard", 10, "Chain"));

        var first = state.Tick(T0.AddMinutes(10), config);
        Assert.That(first.Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        Assert.That(first.IsOverride, Is.True);
        Assert.That(first.ConfiguredChainState, Is.EqualTo("Chain"));

        Assert.That(state.Tick(T0.AddMinutes(10).AddSeconds(5), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        Assert.That(state.Tick(T0.AddMinutes(30), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        Assert.That(state.Phase, Is.EqualTo(MetaChainWatchPhase.ResolvingChainState));
    }

    [Test]
    public void ChainStateFoundThenNewMetaCancelsFallback()
    {
        var state = new MetaChainWatchState();
        var config = Config(("zzMCW_Stall", 0.5, null));
        state.TurnOn(2, T0);
        state.OnMetaLoaded("zzMCW_Stall", T0);

        var timeout = state.Tick(T0.AddSeconds(30), config);
        Assert.That(timeout.Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        Assert.That(timeout.ConfiguredChainState, Is.Null);

        var result = state.OnChainStateResult(true, "** Chain **", T0.AddSeconds(31), config);
        Assert.That(result.Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        Assert.That(state.Phase, Is.EqualTo(MetaChainWatchPhase.AwaitingNextMeta));
        Assert.That(state.GetStatus(T0.AddSeconds(40)).FallbackDeadline, Is.EqualTo(T0.AddSeconds(91)));

        state.OnMetaLoaded("zzMCW_BrokenChain", T0.AddSeconds(40));
        Assert.That(state.Phase, Is.EqualTo(MetaChainWatchPhase.Timing));
        Assert.That(state.Tick(T0.AddSeconds(95), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None), "fallback cancelled");

        // The new meta is timed from its own load, against the 2 minute main timeout.
        Assert.That(state.Tick(T0.AddSeconds(159), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        var next = state.Tick(T0.AddSeconds(160), config);
        Assert.That(next.Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        Assert.That(next.MetaName, Is.EqualTo("zzMCW_BrokenChain"));
    }

    [Test]
    public void ChainStateFoundButNoNewMetaRunsFallbackAfterOneMinute()
    {
        var state = new MetaChainWatchState();
        var config = Config();
        state.TurnOn(1, T0);
        state.OnMetaLoaded("Stuck", T0);

        Assert.That(state.Tick(T0.AddMinutes(1), config).Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        state.OnChainStateResult(true, "Chain", T0.AddMinutes(1), config);

        Assert.That(state.Tick(T0.AddMinutes(2).AddSeconds(-1), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        var fallback = state.Tick(T0.AddMinutes(2), config);
        Assert.That(fallback.Kind, Is.EqualTo(MetaChainWatchActionKind.RunFallback));
        Assert.That(fallback.Command, Is.EqualTo("/re sm aFarm"));
        Assert.That(fallback.Reason, Is.EqualTo(MetaChainWatchAction.ReasonNoNextMeta));
        Assert.That(fallback.ChainState, Is.EqualTo("Chain"));
        Assert.That(fallback.MetaName, Is.EqualTo("Stuck"));

        // Timing restarts from the fallback, so nothing fires on the next ticks.
        Assert.That(state.Phase, Is.EqualTo(MetaChainWatchPhase.Timing));
        Assert.That(state.Tick(T0.AddMinutes(2).AddSeconds(5), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        Assert.That(state.Tick(T0.AddMinutes(3), config).Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
    }

    [Test]
    public void ChainStateNotFoundRunsFallbackRightAway()
    {
        var state = new MetaChainWatchState();
        var config = MetaChainWatchRules.Merge(null, new MetaChainWatchFileData { FallbackCommand = "/vt meta load zzMCW_End" });
        state.OnMetaLoaded("zzMCW_NoChain", T0);
        state.TurnOn(2, T0);

        Assert.That(state.Tick(T0.AddMinutes(2), config).Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        var fallback = state.OnChainStateResult(false, null, T0.AddMinutes(2), config);
        Assert.That(fallback.Kind, Is.EqualTo(MetaChainWatchActionKind.RunFallback));
        Assert.That(fallback.Command, Is.EqualTo("/vt meta load zzMCW_End"));
        Assert.That(fallback.Reason, Is.EqualTo(MetaChainWatchAction.ReasonNoChainState));
        Assert.That(state.Tick(T0.AddMinutes(2).AddSeconds(5), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
    }

    [Test]
    public void ReloadOfSameMetaRestartsTiming()
    {
        var state = new MetaChainWatchState();
        state.TurnOn(10, T0);
        state.OnMetaLoaded("aFarm", T0);
        state.OnMetaLoaded("aFarm.met", T0.AddMinutes(8));
        Assert.That(state.Tick(T0.AddMinutes(12), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        Assert.That(state.Tick(T0.AddMinutes(18), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
    }

    [Test]
    public void ChainStateResultIsIgnoredWithoutAnOutstandingTimeout()
    {
        var state = new MetaChainWatchState();
        state.TurnOn(1, T0);
        Assert.That(state.OnChainStateResult(false, null, T0, Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.None));

        Assert.That(state.Tick(T0.AddMinutes(1), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        state.OnMetaLoaded("Next", T0.AddMinutes(1));
        Assert.That(state.OnChainStateResult(false, null, T0.AddMinutes(1), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
    }

    [Test]
    public void TurnOffWhenAlreadyOff()
    {
        var state = new MetaChainWatchState();
        Assert.That(state.TurnOff().WasOn, Is.False);

        state.TurnOn(60, T0);
        Assert.That(state.TurnOff().WasOn, Is.True);
        Assert.That(state.TurnOff().WasOn, Is.False);
        Assert.That(state.Tick(T0.AddHours(2), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
    }

    [Test]
    public void TurnOffCancelsPendingFallback()
    {
        var state = new MetaChainWatchState();
        state.TurnOn(1, T0);
        state.Tick(T0.AddMinutes(1), Config());
        state.OnChainStateResult(true, "Chain", T0.AddMinutes(1), Config());
        state.TurnOff();
        Assert.That(state.Tick(T0.AddMinutes(5), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
    }

    [Test]
    public void TurnOnWhenAlreadyOnChangesMinutesOnly()
    {
        var state = new MetaChainWatchState();
        state.OnMetaLoaded("Daily", T0);
        state.TurnOn(60, T0);

        var again = state.TurnOn(30, T0.AddMinutes(20));
        Assert.That(again.Started, Is.False);
        Assert.That(again.PreviousMinutes, Is.EqualTo(60));
        Assert.That(state.MainMinutes, Is.EqualTo(30));

        // Timing was not restarted: the meta has run 30 minutes since the watch was first turned on.
        Assert.That(state.Tick(T0.AddMinutes(30), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
    }

    // --- Snapshot and resume ---

    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(4);

    [Test]
    public void SnapshotIsNullWhenOff()
    {
        var state = new MetaChainWatchState();
        state.OnMetaLoaded("Daily", T0);
        Assert.That(state.GetSnapshot(T0), Is.Null);

        state.TurnOn(60, T0);
        state.TurnOff();
        Assert.That(state.GetSnapshot(T0.AddMinutes(1)), Is.Null);
    }

    [Test]
    public void SnapshotHoldsTimingAndMeta()
    {
        var state = new MetaChainWatchState();
        state.OnMetaLoaded("aKTHoshino", T0);
        state.TurnOn(60, T0.AddMinutes(2));

        var snapshot = state.GetSnapshot(T0.AddMinutes(12));
        Assert.That(snapshot.IsOn, Is.True);
        Assert.That(snapshot.MainMinutes, Is.EqualTo(60));
        Assert.That(snapshot.CurrentMeta, Is.EqualTo("aKTHoshino"));
        Assert.That(snapshot.SinceLoadMinutes, Is.EqualTo(12).Within(1e-9));
        Assert.That(snapshot.ElapsedMinutes, Is.EqualTo(10).Within(1e-9));
        Assert.That(snapshot.SavedAt, Is.EqualTo(T0.AddMinutes(12)));
    }

    [Test]
    public void RevisionChangesOnEverythingASnapshotHolds()
    {
        var state = new MetaChainWatchState();
        var revisions = new List<int> { state.Revision };
        state.OnMetaLoaded("Daily", T0);
        revisions.Add(state.Revision);
        state.TurnOn(60, T0);
        revisions.Add(state.Revision);
        state.TurnOn(30, T0);
        revisions.Add(state.Revision);
        state.TurnOff();
        revisions.Add(state.Revision);

        Assert.That(revisions, Is.Unique);
        Assert.That(revisions, Is.Ordered);
    }

    [Test]
    public void ResumeCarriesRunTimeButNotDowntime()
    {
        var before = new MetaChainWatchState();
        before.OnMetaLoaded("aKTHoshino", T0);
        before.TurnOn(60, T0);
        var saved = before.GetSnapshot(T0.AddMinutes(20)); // crash at 20 min

        var after = new MetaChainWatchState();
        var relog = T0.AddMinutes(30); // 10 min down
        var result = after.Resume(saved, relog, MaxAge);

        Assert.That(result.Resumed, Is.True);
        Assert.That(result.Age, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(after.IsOn, Is.True);
        Assert.That(after.MainMinutes, Is.EqualTo(60));
        Assert.That(after.CurrentMeta, Is.EqualTo("aKTHoshino"));
        Assert.That(after.Phase, Is.EqualTo(MetaChainWatchPhase.Timing));
        Assert.That(after.GetStatus(relog).Elapsed, Is.EqualTo(TimeSpan.FromMinutes(20)));
        Assert.That(after.GetStatus(relog).SinceLoad, Is.EqualTo(TimeSpan.FromMinutes(20)));

        var config = Config();
        Assert.That(after.Tick(relog.AddMinutes(40).AddSeconds(-1), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        var timeout = after.Tick(relog.AddMinutes(40), config);
        Assert.That(timeout.Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        Assert.That(timeout.MetaName, Is.EqualTo("aKTHoshino"));
    }

    [Test]
    public void ResumePastTheLimitWaitsForTheGrace()
    {
        var before = new MetaChainWatchState();
        before.OnMetaLoaded("Attributes4.0", T0);
        before.TurnOn(60, T0);
        var saved = before.GetSnapshot(T0.AddMinutes(15));
        var config = Config(("Attributes4.0", 10, null));

        var after = new MetaChainWatchState();
        var relog = T0.AddMinutes(20);
        after.Resume(saved, relog, MaxAge);

        Assert.That(after.Tick(relog.AddSeconds(5), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        Assert.That(after.Tick(relog + MetaChainWatchState.ResumeGrace - TimeSpan.FromSeconds(1), config).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        var timeout = after.Tick(relog + MetaChainWatchState.ResumeGrace, config);
        Assert.That(timeout.Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        Assert.That(timeout.IsOverride, Is.True);
    }

    [Test]
    public void MetaLoadAfterResumeEndsTheGraceAndRestartsTiming()
    {
        var before = new MetaChainWatchState();
        before.OnMetaLoaded("Stuck", T0);
        before.TurnOn(1, T0);
        var saved = before.GetSnapshot(T0.AddMinutes(5));

        var after = new MetaChainWatchState();
        after.Resume(saved, T0.AddMinutes(6), MaxAge);
        after.OnMetaLoaded("Next", T0.AddMinutes(6).AddSeconds(10));

        Assert.That(after.Tick(T0.AddMinutes(7), Config()).Kind, Is.EqualTo(MetaChainWatchActionKind.None));
        var timeout = after.Tick(T0.AddMinutes(7).AddSeconds(10), Config());
        Assert.That(timeout.Kind, Is.EqualTo(MetaChainWatchActionKind.Timeout));
        Assert.That(timeout.MetaName, Is.EqualTo("Next"));
    }

    [Test]
    public void ResumeKeepsAMetaLoadedBeforeIt()
    {
        var saved = new MetaChainWatchSnapshot { IsOn = true, MainMinutes = 60, CurrentMeta = "Old", ElapsedMinutes = 50, SinceLoadMinutes = 50, SavedAt = T0 };

        var state = new MetaChainWatchState();
        state.OnMetaLoaded("New", T0.AddMinutes(1));
        Assert.That(state.Resume(saved, T0.AddMinutes(2), MaxAge).Resumed, Is.True);

        Assert.That(state.CurrentMeta, Is.EqualTo("New"));
        Assert.That(state.GetStatus(T0.AddMinutes(2)).Elapsed, Is.EqualTo(TimeSpan.FromMinutes(1)));
    }

    [Test]
    public void ResumeWithNoMetaSeen()
    {
        var saved = new MetaChainWatchSnapshot { IsOn = true, MainMinutes = 60, ElapsedMinutes = 5, SavedAt = T0 };

        var state = new MetaChainWatchState();
        Assert.That(state.Resume(saved, T0.AddMinutes(1), MaxAge).Resumed, Is.True);
        Assert.That(state.CurrentMeta, Is.Null);
        Assert.That(state.LoadedAt, Is.Null);
        Assert.That(state.GetStatus(T0.AddMinutes(1)).Elapsed, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public void ResumeIgnoresAStaleSave()
    {
        var saved = new MetaChainWatchSnapshot { IsOn = true, MainMinutes = 60, CurrentMeta = "Daily", ElapsedMinutes = 1, SavedAt = T0 };

        var state = new MetaChainWatchState();
        var result = state.Resume(saved, T0 + MaxAge + TimeSpan.FromSeconds(1), MaxAge);
        Assert.That(result.Resumed, Is.False);
        Assert.That(result.Reason, Is.EqualTo(ResumeResult.ReasonStale));
        Assert.That(state.IsOn, Is.False);
        Assert.That(state.Phase, Is.EqualTo(MetaChainWatchPhase.Off));

        Assert.That(new MetaChainWatchState().Resume(saved, T0 + MaxAge, MaxAge).Resumed, Is.True, "exactly max age still resumes");
    }

    [Test]
    public void ResumeIgnoresNoSaveOrOff()
    {
        Assert.That(new MetaChainWatchState().Resume(null, T0, MaxAge).Reason, Is.EqualTo(ResumeResult.ReasonNoSave));
        Assert.That(new MetaChainWatchState().Resume(new MetaChainWatchSnapshot { IsOn = false, MainMinutes = 60, SavedAt = T0 }, T0, MaxAge).Reason,
            Is.EqualTo(ResumeResult.ReasonNoSave));
    }

    [TestCase(0, 1, 0)]
    [TestCase(-5, 1, 0)]
    [TestCase(double.NaN, 1, 0)]
    [TestCase(60, -1, 0)]
    [TestCase(60, double.NaN, 0)]
    [TestCase(60, 1, -10)] // saved 10 min in the future
    public void ResumeIgnoresAnInvalidSave(double mainMinutes, double elapsedMinutes, double savedMinutesAgo)
    {
        var saved = new MetaChainWatchSnapshot { IsOn = true, MainMinutes = mainMinutes, ElapsedMinutes = elapsedMinutes, SavedAt = T0.AddMinutes(-savedMinutesAgo) };

        var state = new MetaChainWatchState();
        var result = state.Resume(saved, T0, MaxAge);
        Assert.That(result.Resumed, Is.False);
        Assert.That(result.Reason, Is.EqualTo(ResumeResult.ReasonInvalid));
        Assert.That(state.IsOn, Is.False);
    }

    [Test]
    public void ResumeDoesNothingWhenAlreadyOn()
    {
        var saved = new MetaChainWatchSnapshot { IsOn = true, MainMinutes = 30, ElapsedMinutes = 25, SavedAt = T0 };

        var state = new MetaChainWatchState();
        state.TurnOn(60, T0);
        var result = state.Resume(saved, T0.AddMinutes(1), MaxAge);
        Assert.That(result.Resumed, Is.False);
        Assert.That(result.Reason, Is.EqualTo(ResumeResult.ReasonAlreadyOn));
        Assert.That(state.MainMinutes, Is.EqualTo(60));
        Assert.That(state.GetStatus(T0.AddMinutes(1)).Elapsed, Is.EqualTo(TimeSpan.FromMinutes(1)));
    }

    // --- File ---

    [Test]
    public void StateFileIsInTheStateFolder()
    {
        var path = MetaChainWatchRules.StateFilePath(TestTemporaryDirectory, "Bob");
        Assert.That(path, Is.EqualTo(Path.Combine(TestTemporaryDirectory, "MetaChainWatch", "State", "Bob.json")));
    }

    [Test]
    public void StateFileRoundTripsAndDeletes()
    {
        var path = MetaChainWatchRules.StateFilePath(TestTemporaryDirectory, "Bob");
        Assert.That(MetaChainWatchFile.LoadState(path), Is.Null);

        var snapshot = new MetaChainWatchSnapshot
        {
            IsOn = true,
            MainMinutes = 60,
            CurrentMeta = "Attributes4.0",
            SinceLoadMinutes = 3.25,
            ElapsedMinutes = 2.5,
            SavedAt = T0,
        };
        MetaChainWatchFile.SaveState(path, snapshot);
        MetaChainWatchFile.SaveState(path, snapshot); // the second save replaces the file
        Assert.That(File.Exists(path + ".tmp"), Is.False);
        Assert.That(File.ReadAllText(path), Does.Contain("\"currentMeta\": \"Attributes4.0\""));

        var loaded = MetaChainWatchFile.LoadState(path);
        Assert.That(loaded.IsOn, Is.True);
        Assert.That(loaded.MainMinutes, Is.EqualTo(60));
        Assert.That(loaded.CurrentMeta, Is.EqualTo("Attributes4.0"));
        Assert.That(loaded.SinceLoadMinutes, Is.EqualTo(3.25));
        Assert.That(loaded.ElapsedMinutes, Is.EqualTo(2.5));
        Assert.That(loaded.SavedAt, Is.EqualTo(T0));

        MetaChainWatchFile.DeleteState(path);
        Assert.That(File.Exists(path), Is.False);
        MetaChainWatchFile.DeleteState(path); // already gone
    }


    [Test]
    public void LoadMissingFileReturnsNull()
    {
        Assert.That(MetaChainWatchFile.Load(Path.Combine(TestTemporaryDirectory, "nope.json")), Is.Null);
    }

    [Test]
    public void FileRoundTripsWithCamelCaseAndUnchangedMetaNames()
    {
        var path = MetaChainWatchRules.GlobalFilePath(TestTemporaryDirectory);
        var data = new MetaChainWatchFileData
        {
            FallbackCommand = "/re sm aFarm",
            ChainStates = new List<string>(MetaChainWatchRules.DefaultChainStates),
        };
        data.Timeouts["Attributes4.0"] = new MetaTimeoutEntry { Minutes = 10 };
        data.Timeouts["aKTTouTou"] = new MetaTimeoutEntry { Minutes = 30.5, ChainState = "TT-chain" };

        MetaChainWatchFile.Save(path, data);
        MetaChainWatchFile.Save(path, data); // the second save replaces the file
        Assert.That(File.Exists(path + ".tmp"), Is.False);

        var text = File.ReadAllText(path);
        Assert.That(text, Does.Contain("\"fallbackCommand\""));
        Assert.That(text, Does.Contain("\"chainStates\""));
        Assert.That(text, Does.Contain("\"timeouts\""));
        Assert.That(text, Does.Contain("\"Attributes4.0\""));
        Assert.That(text, Does.Contain("\"chainState\": \"TT-chain\""));
        Assert.That(text, Does.Contain("\"minutes\": 10"));

        var loaded = MetaChainWatchFile.Load(path);
        Assert.That(loaded.FallbackCommand, Is.EqualTo("/re sm aFarm"));
        Assert.That(loaded.ChainStates, Is.EqualTo(MetaChainWatchRules.DefaultChainStates));
        Assert.That(loaded.Timeouts.Keys, Is.EquivalentTo(new[] { "Attributes4.0", "aKTTouTou" }));
        Assert.That(loaded.Timeouts["aKTTouTou"].Minutes, Is.EqualTo(30.5));
        Assert.That(loaded.Timeouts["aKTTouTou"].ChainState, Is.EqualTo("TT-chain"));
        Assert.That(loaded.Timeouts["Attributes4.0"].ChainState, Is.Null);
    }

    [Test]
    public void LoadPartialCharacterFile()
    {
        var path = MetaChainWatchRules.CharacterFilePath(TestTemporaryDirectory, "Bob");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, "{ \"fallbackCommand\": \"/vt meta load zzMCW_End\" }");

        var loaded = MetaChainWatchFile.Load(path);
        Assert.That(loaded.FallbackCommand, Is.EqualTo("/vt meta load zzMCW_End"));
        Assert.That(loaded.ChainStates, Is.Null);
        Assert.That(loaded.Timeouts, Is.Empty);
    }
}
