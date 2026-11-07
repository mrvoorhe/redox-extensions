"""Example farm-spot meta (see farmspot.py): go to Breach Zero, walk its hallways looking for a farming spot without
another player, farm there.  Builds aFBreach.met in the VT folder.

    py tools/vt/examples/aFBreach.py [<VT folder>]
    py tools/vt/examples/aFBreach.py --test <spots> [<VT folder>]     test build: aFBreach-test.met

--test makes every spot except the listed ones count as taken, to test a path when other players decide which
spots are free: "none" (a full pass, then the recall), "side" (every side spot), or names like 10R,17S (checkpoint
number and L/R/S; a fixed spot is just the number).  Spots that are listed still need to be free.

Breach Zero (landblock 0x5766) is busy and players take the good spots.  The route (aFBreach-route.spec, recorded on
Virmar) passes 21 checkpoints; side spots are behind doors.  Players behind a closed door are still visible to the
client, so the check works from the hallway.

Spot coordinates: side spots are 6 m from the hallway centre (every player seen during the recording stood within
0.3-3.6 m of the estimate); a fixed spot is where the user stood.  Doors are measured (aFBreach-doors.spec); 11S
(behind a door that is really locked) is left out.

Doors: Breach Zero's doors are flagged Locked (lockpick 50), so VT's OpenDoors skips them ("No usable lockpicks
found"), but using one opens it, so the navs use them with UB (farmspot.door_lines).
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..'))
import farmspot

BREACH = farmspot.Dungeon(
    name='aFBreach',
    landblock=0x57660000,
    here=HERE,
    entry=(390.0, -80.0),  # where the Breach Zero portal and the Portal Recall Gem drop you
    side_offset=6.0,
    side_door=2.3,
    side_radius=4.5,       # a player this close to a side spot has it
    fixed_radius=6.0,      # players stood 2-5 m from the user's position at the fixed spots
)

if __name__ == '__main__':
    farmspot.main(BREACH, sys.argv[1:])
