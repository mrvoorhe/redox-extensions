---
name: vt-nav
description: Record, build, inspect and test Virindi Tank (VT) nav routes (.nav) for the user's Asheron's Call characters through the agent bridge. Use when asked to make, fix or check a VT nav route, record where a character goes, or turn a recorded path into a route.
---

# VT nav routes

Tools are in `tools/vt` (Python 3; run with `py`, `python` is a Store stub on this machine).  Characters are real, on a
public server: only send the commands the task needs.  The agent bridge must be on in game (`rectl list` shows the
character with mode `rw`); see `docs/design/Feature-AgentBridge.md`.

The VT folder (navs, metas, profiles) is the user's VirindiTank directory: `py tools/vt/rectl.py vt-dir` prints
it (from `rectl paths`; `VT_DIR` overrides it).  Look at existing navs there before inventing
anything (`py tools/vt/vtnav.py dump <file>`); many routes already exist (`aGotoJam.nav`, `JamBridge.nav`, ...).
Never overwrite a file there without asking, and delete test files you create.

## 1. Record the route

Use the bridge recorder, not `/re rr`: the user prefers it.  It merges positions with the actual network actions by
time, so it gets gem uses, portal/NPC uses with the object's exact coordinates, and jumps right.

```
py tools/vt/vtrecord.py record <Character> <scratch dir>      # run in the background
```

Tell the user it's recording and to play the route.  When they say they're done, create `<scratch dir>/STOP` (it
exits within a second and turns net capture off).  If the process was killed instead, run
`rectl -c <Character> net off`, or net capture keeps filling the journal.

```
py tools/vt/vtrecord.py spec <scratch dir>        # writes <scratch dir>/route.spec
```

Read the draft spec and fix it before building:

* Points at the very start that aren't part of the route (e.g. a meta walking somewhere) can go.
* Check each `jump` line: heading comes from the character, shift `False` and `500` ms are guesses.  The network
  message doesn't carry them.
* `# ...` lines are uses the tool couldn't convert (use-with-target, objects gone before lookup): add them by hand.
* Long straight runs can lose points (`--min-move` sets the spacing, default 4 m).

## 2. Build

```
py tools/vt/vtnav.py build <spec> "<VT folder>\<Name>.nav"
py tools/vt/vtnav.py dump <file.nav>
py tools/vt/vtnav.py coord <landcell hex> <x> <y> <z>   # nav coords for a game position
```

The spec format is in the `vtnav.py` docstring.  `tools/vt/examples/TestGoToJamBridge.spec` is a tested route
(gem, two portals, a fall and a jump).  Route type: `once` for travel, `circular` / `linear` for hunting loops.

## 3. Test in game

1. Ask the user how to reset to the start if it isn't obvious (for the Jam route: the Abandoned Mines Portal Sending
   Gem).  Stop the macro first (`/vt stop`) if the character may be fighting: a gem use mid-fight silently fails.
2. If a meta is loaded and enabled, it will take over: `/vt opt set EnableMeta False` for the test, and restore it.
3. `/vt nav load <Name>` (prints nothing), `/vt opt set EnableNav True`, `/vt start`.
4. `py tools/vt/vtfollow.py <Character> --nav <file.nav>` watches until the last waypoint (exit 0 ARRIVED, 1 STUCK or
   TIMEOUT).  It turns on the motion monitor and stops on its first `stuck` event, usually 2-3 s after the character
   hits something, and prints that event (cell, position, heading, intent).  Use that, not screenshots, to find where
   a route gets stuck; take a screenshot only when you need to see what is in the way.  The ~10 s stall after each
   teleport is the pause node and isn't reported as stuck.
5. Afterwards put things back: EnableNav / EnableMeta as they were, reload the meta or nav the user had, delete test
   navs.  Say what you changed.

Reading options: `py -c "import sys; sys.path.insert(0,'tools/vt'); import rectl; print(rectl.vt_option('<Char>', 'EnableNav'))"`.
Run in-game commands through `rectl.exec_wait` or `rectl.cmd exec`; don't put `|` in a `rectl.cmd --wait` regex (cmd
treats it as a pipe and the command isn't sent).

## Nav format (uTank2 NAV 1.2)

Header: `uTank2 NAV 1.2`, route type (1 circular, 2 linear, 4 once; 3 is a follow route, not supported), node count.
Each node: type, ew, ns, z, `0`, then by type:

| Type | Node | Extra lines |
|---|---|---|
| 0 | waypoint | none |
| 2 | recall (probably) | one number, a spell id |
| 3 | pause | milliseconds |
| 4 | chat command | text |
| 5 | open vendor | vendor id, name |
| 6 | use portal / NPC, wait for the teleport | name, ObjectClass (14 portal, 37 NPC), `True`, object ew, ns, z |
| 7 | talk to an NPC, then go on | as 6 |
| 9 | jump | heading, shift `True`/`False`, milliseconds (as `/vt addnavjump` writes it) |

* Nav coords: `ew = ((landblockX - 127) * 192 + x - 84) / 240`, same for ns with landblockY and y, `z = z / 240`
  (landblockX/Y are the top two bytes of the landcell).  Works in dungeons too.
* Non-waypoint nodes ignore their own position (VT writes the character's position there; 0 works).
* A use node's coordinates are the object's: `rectl object <id>` / `world` give them as `nav` ([ew, ns, z]) and the
  class as `classId`.  The values in `aGotoJam.nav` match what the bridge reports.
* `parse` + `serialize` reproduces every nav in the VT folder byte for byte.

## Things learned the hard way

* Dungeon ledges and railings need a jump node.  A route that stops moving at a ledge is missing one.
* Portals: VT sends the use from a distance and the character walks up after.  Points recorded during that walk
  belong to the old side; the recorder drops them, check by hand when editing.
* After a teleport VT waits on the pause node; 5000 ms after gems and portals worked.
* A portal NPC like "Help Wanted" may take ~5 s and be used twice; one use node is enough.
* Quest/task NPCs need a talk node (spec `talk`, type 7).  On a type 6 node (spec `npc`) VT uses the NPC every
  ~3 s waiting for a teleport and never moves on.  The recorder writes `talk` for NPCs; change a teleporting NPC
  to `npc` by hand.  A talk node may still use the NPC 2-5 times; harmless.
