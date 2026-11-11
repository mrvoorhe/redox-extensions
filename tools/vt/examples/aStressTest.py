"""Client stability stress test: an endless travel loop with combat off.  Builds aStressTest.met in the VT folder.

    py tools/vt/examples/aStressTest.py [<VT folder>]

Arwic, the TouTou hunt path and the Hoshino hunt path are common client crash points, so the meta goes through
all three again and again:

  S1-arwic         wait 10 s in Arwic (0xC6A9)
  S2-tn            Town Network Sending Gem, walk, Portal to Tou-Tou       -> Tou-Tou landblock 0xF559
  S3-toutou-walk   past the guards (no talks) to the hunt loop             -> cell 0xF65B0030
  S4-toutou-loop   one lap of the TouTou hunt loop                         -> back near its first node
  S5-hoshino-gem / S5a-halls / S5b-tent   as aKTHoshino.py's travel steps  -> Hoshino NPC area 0x49E4
  S6-hoshino-walk  the NPC route with the talks taken out                  -> hunt entrance 0x49E40032
  S7-hoshino-goto  to the start of the hunt loop                           -> 0x4BE3002B
  S8-hoshino-loop  T1..T5 once                                             -> back at the T1 cell after T5
  S9-lifestone     Lifestone Sending Gem                                   -> Arwic, S1 again

No NPC is talked to, so the character's kill tasks aren't touched.  Every state turns combat and looting off on
entry and echoes "Stress: <state>" for the monitor.  Travel steps re-embed their nav (which uses the gem/portal
again) at RETRY_SECONDS; a step that doesn't finish by its give-up time echoes "Stress: GIVE UP <state>" and goes
to S9-lifestone, which starts the cycle over.  Dying goes to S-dead, which waits to arrive in Arwic.

To run it: /vt stop, /vt meta load aStressTest, /vt opt set EnableMeta True, /vt start (start in Arwic or anywhere:
Default recalls with the Lifestone gem if not in Arwic).  Watch it with aStressTest-monitor.py.
First run 2026-11-10, Rockdown: one full cycle in 563 s (TouTou lap 102 s, Hoshino lap 328 s), no crash.

The walk-only navs are made from the source navs here (talk and pause nodes dropped).  aKTTouTou-*.nav are the
navs embedded in the user's aKTTouTou.met, exported unchanged on 2026-11-10.
"""
import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import rectl
import vtmeta as m
import vtnav

VT = sys.argv[1] if len(sys.argv) > 1 else rectl.vt_dir()
HERE = os.path.dirname(os.path.abspath(__file__))

ARWIC = 0xC6A90000
TOUTOU = 0xF5590000
TOUTOU_LOOP_CELL = 0xF65B0030          # where aKTTouTou.met starts its hunt loop
HALLS, HUB, NPC_AREA = 0x012F0000, 0x64440000, 0x49E40000
HOSHINO_ENTRANCE = 0x49E40032
HOSHINO_LOOP = 0x4BE3002B
# Hoshino loop segments: the cell each starts in, as in aKTHoshino.py.  T5 ends back in the T1 cell (2B or 2C).
SEGMENTS = [(0x4BE3002B, 'aKTHoshino-T1.nav'), (0x4CE20026, 'aKTHoshino-T2.nav'), (0x4BE00030, 'aKTHoshino-T3.nav'),
            (0x4BE10039, 'aKTHoshino-T4.nav'), (0x4CE20034, 'aKTHoshino-T5.nav'), (0x4BE3002C, 'aKTHoshino-T1.nav')]

RETRY_SECONDS = (15, 30, 90)
GIVE_UP_SECONDS = 120
TN_RETRY_SECONDS = (35, 70)            # the TN nav itself takes ~25 s (pauses + walk)
TOUTOU_LAP_MIN, TOUTOU_LAP_MAX = 90, 600
HOSHINO_LAP_MAX = 900
LAP_RADIUS_M = 6                       # loop node 26 passes 11 m from the first node


def path(name):
    return os.path.join(HERE, name)


def walk_only(src, dst, *more):
    """Concatenate navs, keeping only waypoints, as a once route."""
    nodes = []
    for name in (src, *more):
        nodes += [n for n in vtnav.load(path(name)).nodes if n.type == 0]
    vtnav.Route(4, nodes).save(path(dst))
    return dst


def write_lifestone_nav():
    route = vtnav.build_spec(open(path('aStressTest-lifestone.spec')).read())
    route.save(path('aStressTest-lifestone.nav'))


def nav(name):
    return m.embed_nav(path(name))


_WE, _NS = 'coordinategetwe[getplayercoordinates[]]', 'coordinategetns[getplayercoordinates[]]'


def near(ew, ns, metres):
    r2 = (metres / 240) ** 2
    return f'({_WE}-({ew}))*({_WE}-({ew}))+({_NS}-({ns}))*({_NS}-({ns}))<{r2:.8f}'


def enter(state, *extra):
    """Rule run on entering a state: combat/looting off, nav on, echo, then extra actions."""
    return (state, m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableLooting', False),
                                       m.opt('EnableNav', True), m.chat(f'/vt echo Stress: {state}'), *extra))


def give_up(state, seconds):
    return (state, m.seconds_in_state(seconds),
            m.all_a(m.chat(f'/vt echo Stress: GIVE UP {state}'), m.set_state('S9-lifestone')))


def travel_step(state, nav_name, ahead, retries=RETRY_SECONDS, give_up_s=GIVE_UP_SECONDS):
    """ahead: [(landblock, next state)], earliest first."""
    rules = [enter(state, nav(nav_name))]
    rules += [(state, m.landblock(lb), m.set_state(to)) for lb, to in ahead]
    rules += [(state, m.seconds_in_state(s), nav(nav_name)) for s in retries]
    rules.append(give_up(state, give_up_s))
    return rules


def build_rules():
    toutou_walk = walk_only('aKTTouTou-guards.nav', 'aStressTest-toutou-walk.nav', 'aKTTouTou-goto.nav')
    hoshino_walk = walk_only('aKTHoshino-getquest.nav', 'aStressTest-hoshino-walk.nav')
    write_lifestone_nav()
    loop0 = vtnav.load(path('aKTTouTou-huntloop.nav')).nodes[0]
    lap_done = near(loop0.ew, loop0.ns, LAP_RADIUS_M)

    rules = [
        ('Default', m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableLooting', False),
                                        m.opt('EnableBuffing', True), m.opt('IdlePeaceMode', False),
                                        m.chat('/ub opt set VTank.PatchExpressionEngine True'),
                                        m.chat('/vt echo Stress: Default'))),
        ('Default', m.all_c(m.seconds_in_state(3), m.landblock(ARWIC)), m.set_state('S1-arwic')),
        ('Default', m.all_c(m.seconds_in_state(3), m.not_c(m.landblock(ARWIC))), m.set_state('S9-lifestone')),

        enter('S1-arwic', m.opt('EnableNav', False)),
        ('S1-arwic', m.seconds_in_state(10), m.set_state('S2-tn')),

        *travel_step('S2-tn', 'aKTTouTou-tn.nav', [(TOUTOU, 'S3-toutou-walk')], TN_RETRY_SECONDS),

        enter('S3-toutou-walk', nav(toutou_walk)),
        ('S3-toutou-walk', m.landcell(TOUTOU_LOOP_CELL), m.set_state('S4-toutou-loop')),
        ('S3-toutou-walk', m.all_c(m.seconds_in_state(5), m.nav_empty()), m.set_state('S4-toutou-loop')),
        give_up('S3-toutou-walk', 180),

        enter('S4-toutou-loop', nav('aKTTouTou-huntloop.nav')),
        ('S4-toutou-loop', m.all_c(m.seconds_in_state(TOUTOU_LAP_MIN), m.expr_c(lap_done)),
         m.all_a(m.chat('/vt echo Stress: TouTou lap done'), m.set_state('S5-hoshino-gem'))),
        ('S4-toutou-loop', m.seconds_in_state(TOUTOU_LAP_MAX),
         m.all_a(m.chat('/vt echo Stress: TouTou lap time cap'), m.set_state('S5-hoshino-gem'))),

        *travel_step('S5-hoshino-gem', 'aKTHoshino-gem.nav',
                     [(HALLS, 'S5a-halls'), (HUB, 'S5b-tent'), (NPC_AREA, 'S6-hoshino-walk')]),
        *travel_step('S5a-halls', 'aKTHoshino-halls.nav', [(HUB, 'S5b-tent'), (NPC_AREA, 'S6-hoshino-walk')]),
        *travel_step('S5b-tent', 'aKTHoshino-tent.nav', [(NPC_AREA, 'S6-hoshino-walk')]),

        enter('S6-hoshino-walk', nav(hoshino_walk)),
        ('S6-hoshino-walk', m.landcell(HOSHINO_ENTRANCE), m.set_state('S7-hoshino-goto')),
        give_up('S6-hoshino-walk', 120),

        enter('S7-hoshino-goto', nav('aKTHoshino-gotoloop.nav')),
        ('S7-hoshino-goto', m.landcell(HOSHINO_LOOP), m.set_state('S8-hoshino-loop')),
        give_up('S7-hoshino-goto', 180),

        enter('S8-hoshino-loop', m.expr_a('setvar[StressT5,0]')),
        *[('S8-hoshino-loop', m.landcell(cell), nav(name)) for cell, name in SEGMENTS],
        ('S8-hoshino-loop', m.landcell(SEGMENTS[4][0]), m.expr_a('setvar[StressT5,1]')),
        ('S8-hoshino-loop', m.all_c(m.any_c(m.landcell(0x4BE3002B), m.landcell(0x4BE3002C)),
                                    m.expr_c('$StressT5==1')),
         m.all_a(m.chat('/vt echo Stress: Hoshino lap done'), m.set_state('S9-lifestone'))),
        give_up('S8-hoshino-loop', HOSHINO_LAP_MAX),

        enter('S9-lifestone', nav('aStressTest-lifestone.nav')),
        ('S9-lifestone', m.landblock(ARWIC), m.set_state('S1-arwic')),
        *[('S9-lifestone', m.seconds_in_state(s), nav('aStressTest-lifestone.nav')) for s in RETRY_SECONDS],
        ('S9-lifestone', m.seconds_in_state(GIVE_UP_SECONDS),
         m.all_a(m.opt('EnableNav', False), m.chat('/vt echo Stress: GIVE UP S9-lifestone, trying /ls'),
                 m.chat('/ls'))),

        enter('S-dead', m.opt('EnableNav', False)),
        ('S-dead', m.landblock(ARWIC), m.set_state('S1-arwic')),
        ('S-dead', m.seconds_in_state(60), m.set_state('S9-lifestone')),
    ]
    states = sorted({r[0] for r in rules} - {'S-dead'})
    rules += [(s, m.died(), m.all_a(m.chat(f'/vt echo Stress: DIED in {s}'), m.set_state('S-dead'))) for s in states]
    return rules


if __name__ == '__main__':
    rules = build_rules()
    out = os.path.join(VT, 'aStressTest.met')
    data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}')
