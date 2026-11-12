---
name: stress-test
description: Run the client stability stress test (the aStressTest VT meta, a combat-off loop through Arwic, the TouTou hunt path and the Hoshino hunt path, which are common crash points) on one of the user's Asheron's Call characters, watch it for crashes, hangs and stalls, stop after N cycles and put the character back on its farming meta, then report. Use when asked to stress test the game or client, run the stress test or stress loop, or check client stability on a character.
---

# Client stability stress test

The `aStressTest` meta runs this loop with combat and looting off:

1. Arwic
2. Town Network to Tou-Tou, then one lap of the TouTou hunt loop
3. Halls gem to Hoshino, then one lap of T1 to T5
4. The Lifestone Sending Gem back to Arwic

No NPC talks happen, so kill tasks aren't touched.  One cycle takes about 9-10 minutes.

* `tools/vt/examples/aStressTest.py` builds the meta.  Its docstring lists the states and helper navs.
* `tools/vt/stresstest.py` runs and watches it.

The characters are real, on a public server.  Send only what the run needs; the runner sends everything itself.

## Before starting

1. `rectl list` shows the character with the bridge on in `rw` mode.  If it doesn't, ask the user to run
   `/re agent rw` in game.
2. Note what the character is doing now, so you can put it back afterwards:
   * Read the journal's `Meta transitioning to state` / `Embedded Nav` lines with
     `rectl -c <Char> events --kinds plugin`.  Map the state and nav names to the meta: e.g. GoToPillar/Farm with
     `TestGoToJamPillar.nav` is `aFJamPillar`.
   * Read `rectl state` for the location.
   * If the meta isn't clear, ask the user which one to go back to.
3. The character's lifestone must be in Arwic, because the meta comes back with the Lifestone Sending Gem.
   * Rockdown's and Cowhead's are (checked 2026-11-10/11).
   * For another character, ask the user, or watch the first `S9-lifestone` step: it should reach `S1-arwic` in
     about 10 s.  If it doesn't, the meta gives up and tries `/ls`.
4. `aStressTest.met` must be in the VT folder.  If it isn't, rebuild it with `py tools/vt/examples/aStressTest.py`.
   The runner itself checks the gems (Town Network Sending, Halls of Introduction, Lifestone Sending).

## Run

Use an archive folder: `F:\ac-files\decaldev\MetaAnalysis\<date>\stresstest-<char>` (memory: meta analysis
archive).  Run the command in the background with the Bash tool (`run_in_background`):

```
py -u tools/vt/stresstest.py run <Char> <outdir> --cycles 5 --then <farming meta>
```

* Leave out `--cycles` to run until told to stop.  Leave out `--then` to just stop the macro at the end.
* `--no-unstick` turns the stall handling off.
* `--no-start` watches an `aStressTest` that is already running.

Then watch the log with a Monitor, so you get one notification per line that matters:

```
tail -n 0 -F "<outdir>/stress.log" | grep -E --line-buffered "CRASH|HANG|STALL|unstick|GIVE UP|DIED|CYCLE|S4-toutou|S8-hoshino|NOTE|chat:|client back|done:"
```

A Monitor expires after 30 minutes; re-arm it, still with `tail -n 0`.  The runner keeps going on its own.  Give
the user a one-line update per event; they asked to be kept informed.

### What the log lines mean

| Line | Meaning | What to do |
|---|---|---|
| `step <state>` | the meta's next step | nothing |
| `CYCLE n done in s` | back in Arwic, a full cycle | report the time |
| `STALL: ...` then `unstick: combat off, moved x m` | moved < 2 m in 20 s on a walking step, so combat was on for 30 s | Usually a monster in the path, and expected.  Moving ~0 m while combat was on is fine (missile/magic fight in place); check with `rectl state` a few seconds later that the character moves again. |
| `GIVE UP <state>` | a step timed out; the meta recalls and starts the cycle over | report it.  If it repeats, look at where (`rectl state`, `rectl screenshot`) |
| `DIED in <state>` | death; the meta waits for Arwic, then goes on | report it |
| `HANG?` | pid alive, bridge silent for 60 s | possibly a frozen client: report it and take a screenshot if the bridge answers again |
| `CRASH: client pid ... gone` | the client crashed | Report it now: the time, the step and the last cell.  You can't relaunch the client.  When the user logs back in and turns the bridge on, the runner logs `client back`.  They then restart the meta with `/vt start` (aStressTest starts over from Default, which recalls to Arwic). |
| `done: ...` | finished, and SUMMARY.md is written | report |

To stop early, create `<outdir>/STOP`.  The runner writes SUMMARY.md and leaves the macro running.  Then send
`/vt stop` (and load the farming meta) yourself, through Python or PowerShell, not Git Bash (memory).

## After

1. With `--then`, check that the farming meta got where it should, e.g. it reached its Farm state.  Use
   `rectl events --kinds plugin` and `rectl state`, and read `EnableCombat` with `rectl.vt_option`.
2. Report from `<outdir>/SUMMARY.md`:
   * the table of cycles: time per cycle, the TouTou and Hoshino lap times, and the number of stalls;
   * any incidents;
   * where the character is now and what it's running.

   Typical times: cycle 535-600 s, TouTou lap 102-113 s, Hoshino lap 267-330 s, and about one stall every five
   cycles on the Hoshino path.
