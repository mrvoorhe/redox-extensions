"""Watch a character until it reaches a spot, gets stuck, or time runs out.  For testing navs and metas.

    py tools/vt/vtfollow.py <character> --cell 01A30153 --x 50.21 --y -64.52 [--tol 1.5] [--timeout 180]
    py tools/vt/vtfollow.py <character> --nav <file.nav>          target = the route's last waypoint

Prints a position line every few seconds.  Exit code 0 = ARRIVED, 1 = STUCK or TIMEOUT.

Uses the plugin's motion monitor (`rectl motion`), which reports stuck as soon as the character has been trying to
move without moving for --stuck seconds (default 2.5), and ignores portal space and VT's pause nodes.  Builds
without it fall back to polling positions, where STUCK means no movement at all for --stuck seconds (default 18,
since a stall after a teleport is normal).
"""
import argparse, math, os, sys, time

sys.path.insert(0, os.path.dirname(__file__))
import rectl, vtnav


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('character')
    ap.add_argument('--cell', help='target landcell (hex)')
    ap.add_argument('--x', type=float)
    ap.add_argument('--y', type=float)
    ap.add_argument('--z', type=float, help='target height; without it only horizontal distance counts')
    ap.add_argument('--nav', help='use the last waypoint of this nav as the target')
    ap.add_argument('--tol', type=float, default=1.5, help='meters')
    ap.add_argument('--timeout', type=float, default=180)
    ap.add_argument('--stuck', type=float, help='seconds without moving (default 2.5, or 18 when polling)')
    ap.add_argument('--interval', type=float, default=3)
    a = ap.parse_args()

    if a.nav:
        points = [n for n in vtnav.load(a.nav).nodes if n.type == 0]
        target = points[-1].position()
    elif a.cell and a.x is not None and a.y is not None:
        target = vtnav.nav_coord(int(a.cell, 16), a.x, a.y, a.z if a.z is not None else 0)
        if a.z is None:
            target = (target[0], target[1], None)
    else:
        ap.error('give --nav, or --cell with --x and --y')

    motion_args = ['motion', 'on'] + (['--stuck', a.stuck] if a.stuck else [])
    try:
        motion = rectl.run(a.character, *motion_args)
    except rectl.RectlError:
        motion = None
    if motion is None:
        print('motion monitor not available, polling positions', flush=True)
        return poll(a, target)

    start = time.time()
    while time.time() - start < a.timeout:
        s = rectl.state(a.character)
        if report(s, target) <= a.tol:
            print('ARRIVED')
            return 0
        if (s.get('motion') or {}).get('state') == 'stuck':
            print('STUCK', flush=True)
            return 1
        r = rectl.run(a.character, 'wait', '^stuck', '--kinds', 'motion', '--timeout', int(a.interval * 1000),
                      check=False)
        if r and r.get('matched'):
            print(r['event']['text'])
            print('STUCK')
            return 1
    print('TIMEOUT')
    return 1


def report(s, target):
    """Prints the position line and returns the distance to target in meters."""
    l = s['location']
    cell = rectl.landcell(s)
    ew, ns, z = vtnav.nav_coord(cell, l['x'], l['y'], l['z'])
    dz = 0 if target[2] is None else z - target[2]
    dist = math.sqrt((ew - target[0]) ** 2 + (ns - target[1]) ** 2 + dz ** 2) * 240
    motion = (s.get('motion') or {}).get('state')
    print(f"{time.strftime('%H:%M:%S')} {cell:08X} {l['x']:.1f},{l['y']:.1f},{l['z']:.1f}  {dist:.1f} m to target"
          + (f'  {motion}' if motion else ''), flush=True)
    return dist


def poll(a, target):
    stuck = a.stuck or 18
    start, last, still_since = time.time(), None, None
    while time.time() - start < a.timeout:
        s = rectl.state(a.character)
        if report(s, target) <= a.tol:
            print('ARRIVED')
            return 0
        l = s['location']
        pos = (rectl.landcell(s), round(l['x'], 1), round(l['y'], 1))
        if pos != last:
            last, still_since = pos, time.time()
        elif time.time() - still_since >= stuck:
            print('STUCK')
            return 1
        time.sleep(a.interval)
    print('TIMEOUT')
    return 1


if __name__ == '__main__':
    sys.exit(main())
