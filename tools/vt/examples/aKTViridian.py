"""Viridian Rise kill tasks meta.  Builds aKTViridian.met in the VT folder.

    py tools/vt/examples/aKTViridian.py [<output folder, default the VT folder>] [--test]

--test skips the "no open tasks" exit, so the zones can be tested on a character whose tasks are on cooldown.

Research notes (statues, flames, NPCs, quest flags): docs/research/Statue-Flame-Meta.md.

The tasks: three plaques next to where the entrance portal drops you in zone 1 (Brellegia: A'nekshen,
Aangesilus: wasps, Swift: Zefir), plus the optional boss essence task from the Essence of the Viridian Rise at the
center.  The center is only reachable with a Viridian Rise Deru Portal Sending Gem, which is used up, and Mites drop
them (QuestLoot.utl keeps them, and boss "Essence of ..." items, since 2026-11-11).  A run needs two: one to see the
Essence NPC at the start and one to turn in.  With fewer than two at the start it skips the Essence NPC and keeps
the gem for the turn-in.  With none at the turn-in it says so and moves on: finished tasks wait for the next run.

States:
  VR1-start      options, then skip ahead if already at the entrance or in zone 1
  VR2-gem / VR2a-halls / VR2b-hub   Halls of Introduction Gem, Daily Portals, Base of the Viridian Rise
  VR3-enter      entrance statue (53130) + its Viridian Portal (53060) -> zone 1 drop-off
  VR4-plaques    use the three plaques, /ub myquests
  VR4a-deru / VR4b-center / VR4c-recall   (2+ gems) Deru gem, Essence NPC, Portal Recall Gem -> VR3-enter
  VR5-zone1  -> VR5f-flame1 -> VR6-zone2 -> VR6f-flame2 -> VR7-zone3 -> VR7r-recall -> VR3-enter (-> VR8-final)
                 Each zone hunts its loop (VRMetaZone1/2/3.nav) with combat and looting on.  From the second lap,
                 when the character has ESSENCE_MIN Viridian Essence and passes the statue with the blue flame (a
                 Fiery Remains, template 53026, on one of the zone's statues), it uses the statue and then the
                 portal the statue makes.  That takes all the essences, gives the next key and moves you to the next
                 zone.  Zone 3 does one lap, then recalls and goes back in to zone 1.
  VR8-final      zone 1 again until the three tasks are done or HUNT_SECONDS have passed
  VR9-turnin / VR9a-center   Deru gem, then the four NPCs (essences are given to the Essence NPC)
  VR10-dead      wait for the lifestone, then start over

Positions are flat distances in nav units (squared), like aKTRynthid.py.  Statue positions are fixed, but their
object ids change, so statues are found by template type and flames by position.
"""
import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import rectl
import vtmeta as m

ARGS = [a for a in sys.argv[1:] if not a.startswith('--')]
TEST = '--test' in sys.argv
VT_NAVS = rectl.vt_dir()          # the user's VRMetaZone1/2/3.nav are here
VT = ARGS[0] if ARGS else VT_NAVS  # where the .met goes
NAVS = os.path.dirname(os.path.abspath(__file__))

HUNT_SECONDS = 900      # VR8-final: zone 1 after zone 3, until the tasks are done or this long
ZONE_SECONDS = 1200     # a zone that hasn't moved on by then: recall and go to VR8-final (zone 3 laps take 10+ min)
STUCK_METERS, STUCK_SECONDS = 5, 120   # hunting but not moved this far in this long: walk on with combat off
UNSTICK_SECONDS = 20
ESSENCE_MIN = 5         # Viridian Essence needed for the flame statue (Woodford, with Lockpick; more without)
ESSENCE_STEP = 5        # a statue that didn't make a portal: ask for this many more next time
FLAME_RANGE = 15        # meters from the flame statue (on the loop) to go and use it
REFRESH_SECONDS = 60    # /ub myquests interval in VR8-final
CHAIN = None            # meta to load when done; None just echoes
LOOT_PROFILE = 'QuestLoot.utl'   # reloaded at the start: it keeps the Deru gems, and a character that loaded it
                                 # before an edit wouldn't loot them (Cowhead, 2026-11-11)
TRAVEL_RETRY = (15, 30, 90)
TRAVEL_GIVE_UP = 150
CENTER_SECONDS = 240    # the four center NPCs took ~2.5 minutes on 2026-11-11 (VT waits at each talk node)

DERU_GEM = 'Viridian Rise Deru Portal Sending Gem'
RECALL_GEM = 'Portal Recall Gem'
FLAME = 53026           # Fiery Remains, the blue flame
ENTRANCE_STATUE, ENTRANCE_PORTAL = 53130, 53060
ESSENCE_NPC = 64454007

HALLS, HUB = 0x012F0000, 0x64440000

# Kill task counters (from /myquests).  killtaskviridiananek is assumed from killtaskviridiananekwait: it wasn't seen,
# because Woodford's A'nekshen task was already turned in.  A missing counter has required 0, so it counts as done.
TASKS = ['killtaskviridiananek', 'killtaskviridianwasp', 'killtaskviridianzefir']
ALL_DONE = 'isrefreshingquests[]==0&&' + '&&'.join(
    f'getquestktprogress[{f}]>=getquestktrequired[{f}]' for f in TASKS)

# Places (nav coordinates: ew, ns)
BASE = (41.556125, -43.251167)          # where the Base of the Viridian Rise portal drops you
CENTER = (42.849167, -42.701667)        # where the Deru gem drops you (the Deru tree?), 85 m SW of Brellegia
ESSENCE_NPC_AT = (43.499225, -42.306721)
# (zone, nav, loop start = node 0, where you arrive, statues {template: (ew, ns)}, portal its flame statue makes)
ZONES = {
    1: ('VRMetaZone1.nav', (41.9319, -43.1630), (41.9319, -43.1630),
        {53033: (41.772500, -42.696667), 53034: (41.676667, -42.516667), 53035: (41.695833, -42.294583),
         53036: (41.785000, -41.987500), 53037: (42.038333, -42.457083)}, 53061),
    2: ('VRMetaZone2.nav', (41.9315, -41.8395), (41.8800, -41.9100),
        {53049: (42.077083, -41.634583), 53051: (42.281250, -41.287500), 53053: (42.279167, -41.737500),
         53055: (42.435000, -41.505417), 53057: (42.747083, -41.165833)}, 53062),
    3: ('VRMetaZone3.nav', (42.8870, -41.8977), (42.8900, -41.9200),
        {53076: (42.857917, -41.323333), 53099: (43.229167, -41.170833), 53100: (43.154167, -41.537500),
         53101: (43.633333, -41.551250), 53102: (43.571250, -41.175417)}, None),
}

_WE, _NS = 'coordinategetwe[getplayercoordinates[]]', 'coordinategetns[getplayercoordinates[]]'


def near(at, meters):
    ew, ns = at
    return f'({_WE}-({ew}))*({_WE}-({ew}))+({_NS}-({ns}))*({_NS}-({ns}))<{(meters / 240) ** 2:.8f}'


def far(at, meters):
    return f'!({near(at, meters)})'


def flame_at(at):
    """A Fiery Remains within 1 m (flat) of a statue position."""
    ew, ns = at
    we, ns_ = 'coordinategetwe[wobjectgetphysicscoordinates[$1]]', 'coordinategetns[wobjectgetphysicscoordinates[$1]]'
    return (f'listcount[listfilter[wobjectfindallbytemplatetype[{FLAME}],'
            f'`({we}-({ew}))*({we}-({ew}))+({ns_}-({ns}))*({ns_}-({ns}))<{(1 / 240) ** 2:.8f}`]]>0')


def exists(template):
    return f'getobjectinternaltype[wobjectfindnearestbytemplatetype[{template}]]==7'


def use_template(template):
    return f'actiontryuseitem[wobjectfindnearestbytemplatetype[{template}]]'


def count(name):
    return f'getitemcountininventorybyname[{name}]'


NO_NAV = (4, ('ba', b'\r\n0\r\n'))     # an empty embedded nav (copied from aKTNeftet), stops the route
# Entering a zone: lap counting starts over, and the zone's time limit starts
ENTER_ZONE = [m.expr_a('setvar[VRLap,0]'), m.expr_a('setvar[VRFar,0]'),
              m.expr_a('setvar[VRZoneSW,stopwatchstart[stopwatchcreate[]]]')]


def nav(name):
    path = os.path.join(NAVS, name)
    return m.embed_nav(path if os.path.exists(path) else os.path.join(VT_NAVS, name))


def travel():
    return [m.clear_watchdog(), m.opt('EnableCombat', False), m.opt('EnableLooting', False), m.opt('EnableNav', True)]


def hunting():
    return [m.opt('EnableCombat', True), m.opt('EnableLooting', True), m.opt('EnableNav', True),
            m.opt('IdlePeaceMode', False)]


def center():
    """Walking the center NPCs: peace mode, so the talks don't turn into combat-mode shuffling (user, 2026-11-11)."""
    return [*travel(), m.opt('IdlePeaceMode', True), m.chat('/ub combatstate peace')]


def echo(text):
    return m.chat(f'/vt echo Viridian: {text}')


def stopwatch(var):
    return m.expr_a(f'setvar[{var},stopwatchstart[stopwatchcreate[]]]')


def elapsed(var, seconds):
    return f'ifthen[testvar[{var}],`stopwatchelapsedseconds[${var}]>={seconds}`,`0`]'


def chain():
    return m.chat(f'/vt meta load {CHAIN}') if CHAIN else echo('finished')


def died(state):
    return (state, m.died(), m.set_state('VR10-dead'))


HAS_ESSENCES = 'getitemcountininventorybynamerx[`Essence of `]>0'
GIVE_TRIES = 12


def center_route(state, done):
    """Walk the center NPCs, then give boss essences to the Essence NPC, then do `done` (a list of actions).

    The gives are in two states that hand over to each other every 2 seconds, each giving one essence on entry, until
    none are left (or GIVE_TRIES).  On Cowhead (2026-11-11) a single rule retried on a stopwatch fired once, in the
    same second as the talk node's use of the NPC, and the server dropped that give.  It never fired again, because
    its condition never went false as far as VT could see.  So the gives wait for the route to finish, and don't
    rely on a condition going false and true again."""
    give = m.all_a(m.expr_a(f'actiontrygiveitem[wobjectfindininventorybynamerx[`Essence of `],'
                            f'wobjectfindnearestbytemplatetype[{ESSENCE_NPC}]]'),
                   m.expr_a('setvar[VRGiveN,getvar[VRGiveN]+1]'))
    at_npc = m.all_c(m.seconds_in_state(8), m.nav_empty(), m.expr_c(near(ESSENCE_NPC_AT, 8)))
    rules = [
        (state, m.always(), m.all_a(*center(), m.expr_a('setvar[VRGiveN,0]'), nav('aKTViridian-center.nav'))),
        (state, m.seconds_in_state(2), m.chat('/ub combatstate peace')),
        (state, m.all_c(at_npc, m.expr_c(HAS_ESSENCES)), m.set_state(f'{state}-give1')),
        (state, m.any_c(m.all_c(at_npc, m.not_c(m.expr_c(HAS_ESSENCES))), m.seconds_in_state(CENTER_SECONDS)),
         m.all_a(*done)),
        died(state),
    ]
    # Decide only at the 2 s handover: while a give is going through, the count briefly reads 0 (Cowhead left with
    # one essence still in the pack when this was checked straight after the give)
    for a, b in (('give1', 'give2'), ('give2', 'give1')):
        s = f'{state}-{a}'
        rules += [(s, m.always(), give),
                  (s, m.all_c(m.seconds_in_state(2), m.expr_c(HAS_ESSENCES)), m.set_state(f'{state}-{b}')),
                  (s, m.all_c(m.seconds_in_state(2), m.not_c(m.expr_c(HAS_ESSENCES))), m.all_a(*done)),
                  (s, m.expr_c(f'getvar[VRGiveN]>={GIVE_TRIES}'),
                   m.all_a(echo('the Essence NPC did not take every essence, moving on'), *done)),
                  died(s)]
    return rules


def gem_step(state, command, arrived, next_state, where, used_up=None):
    """A travel step that ends in a teleport: send the command (again at TRAVEL_RETRY), done on arrival.
    used_up: the item name, for gems that are used up (the Deru gem).  Retries are then only sent while the count
    is what it was on entering the state, so a gem that worked is never followed by a second one.  (On 2026-11-11
    the first test used three Deru gems: the arrival check was wrong, and every retry teleported again.)"""
    if used_up:
        rules = [(state, m.always(), m.all_a(*travel(), m.expr_a(f'setvar[VRGems,{count(used_up)}]'), m.chat(command)))]
        rules += [(state, m.all_c(m.seconds_in_state(s), m.expr_c(f'{count(used_up)}>=getvar[VRGems]')), m.chat(command))
                  for s in TRAVEL_RETRY]
    else:
        rules = [(state, m.always(), m.all_a(*travel(), m.chat(command)))]
        rules += [(state, m.seconds_in_state(s), m.chat(command)) for s in TRAVEL_RETRY]
    rules += [(state, arrived, m.set_state(next_state)),
              (state, m.seconds_in_state(TRAVEL_GIVE_UP),
               m.all_a(m.opt('EnableNav', False), echo(f'could not get to {where}, giving up'), m.set_state('VR11-end'))),
              died(state)]
    return rules


def nav_step(state, nav_name, arrived, next_state, where):
    """Like gem_step, but the use is at the end of an embedded nav."""
    rules = [(state, m.always(), m.all_a(*travel(), nav(nav_name)))]
    rules += [(state, m.seconds_in_state(s), nav(nav_name)) for s in TRAVEL_RETRY]
    rules += [(state, arrived, m.set_state(next_state)),
              (state, m.seconds_in_state(TRAVEL_GIVE_UP),
               m.all_a(m.opt('EnableNav', False), echo(f'could not get to {where}, giving up'), m.set_state('VR11-end'))),
              died(state)]
    return rules


def statue_and_portal(state, statue, portal_template, arrived, next_state, fail_state, on_arrive=(), fail_actions=()):
    """Use a statue (a wobject expression) until its portal shows up, then the portal until we arrive.  Combat
    stays on (monsters around), so uses can be dropped: retry every few seconds."""
    use_statue = m.expr_a(f'actiontryuseitem[{statue}]')
    rules = [(state, m.always(), m.all_a(m.clear_watchdog(), NO_NAV, m.opt('EnableNav', False), m.opt('EnableLooting', False),
                                         use_statue))]
    for s in (3, 6, 10, 15, 20, 25):
        rules.append((state, m.all_c(m.seconds_in_state(s), m.not_c(m.expr_c(exists(portal_template)))), use_statue))
    rules.append((state, m.expr_c(exists(portal_template)), m.expr_a(use_template(portal_template))))
    for s in (5, 9, 13, 18, 23, 28, 33):
        rules.append((state, m.all_c(m.seconds_in_state(s), m.expr_c(exists(portal_template))),
                      m.expr_a(use_template(portal_template))))
    rules += [(state, m.expr_c(arrived), m.all_a(*on_arrive, m.set_state(next_state))),
              (state, m.seconds_in_state(40), m.all_a(*fail_actions, m.set_state(fail_state))),
              died(state)]
    return rules


def zone_hunt(zone, state, flame_state, laps_then=None):
    """Hunt a zone's loop.  Counts laps: a lap is done when the character comes back within 15 m of the loop's
    start after being more than 60 m away.  laps_then=(n, state): go to state after n laps (zone 3)."""
    nav_name, start, _, statues, _ = ZONES[zone]
    unstick = f'{state}-unstick'
    rules = [
        (state, m.always(), m.all_a(*hunting(), nav(nav_name), m.watchdog(unstick, STUCK_METERS, STUCK_SECONDS))),
        # Seen in zone 3 on 2026-11-11: VT stood for 15 minutes trying to loot a boss essence from a Corpse of Wind
        # Fury, which the server refused every 3 s ("You may complete this quest again in 2h ...": the pickup is on a
        # timer).  Walk on with looting and combat off, then hunt again (lap counting carries on).
        (unstick, m.always(), m.all_a(m.clear_watchdog(), m.opt('EnableCombat', False), m.opt('EnableLooting', False),
                                      m.opt('EnableNav', True),
                                      echo(f'zone {zone}: not moving, walking on'))),
        (unstick, m.seconds_in_state(UNSTICK_SECONDS), m.set_state(state)),
        died(unstick),
        # Not in the first minute: on 2026-11-11 the entrance portal's teleport counted as a lap at once
        (state, m.expr_c(f'getvar[VRFar]==0&&{elapsed("VRZoneSW", 60)}&&{far(start, 60)}'), m.expr_a('setvar[VRFar,1]')),
        (state, m.expr_c(f'getvar[VRFar]==1&&{near(start, 15)}'),
         m.all_a(m.expr_a('setvar[VRFar,0]'), m.expr_a('setvar[VRLap,getvar[VRLap]+1]'),
                 echo(f'zone {zone} lap done'))),
        (state, m.expr_c(elapsed('VRZoneSW', ZONE_SECONDS)),
         m.all_a(echo(f'zone {zone} took more than {ZONE_SECONDS // 60} minutes, going back to zone 1'),
                 m.set_state('VR7r-recall'))),
        died(state),
    ]
    if laps_then:
        n, to = laps_then
        rules.append((state, m.expr_c(f'getvar[VRLap]>={n}'), m.set_state(to)))
    else:
        for template, at in statues.items():
            rules.append((state, m.expr_c(f'getvar[VRLap]>=1&&{count("Viridian Essence")}>=getvar[VREssNeed]&&'
                                          f'{near(at, FLAME_RANGE)}&&{flame_at(at)}'),
                          m.all_a(m.expr_a(f'setvar[VRStatue,{template}]'),
                                  echo(f'zone {zone} flame statue {template}, using it'),
                                  m.set_state(flame_state))))
    return rules


def flame_use(zone, state, hunt_state, next_hunt_state):
    portal = ZONES[zone][4]
    next_arrive = ZONES[zone + 1][2]
    return statue_and_portal(
        state, 'wobjectfindnearestbytemplatetype[getvar[VRStatue]]', portal, near(next_arrive, 30),
        next_hunt_state, hunt_state, on_arrive=ENTER_ZONE,
        fail_actions=[m.expr_a(f'ifthen[{count("Viridian Essence")}>0,'
                               f'`setvar[VREssNeed,getvar[VREssNeed]+{ESSENCE_STEP}]`,`0`]'),
                      echo('the flame statue did not make a portal, back to hunting')])




rules = [
    ('Default', m.chat_message('^\\[Fellowship\\] (|).* (say|says), \\"!start\\"$'), m.set_state('VR1-start')),
    ('Default', m.seconds_in_state_persist(10), m.set_state('VR1-start')),

    ('VR1-start', m.always(), m.all_a(
        *travel(), m.opt('EnableBuffing', True), m.opt('IdlePeaceMode', False), m.opt('NavPriorityBoost', False),
        m.chat('/ub opt set VTank.PatchExpressionEngine True'), m.chat(f'/vt loot load {LOOT_PROFILE}'),
        m.expr_a(f'setvar[VREssNeed,{ESSENCE_MIN}]'), m.expr_a('setvar[VRFinal,0]'), m.expr_a('clearvar[VRHuntSW]'))),
    ('VR1-start', m.all_c(m.seconds_in_state(3), m.expr_c(near(BASE, 40))), m.set_state('VR3-enter')),
    ('VR1-start', m.all_c(m.seconds_in_state(3), m.expr_c(near(ZONES[1][2], 40))), m.set_state('VR4-plaques')),
    ('VR1-start', m.seconds_in_state(4), m.set_state('VR2-gem')),

    # Travel: from anywhere to the Base of the Viridian Rise
    *gem_step('VR2-gem', '/ub usei Halls of Introduction Gem', m.landblock(HALLS), 'VR2a-halls',
              'the Halls of Introduction'),
    ('VR2-gem', m.landblock(HUB), m.set_state('VR2b-hub')),
    *nav_step('VR2a-halls', 'aKTHoshino-halls.nav', m.landblock(HUB), 'VR2b-hub', 'the Daily Portals hub'),
    *nav_step('VR2b-hub', 'aKTViridian-hub.nav', m.expr_c(near(BASE, 40)), 'VR3-enter', 'the Viridian Rise'),

    # Entrance statue + portal into zone 1.  VRFinal says where to go once in.
    *statue_and_portal('VR3-enter', f'wobjectfindnearestbytemplatetype[{ENTRANCE_STATUE}]', ENTRANCE_PORTAL,
                       near(ZONES[1][2], 30), 'VR3a-in', 'VR1-start'),
    ('VR3a-in', m.all_c(m.always(), m.expr_c('getvar[VRFinal]==0')), m.set_state('VR4-plaques')),
    ('VR3a-in', m.all_c(m.always(), m.expr_c('getvar[VRFinal]==1')), m.all_a(*ENTER_ZONE, m.set_state('VR5-zone1'))),
    ('VR3a-in', m.all_c(m.always(), m.expr_c('getvar[VRFinal]==2')), m.set_state('VR8-final')),

    # The three plaques, then (with 2+ Deru gems) the Essence NPC at the center
    ('VR4-plaques', m.always(), m.all_a(*travel(), nav('aKTViridian-plaques.nav'))),
    ('VR4-plaques', m.all_c(m.seconds_in_state(5), m.nav_empty()),
     m.all_a(m.chat('/ub myquests'), m.expr_a('setvar[VRFinal,1]'), m.set_state('VR4x-check'))),
    ('VR4-plaques', m.seconds_in_state(90),
     m.all_a(m.chat('/ub myquests'), m.expr_a('setvar[VRFinal,1]'), m.set_state('VR4x-check'))),
    died('VR4-plaques'),

    ('VR4x-check', m.always(), m.all_a(NO_NAV, m.opt('EnableNav', False))),
    *([] if TEST else [
        ('VR4x-check', m.all_c(m.seconds_in_state(5), m.expr_c(ALL_DONE)),
         m.all_a(echo('no open kill tasks (all on cooldown)'), m.set_state('VR11-end')))]),
    ('VR4x-check', m.all_c(m.seconds_in_state(10), m.expr_c(f'{count(DERU_GEM)}>=2')), m.set_state('VR4a-deru')),
    ('VR4x-check', m.all_c(m.seconds_in_state(10), m.expr_c(f'{count(DERU_GEM)}<2')),
     m.all_a(echo('fewer than 2 Deru gems, skipping the Essence task and keeping the gem for the turn-in'),
             *ENTER_ZONE, m.set_state('VR5-zone1'))),

    *gem_step('VR4a-deru', f'/ub usei {DERU_GEM}', m.expr_c(near(CENTER, 40)), 'VR4b-center', 'the center',
              used_up=DERU_GEM),
    *center_route('VR4b-center', [m.set_state('VR4c-recall')]),
    *gem_step('VR4c-recall', f'/ub usei {RECALL_GEM}', m.expr_c(near(BASE, 40)), 'VR3-enter', 'the entrance'),

    # Zones
    *zone_hunt(1, 'VR5-zone1', 'VR5f-flame1'),
    *flame_use(1, 'VR5f-flame1', 'VR5-zone1', 'VR6-zone2'),
    *zone_hunt(2, 'VR6-zone2', 'VR6f-flame2'),
    *flame_use(2, 'VR6f-flame2', 'VR6-zone2', 'VR7-zone3'),
    *zone_hunt(3, 'VR7-zone3', None, laps_then=(1, 'VR7r-recall')),

    # Back to the entrance, then zone 1 until the tasks are done
    *gem_step('VR7r-recall', f'/ub usei {RECALL_GEM}', m.expr_c(near(BASE, 40)), 'VR3-enter', 'the entrance'),
    ('VR7r-recall', m.always(), m.all_a(m.expr_a('setvar[VRFinal,2]'), m.chat('/ub myquests'))),

    ('VR8-final', m.always(), m.all_a(*hunting(), nav(ZONES[1][0]), m.watchdog('VR8-final-unstick', STUCK_METERS, STUCK_SECONDS),
                                      m.chat('/ub myquests'), stopwatch('VRQuestSW'),
                                      m.expr_a(f'ifthen[testvar[VRHuntSW],`0`,`setvar[VRHuntSW,stopwatchstart[stopwatchcreate[]]]`]'))),
    ('VR8-final', m.expr_c(elapsed('VRQuestSW', REFRESH_SECONDS)), m.all_a(m.chat('/ub myquests'), stopwatch('VRQuestSW'))),
    ('VR8-final', m.all_c(m.seconds_in_state(8), m.expr_c(ALL_DONE)),
     m.all_a(echo('all kill tasks done, turning in'), m.set_state('VR9-turnin'))),
    ('VR8-final', m.expr_c(elapsed('VRHuntSW', HUNT_SECONDS)),
     m.all_a(echo(f'{HUNT_SECONDS // 60} minute hunt limit reached, turning in'), m.set_state('VR9-turnin'))),
    died('VR8-final'),
    ('VR8-final-unstick', m.always(), m.all_a(m.clear_watchdog(), m.opt('EnableCombat', False), m.opt('EnableLooting', False),
                                              m.opt('EnableNav', True),
                                              echo('zone 1: not moving, walking on'))),
    ('VR8-final-unstick', m.seconds_in_state(UNSTICK_SECONDS), m.set_state('VR8-final')),
    died('VR8-final-unstick'),

    # Turn in
    ('VR9-turnin', m.all_c(m.always(), m.expr_c(f'{count(DERU_GEM)}<1')),
     m.all_a(*travel(), echo('no Deru gem left, the tasks will be turned in next time'), m.set_state('VR11-end'))),
    *gem_step('VR9-turnin', f'/ub usei {DERU_GEM}', m.expr_c(near(CENTER, 40)), 'VR9a-center', 'the center',
              used_up=DERU_GEM),
    *center_route('VR9a-center', [m.chat('/ub myquests'), m.chat('/b d'), m.set_state('VR11-end')]),

    ('VR10-dead', m.seconds_in_state(20), m.set_state('VR1-start')),

    ('VR11-end', m.always(), m.all_a(NO_NAV, m.opt('EnableNav', False), m.opt('EnableCombat', False),
                                     m.opt('EnableLooting', False), chain())),
]

if __name__ == '__main__':
    out = os.path.join(VT, 'aKTViridian.met')
    data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}' + (' (test build)' if TEST else ''))
