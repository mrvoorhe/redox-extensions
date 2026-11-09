---
name: meta-chain-monitor
description: Watch a running VT meta chain on the user's Asheron's Call characters without touching it, record everything through the agent bridge, and report per-meta timing and kill-task results (which tasks were still open at each hunt timeout, finish times, kill rates) with timeout recommendations. Use when asked to monitor a meta chain or the dailies, collect data on metas, or work out how to tune meta timeouts.
---

# Monitor a meta chain and collect data

The job is to record, not to play.  Once a chain has started, never send a game command to that character, even if
a meta looks stuck: the user wants to see how robust the metas are on their own.  Everything here is read-only
(`rectl events`, `rectl state`, `rectl list`).  To start chains at set times, use `meta-chain-start`.

## Tools

`tools/vt/metachain.py`, run with `py`:

* `record <Char> <outdir> [--start HH:MM] [--stop YYYY-MM-DDTHH:MM | --hours 8]`
  * Polls the journal every 3 s and `state` every 30 s, and appends everything to `<outdir>/<Char>.jsonl`.
  * Follows the character across crashes and relogs.  Gaps and new client instances become `kind: meta` records.
  * When it starts, it dumps every installed meta to `<outdir>/metas/`.  The user edits metas between runs, so
    analysis must read the snapshot, not the current files.
  * Create `<outdir>/STOP` to end it early.
* `analyze <outdir>/<Char>.jsonl [--since ISO] [--json out.json]`
  * Prints markdown: the chain timeline, then one section per hunt meta.  A hunt meta is one that starts an
    `<X>HuntSW` stopwatch.
  * Each section has the hunt limit, the timeout time, every kill task (killed/required, finish minute, kills per
    minute), minutes per state, stationary periods, NPC lines and the meta's own `/myquests` flags.
  * It ends with incidents (client lost, journal gaps, deaths) and deduplicated warnings.

## Steps

1. **Before the chain starts:**
   * Check `rectl list` shows each character.  Mode `r` is enough for watching.
   * Turn on the motion monitor on each character: `rectl -c <Char> motion on`.  It only watches (no game command),
     and puts `motion` events (`stuck`, `moving`, `idle`, `portal`) in the journal, so stuck periods show up in the
     log with position and time instead of having to be spotted in screenshots.  It resets on `/re reload`.
   * Pick an output folder in the scratchpad.
   * Start one recorder per character with the Bash tool's `run_in_background`.  Start it about 10 minutes before
     the chain, and stop it well after the chain should end (a daily chain takes about 85 minutes; leave room for
     retries and relogs).
2. **One subagent per character.**  If the user asks for live monitoring, give each subagent:
   * the character name, the log path and the rules above (read-only, never intervene)
   * the facts below
   * the task: follow the log while the chain runs, then run `analyze` and write the report.

   Subagents may not be able to write files: have them return the report text, and save it yourself as
   `<outdir>/report_<Char>.md`.  Without subagents, wait for the chain to finish and run `analyze` yourself.
3. **Check the analysis against the raw log before you trust it.**  Spot-check the open tasks against the NPC
   turn-in lines ("I see you haven't completed...") and against the last `/myquests` counters.  Fix
   `metachain.py` if it's wrong for a new meta.
4. **Write the report** for each character:
   * chain timing per meta
   * per KT meta: limit, used, timed out or not, tasks open at timeout with counts, slowest finished task
   * per-task detail
   * coins received, per meta and in total
   * incidents and meta gaps
   * a timeout recommendation table: current limit, suggested limit, why

   Base suggested limits on finish times and kill rates (minutes still needed = remaining kills / kills per
   minute).  Say it is one run.  Flag tasks a longer timeout can't fix (unhittable targets, a route that never
   reaches the mob).  Save the report, give the user its path and the headline numbers.

## Facts learned from the 2026-11-07 run

* **Server kill lines:**
  * `You have killed N Xs! You must kill M to complete your task.`
  * `You have killed N Xs! Your task is complete!` repeats on every later kill of that task.  Don't count those as
    progress.
  * At the NPC: `You've killed N out of M X.`
* **One counter per task, not per mob:** "Clay Golem Samurais 1" is followed by "Bronze Golem Samurais 2".
  * The analyzer groups mobs with the meta's own `^You have killed` triggers.
  * A mob the meta's triggers miss is joined to the task whose count it continues, and reported.  This found
    Hoshino's missing `Spectral Claw Master` and Frozen's missing `Frozen Wight Archer`.
* **Carried progress:** a task's first kill line can start above 1, because progress carries over from an earlier
  day.  Kill rates use only the kills gained this run.
* **Timeouts:** the hunt timeout is `stopwatchelapsedseconds[$<X>HuntSW]>=N`.  On timeout the meta echoes
  `<Name>: N minute hunt limit reached`, which is the authoritative limit for that day.  The stopwatch includes the
  walk to the hunt area (0.3 to 0.9 minutes).
* **Coins:** `<NPC or mob> gives you N Enlightened Coins.` (also `Weakly Enlightened Coins`) comes from turn-ins and
  kill-task rewards.  `analyze` totals them per meta and giver under "Coins received".  Count coins from these lines,
  not from `[BANK] Enlightened Coins:` changes: coins sit in inventory until the meta runs `/b d`, so a bank jump
  also includes coins picked up earlier.
* **`/ub myquests`** output is hidden in game, but every line still lands in the bridge journal (about 700 lines).
  The analyzer only keeps the flags the meta reads.
* **Journal:** the ring holds 5000 events, and `--since` is inclusive (pass `next`).  A 3 s poll easily keeps up.
* **Already known issues** (report again only if they change):
  * RedoxExtensions `InvalidOperationException "Expected to have a target name"` on NPC give lines (fixed in
    256d2ce, deployed only when the user says so).
  * VTank "Blacklisting unhittable target".
  * The Attributes4.0 timeout message has no `/`, so it is said in public chat.
