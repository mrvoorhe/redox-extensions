# Request

We are trying to make metas more robust to various failure cases and quirks.  This is proving challenging.  We will continue but we need a more deterministic way to ensure that our daily chain of metas gets to the end and returns the character to farming.  If we get stuck in the chain somewhere and don't return the farming for hours that can greatly reduce the value of running the chain vs just staying at the farming spot.

I have some ideas for a scheme to help ensure that even if a character gets stuck we do eventually get moving again.  Putting timeouts into every state in every meta is going to be tedious and error prone.  Let's leverage Redox Extensions to add an umbrella timeout in case meta timeouts fail to unstick us.

Part 1 -

Let's add `/re metachainwatch on/off <timeout>`.  For example,

`/re metachainwatch on 15` - Enables the meta chain watcher with a max runtime of an indiviual meta of 15 minutes.
`/re metachainwatch off` - Turns off the meta chain watcher.  Make sure the already off scenario is handled.

Part 2 -

Apply these new commands to our chain..

"E:\ac\Games\VirindiPlugins\VirindiTank\Daily.met" - Should run `/re metachainwatch on 30` when it starts. Fact check that 30 minutes for me based on our MetaAnalysis data.  Is that safely over the longest running (Non-stuck) meta run?  Proposed a safer timeout if you feel one is warranted.

"E:\ac\Games\VirindiPlugins\VirindiTank\aFarm.met" - Should run `/re metachainwatch off`.


Part 3 -

Per meta timeout overrides.  This should be a table.

```
meta name -> custom timeout
```

This table needs to be persisted somewhere.  Follow a scheme similar to what we did with Scheduled Commands maybe?

I want the per meta timeout to be implemented as a second timeout that can fire before the main timeout.  That way we can trust the overall meta timeout is always there to save us.

Part 4 -

We have data on the metas we've been running in MetaAnalysis.  Can you seed our per meta override table?  Let's exclude the aKT metas.  We have those configured with their own timeouts that we may tweak in the future.  And they tend to be the longest running so let's lean on the default timeout for these.

# Design

## Overview

The plugin times each VT meta from the moment it loads.  When a meta runs past its limit, the watcher jumps the meta
to its chain state so the chain moves on to the next meta.  If no other meta loads within a minute, or the meta has
no chain state, it runs a fallback command (by default `/re sm aFarm`) to send the character back to farming.

* `Listeners.Monitors/MetaChainWatcher.cs` is the plugin side: a monitor registered in `Core/MonitorManager.cs`,
  modeled on `CommandScheduler`.
* `MetaChainWatch/MetaChainWatchConfig.cs` has the file model and the rules (merge, effective limit, chain state
  scan).  rectl shares it as a linked source, like `Scheduling/ScheduledCommand.cs`.
* `MetaChainWatch/MetaChainWatchState.cs` is the state machine.  It has no Decal or clock dependency, so it is unit
  tested (`RedoxExtensions.Tests/MetaChainWatchTests.cs`).

## Detecting meta loads

* `Commands/CommandListener.cs` looks for `/vt meta load <name>` (any case or spacing) in `CommandLineText`.  It never
  eats the text, so VT still sees it.  The parser is `MetaChainWatch/MetaLoadCommand.cs`.
* That covers typed loads, `/re sm` (`VTActions.LoadMetaProfile` goes back through the plugin intercept) and,
  VT `ChatCommand` actions and UB `chatbox[]` expressions (both checked in game).
* Names are normalized: trimmed, a trailing `.met` removed, and matched case-insensitively.
* Loads are tracked even while the watch is off.  When `Daily` runs `/re metachainwatch on 60`, the watcher already
  knows the current meta is Daily.

## Timing

* **Two timers.**  Each meta's limit is `min(override, main)`.  An override only counts when it is shorter than the
  main timeout, so the main timeout is always there as the backstop.
* Turning on starts timing the current meta from that moment.  Turning on again while on only changes the main
  timeout.
* Every meta load restarts timing (a reload of the same meta too) and cancels a pending fallback.
* A 5 s `System.Threading.Timer` checks the limits.  File IO and the `.met` scan run on that timer thread, and
  anything that touches the game is queued to the game thread.  Ticks are skipped in portal space.

## When a meta times out

1. Chat: `[RE] MetaChainWatch : <meta> timed out after <n> min (override|default)`.
2. Find the chain state.
   * The entry's `chainState` from the table, if it has one.
   * Otherwise scan `<VTankDirectory>\<meta>.met` for a line that is exactly one of the candidate names, in order:
     `** Chain **`, `* Chain *`, `TT-chain`, `Chain`.  These are the names the current metas use.
3. **Found:** chat `[RE] MetaChainWatch : <meta> -> state '<state>'`, then `/vt setmetastate <state>`, `EnableMeta`
   on and `/vt start` (existing `VTActions`).  Wait **1 minute** for a different meta to load.
4. **No state, or no new meta within 1 minute:** run the fallback with `ACUtilities.ProcessArbitraryCommand`.
   * `[RE] MetaChainWatch : <meta> no chain state, running fallback : /re sm aFarm`
   * `[RE] MetaChainWatch : <meta> no next meta after state '<state>', running fallback : /re sm aFarm`
5. Timing restarts after the fallback.  aFarm turns the watch off; if the fallback gets stuck instead, the timeout
   fires again later.

If no meta load has been seen, the meta shows as `(no meta)` and the timeout goes straight to the fallback.

## In-game commands

`/re metachainwatch` (alias `mcw`).  Both are in `/re help`.

| Command | Output |
|---|---|
| `on <minutes>` (decimals allowed) | `[RE] MetaChainWatch : on, 60 min (meta: Daily)` (`meta: none` if no load was seen) |
| `on` while on | `[RE] MetaChainWatch : already on, timeout 60 -> 30 min` |
| `on` with a bad number | `[RE] MetaChainWatch : on needs a timeout in minutes greater than 0, e.g. /re metachainwatch on 60` |
| `off` | `[RE] MetaChainWatch : off`, or `[RE] MetaChainWatch : already off` |
| `reload` | `[RE] MetaChainWatch : reloading <Global.json path> and <Char.json path>` |
| anything else | `Usage : /re metachainwatch\|mcw [on <minutes>\|off\|status\|reload]` |

`/re metachainwatch` with no argument (or `status`) shows:

```
[RE] MetaChainWatch : on, 60 min, Timing          (or: off)
  meta : aKTHoshino, 12.4 / 35 min (override)     (off: "loaded 3 min ago"; nothing seen: "none seen")
  waiting for the next meta, fallback at 07:42:10 (only after a chain state jump)
  fallback : /re sm aFarm
  chain states : '** Chain **', '* Chain *', 'TT-chain', 'Chain'
  timeouts : 21
  aKTHoshino : 35 min (global)                    (a set chain state shows as [** Chain **])
  ...
```

The phase is `Off`, `Timing`, `ResolvingChainState` or `AwaitingNextMeta`.

Other watcher lines:

* `[RE] MetaChainWatch : config loaded : 21 timeout overrides, fallback /re sm aFarm` when a config file is first
  read or changes.
* `[RE] MetaChainWatch : config file unreadable, ignoring it : <path> : <error>`.  Nothing from that file is used
  until it changes.

All of these are chat lines, so the agent bridge journals them as `plugin` events.

The on/off state is in memory only.  It doesn't survive a plugin reload or relog, on purpose: a relog breaks the
chain anyway.

## Config files

* Global: `<install>\MetaChainWatch\Global.json`
* Per character: `<install>\MetaChainWatch\<Character>.json`, same layout

`<install>` is the RedoxExtensions install directory, as for Scheduled Commands.  Every field is optional.

```json
{
  "fallbackCommand": "/re sm aFarm",
  "chainStates": ["** Chain **", "* Chain *", "TT-chain", "Chain"],
  "timeouts": {
    "Attributes4.0": { "minutes": 10 },
    "aKTTouTou": { "minutes": 30, "chainState": "TT-chain" }
  }
}
```

Merge rules:

* A character `timeouts` entry replaces the whole global entry with the same name.
* A character `fallbackCommand` or `chainStates` replaces the global one.  Global replaces the defaults.
  A blank command or an empty list counts as not set.
* `chainState` left out means the `.met` file is scanned.
* The plugin rereads the files when their write time changes (checked every 5 s), or on `/re metachainwatch reload`.
* Writes go to a `.tmp` file that is swapped in, so a reader never sees half a file.

## rectl

`rectl metatimeout` edits the files directly.  The bridge isn't needed, and the plugin picks the change up on its
next tick.  Without `-c` it edits `Global.json`; with `-c` it edits that character's file.

```
rectl metatimeout list [-c <Character>]          merged table, with each entry's source
rectl [-c <Character>] metatimeout set <meta> <minutes> [--chain-state "<state>"]
rectl [-c <Character>] metatimeout remove|rm <meta>
rectl [-c <Character>] metatimeout fallback <command>|--clear
```

* Output is JSON.  `list` gives `fallbackSource` and `chainStatesSource` (`default`, `global` or `character`) and a
  `source` per timeout.
* `-c` must be the exact character name.  `set` and `fallback` print a note if the character has no file yet and
  isn't running with the bridge on.
* `<minutes>` takes decimals (e.g. `0.5`), which is useful for testing.

## Main timeout: 60 minutes

The request proposed 30.  That is too short: KinokoPublic2.0 has run 48.5 minutes without being stuck (Rockdown
11-10; normal runs are 28.4-48.5).  An override can only shorten the limit, so the main timeout has to cover the
longest normal meta.  60 gives Kinoko about 1.24x headroom.

## Seeded timeouts

Source: the `metas` load events in `F:\ac-files\decaldev\MetaAnalysis\<date>\<Char>.analysis.json`, 2026-11-09 to
11-12 (11-12 Rockdown and Woodford analyzed for this).  A run lasts from its load to the next load.  Known stuck,
looped and crashed runs are left out.  Starting rule: longest normal run x 1.5, rounded up to the next 5 minutes,
at least 10; dispatchers 5.  Any result of 60 or more is left to the main timeout.  aKT values must also clear the
hunt limit plus turn-in overhead.

| Meta | Minutes | Longest normal run | Why |
|---|---|---|---|
| Daily | 5 | about 2 s | Dispatcher.  No chain state, so a timeout goes to the fallback. |
| zKillTasks | 5 | 1-2 s | Dispatcher. |
| zKillTasks`<Char>` (Cowhead, Rockdown, Virmar, Woodford) | 5 each | under 1 s | Dispatchers, loaded by UB `chatbox[]`. |
| zDailyQuests | 5 | about 1 s | Dispatcher. |
| zDailyQuests`<Char>` (Cowhead, Rockdown, Virmar, Woodford) | 5 each | under 1 s | Dispatchers, loaded by UB `chatbox[]`. |
| Attributes4.0 | 10 | 4.8 | Normal 4.1-4.8.  Would have saved all 4 stuck runs (2 riddle hangs of 3 h or more, a 133 min Fiun loop, a 96.5 min riddle hang). |
| aKTHoshino | 35 | 16.2 | 12 min hunt.  The rule gives 25, but a crash-and-resume run (24.3-28.7, still turns in) needs room. |
| aKTRynthid | 35 | 22.0 | 15 min hunt plus up to 4 min turn-in.  Covers a hunt limit raised to 18-20.  Its goto states can loop, so the watch is useful here. |
| aKTNeftet | 30 | 16.7 | 15 min hunt, short overhead. |
| aKTFrozen | 30 | 16.4 | 14 min hunt.  The rule gives 25; 30 leaves room to raise the limit to 18-20 (it times out on almost every run). |
| aKTTouTou | 30 | 18.6 | 16 min hunt.  Chain state `TT-chain`. |
| ThreeGeneralsPublic | 20 | 8.4 | Rockdown only, one normal sample.  Gen states wait on spawns. |
| aQD9ELerRhan | 20 | 9.4 | Kept tight because of the unresolved door problem.  The Chamber wait for Ler Rhan's spawn has no limit of its own. |
| HowardTheMad | 5 | 3.8 | Normal 3.0-3.8. |
| LaLaRing | 4 | 2.6 | Its rewards aren't important, so max normal x 1.2 rather than the 10 floor.  It has its own 3 min fight skip. |
| KinokoPublic2.0 | none | 48.5 | The rule gives 75, so the 60 main timeout covers it. |
| aFarm, zFarm`<Char>`, aFBreach, aFJamPillar | none | - | aFarm turns the watch off.  The farm metas must never be watched. |

**Stuck runs found while seeding** (not in any `summary.md`):

* Woodford 11-12, Attributes4.0: 96.5 min, stuck in 6.3 Temple Entrance Riddle (the yarrow give gave 0 items).
* Rockdown 11-12, ThreeGeneralsPublic: 60.4 min.  Died in AyanGen, and `Died -> ** Chain **` didn't fire.

## Chain meta changes

Made 2026-11-12 after the in-game test passed (originals kept as `Daily.met.bak` / `aFarm.met.bak` in the VT folder).

* `Daily.met`, Default state: `ChatCommand '/re metachainwatch on 60'` at the start of the existing `All{...}`.
* `aFarm.met`, Default state: `ChatCommand '/re metachainwatch off'` at the start of the existing `All{...}`.

## In-game test (2026-11-12, Woodford)

Throwaway `zzMCW_*` metas (`/vt echo`, `/vt meta load` and `/re metachainwatch` only) plus a `Woodford.json` with
0.5 min overrides and fallback `/vt meta load zzMCW_End`.  All of this was deleted afterwards.

| Case | Result |
|---|---|
| `on 2` from a VT ChatCommand, then the next meta loaded from a ChatCommand | Detected; timing started on the new meta |
| Override 0.5 min on a stalled meta with `** Chain **` | Timed out at 0.55 min (override), jumped to `** Chain **`, next meta loaded |
| Override 0.5 min on a meta whose `* Chain *` doesn't load anything | Jumped to `* Chain *`, fallback ran exactly 1 min later |
| No chain state, main timeout 1 min | Timed out at 1.03 min (default), fallback ran straight away |
| Load from a UB `chatbox[]` expression | Detected (status showed the new meta, loaded at the right time) |
| `off` twice | `off`, then `already off` |

## Known gaps and follow-ups

* **A jump to the chain state skips the meta's normal exit cleanup.**  Every chain state is standalone
  (`Always -> /vt meta load <next>`), so the jump works, but:
  * aKTHoshino leaves `HoshinoRun=1`.  The next day's Default takes the resume path and skips Hoshino1-start's quest
    checks.  Fix: move `clearallpvars[]` and `EnableNav False` into `** Chain **` itself.
  * aKTFrozen leaves its `FV*` pvars (lower risk: it doesn't branch on them at start).
  * KinokoPublic2.0 skips the token turn-in and the `idlepeacemode`/`opendoors` resets in TurnInToken.
  * LaLaRing skips its combat, looting and attack resets.  aFarm and the farm metas set their own options.
  * aQD9ELerRhan can leave a Head or Chest key in the pack.
* **Dispatcher metas have no chain state**, so a dispatcher timeout goes straight to the fallback and skips the rest
  of the chain.  Acceptable for a 5 min hang in something that normally takes under 2 s.
* **Watch on/off is in memory only** (see In-game commands).

The `meta-chain-watch` skill (`.claude/skills/meta-chain-watch`) says how Claude lists and changes the timeouts.
