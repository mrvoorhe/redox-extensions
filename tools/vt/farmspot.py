"""Farm-spot metas: go to a dungeon, walk a route past checkpoints, farm at the first spot without another player.

    py tools/vt/farmspot.py route <recording dir> <Name> [--out-dir <dir>] [--min-move 4]
    py tools/vt/farmspot.py doors <Char> <Name>-doors.spec <landblock hex> [--seconds 600]

route: turns a vtrecord.py recording with the user's chat markers into <Name>-goto.spec and <Name>-route.spec
(default: next to the recording) for a meta script like examples/aFBreach.py.  Markers (any chat channel):

    <x> - begin / <x> - end      travel to the dungeon (the first begin/end pair that isn't findspot)
    findspot - begin / end       the search route, from the dungeon entrance
    spot - left / right          a checkpoint with spots on both sides of the hallway
    spot - left, spot - right,
    spot - straight              one side, or ahead; several within 3 m of each other make one checkpoint (RS, LS...)
    spot fixed / spot - fixed    the spot is where the character stands

Each checkpoint becomes "# checkpoint N <kind> [dx dy]" in the route spec, after a point where the character
stood when the marker was sent.  <kind> is fixed, or letters L R S; dx dy is the walking direction into the
checkpoint (snapped to an axis when within 25 degrees), which says where left, right and straight are.  Door uses
(object class 26) become "door <ew> <ns> <z>" lines, which the meta turns into a UB use of that door.  Read both
specs before building: fix typos the parser missed, drop points that aren't part of the route.

doors: while the character walks the whole route (e.g. a --test none build), records every door the client sees.
Spots behind a door then use the measured door instead of an estimate: the side doors are 2.1-2.6 m from the
hallway centre in Breach Zero, but a door straight ahead was 4.7 m.  Stop it early by creating <spec>.stop.

As a library (see examples/aFBreach.py):

    import farmspot
    D = farmspot.Dungeon(name='aFBreach', landblock=0x57660000, here=os.path.dirname(__file__))
    farmspot.main(D, sys.argv[1:])     # [--test <spots>] [<VT folder>]: writes <name>.met or <name>-test.met

The meta:
  Default    load the settings profile; travel there (GoTo), start the search at the entrance (Walk01), farm again
             if standing on a spot, otherwise recall.
  GoTo       the goto nav, combat off.  At the entrance -> Walk01.
  WalkNN     the leg to checkpoint NN, combat off.  Arrived -> CheckNN.
  CheckNN    after 2 s (the client has loaded who's there), the first spot without another player wins: GoNN<side>
             (walk in, through its door) or, for a fixed spot, FarmNN.  All taken -> the next walk, or Recall after
             the last checkpoint.
  Farm<spot> one-point circular nav on the spot, nav and combat on, attack bar; quits a fellowship it's alone in.
             With fellow_invite set, in a fellowship: /fship landblock 5 s in, then every fellow_invite seconds
             (stopwatch fsInviteSW), to recruit players who come into the area.
  Recall     the recall gem; at the entrance -> Walk01.
  Dead       20 s, then Default (which travels back).
Travel states have a watchdog: moving less than 2 m in 8 s (a door that someone left open and our use closed)
uses the nearest door within 4 m and carries on along the same nav; after 5 tries, Recall.
UB functions in expressions need VTank.PatchExpressionEngine, which Default turns on.
"""
import argparse, dataclasses, math, os, re, sys, tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import vtmeta as m
import vtnav
from vtnav import fmt

# Use the nearest door if it's within 4 m (wobjectfindnearestdoor gives 0 when there is none)
USE_NEAR_DOOR = ('ifthen[getobjectinternaltype[wobjectfindnearestdoor[]]==7,'
                 '`ifthen[coordinatedistancewithz[getplayercoordinates[],'
                 'wobjectgetphysicscoordinates[wobjectfindnearestdoor[]]]<4,'
                 '\\`actiontryuseitem[wobjectfindnearestdoor[]]\\`]`]')


@dataclasses.dataclass
class Dungeon:
    name: str                    # meta name; specs are <here>/<name>-goto.spec and <here>/<name>-route.spec
    landblock: int               # the dungeon's landblock in the high word, e.g. 0x57660000
    here: str                    # folder with the specs
    entry: tuple = None          # (x, y) where the portal and the recall drop you; default: the route's first point
    entry_radius: float = 5.0
    profile: str = 'Farm'        # /vt settings loadchar <profile>
    recall: str = '/ub usei Portal Recall Gem'
    side_offset: float = 6.0     # side spot: meters from the checkpoint (hallway centre)
    side_door: float = 2.3       # side spot's door: meters from the checkpoint; None when side spots have no door
    side_radius: float = 4.5     # another player this close to a side spot has it
    fixed_radius: float = 6.0    # ... to a fixed spot
    arrive: float = 3.0          # meters: "at" a checkpoint or spot
    attack_bar: float = None     # /vt setattackbar <value> at each spot; None leaves the profile's attack power
    goto_timeout: int = 180
    walk_timeout: int = 90
    go_timeout: int = 30
    fellow_invite: int = None    # seconds: /fship landblock this often while farming in a fellowship; None: never
    doors: list = None           # measured doors [(ew, ns, z)]; default: <here>/<name>-doors.spec if it exists

    def __post_init__(self):
        if self.doors is None:
            self.doors = read_doors(self.spec('doors'))

    def spec(self, part):
        return os.path.join(self.here, f'{self.name}-{part}.spec')


# ---------------------------------------------------------------- route spec

def pt_line(p):
    cell, x, y, z = p
    return f'pt {cell:08X} {x:.2f} {y:.2f} {z:.2f}'


def game_xy(cell, ew, ns):
    """Nav (ew, ns) to the local x, y of the landblock of `cell` (the inverse of vtnav.nav_coord)."""
    lbx, lby = (cell >> 24) & 0xFF, (cell >> 16) & 0xFF
    return ew * 240 - (lbx - 127) * 192 + 84, ns * 240 - (lby - 127) * 192 + 84


def parse_pt(line):
    f = line.split()
    return int(f[1], 16), float(f[2]), float(f[3]), float(f[4])


def read_route(path):
    """The route spec split at checkpoint lines: [{'k', 'kind', 'fwd', 'pt', 'leg'}].  'leg' is the spec lines from
    the previous checkpoint's point to this one; 'pt' is the last point before the checkpoint line."""
    cps, cur, last = [], [], None
    with open(path, encoding='utf-8') as f:
        text = f.read()
    for raw in text.splitlines():
        line = raw.split('    #')[0].strip()  # trailing comments as vtrecord writes them
        if line.startswith('# checkpoint'):
            f = line.split()
            kind = f[3] if len(f) > 3 else None
            try:
                fwd = (float(f[4]), float(f[5])) if len(f) > 5 else None
            except ValueError:
                fwd = None
            if kind is None or kind != 'fixed' and fwd is None:
                raise ValueError(f'{path}: checkpoint needs a kind and, unless fixed, a direction: {raw.strip()}')
            cps.append({'k': len(cps) + 1, 'kind': kind, 'fwd': fwd, 'pt': last, 'leg': cur})
            cur = [pt_line(last)]
        elif line and not line.startswith('#') and not line.startswith('type '):
            cur.append(line)
            if line.startswith('pt '):
                last = parse_pt(line)
    return cps


# ---------------------------------------------------------------- expressions

def _dist2(we, n, ew, ns, r):
    """Squared flat distance in nav units (no sqrt) < r^2.  The sign goes into the constant: "x--30.7" may not parse."""
    dx, dy = f'({we}{-ew:+.6f})', f'({n}{-ns:+.6f})'
    return f'{dx}*{dx}+{dy}*{dy}<{(r / 240) ** 2:.9f}'


ME = ('coordinategetwe[getplayercoordinates[]]', 'coordinategetns[getplayercoordinates[]]')
OBJ = ('coordinategetwe[wobjectgetphysicscoordinates[$1]]', 'coordinategetns[wobjectgetphysicscoordinates[$1]]')


def player_near(lb, x, y, r):
    """True when this character is within r m (flat) of a position in landblock lb."""
    ew, ns = vtnav.nav_coord(lb, x, y, 0)[:2]
    return _dist2(*ME, ew, ns, r)


def others_at(lb, x, y, r):
    """Number of other players within r m (flat) of a position.  No "!=" in the language: the id difference is
    non-zero (true) for everyone but this character."""
    ew, ns = vtnav.nav_coord(lb, x, y, 0)[:2]
    f = _dist2(*OBJ, ew, ns, r) + '&&wobjectgetid[$1]-wobjectgetid[wobjectgetplayer[]]'
    return f'listcount[listfilter[wobjectfindallbyobjectclass[24],`{f}`]]'


def door_lines(ew, ns):
    """Spec lines that open the door nearest (ew, ns), within 1.5 m.  A nav use node doesn't work on doors: VT keeps
    using the door until something happens, so it opens and closes for ever."""
    near = _dist2(*OBJ, ew, ns, 1.5)
    return [f'chat /ub mexec actiontryuseitem[listgetitem[listfilter[wobjectfindallbyobjectclass[26],`{near}`],0]]',
            'pause 1500']


def save_nav(dirname, name, lines, route_type='once'):
    """Writes a nav from spec lines, expanding door lines.  A door also gets a waypoint 1 m in front of it (on the side
    of the previous point): the recorded point before a door can be 3-4 m short of it, too far for the use."""
    spec, prev = [f'type {route_type}'], None
    for line in lines:
        if line.startswith('door '):
            ew, ns = map(float, line.split()[1:3])
            if prev:
                cell, px, py, pz = prev
                dx, dy = game_xy(cell, ew, ns)
                d = math.hypot(px - dx, py - dy)
                if d > 1.0:
                    spec.append(pt_line((cell, dx + (px - dx) / d, dy + (py - dy) / d, pz)))
            spec += door_lines(ew, ns)
        else:
            spec.append(line)
            if line.startswith('pt '):
                prev = parse_pt(line)
    path = os.path.join(dirname, name)
    vtnav.build_spec('\n'.join(spec)).save(path)
    return path


# ---------------------------------------------------------------- meta

def read_doors(path):
    """Measured doors from a doors spec ("door <ew> <ns> <z>" lines, from `farmspot.py doors`): [(ew, ns, z)]."""
    if not os.path.exists(path):
        return []
    with open(path, encoding='utf-8') as f:
        return [tuple(map(float, l.split()[1:4])) for l in f if l.startswith('door ')]


def side_door(d, cell, cx, cy, vx, vy):
    """The measured door in direction (vx, vy) from the checkpoint: the nearest one between 0.5 m and side_offset
    out, within 1.5 m of that line.  Returns (distance out, ew, ns, z) or None."""
    best = None
    for ew, ns, z in d.doors:
        x, y = game_xy(cell, ew, ns)
        t = (x - cx) * vx + (y - cy) * vy
        off = abs((x - cx) * vy - (y - cy) * vx)
        if 0.5 < t < d.side_offset and off < 1.5 and (best is None or t < best[0]):
            best = (t, ew, ns, z)
    return best


def spots_of(d, cp):
    """The spots at a checkpoint: name, position, radius, and for a side spot the door line (or None).  With measured
    doors, a side spot behind a door is side_offset - side_door past it (as with the estimate), however far out the
    door is; without them, the door is estimated side_door out."""
    k, (cell, cx, cy, cz) = cp['k'], cp['pt']
    if cp['kind'] == 'fixed':
        return [{'name': f'{k:02d}', 'x': cx, 'y': cy, 'r': d.fixed_radius, 'fixed': True, 'pt': cp['pt']}]
    fx, fy = cp['fwd']
    n = math.hypot(fx, fy)
    fx, fy = fx / n, fy / n
    vec = {'L': (-fy, fx), 'R': (fy, -fx), 'S': (fx, fy)}
    spots = []
    for side in cp['kind']:
        vx, vy = vec[side]
        door, out = None, d.side_offset
        found = side_door(d, cell, cx, cy, vx, vy) if d.doors else None
        if found:
            t, ew, ns, z = found
            door = f'door {ew!r} {ns!r} {z!r}'
            out = t + d.side_offset - (d.side_door or 0)
        elif d.side_door is not None:
            if d.doors:
                print(f'note: spot {k:02d}{side}: no measured door, using the estimate')
            ew, ns, z = vtnav.nav_coord(cell, cx + vx * d.side_door, cy + vy * d.side_door, cz)
            door = f'door {ew!r} {ns!r} {z!r}'
        x, y = cx + vx * out, cy + vy * out
        spots.append({'name': f'{k:02d}{side}', 'x': x, 'y': y, 'r': d.side_radius, 'fixed': False, 'door': door,
                      'pt': (cell, x, y, cz)})
    return spots


def build_rules(d, navdir, test=None):
    """test: None, or the spots that may count as free ('none', 'side', or names like 10R,17S); every other spot
    counts as taken, to test the skip and recall paths while players decide which spots are free.

    test 'tour' visits every spot in route order, to check each can be reached: walk in, wait 3 s with combat off
    (TourAt<spot>), recall, go for the next, whether or not another player is there.  A spot it
    can't reach is TourFail<spot>.  The variable fsTour holds the number of the next spot; TourDone at the end.
    Clear it (/ub mexec clearvar[fsTour]) to start a tour over."""
    lb = d.landblock
    tour = test == 'tour'

    def allowed(s):
        return test in (None, 'tour') or test == 'side' and not s['fixed'] or s['name'] in test.split(',')

    def free(s):
        return m.expr_c(others_at(lb, s['x'], s['y'], s['r']) + '<1') if allowed(s) else m.never()

    def taken(s):
        return m.expr_c(others_at(lb, s['x'], s['y'], s['r']) + '>0') if allowed(s) else m.always()

    goto_path = os.path.join(navdir, f'{d.name}-goto.nav')
    with open(d.spec('goto'), encoding='utf-8') as f:
        vtnav.build_spec(f.read()).save(goto_path)

    cps = read_route(d.spec('route'))
    for cp in cps:
        cp['spots'] = spots_of(d, cp)
    all_spots = [s for cp in cps for s in cp['spots'] if allowed(s)]  # a test build only resumes its spots
    for n, s in enumerate(all_spots, 1):
        s['n'] = n
    resume = [] if tour else all_spots  # a tour doesn't resume: it recalls and goes for the next spot

    if d.entry:
        ex, ey = d.entry
    else:
        first = next(parse_pt(l) for l in cps[0]['leg'] if l.startswith('pt '))
        ex, ey = first[1], first[2]
    at_entry = m.expr_c(player_near(lb, ex, ey, d.entry_radius))
    there = m.landblock(lb)

    def at(x, y):
        return m.expr_c(player_near(lb, x, y, d.arrive))

    def died(state):
        return (state, m.died(), m.all_a(m.clear_watchdog(), m.set_state('Dead')))

    def moving(name, nav_path, arrived, then, timeout, fail='Recall'):
        """Travel with a nav, opening doors that are in the way.  <name> loads the nav and goes to <name>Mv, which
        doesn't reload it.  <name>Mv's watchdog fires when he moves less than 2 m in 8 s: <name>Stuck uses the
        nearest door within 4 m and goes back to <name>Mv, and VT carries on along the same nav.  After 5 tries,
        recall."""
        mv, stuck = f'{name}Mv', f'{name}Stuck'
        leave = lambda st: m.all_a(m.clear_watchdog(), m.set_state(st))
        return [
            (name, m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True),
                                       m.expr_a('setvar[faStuck,0]'), m.embed_nav(nav_path), m.set_state(mv))),
            (mv, m.always(), m.watchdog(stuck, 2, 8)),
            (mv, arrived, leave(then)),
            (mv, m.seconds_in_state(timeout), leave(fail)),  # moving but not getting there
            died(mv),
            (stuck, m.always(), m.all_a(m.clear_watchdog(), m.expr_a('setvar[faStuck,getvar[faStuck]+1]'),
                                        m.expr_a(USE_NEAR_DOOR))),
            (stuck, m.all_c(m.seconds_in_state(2), m.expr_c('getvar[faStuck]<5')), m.set_state(mv)),
            (stuck, m.all_c(m.seconds_in_state(2), m.expr_c('getvar[faStuck]>4')), m.set_state(fail)),
            died(stuck),
        ]

    rules = [
        # Settings first: the profile may turn combat and nav on, so keep nav off until it has loaded
        ('Default', m.always(), m.all_a(m.clear_watchdog(), m.opt('EnableNav', False),
                                        m.chat('/ub opt set VTank.PatchExpressionEngine true'),
                                        m.chat(f'/vt settings loadchar {d.profile}'),
                                        *([m.expr_a('ifthen[testvar[fsTour]==0,`setvar[fsTour,1]`]')] if tour else []))),
        ('Default', m.seconds_in_state(1), m.opt('EnableNav', False)),
        ('Default', m.all_c(m.seconds_in_state(2), m.not_c(there)), m.set_state('GoTo')),
        ('Default', m.all_c(m.seconds_in_state(2), there, at_entry), m.set_state('Walk01')),
    ]
    # Restarted in a spot: farm it again
    for s in resume:
        rules.append(('Default', m.all_c(m.seconds_in_state(2), at(s['x'], s['y'])), m.set_state(f'Farm{s["name"]}')))
    rules.append(('Default', m.all_c(m.seconds_in_state(2), there, m.not_c(at_entry),
                                     *[m.not_c(at(s['x'], s['y'])) for s in resume]),
                  m.set_state('Recall')))

    # To the dungeon entrance, not fighting
    rules += [
        ('GoTo', m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True), m.embed_nav(goto_path))),
        ('GoTo', m.all_c(there, at_entry), m.set_state('Walk01')),
        ('GoTo', m.seconds_in_state(d.goto_timeout), m.set_state('Default')),  # stuck: start over
        died('GoTo'),
    ]

    for i, cp in enumerate(cps):
        k, (cell, cx, cy, cz) = cp['k'], cp['pt']
        walk, check = f'Walk{k:02d}', f'Check{k:02d}'
        nxt = f'Walk{k + 1:02d}' if i + 1 < len(cps) else ('TourDone' if tour else 'Recall')
        leg = save_nav(navdir, f'{d.name}-leg{k:02d}.nav', cp['leg'])
        rules += moving(walk, leg, at(cx, cy), check, d.walk_timeout)
        if tour:
            # Only the spot whose number is in fsTour, whoever is there (reachability, not who holds it); past
            # this checkpoint's spots, walk on
            for s in cp['spots']:
                target = f'Farm{s["name"]}' if s['fixed'] else f'Go{s["name"]}'
                mine = m.expr_c(f'getvar[fsTour]=={s["n"]}')
                rules.append((check, m.all_c(m.seconds_in_state(2), mine), m.set_state(target)))
            last = max(s['n'] for s in cp['spots'])
            rules.append((check, m.all_c(m.seconds_in_state(2), m.expr_c(f'getvar[fsTour]>{last}')), m.set_state(nxt)))
        else:
            # Look at each spot in order; give the client 2 s to see who's there.  Conditions are exclusive.
            taken_before = []
            for s in cp['spots']:
                target = f'Farm{s["name"]}' if s['fixed'] else f'Go{s["name"]}'
                rules.append((check, m.all_c(m.seconds_in_state(2), *taken_before, free(s)), m.set_state(target)))
                taken_before.append(taken(s))
            rules.append((check, m.all_c(m.seconds_in_state(2), *taken_before), m.set_state(nxt)))
        rules.append(died(check))

        for s in cp['spots']:
            farm = f'Farm{s["name"]}'
            if not s['fixed']:
                lines = [pt_line(cp['pt'])] + ([s['door']] if s['door'] else []) + [pt_line(s['pt'])]
                gonav = save_nav(navdir, f'{d.name}-go{s["name"]}.nav', lines)
                rules += moving(f'Go{s["name"]}', gonav, at(s['x'], s['y']), farm, d.go_timeout,
                                fail=f'TourFail{s["name"]}' if tour else 'Recall')
            if tour:
                done = 'TourDone' if s['n'] == len(all_spots) else 'Recall'
                at_s, fail_s = f'TourAt{s["name"]}', f'TourFail{s["name"]}'
                rules += [
                    (farm, m.always(), m.set_state(at_s)),
                    (at_s, m.always(), m.all_a(m.opt('EnableNav', False), m.opt('EnableCombat', False))),
                    (at_s, m.seconds_in_state(3), m.all_a(m.expr_a('setvar[fsTour,getvar[fsTour]+1]'),
                                                          m.set_state(done))),
                    died(at_s),
                    (fail_s, m.always(), m.all_a(m.opt('EnableNav', False),
                                                 m.expr_a('setvar[fsTour,getvar[fsTour]+1]'))),
                    (fail_s, m.seconds_in_state(1), m.set_state(done)),
                ]
                continue
            hold = save_nav(navdir, f'{d.name}-hold{s["name"]}.nav', [pt_line(s['pt'])], route_type='circular')
            start = [m.embed_nav(hold), m.opt('EnableNav', True), m.opt('EnableCombat', True)]
            if d.attack_bar is not None:
                start.append(m.chat(f'/vt setattackbar {d.attack_bar}'))
            rules += [
                (farm, m.always(), m.all_a(*start)),
                (farm, m.all_c(m.seconds_in_state(3), m.expr_c('getfellowshipcount[]==1')), m.chat('/ub fellow quit')),
                died(farm),
                (farm, m.not_c(there), m.set_state('Default')),  # left the dungeon: go back
            ]
            if d.fellow_invite:
                # Invite the landblock into the fellowship on arrival (after the alone-in-it quit at 3 s), then
                # every fellow_invite seconds.  Rules fire when their condition becomes true; restarting the
                # stopwatch makes the second one false again.
                invite = m.all_a(m.chat('/fship landblock'), m.expr_a('setvar[fsInviteSW,stopwatchstart[stopwatchcreate[]]]'))
                rules += [
                    (farm, m.all_c(m.seconds_in_state(5), m.expr_c('getfellowshipcount[]>1')), invite),
                    (farm, m.all_c(m.seconds_in_state(6), m.expr_c(
                        'getfellowshipcount[]>1&&ifthen[testvar[fsInviteSW],'
                        f'`stopwatchelapsedseconds[$fsInviteSW]>={d.fellow_invite}`,`1`]')), invite),
                ]

    # Every spot taken (or stuck): back to the entrance and look again
    rules += [
        ('Recall', m.always(), m.all_a(m.opt('EnableNav', False), m.opt('EnableCombat', False))),
        ('Recall', m.seconds_in_state(2), m.chat(d.recall)),
        ('Recall', m.all_c(m.seconds_in_state(5), there, at_entry), m.set_state('Walk01')),
        ('Recall', m.seconds_in_state(25), m.set_state('Default')),  # didn't recall: Default sorts it out
        died('Recall'),
        ('Dead', m.seconds_in_state(20), m.set_state('Default')),
    ]
    if tour:
        rules.append(('TourDone', m.always(), m.all_a(m.opt('EnableNav', False), m.opt('EnableCombat', False),
                                                      m.expr_a('clearvar[fsTour]'))))
    return rules


def main(d, argv):
    """[--test <spots>] [<VT folder>]: writes <name>.met (or <name>-test.met) to the VT folder."""
    test = None
    if argv[:1] == ['--test']:
        test, argv = argv[1], argv[2:]
    if argv:
        vt = argv[0]
    else:
        import rectl
        vt = rectl.vt_dir()
    out = os.path.join(vt, f'{d.name}-test.met' if test else f'{d.name}.met')
    with tempfile.TemporaryDirectory() as navdir:
        rules = build_rules(d, navdir, test)
        data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}')


# ---------------------------------------------------------------- recording -> specs

MARK = re.compile(r'"(.*?)"\s*$')
SIDE_WORDS = {'left': 'L', 'let': 'L', 'lft': 'L', 'right': 'R', 'rigth': 'R', 'rght': 'R',
              'straight': 'S', 'ahead': 'S', 'front': 'S'}


def markers(chat):
    """[(t, text)] of marker chat lines: the quoted text of lines the character said, lower case."""
    out = []
    for c in chat:
        txt = c.get('text', '')
        mm = MARK.search(txt)
        if 'You say' in txt and mm:
            out.append((c['t'], mm.group(1).strip().lower()))
    return out


def parse_spot(text):
    """'fixed', a string of L/R/S, or None when the text isn't a spot marker."""
    if not text.startswith('spot'):
        return None
    words = re.findall(r'[a-z]+', text[4:])
    if 'fixed' in words:
        return 'fixed'
    sides = ''.join(SIDE_WORDS[w] for w in words if w in SIDE_WORDS)
    return sides or None


def heading_into(samples, t, back=1.5):
    """Walking direction (dx, dy) into the position at time t: from the last sample at least `back` m before it."""
    if not samples:
        return None
    here = None
    for s in reversed(samples):
        if s['t'] > t:
            continue
        if here is None:
            here = s
            continue
        if s['cell'][:4] == here['cell'][:4] and math.hypot(here['x'] - s['x'], here['y'] - s['y']) >= back:
            dx, dy = here['x'] - s['x'], here['y'] - s['y']
            n = math.hypot(dx, dy)
            dx, dy = dx / n, dy / n
            for ax in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if dx * ax[0] + dy * ax[1] > math.cos(math.radians(25)):
                    return ax
            return round(dx, 2), round(dy, 2)
    return None


def route_specs(positions, actions, chat, min_move=4.0):
    """(goto spec lines, route spec lines, notes) from a marked recording."""
    import vtrecord
    marks = markers(chat)
    notes = []

    def span(pred):
        b = next((t for t, x in marks if x.endswith('- begin') and pred(x)), None)
        e = next((t for t, x in marks if x.endswith('- end') and pred(x) and b is not None and t > b), None)
        return b, e

    gb, ge = span(lambda x: not x.startswith('findspot'))
    fb, fe = span(lambda x: x.startswith('findspot'))
    if fb is None or fe is None:
        raise ValueError('no "findspot - begin" ... "findspot - end" markers in the chat log')

    def within(rows, a, b):
        return [r for r in rows if a <= r['t'] <= b]

    goto = []
    if gb is not None and ge is not None:
        # Until the first position in the dungeon after the end marker, or the end marker
        goto = [l for l in vtrecord.build_spec(within(positions, gb, ge), within(actions, gb, ge), min_move)
                if not l.endswith('# start')]
    else:
        notes.append('no travel markers: write the goto spec by hand')

    # Checkpoints: spot markers, merged when the character hardly moved between them
    cps = []
    for t, x in marks:
        if not fb < t < fe:
            continue
        kind = parse_spot(x)
        if kind is None:
            if x.startswith('spot'):
                notes.append(f'not understood, skipped: "{x}"')
            continue
        pos = next((p for p in reversed(positions) if p['t'] <= t), None)
        if cps and kind != 'fixed' and cps[-1]['kind'] != 'fixed' and \
                math.hypot(pos['x'] - cps[-1]['pos']['x'], pos['y'] - cps[-1]['pos']['y']) < 3:
            cps[-1]['kind'] += ''.join(c for c in kind if c not in cps[-1]['kind'])
            continue
        cps.append({'t': t, 'kind': kind, 'pos': pos, 'fwd': heading_into(positions, t)})

    route, prev_t = [], fb
    for i, cp in enumerate(cps, 1):
        seg = vtrecord.build_spec(within(positions, prev_t, cp['t']), within(actions, prev_t, cp['t']), min_move)
        if route:  # the segment's first point is the previous checkpoint's
            seg = [l for l in seg if not l.endswith('# start')]
        route += [_door(l) for l in seg]
        p = cp['pos']
        pt = (int(p['cell'], 16), p['x'], p['y'], p['z'])
        if pt_line(pt) != (route[-1].split('    #')[0] if route else ''):
            route.append(pt_line(pt))
        if cp['kind'] == 'fixed':
            route.append(f'# checkpoint {i} fixed')
        elif cp['fwd'] is None:
            route.append(f'# checkpoint {i} {cp["kind"]} ? ?')
            notes.append(f'checkpoint {i}: no walking direction, fill in dx dy')
        else:
            route.append(f'# checkpoint {i} {cp["kind"]} {cp["fwd"][0]:g} {cp["fwd"][1]:g}')
        prev_t = cp['t']
    return goto, route, notes


def _door(line):
    """'use 26 <ew> <ns> <z> Door' -> 'door <ew> <ns> <z>'."""
    f = line.split()
    if f[:2] == ['use', '26']:
        return f'door {f[2]} {f[3]} {f[4]}'
    return line


def collect_doors(char, path, landblock, seconds, interval=2.0):
    """Adds every door the client sees in the landblock to the doors spec at `path` (keeping what's there), polling
    while the character walks, until `seconds` pass or <path>.stop exists."""
    import time, rectl
    doors = {}
    if os.path.exists(path):
        with open(path, encoding='utf-8') as f:
            for l in f:
                if l.startswith('door '):
                    doors[int(l.split('#')[1])] = tuple(map(float, l.split()[1:4]))
    t0, n0 = time.time(), len(doors)
    while time.time() - t0 < seconds and not os.path.exists(path + '.stop'):
        try:
            if rectl.landcell(rectl.state(char)) & 0xFFFF0000 == landblock:
                for o in rectl.run(char, 'world', '--class', 'Door', '--near', 1000, '--limit', 1000).get('objects', []):
                    if o['id'] not in doors:
                        doors[o['id']] = tuple(o['nav'])
                        print(f'{len(doors)} doors (new: {o["id"]})', flush=True)
        except Exception as e:  # portal loads etc.
            print('error:', e, flush=True)
        with open(path, 'w', encoding='utf-8') as f:
            f.write('# Doors measured with farmspot.py doors; spots behind a door use the nearest one\n')
            for i, (ew, ns, z) in sorted(doors.items()):
                f.write(f'door {fmt(ew)} {fmt(ns)} {fmt(z)}    #{i}\n')
        time.sleep(interval)
    if os.path.exists(path + '.stop'):
        os.remove(path + '.stop')
    print(f'{len(doors)} doors ({len(doors) - n0} new) in {path}')


def cli(argv):
    import vtrecord
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    r = sub.add_parser('route')
    r.add_argument('dir')
    r.add_argument('name')
    r.add_argument('--out-dir')
    r.add_argument('--min-move', type=float, default=4.0)
    dr = sub.add_parser('doors')
    dr.add_argument('char')
    dr.add_argument('spec', help='<Name>-doors.spec to write (doors already in it are kept)')
    dr.add_argument('landblock', help='hex, e.g. 57660000')
    dr.add_argument('--seconds', type=float, default=600)
    a = ap.parse_args(argv)
    if a.cmd == 'doors':
        collect_doors(a.char, a.spec, int(a.landblock, 16), a.seconds)
        return
    rd = lambda f: vtrecord.read_jsonl(os.path.join(a.dir, f))
    goto, route, notes = route_specs(rd('positions.jsonl'), rd('actions.jsonl'), rd('chat.jsonl'), a.min_move)
    out = a.out_dir or a.dir
    for part, lines, head in (('goto', goto, []),
                              ('route', route, [f'# Search route from the entrance; "# checkpoint N <spots> [dx dy]" lines are '
                                                f'where {a.name} looks for free spots'])):
        path = os.path.join(out, f'{a.name}-{part}.spec')
        with open(path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(['type once'] + head + lines) + '\n')
        print(f'wrote {len(lines)} lines to {path}')
    print(f'{sum(l.startswith("# checkpoint") for l in route)} checkpoints')
    for n in notes:
        print('note:', n)


if __name__ == '__main__':
    cli(sys.argv[1:])
