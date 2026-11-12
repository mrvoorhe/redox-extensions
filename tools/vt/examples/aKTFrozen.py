"""Frozen Valley kill tasks: rebuilds the hunt part of aKTFrozen.met.  Writes aKTFrozen.met in the VT folder.

    py tools/vt/examples/aKTFrozen.py [<VT folder>] [--loot all|tusker] [--out <name>] [--chain <meta>]

--chain replaces the meta ** Chain ** loads (for a test run: --chain zFarmVirmar).

aKTFrozen-base.met (next to this script) is the community meta as it was installed on 2026-11-06.  Everything
except the hunt (travel, task pickup, turn-in, ** Chain **) is copied from it unchanged.  FV3-gotoloop,
FV3-huntloop and FV3-shankin are replaced:

* One circular hunt route.  The base meta walked seven "once" navs (zFV1..zFV7), each loaded by a LandCellE rule
  in FV3-huntloop.  A rule fires again every time the meta comes back from a fight in that cell, so the segment
  restarted from its first point (Woodford, 2026-11-11: zFV2 loaded 4 times in 45 s).  In the 11-11 runs both
  characters spent the whole 14 minutes in zFV1 and zFV2 and never reached the rest of the valley.  The seven navs
  are joined into one circular route, loaded once on arriving at the loop.  A fight doesn't reload it.
* Only open tasks pull the character into a fight.  The base meta stopped for every task monster, done or not
  (Wights are done in ~5 minutes but were still fought for the rest of the hunt).  Snow Tuskers of each kind are
  also fought while there are fewer than BLOOD_NEEDED blood samples in the pack.
* A task is done when the server says "You have killed N <mobs>! Your task is complete!" for any of its mobs.
  The base meta matched "You have killed 20 Frozen Wights" etc., which missed the Archers that count for the
  Wight task.  Frozen Dread is now part of "all done" too.
* Arrival at the loop: the landcell, or the goto nav finished (its end point is on a cell boundary).
* --loot tusker (default): VT looting is on only after a Snow Tusker fight while blood samples are short (and for
  Edgar, until his snowball is in the pack).  Every other fight turns it off, so VT doesn't walk to and open the
  corpses of the other ~100 kills a hunt.  --loot all keeps the base behaviour (loot everything until 10 samples and
  the snowball).

Kill-line names are from the 11-09..11-11 recordings.  pvars (FVGurog1 ...) are the base meta's: the task pickup
states set them to 0.
"""
import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import rectl
import vtmeta as m
import vtnav

import argparse
_ap = argparse.ArgumentParser()
_ap.add_argument('vt', nargs='?')
_ap.add_argument('--loot', choices=['tusker', 'all'], default='tusker')
_ap.add_argument('--out', default='aKTFrozen')
_ap.add_argument('--chain', help='meta for ** Chain ** to load instead of aKTTouTou')
_args = _ap.parse_args()
VT = _args.vt or rectl.vt_dir()
LOOT, OUT, CHAIN = _args.loot, _args.out, _args.chain
HERE = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(HERE, 'aKTFrozen-base.met')

HUNT_SECONDS = 840          # same as the base meta (14 min)
TURNIN_SECONDS = 90         # base meta's turn-in limit, checked in every state
TARGET_RANGE = 30           # meters, bow ranges from the base meta
FIGHT_GIVE_UP = 150         # seconds in one fight before walking on (base meta)
BLOOD = 'Snow Tusker Blood Sample'
BLOOD_NEEDED = 10
SNOWBALL = "Edgar's Snowball"
LOOP_CELL = 2028404796      # 0x78E7003C, first point of zFV1

# (pvar, monster name regex for MonsterCountWithinDistance, plural names in the kill line, tusker?)
TASKS = [
    ('FVGurog1',  '^Gurog Henchman',       'Gurog Henchm[ae]ns?', False),
    ('FVGurog2',  '^Gurog Minion',         'Gurog Minions',  False),
    ('FVGurog3',  '^Gurog Soldier',        'Gurog Soldiers', False),
    ('FVTusker1', '^Snow Tusker$',         'Snow Tuskers', True),
    ('FVTusker2', '^Snow Tusker Warrior',  'Snow Tusker Warriors', True),
    ('FVTusker3', '^Snow Tusker Leader',   'Snow Tusker Leaders', True),
    ('FVWight',   '^Frozen Wight',         'Frozen Wight( Archer| Captain| Sorcerer)?s', False),
    ('FVGolem',   '^Frost Golem',          'Frost Golems', False),
    ('FVDread',   '^Frozen Dread',         'Frozen Dreads', False),
    ('FVCrystal', '^Frozen Crystal',       'Frozen Crystals', False),
    ('FVCow',     '^Frozen Cow',           'Frozen Cows', False),
]
EDGAR = '^Edgar The Snowman'

BLOOD_SHORT = f'getitemcountininventorybyname[{BLOOD}]<{BLOOD_NEEDED}'
NO_SNOWBALL = f"getitemcountininventorybyname[{SNOWBALL}]<1"
def task_open(pvar, tusker):
    e = f'getpvar[{pvar}]!=1'
    return f'({e}||{BLOOD_SHORT})' if tusker else e
ALL_DONE = '&&'.join(f'getpvar[{p}]==1' for p, *_ in TASKS) + f'&&getitemcountininventorybyname[{BLOOD}]>={BLOOD_NEEDED}'

def wanted(regex, pvar, tusker):
    return m.all_c(m.monster_count(regex, 1, TARGET_RANGE), m.expr_c(task_open(pvar, tusker)))
TUSKER_WANTED = m.any_c(*[wanted(r, p, t) for p, r, _, t in TASKS if t],
                        m.all_c(m.monster_count(EDGAR, 1, TARGET_RANGE), m.expr_c(NO_SNOWBALL)))
OTHER_WANTED = m.any_c(*[wanted(r, p, t) for p, r, _, t in TASKS if not t])
ANY_WANTED = m.any_c(TUSKER_WANTED, OTHER_WANTED)
# Loot only for blood / the snowball: on when a tusker (or Edgar) is in the fight, off for fights without one
LOOT_FOR = m.all_c(TUSKER_WANTED, m.expr_c(f'{BLOOD_SHORT}||{NO_SNOWBALL}'))

HUNT_TIMEOUT = f'ifthen[testvar[FVHuntSW],`stopwatchelapsedseconds[$FVHuntSW]>={HUNT_SECONDS}`,`0`]'
TURNIN_TIMEOUT = f'ifthen[testvar[FVTurninSW],`stopwatchelapsedseconds[$FVTurninSW]>={TURNIN_SECONDS}`,`0`]'

_, BASE_TABLE, _ = m.load(BASE)
# The base meta's turn-in limit action (clear the nav, combat off, echo, /b d, clear vars, ** Chain **), the same in
# every state
TURNIN_GIVE_UP = next((int(at[1]), ad) for ct, at, cd, ad, st in BASE_TABLE.rows
                      if st[1] == 'FV4-turnin' and int(ct[1]) == 26)


def base_navs(state):
    """(name, nav bytes) of each EmbedNav in a state of the base meta, in rule order."""
    out = []
    for ct, at, cd, ad, st in BASE_TABLE.rows:
        if st[1] == state and int(at[1]) == 4:
            name, count, body = ad[1].split(b'\r\n', 2)
            if int(count) > 0:
                out.append((name.decode('latin-1'), body))
    return out


def hunt_route():
    """zFV1..zFV7 joined into one circular route (each segment starts where the last ended)."""
    route = vtnav.Route(1)
    for name, body in base_navs('FV3-huntloop'):
        r = vtnav.parse(body.decode('latin-1'))
        nodes = r.nodes
        if route.nodes:
            a, b = route.nodes[-1].position(), nodes[0].position()
            if ((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2) ** .5 * 240 < 1:
                nodes = nodes[1:]
        route.nodes += nodes
    return route


def embed(name, body):
    body = body[body.index(b'uTank2'):] if b'uTank2' in body else body
    count = body.split(b'\r\n')[2]
    return (4, ('ba', name.encode('latin-1') + b'\r\n' + count + b'\r\n' + body))


def kill_lines(state):
    return [(state, m.chat_message(f'^You have killed [0-9]+ {names}! Your task is complete!'),
             m.expr_a(f'setpvar[{p},1]')) for p, _, names, _ in TASKS]


def hunt_checks(state):
    return kill_lines(state) + [
        (state, m.all_c(m.seconds_in_state(2), m.expr_c(ALL_DONE)),
         m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True),
                 m.chat('/vt echo Frozen: all tasks done, turning in'), m.set_state('FV4-turnin'))),
        (state, m.expr_c(HUNT_TIMEOUT),
         m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True),
                 m.chat(f'/vt echo Frozen: {HUNT_SECONDS // 60} minute hunt limit reached, turning in'),
                 m.set_state('FV4-turnin'))),
        (state, m.expr_c(TURNIN_TIMEOUT),
         TURNIN_GIVE_UP),
    ]


def loot(on):
    return m.opt('EnableLooting', on)


def hunt_rules(route_bytes):
    goto_name, goto_body = base_navs('FV3-gotoloop')[0]
    tusker = LOOT == 'tusker'
    rules = [
        ('FV3-gotoloop', m.always(), m.all_a(embed(goto_name, goto_body), *( [loot(False)] if tusker else []))),
        ('FV3-gotoloop', m.any_c(m.landcell(LOOP_CELL), m.all_c(m.seconds_in_state(5), m.nav_empty())),
         m.all_a(embed('FrozenHunt.nav', route_bytes), m.chat('/vt echo Frozen: hunting'),
                 m.set_state('FV3-huntloop'))),
        *hunt_checks('FV3-gotoloop'),

        # Walk the loop with combat off.  No nav here: the route stays loaded through fights.
        ('FV3-huntloop', m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True))),
    ]
    if tusker:
        rules += [
            ('FV3-huntloop', LOOT_FOR, m.all_a(loot(True), m.set_state('FV3-shankin'))),
            ('FV3-huntloop', m.all_c(OTHER_WANTED, m.not_c(LOOT_FOR)), m.all_a(loot(False), m.set_state('FV3-shankin'))),
            ('FV3-huntloop', m.all_c(TUSKER_WANTED, m.not_c(LOOT_FOR)), m.set_state('FV3-shankin')),
        ]
    else:
        rules += [
            ('FV3-huntloop', ANY_WANTED, m.set_state('FV3-shankin')),
            ('FV3-huntloop', m.all_c(m.monster_count(EDGAR, 1, TARGET_RANGE)), loot(True)),
        ]
    rules += hunt_checks('FV3-huntloop')

    rules += [
        ('FV3-shankin', ANY_WANTED, m.opt('EnableCombat', True)),
        ('FV3-shankin', m.all_c(m.no_monsters_within(5), m.not_c(ANY_WANTED)),
         m.all_a(m.opt('EnableCombat', False), m.set_state('FV3-huntloop'))),
        ('FV3-shankin', m.seconds_in_state_persist(FIGHT_GIVE_UP),
         m.all_a(m.opt('EnableCombat', False), m.set_state('FV3-huntloop'))),
    ]
    if tusker:
        rules.append(('FV3-shankin', LOOT_FOR, loot(True)))
    else:
        rules.append(('FV3-shankin', m.expr_c(f'getitemcountininventorybyname[{BLOOD}]>={BLOOD_NEEDED}&&!({NO_SNOWBALL})'),
                      loot(False)))
    rules += hunt_checks('FV3-shankin')
    return rules


def build():
    route = hunt_route()
    route_bytes = route.serialize().encode('latin-1')
    replaced = {'FV3-gotoloop', 'FV3-huntloop', 'FV3-shankin'}
    rows = [r for r in BASE_TABLE.rows if r[4][1] not in replaced]
    if CHAIN:
        replaced.add('** Chain **')
        rows = [r for r in rows if r[4][1] != '** Chain **']
    new = hunt_rules(route_bytes)
    if CHAIN:
        new.append(('** Chain **', m.always(), m.chat(f'/vt meta load {CHAIN}')))
    rows += [[m._i(c[0]), m._i(a[0]), c[1], a[1], m._s(s)] for s, c, a in new]
    table = m.Table(BASE_TABLE.cols, rows, BASE_TABLE.flags)
    data = m.write_bytes(['1', 'CondAct'], table)
    assert m.write_bytes(*m.read_bytes(data)) == data
    return route, data


if __name__ == '__main__':
    route, data = build()
    out = os.path.join(VT, OUT + '.met')
    with open(out, 'wb') as f:
        f.write(data)
    route.save(os.path.join(HERE, 'aKTFrozen-hunt.nav'))
    print(f'loot={LOOT}: hunt route {len(route.nodes)} points; wrote {len(data)} bytes to {out}')
