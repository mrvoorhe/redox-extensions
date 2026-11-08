---
name: town-travel
description: Send one of the user's Asheron's Call characters to a town (or any Town Network destination such as the Marketplace, Facility Hub or Abandoned Mine) with the aTownNetwork VT meta, through the agent bridge. Use when a task needs a character in a town, or when asked to travel, go to or take a character to a town; also to change or extend the meta's destinations, names or routes.
---

# Town travel (aTownNetwork)

`aTownNetwork.met` sends a character through Town Network: say `!<town>` in fellowship chat and every member running
the meta uses the Town Network Sending Gem, walks to that town's portal and goes through.  It covers 52 destinations.
Use it instead of building a nav by hand.

* Builder: `tools/vt/examples/aTownNetwork.py` (writes the .met into the VT folder), portal positions in
  `tools/vt/examples/TownNetworkPortals.json`.
* Names: `py tools/vt/examples/aTownNetwork.py --list` prints every destination, its names and its route.  Matching
  ignores case, spaces, hyphens and apostrophes; most long names have short ones (`qal`, `teth`, `crag`, `fh`, `mp`).

Characters are real, on a public server.  Only touch the character the task is about: send the `/f` line from that
character itself (`[Fellowship] You say, "!x"` matches too), never from another one.

## Send a character

```
py tools/vt/towntravel.py <Character> <town> [--restore <meta>]
```

It loads aTownNetwork, turns EnableMeta on, starts VT, says `/f !<town>`, waits until the character has passed
through Town Network (landblock 0x0007) and the meta is back in Default, and prints the meta log and where it ended up
(exit 0 = arrived).  `--restore` loads another meta afterwards.

Before:

* VT runs one meta at a time, so this replaces whatever the character was running (often a farm meta).  Find out
  which meta that was if it should go back (ask the user if you can't tell), and pass it with `--restore`.
* If the character is fighting, `/vt stop` first.  The meta retries the gem at 15 s and 30 s, but a gem use while
  fighting, casting or looting fails silently.  Buffing (if the profile has it on) only makes the trip slower.
* If `aTownNetwork.met` isn't in the VT folder (`py tools/vt/rectl.py vt-dir`), build it:
  `py tools/vt/examples/aTownNetwork.py`.

Everyone in the fellowship who runs the meta goes too.  Ask before sending a command when another of the user's
characters in the fellowship has it loaded.

## How the meta works

* `Default`: nav off, waits for the chat line.  `G_<key>`: uses the gem, then waits until the character is in the
  Annex landing cell 0x00070145 (70, -80).  Don't use PortalspaceExit for this: it fired a second after the gem use,
  before the teleport.
* `N_<key>`: embedded nav from the landing to about 3 m in front of the portal, then a portal use node.  Leaving the
  landblock ends it (back to Default); so does a 90 s timeout.
* The building is a cross: north wing (Aluvian towns), south wing (Facility Hub / Marketplace crossbar at y -100, then
  a big room with a bar), west wing (Gharu'ndim), east wing (Sho).  Every wing has furniture in the middle, so routes
  run along the walls.  Linvak Extas and Korway are on a lower floor (z -6), down stairs on the north side of the pit in
  the south room, east of the bar.

## Changing it

* Edit `NAMES` (destination -> names; the build stops if two destinations share a name) or `route_points` /
  `STAIRS` / `LOWER` in the builder, rebuild, then `/vt meta load aTownNetwork` again (the meta embeds its navs).
* New or moved portals: in Town Network, `rectl world --class Portal` gives the exact nav coords; update the JSON.
* Walk new approach points before trusting them, and keep waypoints a metre or more from walls and corners.  A
  character jammed on a corner shows "Using the <portal>" at the top of the screen and never goes through.  For
  anything awkward, have the user walk it with the bridge recorder (`vt-nav` skill).
* VT treats a waypoint within about 2 m as reached.

## Status (2026-11-08)

* Tested in game on Woodford: Cragstone, Rithwic, Fort Tethana, Stonehold, Holtburg, Facility Hub, Marketplace,
  Silyun, Danby's Outpost, Abandoned Mine, Oolutanga's Refuge, Samsur, Yaraq, Al-Jalima, Tufa, Qalaba'r, Westwatch,
  Mayoi, Tou-Tou, Kryst, Freehold, Linvak Extas, Korway.  The rest use the same route shapes but haven't been run.
* Not included: Quest Explorer Portal (its name has the week number and it probably changes weekly).
