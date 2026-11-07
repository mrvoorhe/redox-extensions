---
name: farm-spot-meta
description: Make a Virindi Tank farming meta for a busy dungeon where the meta walks a route of candidate farming spots, skips spots another player holds, farms at the first free one and recalls to try again when all are taken (like aFBreach for Breach Zero). Use when asked to make a farming / farm spot meta for a dungeon, or to add or change spots in one.
---

# Farm-spot metas

`tools/vt/farmspot.py` builds these metas; `tools/vt/examples/aFBreach.py` (Breach Zero, tested on Virmar) is the
example.  A dungeon is a small config script plus two specs recorded from the user walking the route.  Read the
`farmspot.py` docstring for the states.  The `vt-nav` and `vt-meta` skills cover the underlying tools; their rules
apply here too: real characters on a public server, only documented commands (AGENTS.md), never overwrite VT folder
files without asking, delete test files.

**Send in-game commands from the PowerShell tool or Python (`rectl.exec_wait`), never the Bash tool**: Git Bash
turns `/vt ...` into `C:/Program Files/Git/vt ...` and the character says it in public chat.

## 1. Ask

* Character, dungeon, meta name (the user's are `aF<Place>`), settings profile (`Farm` so far).
* How to recall to the entrance (`/ub usei Portal Recall Gem` so far) and how to reset the character to a start.
* The character must be on the bridge in `rw` (`rectl list`).

## 2. Record

Start `py tools/vt/vtrecord.py record <Char> <scratch dir>` in the background, then tell the user to walk the route
and mark it in chat (fellowship chat works; only what the character itself says counts):

| Marker | Meaning |
|---|---|
| `goto<x> - begin` ... `goto<x> - end` | travel to the dungeon (gem, town, portal); any `<x> - begin/end` other than findspot |
| `findspot - begin` ... `findspot - end` | the search route, starting at the dungeon entrance |
| `spot - left / right` | stop in the hallway: spots on both sides |
| `spot - left`, `spot - right`, `spot - straight` | one side, or ahead; markers within 3 m merge (`RS`) |
| `spot fixed` | the spot is where the character stands |

Ask them to stand still at the hallway centre for each marker and to open doors on the way as the meta should.
When they're done, create `<scratch dir>/STOP`.

## 3. Turn it into specs

```
py tools/vt/farmspot.py route <scratch dir> <Name> --out-dir tools/vt/examples
```

This writes `<Name>-goto.spec` and `<Name>-route.spec` and prints notes (markers it couldn't read, checkpoints
without a direction).  Read both (on the Breach Zero recording the output matched the hand-made specs):

* Goto: starts with the gem use, ends with the portal and the arrival point.  Drop anything before the gem.
* Route: starts at the entrance point; one `# checkpoint N <spots> [dx dy]` per stop, where `<spots>` is `fixed` or
  L/R/S letters and `dx dy` the walking direction into it (left/right/straight are relative to it).  Count them
  against the user's markers.  Door uses are `door <ew> <ns> <z>` lines.  Jumps and other vtrecord lines are kept
  as they are; check them as in the `vt-nav` skill.
* Spot names in the meta: checkpoint number plus side (`10R`), or just the number for a fixed spot (`09`).

## 4. Config script

Copy `examples/aFBreach.py` to `examples/<Name>.py` and set the `Dungeon` fields:

* `landblock`: high word of the cells in the route spec (`0x57660000` for cell `576602B8`).
* `entry`: where the portal and the recall drop you (default: the route's first point).  Check both land there.
* Side-spot geometry: `side_offset` (spot from the hallway centre, 6 m in Breach Zero), `side_door` (the side
  spot's door, 2.3 m; `None` if side spots have no door), `side_radius` / `fixed_radius` (another player that close
  holds the spot).  Measure them: positions of players at the spots (`rectl -c <Char> world --class Player --near 60`
  while walking, or during the recording) and of the doors (`--class Door`).  Ask the user if the layout is unclear.
* Measure the doors: run a `--test none` build (a full pass, no farming) with
  `py tools/vt/farmspot.py doors <Char> tools/vt/examples/<Name>-doors.spec <landblock hex>` in the background.
  The client only sees nearby doors, so it has to walk the whole route.  With `<Name>-doors.spec` present, each
  spot behind a door uses the nearest measured door in its direction instead of `side_door`: in Breach Zero the
  side doors were 1.8-2.9 m out but one straight-ahead door was 5.5 m, so the estimate missed it, the character
  "arrived" outside the closed door and fought the wall.  The build prints a note for spots with no measured door.
* Some doors really are locked (Breach Zero 11S: using it does nothing, the `*Stuck` states repeat).  The user's
  call there was to drop the spot: remove its letter from the checkpoint line.
* Doors: check what kind they are (`rectl object <id>`).  Breach Zero's are flagged Locked, so VT's `OpenDoors`
  skips them, and a nav use node toggles them for ever; the `door` lines (a UB `actiontryuseitem` chat node, from a
  point 1 m in front) work.  If VT's `OpenDoors` works in a new dungeon, it may be simpler; test before choosing.

Build: `py tools/vt/examples/<Name>.py`, then `py tools/vt/vtmeta.py dump "<VT folder>\<Name>.met"` and read it.

## 5. Test in game

`/vt meta load <Name>`, `EnableMeta` True, `/vt start`; follow `[VTank] Meta transitioning to state:` lines in
`rectl -c <Char> events --kinds plugin` and the position (`rectl state`).  `/vt stop` before every rebuild, or the
old meta keeps running.  Test builds: `py tools/vt/examples/<Name>.py --test <spots>` writes `<Name>-test.met` where
only the listed spots can count as free (`none`, `side`, or names like `10R,17S`); delete it afterwards.

| Scenario | How | Expect |
|---|---|---|
| From town | reset as the user says, start | GoTo -> Walk01 -> ... -> Farm<spot> |
| Taken spots skipped | real players, or `--test <a later spot>` | Check states move on |
| Side spot through its door | `--test <a free side spot>` | Go<spot> opens the door, stands in the spot, kills |
| Every spot taken | `--test none` | full pass, Recall (gem), Walk01 again |
| Restart on a spot | `/vt meta load` while farming | Default -> Farm<spot> |
| Restart mid-dungeon | load away from spots and entrance | Default -> Recall |
| Alone in a fellowship | (the user's farm setup) | `/ub fellow quit` 3 s into Farm |

`*Stuck` states are the door watchdog (a door someone left open, closed by our use); a few per pass are normal.
Many in one place means a missing door line or a waypoint that needs moving.  Death and someone taking a spot
mid-approach can't be tested safely: say so.

Leave the character running the real meta (or as the user wants), and report what was tested.  Commit
`<Name>.py` and both specs together when the user asks (one commit; no local paths).
