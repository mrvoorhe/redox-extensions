"""Farm-spot metas: go to a dungeon, walk a route past checkpoints, farm at the first spot without another player.

    py tools/vt/farmspot.py route <recording dir> <Name> [--out-dir <dir>] [--min-move 4]

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
    attack_bar: float = 0.3
    goto_timeout: int = 180
    walk_timeout: int = 90
    go_timeout: int = 30

    def spec(self, part):
        return os.path.join(self.here, f'{self.name}-{part}.spec')


# ---------------------------------------------------------------- route spec

def pt_line(p):
    cell, x, y, z = p
    return f'pt {cell:08X} {x:.2f} {y:.2f} {z:.2f}'


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
                lbx, lby = (cell >> 24) & 0xFF, (cell >> 16) & 0xFF
                dx, dy = ew * 240 - (lbx - 127) * 192 + 84, ns * 240 - (lby - 127) * 192 + 84
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

def spots_of(d, cp):
    """The spots at a checkpoint: name, position, radius, and for a side spot the door line (or None)."""
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
        door = None
        if d.side_door is not None:
            ew, ns, z = vtnav.nav_coord(cell, cx + vx * d.side_door, cy + vy * d.side_door, cz)
            door = f'door {ew!r} {ns!r} {z!r}'
        x, y = cx + vx * d.side_offset, cy + vy * d.side_offset
        spots.append({'name': f'{k:02d}{side}', 'x': x, 'y': y, 'r': d.side_radius, 'fixed': False, 'door': door,
                      'pt': (cell, x, y, cz)})
    return spots


def build_rules(d, navdir, test=None):
    """test: None, or the spots that may count as free ('none', 'side', or names like 10R,17S); every other spot
    counts as taken, to test the skip and recall paths while players decide which spots are free."""
    lb = d.landblock

    def allowed(s):
        return test is None or test == 'side' and not s['fixed'] or s['name'] in test.split(',')

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

    def moving(name, nav_path, arrived, then, timeout):
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
            (mv, m.seconds_in_state(timeout), leave('Recall')),  # moving but not getting there
            died(mv),
            (stuck, m.always(), m.all_a(m.clear_watchdog(), m.expr_a('setvar[faStuck,getvar[faStuck]+1]'),
                                        m.expr_a(USE_NEAR_DOOR))),
            (stuck, m.all_c(m.seconds_in_state(2), m.expr_c('getvar[faStuck]<5')), m.set_state(mv)),
            (stuck, m.all_c(m.seconds_in_state(2), m.expr_c('getvar[faStuck]>4')), m.set_state('Recall')),
            died(stuck),
        ]

    rules = [
        # Settings first: the profile may turn combat and nav on, so keep nav off until it has loaded
        ('Default', m.always(), m.all_a(m.clear_watchdog(), m.opt('EnableNav', False),
                                        m.chat('/ub opt set VTank.PatchExpressionEngine true'),
                                        m.chat(f'/vt settings loadchar {d.profile}'))),
        ('Default', m.seconds_in_state(1), m.opt('EnableNav', False)),
        ('Default', m.all_c(m.seconds_in_state(2), m.not_c(there)), m.set_state('GoTo')),
        ('Default', m.all_c(m.seconds_in_state(2), there, at_entry), m.set_state('Walk01')),
    ]
    # Restarted in a spot: farm it again
    for s in all_spots:
        rules.append(('Default', m.all_c(m.seconds_in_state(2), at(s['x'], s['y'])), m.set_state(f'Farm{s["name"]}')))
    rules.append(('Default', m.all_c(m.seconds_in_state(2), there, m.not_c(at_entry),
                                     *[m.not_c(at(s['x'], s['y'])) for s in all_spots]),
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
        nxt = f'Walk{k + 1:02d}' if i + 1 < len(cps) else 'Recall'
        leg = save_nav(navdir, f'{d.name}-leg{k:02d}.nav', cp['leg'])
        rules += moving(walk, leg, at(cx, cy), check, d.walk_timeout)
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
                rules += moving(f'Go{s["name"]}', gonav, at(s['x'], s['y']), farm, d.go_timeout)
            hold = save_nav(navdir, f'{d.name}-hold{s["name"]}.nav', [pt_line(s['pt'])], route_type='circular')
            rules += [
                (farm, m.always(), m.all_a(m.embed_nav(hold), m.opt('EnableNav', True), m.opt('EnableCombat', True),
                                           m.chat(f'/vt setattackbar {d.attack_bar}'))),
                (farm, m.all_c(m.seconds_in_state(3), m.expr_c('getfellowshipcount[]==1')), m.chat('/ub fellow quit')),
                died(farm),
                (farm, m.not_c(there), m.set_state('Default')),  # left the dungeon: go back
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


def cli(argv):
    import vtrecord
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    r = sub.add_parser('route')
    r.add_argument('dir')
    r.add_argument('name')
    r.add_argument('--out-dir')
    r.add_argument('--min-move', type=float, default=4.0)
    a = ap.parse_args(argv)
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
