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

The plugin times each VT meta from the moment it loads.  When a meta runs past its limit, the watcher first recalls
the character to the Halls of Introduction with the Halls of Introduction Gem (so the next meta starts somewhere
clean), then jumps the meta to its chain state so the chain moves on to the next meta.  If no other meta loads within a minute, or the meta has
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
* Every meta load restarts timing (a reload of the same meta too) and cancels a pending recall or fallback.
* A 5 s `System.Threading.Timer` checks the limits.  File IO and the `.met` scan run on that timer thread, and
  anything that touches the game is queued to the game thread.  Ticks are skipped in portal space.

## When a meta times out

1. Chat: `[RE] MetaChainWatch : <meta> timed out after <n> min (override|default)`.
2. **Recall** (on by default, see Recall below): `/vt stop`, clear the nav (as in 4), peace mode, then 2 s later the
   Halls of Introduction Gem.  Wait up to 30 s for him to arrive, try the gem once more, then carry on whatever
   happened.  The phase is `Recalling` meanwhile.
3. Find the chain state.
   * The entry's `chainState` from the table, if it has one.
   * Otherwise scan `<VTankDirectory>\<meta>.met` for a line that is exactly one of the candidate names, in order:
     `** Chain **`, `* Chain *`, `TT-chain`, `Chain`.  These are the names the current metas use.
4. **Found:** chat `[RE] MetaChainWatch : <meta> -> state '<state>'`, then `/vt stop` and clear the nav (nav off,
   then `/vt nav load zzRedoxEmptyNav`, a 0-point nav written to the VT folder when missing).  1 s later
   `/vt setmetastate <state>`, `EnableMeta` on and `/vt start` (existing `VTActions`).  Wait **1 minute** for a
   different meta to load.
5. **No state, or no new meta within 1 minute:** clear the nav as in 4, then run the fallback with
   `ACUtilities.ProcessArbitraryCommand`.
   * `[RE] MetaChainWatch : <meta> no chain state, running fallback : /re sm aFarm`
   * `[RE] MetaChainWatch : <meta> no next meta after state '<state>', running fallback : /re sm aFarm`
6. Timing restarts after the fallback.  aFarm turns the watch off; if the fallback gets stuck instead, the timeout
   fires again later.

If no meta load has been seen, the meta shows as `(no meta)` and the timeout goes to the fallback (after the recall).

## Recall

Added 2026-11-15 (see the Rockdown incident below).  On a timeout, before the chain state jump, and also before the
fallback when there is no chain state:

1. On the game thread: `/vt stop`, `VTActions.ClearNav`, and `SetCombatMode(Peace)` through Decal.  VT's own
   options (`EnableCombat`, `OpenDoors`, ...) aren't changed, so the next meta or profile gets them as they were;
   `/vt stop` is what keeps VT from fighting or picking locks meanwhile.
2. 2 s later, so the stop and the combat mode switch are done:
   * **Already at the recall spot** (see Arrival): no gem.
   * Otherwise find the gem in inventory by its **exact** name (`WorldObject.Name`, ordinal compare, no partial
     match: UB `usep` partial matching has hit the wrong item before) and use it with Decal's `UseItem`.  The
     plugin uses it itself rather than sending `/mt use`, so a missing gem is known for sure, and nothing depends
     on MagTools parsing.
   * **No gem:** log it and carry on.
3. Wait for arrival, up to **30 s** from the use.  No arrival: use the gem once more (try 2 of 2, same steps from 1).
   Still none: log it and carry on.  Either way the chain state jump (or fallback) follows as before; the watch
   never waits on the recall for more than about 1.5 minutes in all, plus time spent in portal space.

**Arrival.**  He is at the recall spot when his landcell is in the recall landblock (`0x012F` for the Halls; the gem
lands at `0x012F0126`) **and** the cell is `0x0100` or higher.  Dungeon cells start at `0x0100`; a lower cell in a
dungeon landblock (Cowhead was found at `0x012F0025` on 11-14) means he is outside the walls, where nav can't move
him, so that counts as not there and gets another teleport.  The landblock rather than the exact landcell is checked so
that any indoor Halls cell counts (our metas check the landblock too).  Why not a portal space exit alone: it says a
teleport happened, not where to (a portal or recall from something else looks the same); the landcell does.  Portal space is used
as a guard instead:

* While recalling, every tick queues a read of `Actions.Landcell` (and whether he is in portal space) on the game
  thread; the next tick checks it.  Only reads taken after the gem use count, and never one taken in portal space.
* A try never gives up while he is in portal space (ticks are skipped there anyway).  After a portal space exit the
  try waits at least 10 s more, so a read of where he landed comes in before it decides.  This is what keeps a slow
  portal (longer than 30 s) from getting a second, needless gem.
* A try only gives up on a read taken after its deadline.  If the reads stop coming (game thread stuck), it gives up
  15 s after the deadline anyway, so the watch itself can't get stuck.

**Cancelling.**  A meta load or `off` during the recall ends it.  Each try has an id; the game thread checks it right
before using the gem, so a cancelled or replaced try never uses one.  (A gem already used still teleports him.)

**Crash during the recall.**  The phase isn't saved, only the run time, and that is already past the limit.  After
the relog the watch resumes as `Timing`, waits the 1 minute resume grace, times out again and starts a fresh recall.
If the teleport went through before the crash he is in the Halls, so the gem isn't used again (`already at`); if not,
he gets the gem.  A crash after the recall (waiting for the next meta) also recalls again, which is harmless: he is
already in the Halls.

**Config** (`recall` in the config files, every field optional, merged field by field: character, then global, then
the defaults):

```json
"recall": { "enabled": true, "gem": "Halls of Introduction Gem", "landblock": "0x012F" }
```

* `enabled: false` turns it off: a timeout goes straight to the chain state jump, as before.
* `gem` is the exact inventory name.  A blank one counts as not set.
* `landblock` is where the gem goes, 1 to 4 hex digits with or without `0x`.  It must be a dungeon or indoor
  landblock (the arrival check wants a cell of `0x0100` or higher).  An invalid value counts as not set.
* rectl keeps a hand-written `recall` when it rewrites a file, and `rectl metatimeout list` shows the merged
  `recall`.  There is no rectl command to change it; edit the JSON.

Next metas are fine starting from the Halls: KinokoPublic2.0 routes from there (`RouteToShrooms` on landcell
`0x012F0126`), aFarm loads the farm meta, which travels by gem or the Town Network, and the chain metas generally
travel with gems or the Town Network.

Why the nav is cleared (2026-11-13): Virmar's Attributes4.0 timed out with its 6.4 Temple Jumps nav stuck on a
`/vt jump`.  The old handoff (`setmetastate`, `/vt start`) did load the next meta, but VT stayed stuck on the jump
route and no rule that waits on a condition fired in any meta after it.  The fallback's `/re sm aFarm` stopped VT and
turned nav off, but the farm meta's `/vt settings loadchar Farm` turned nav back on with the same route loaded.  The
route was still trying the jump 8 hours later.  Stopping VT and loading an empty nav removes the route itself.

Why the recall was added (2026-11-15, Rockdown): Rockdown's VT Farm profile has OpenDoors on, and he carried Peerless
Lockpicks.  In aQD9ELerRhan (landblock `0x0083`) VT kept trying to pick the key-only door "Ler Rhan's Chamber"; VT's
door handling outranks the nav, so the meta's key nav never ran, and he looped between UnlockDoor and Chamber.  At
07:32 the watch timed the meta out and the hand-off ran (`/vt stop`, nav off, empty nav, `setmetastate '* Chain *'`,
`/vt start`) and KinokoPublic2.0 loaded.  But he was still next to the door with OpenDoors on, so VT went straight back
to picking the lock and Kinoko's first nav (a chat node, `/mt use Halls of Introduction Gem`) never ran.  He stood
there for about 55 minutes until `/vt opt set OpenDoors false` was sent by hand.  Same shape as the Virmar jump: state
left over from the stuck meta (here his position next to the door) outlives the hand-off.  The recall moves him
somewhere clean before the next meta starts.

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

The phase is `Off`, `Timing`, `Recalling`, `ResolvingChainState` or `AwaitingNextMeta`.  While `Recalling` the
status also shows `  recalling : Halls of Introduction Gem, try 1 of 2, gives up at 07:32:40`, and it always shows
`  recall : Halls of Introduction Gem -> 0x012F` (or `  recall : off`) before the fallback line.

Other watcher lines:

* Recall lines, in the order they can come:
  * `[RE] MetaChainWatch : <meta> using Halls of Introduction Gem (try 1 of 2)`
  * `[RE] MetaChainWatch : <meta> already at 0x012F0126, not using Halls of Introduction Gem`
  * `[RE] MetaChainWatch : <meta> no 'Halls of Introduction Gem' in inventory, carrying on without the recall`
  * `[RE] MetaChainWatch : <meta> no teleport within 30 s, trying Halls of Introduction Gem again`, then
    `... using Halls of Introduction Gem (try 2 of 2)`
  * `[RE] MetaChainWatch : <meta> recalled, at 0x012F0126`
  * `[RE] MetaChainWatch : <meta> recall failed, not in 0x012F after 2 tries, carrying on`

  Then the `-> state` or fallback line as before.
* `[RE] MetaChainWatch : config loaded : 21 timeout overrides, fallback /re sm aFarm` when a config file is first
  read or changes.
* `[RE] MetaChainWatch : config file unreadable, ignoring it : <path> : <error>`.  Nothing from that file is used
  until it changes.
* `[RE] MetaChainWatch : resumed after restart, 60 min (meta: aKTHoshino, 12.4 min in, saved 3 min ago)` (see
  Surviving a crash).
* `[RE] MetaChainWatch : saved state from 300 min ago is too old, staying off`, or `saved state is invalid, staying
  off : <path>` / `saved state unreadable, staying off : <path> : <error>`.

All of these are chat lines, so the agent bridge journals them as `plugin` events.

## Surviving a crash

Added 2026-11-14.  A client crash in the middle of the chain used to turn the watch off, so a meta that got stuck after
the relog had no backstop.  The watch now saves itself and turns back on when the plugin starts.

* **State file:** `<install>\MetaChainWatch\State\<Character>.json`, written by the plugin only (rectl doesn't use it).
  It holds `isOn`, `mainMinutes`, `currentMeta`, `sinceLoadMinutes`, `elapsedMinutes` and `savedAt` (local time).
* **Saving:** on the timer thread, on every change (meta load while on, on, off, timeout change, timing restart after a
  fallback) and every 30 s while on, plus once more when the plugin shuts down.  Same `.tmp` swap as the config files.
  Turning the watch off deletes the file.  A failed save is retried every tick and logged once.
* **Resuming:** the first tick after the plugin starts (login, relog after a crash, plugin reload) reads the file.
  * The watch turns on with the saved main timeout and meta.  VT reloads its meta at login without a
    `/vt meta load`, so the saved name is what the watch times.
  * The meta keeps the run time it had when saved, but the time the client was down doesn't count.  A meta that
    keeps crashing the client still times out, and a long relog doesn't fire the timeout the moment he logs in.
  * Nothing times out in the first minute after resuming, so the game and VT can settle.  A meta that was already
    past its limit (or crashed while waiting for the next meta) times out again after that minute and gets a fresh
    chain state jump.  A meta load in that minute ends the wait as usual.
  * A save older than **4 hours** is ignored and deleted: the chain is long over and someone may be playing the
    character by hand.  An invalid or unreadable save is ignored the same way.
  * If a meta load was seen before the first tick, that meta is kept and timed from its load.  If `on` ran before
    the first tick (it can't in practice), the save is ignored.
* A normal logout during the chain saves too, so logging back in within 4 hours resumes the watch.  aFarm turning the
  watch off is what ends it.

## Config files

* Global: `<install>\MetaChainWatch\Global.json`
* Per character: `<install>\MetaChainWatch\<Character>.json`, same layout

`<install>` is the RedoxExtensions install directory, as for Scheduled Commands.  Every field is optional.

```json
{
  "fallbackCommand": "/re sm aFarm",
  "chainStates": ["** Chain **", "* Chain *", "TT-chain", "Chain"],
  "recall": { "enabled": true, "gem": "Halls of Introduction Gem", "landblock": "0x012F" },
  "timeouts": {
    "Attributes4.0": { "minutes": 10 },
    "aKTTouTou": { "minutes": 30, "chainState": "TT-chain" }
  }
}
```

Merge rules:

* A character `timeouts` entry replaces the whole global entry with the same name.
* A character `fallbackCommand` or `chainStates` replaces the global one.  `recall` merges field by field (see
  Recall).  Global replaces the defaults.
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
* **TODO: test Decal's gem use in game.**  The recall uses the gem with Decal's `Actions.UseItem` (not `/mt use`),
  and that hasn't been tried.  Check that it fires the Halls of Introduction Gem, including from combat after
  `/vt stop` and peace.  If it doesn't, switch to `/mt use <gem>` (documented, exact name).  The TODO is also in
  `MetaChainWatcher.cs` at the `UseItem` call.
* **The recall isn't tested in game yet** (unit tested: `MetaChainWatchTests`, Recall).  To check: whether
  `Actions.Landcell` is already the Halls cell on the first read after the portal exit (the 10 s landing wait
  covers a late update), whether 2 s after `/vt stop` and peace is enough for the gem use to go through from combat
  (the retry covers one miss), and the drop cell (expected `0x012F0126`).
* **The recall doesn't reset VT options.**  OpenDoors stays on after the recall; the Halls have no locked doors, and
  the next meta or profile sets its own.  A meta that gets stuck on something that follows him (e.g. a monster
  in the Halls, or a stuck VT state that `/vt stop` doesn't clear) isn't helped.
* **A gem use that a cancel can't undo.**  If a meta loads during the 30 s wait after the gem was used, he still
  teleports while the new meta runs.  Only a stuck meta that suddenly moves on does this.
* **Portal space hang.**  The watch does nothing while in portal space (as before), so a client stuck in portal
  space stops the recall too.
* **Resuming isn't tested in game yet.**  Unit tested (`MetaChainWatchTests`, Snapshot and resume); the next real
  crash during a chain should show the `resumed after restart` line in the journal.

The `meta-chain-watch` skill (`.claude/skills/meta-chain-watch`) says how Claude lists and changes the timeouts.
