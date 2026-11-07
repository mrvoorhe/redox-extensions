"""Example meta: go to Breach Zero, walk its hallways looking for a farming spot without another player, farm there.
Builds aFBreach.met in the VT folder.

    py tools/vt/examples/aFBreach.py [<VT folder>]
    py tools/vt/examples/aFBreach.py --test <spots> [<VT folder>]     test build: aFBreach-test.met

--test makes every spot except the listed ones count as taken, to test a path when other players decide which
spots are free: "none" (a full pass, then the recall), "side" (every side spot), or names like 10R,17S (checkpoint
number and L/R/S; a fixed spot is just the number).  Spots that are listed still need to be free.

Breach Zero (landblock 0x5766) is busy and players take the good spots, so there is no single spot to go to.  The
route (aFBreach-route.spec, recorded on Virmar) passes 21 checkpoints.  At each one the meta looks at the spots next to
it (left / right / straight of the hallway, usually behind a door) or, for a "fixed" checkpoint, the checkpoint itself.
Players behind a closed door are still visible to the client, so the check works from the hallway.  It walks into the
first free spot and farms; if every spot is taken it recalls to the dungeon entrance (Portal Recall Gem) and tries
again.  When it starts farming and is alone in a fellowship, it quits the fellowship.

Spot coordinates: side spots are 6 m from the hallway centre (every player seen during the recording stood within
0.3-3.6 m of the estimate); a fixed spot is where the user stood.

Doors: Breach Zero's doors are flagged Locked (lockpick 50), so VT's OpenDoors skips them ("No usable lockpicks
found"), but using one opens it.  A nav use node doesn't work either: VT keeps using the door until it answers,
so it opens and closes for ever.  So each "door <ew> <ns> <z>" line in a spec here becomes a chat node (run once)
that uses the door within 1.5 m of that position with a UB expression, then a short pause while it opens.  Hallway
doors are where the user opened them; a side spot's door is estimated 2.3 m from the hallway centre (the side doors
measured 2.1-2.6 m).  Using a door that someone left open closes it; then the leg's timeout recalls and the meta
starts over.

UB functions in expressions need VTank.PatchExpressionEngine, which Default turns on.
"""
import math, os, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..'))
import rectl
import vtmeta as m
import vtnav

ARGS = sys.argv[1:]
TEST = None
if ARGS[:1] == ['--test']:
    TEST, ARGS = ARGS[1], ARGS[2:]
VT = ARGS[0] if ARGS else rectl.vt_dir()
BO = 0x57660000  # Breach Zero landblock
ENTRY = (390.0, -80.0)  # where the Breach Zero portal and the Portal Recall Gem drop you
SIDE_OFFSET = 6.0
SIDE_DOOR = 2.3
SIDE_RADIUS = 4.5   # a player this close to a side spot has it
FIXED_RADIUS = 6.0  # players stood 2-5 m from the user's position at the fixed spots
ARRIVE = 3.0        # meters: "at" a checkpoint or spot
ATTACK_BAR = 0.3
# Use the nearest door if it's within 4 m (wobjectfindnearestdoor gives 0 when there is none)
USE_NEAR_DOOR = ('ifthen[getobjectinternaltype[wobjectfindnearestdoor[]]==7,'
                 '`ifthen[coordinatedistancewithz[getplayercoordinates[],'
                 'wobjectgetphysicscoordinates[wobjectfindnearestdoor[]]]<4,'
                 '\\`actiontryuseitem[wobjectfindnearestdoor[]]\\`]`]')

# Spots at each checkpoint, in route order.  'fixed' = the checkpoint itself is the spot; otherwise sides relative to
# the walking direction (dx, dy) into the checkpoint: L, R, S (straight on).
CHECKPOINTS = [
    ('LR', (0, -1)), ('LR', (0, -1)), ('LR', (0, -1)), ('L', (0, -1)), ('fixed', None),
    ('R', (0, -1)), ('LR', (1, 0)), ('LR', (1, 0)), ('fixed', None), ('LR', (1, 0)), ('RS', (1, 0)),
    ('fixed', None), ('LR', (0, 1)), ('LR', (0, 1)), ('LR', (0, 1)), ('R', (0, 1)), ('LS', (-1, 0)),
    ('RS', (0, 1)), ('fixed', None), ('RS', (-1, 0)), ('fixed', None),
]


def read_route():
    """The route spec split at '# checkpoint' lines: [(spec lines of the leg, checkpoint point)].  Each leg starts at
    the previous checkpoint."""
    legs, cur, last = [], [], None
    for line in open(os.path.join(HERE, 'aFBreach-route.spec')):
        line = line.strip()
        if line.startswith(('pt ', 'door ')):
            cur.append(line)
            if line.startswith('pt '):
                f = line.split()
                last = (int(f[1], 16), float(f[2]), float(f[3]), float(f[4]))
        elif line.startswith('# checkpoint'):
            legs.append((cur, last))
            cur = [cur[-1]] if cur[-1].startswith('pt ') else []
    return legs


def pt_line(p):
    cell, x, y, z = p
    return f'pt {cell:08X} {x:.2f} {y:.2f} {z:.2f}'


def nav(ew_ns):
    return vtnav.nav_coord(BO, ew_ns[0], ew_ns[1], 0)[:2]


def _dist2(we, n, ew, ns, r):
    """Squared flat distance in nav units (no sqrt) < r^2.  The sign goes into the constant: "x--30.7" may not parse."""
    dx, dy = f'({we}{-ew:+.6f})', f'({n}{-ns:+.6f})'
    return f'{dx}*{dx}+{dy}*{dy}<{(r / 240) ** 2:.9f}'


def player_near(x, y, r):
    """True when this character is within r m (flat) of a dungeon position."""
    ew, ns = nav((x, y))
    we, n = 'coordinategetwe[getplayercoordinates[]]', 'coordinategetns[getplayercoordinates[]]'
    return _dist2(we, n, ew, ns, r)


def others_at(x, y, r):
    """Number of other players within r m (flat) of a dungeon position.  No "!=" in the language: the id difference
    is non-zero (true) for everyone but this character."""
    ew, ns = nav((x, y))
    we, n = 'coordinategetwe[wobjectgetphysicscoordinates[$1]]', 'coordinategetns[wobjectgetphysicscoordinates[$1]]'
    f = (_dist2(we, n, ew, ns, r) + '&&wobjectgetid[$1]-wobjectgetid[wobjectgetplayer[]]')
    return f'listcount[listfilter[wobjectfindallbyobjectclass[24],`{f}`]]'


def test_allows(spot):
    if TEST is None or TEST == 'side' and not spot['fixed']:
        return True
    return spot['name'] in TEST.split(',')


def free(spot):
    if not test_allows(spot):
        return m.never()
    return m.expr_c(others_at(spot['x'], spot['y'], spot['r']) + '<1')


def taken(spot):
    if not test_allows(spot):
        return m.always()
    return m.expr_c(others_at(spot['x'], spot['y'], spot['r']) + '>0')


def door_lines(ew, ns):
    """Spec lines that open the door nearest (ew, ns), within 1.5 m."""
    we, n = 'coordinategetwe[wobjectgetphysicscoordinates[$1]]', 'coordinategetns[wobjectgetphysicscoordinates[$1]]'
    near = _dist2(we, n, ew, ns, 1.5)
    return [f'chat /ub mexec actiontryuseitem[listgetitem[listfilter[wobjectfindallbyobjectclass[26],`{near}`],0]]',
            'pause 1500']


def save_nav(dirname, name, lines, route_type='once'):
    """Writes a nav from spec lines.  A door line also gets a waypoint 1 m in front of the door (on the side of the
    previous point): the recorded point before a door can be 3-4 m short of it, and the use didn't go through there."""
    spec, prev = [f'type {route_type}'], None
    for line in lines:
        if line.startswith('door '):
            ew, ns = map(float, line.split()[1:3])
            if prev:
                cell, px, py, pz = prev
                lbx, lby = (cell >> 24) & 0xFF, (cell >> 16) & 0xFF
                dx, dy = ew * 240 - (lbx - 127) * 192 + 84, ns * 240 - (lby - 127) * 192 + 84
                d = math.hypot(px - dx, py - dy)
                if d > 1.0:
                    spec.append(pt_line((cell, dx + (px - dx) / d, dy + (py - dy) / d, pz)))
            spec += door_lines(ew, ns)
        else:
            spec.append(line)
            f = line.split()
            prev = (int(f[1], 16), float(f[2]), float(f[3]), float(f[4]))
    path = os.path.join(dirname, name)
    vtnav.build_spec('\n'.join(spec)).save(path)
    return path


def build_rules(navdir):
    goto = vtnav.build_spec(open(os.path.join(HERE, 'aFBreach-goto.spec')).read())
    goto_path = os.path.join(navdir, 'aFBreach-goto.nav')
    goto.save(goto_path)

    legs = read_route()
    assert len(legs) == len(CHECKPOINTS), (len(legs), len(CHECKPOINTS))

    # Spots: name, position, radius, where to walk in from (the checkpoint)
    checkpoints = []
    for k, ((kind, fwd), (leg, cp)) in enumerate(zip(CHECKPOINTS, legs), 1):
        cell, cx, cy, cz = cp
        spots = []
        if kind == 'fixed':
            spots.append({'name': f'{k:02d}', 'x': cx, 'y': cy, 'r': FIXED_RADIUS, 'fixed': True})
        else:
            fx, fy = fwd
            vec = {'L': (-fy, fx), 'R': (fy, -fx), 'S': (fx, fy)}
            for side in kind:
                vx, vy = vec[side]
                ew, ns, z = vtnav.nav_coord(cell, cx + vx * SIDE_DOOR, cy + vy * SIDE_DOOR, cz)
                spots.append({'name': f'{k:02d}{side}', 'x': cx + vx * SIDE_OFFSET, 'y': cy + vy * SIDE_OFFSET,
                              'r': SIDE_RADIUS, 'fixed': False, 'door': f'door {ew!r} {ns!r} {z!r}'})
        for s in spots:
            s['pt'] = (cell, s['x'], s['y'], cz)
        checkpoints.append({'k': k, 'pt': cp, 'leg': leg, 'spots': spots})
    all_spots = [s for c in checkpoints for s in c["spots"] if test_allows(s)]  # a test build only resumes its spots

    ex, ey = ENTRY
    at_entry = m.expr_c(player_near(ex, ey, 5))
    in_bo = m.landblock(BO)

    def died(state):
        return (state, m.died(), m.all_a(m.clear_watchdog(), m.set_state('Dead')))

    def moving(name, nav_path, arrived, then, timeout):
        """Travel with a nav, opening doors that are in the way.  <name> loads the nav and goes to <name>Mv, which
        doesn't reload it.  <name>Mv's watchdog fires when he moves less than 2 m in 8 s (a door that was already
        open and got closed by our use, or one that didn't open): <name>Stuck uses the nearest door within 4 m and goes
        back to <name>Mv, and VT carries on along the same nav.  After 5 tries, recall."""
        mv, stuck = f'{name}Mv', f'{name}Stuck'
        leave = lambda st: m.all_a(m.clear_watchdog(), m.set_state(st))
        return [
            (name, m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True),
                                       m.expr_a('setvar[faStuck,0]'), m.embed_nav(nav_path), m.set_state(mv))),
            (mv, m.always(), m.watchdog(stuck, 2, 8)),
            (mv, arrived, leave(then)),
            (mv, m.seconds_in_state(timeout), leave('Recall')),  # moving but not getting there
            died(mv),
            (stuck, m.always(), m.all_a(m.clear_watchdog(), m.expr_a('setvar[faStuck,getvar[faStuck]+1]'),
                                        m.expr_a(USE_NEAR_DOOR))),
            (stuck, m.all_c(m.seconds_in_state(2), m.expr_c('getvar[faStuck]<5')), m.set_state(mv)),
            (stuck, m.all_c(m.seconds_in_state(2), m.expr_c('getvar[faStuck]>4')), m.set_state('Recall')),
            died(stuck),
        ]

    rules = [
        # Settings first: the Farm profile turns combat and nav on, so keep nav off until it has loaded
        ('Default', m.always(), m.all_a(m.clear_watchdog(), m.opt('EnableNav', False), m.chat('/ub opt set VTank.PatchExpressionEngine true'),
                                        m.chat('/vt settings loadchar Farm'))),
        ('Default', m.seconds_in_state(1), m.opt('EnableNav', False)),
        ('Default', m.all_c(m.seconds_in_state(2), m.not_c(in_bo)), m.set_state('GoToBO')),
        ('Default', m.all_c(m.seconds_in_state(2), in_bo, at_entry), m.set_state('Walk01')),
    ]
    # Restarted in a spot: farm it again
    for s in all_spots:
        rules.append(('Default', m.all_c(m.seconds_in_state(2), m.expr_c(player_near(s['x'], s['y'], ARRIVE))),
                      m.set_state(f'Farm{s["name"]}')))
    rules.append(('Default', m.all_c(m.seconds_in_state(2), in_bo, m.not_c(at_entry),
                                     *[m.not_c(m.expr_c(player_near(s['x'], s['y'], ARRIVE))) for s in all_spots]),
                  m.set_state('Recall')))

    # To the dungeon entrance, not fighting
    rules += [
        ('GoToBO', m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True),
                                       m.embed_nav(goto_path))),
        ('GoToBO', m.all_c(in_bo, at_entry), m.set_state('Walk01')),
        ('GoToBO', m.seconds_in_state(180), m.set_state('Default')),  # stuck: start over
        died('GoToBO'),
    ]

    for i, c in enumerate(checkpoints):
        k, (cell, cx, cy, cz) = c['k'], c['pt']
        walk, check = f'Walk{k:02d}', f'Check{k:02d}'
        nxt = f'Walk{k + 1:02d}' if i + 1 < len(checkpoints) else 'Recall'
        leg = save_nav(navdir, f'aFBreach-leg{k:02d}.nav', c['leg'])
        rules += moving(walk, leg, m.expr_c(player_near(cx, cy, ARRIVE)), check, 90)
        # Look at each spot in order; give the client 2 s to see who's there.  Conditions are exclusive.
        taken_before = []
        for s in c['spots']:
            target = f'Farm{s["name"]}' if s['fixed'] else f'Go{s["name"]}'
            rules.append((check, m.all_c(m.seconds_in_state(2), *taken_before, free(s)), m.set_state(target)))
            taken_before.append(taken(s))
        rules.append((check, m.all_c(m.seconds_in_state(2), *taken_before), m.set_state(nxt)))
        rules.append(died(check))

        for s in c['spots']:
            farm = f'Farm{s["name"]}'
            if not s['fixed']:
                go = f'Go{s["name"]}'
                gonav = save_nav(navdir, f'aFBreach-go{s["name"]}.nav', [pt_line(c['pt']), s['door'], pt_line(s['pt'])])
                rules += moving(go, gonav, m.expr_c(player_near(s['x'], s['y'], ARRIVE)), farm, 30)
            hold = save_nav(navdir, f'aFBreach-hold{s["name"]}.nav', [pt_line(s['pt'])], route_type='circular')
            rules += [
                (farm, m.always(), m.all_a(m.embed_nav(hold), m.opt('EnableNav', True), m.opt('EnableCombat', True),
                                           m.chat(f'/vt setattackbar {ATTACK_BAR}'))),
                (farm, m.all_c(m.seconds_in_state(3), m.expr_c('getfellowshipcount[]==1')), m.chat('/ub fellow quit')),
                died(farm),
                (farm, m.not_c(in_bo), m.set_state('Default')),  # left the dungeon: go back
            ]

    # Every spot taken (or stuck): back to the entrance and look again
    rules += [
        ('Recall', m.always(), m.all_a(m.opt('EnableNav', False), m.opt('EnableCombat', False))),
        ('Recall', m.seconds_in_state(2), m.chat('/ub usei Portal Recall Gem')),
        ('Recall', m.all_c(m.seconds_in_state(5), in_bo, at_entry), m.set_state('Walk01')),
        ('Recall', m.seconds_in_state(25), m.set_state('Default')),  # didn't recall: Default sorts it out
        died('Recall'),
        ('Dead', m.seconds_in_state(20), m.set_state('Default')),
    ]
    return rules


if __name__ == '__main__':
    out = os.path.join(VT, 'aFBreach-test.met' if TEST else 'aFBreach.met')
    with tempfile.TemporaryDirectory() as navdir:
        rules = build_rules(navdir)
        data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}')
