"""Rynthid kill tasks meta.  Builds aKTRynthid.met in the VT folder.

    py tools/vt/examples/aKTRynthid.py [<VT folder>]

Needs aKTRynthid-getq.nav and aKTRynthid-gotoloop.nav (built from the .spec files next to this script with
vtnav.py build) and RynthidHunt.nav (the user's hunting loop) in the VT folder.  They're embedded, so the meta
doesn't need them at runtime; rebuild the meta after changing a nav.

States (modelled on aKTTouTou / aKTHoshino):
  Default -> RK1-start -> RK2-getq (gem, portals, talk to the 9 NPCs, use their contracts)
  -> RK3-gotohunt -> RK4-huntloop <-> RK5-stabbin -> RK6-turnin (the getq route again) -> ** Chain **

Leftover "Contract for Kill: ..." items are used (which deletes them) every few seconds in RK3, RK4 and RK6, and
the turn-in waits for them, up to TURNIN_SECONDS.  Not in RK5, where uses get dropped during combat.

The hunt ends when every task is done, or after HUNT_SECONDS.  A task is done when:
* the server says so while hunting: "You have killed N <mobs>! Your task is complete!" (repeated on every later kill)
  sets the task's RKDone* variable, or
* the /ub myquests refresh sent after picking up the tasks shows it full (getquestktprogress >= getquestktrequired).
  This covers tasks finished on an earlier day, and tasks that weren't handed out (cooldown): those have no
  counter, so their required count is 0.
/myquests lists every quest the character has (~700 lines), so it's only sent after getting the tasks and after
the turn-in, plus every REFRESH_SECONDS while hunting if that's set.  The Rare Boss task is left out: it needs a
rare spawn.
Quest functions need VTank.PatchExpressionEngine; test them with /ub mexec (not /vt mexec).
"""
import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import rectl
import vtmeta as m

VT = sys.argv[1] if len(sys.argv) > 1 else rectl.vt_dir()
HUNT_SECONDS = 900     # give up hunting and turn in after this long
REFRESH_SECONDS = None # /ub myquests interval while hunting (backup for missed kill lines); None = never
TURNIN_SECONDS = 240   # the getq route takes ~2 minutes; also the limit for walking down to the loop
CHAIN = 'aKTNeftet'    # meta to load after turn-in; None stops there
TARGET_RANGE = 40      # meters: fight task monsters this close to the loop

# (quest flag from /myquests, done variable, plural mob names in the server's kill message).  Variants in one group
# share the task's counter.  Names are from the kill lines seen on 2026-11-07.  Rares (killtaskrynthidrares0813,
# Rynthid Berserkers / Ravagers) left out.
TASKS = [
    ('killtaskrynthidcrystals',      'RKDoneCrystals',  'Rynthid Crystals'),               # Lohrn, 6
    ('killtaskrynthidsorcerers0813', 'RKDoneSorcerers', '(Raging )?Rynthid Sorcerers'),    # Wido, 15
    ('killtaskrynthidrifts0813',     'RKDoneRifts',     'Rift of [A-Za-z ]+'),             # Rhys, 10 (4 kinds)
    ('killtaskrynthidragers0813',    'RKDoneRagers',    'Rynthid Ragers'),                 # Ranulf, 15
    ('killtaskrynthidslayers0813',   'RKDoneSlayers',   'Rynthid Slayers'),                # Sylvanus, 15
    ('killtaskrynthidminions0813',   'RKDoneMinions',   'Rynthid Minion(s| of Rages)'),    # Marcus, 25
    ('rynthidcowkillcount',          'RKDoneCows',      'Rynthid Cows'),                   # Royal Guard, 25
    ('killtaskrynthidwisps0813',     'RKDoneWisps',     'Empowered [A-Za-z]+ Wisps'),      # Tillahan, 3 (3 kinds)
]
def task_complete(names):
    return f'^You have killed [0-9]+ {names}! Your task is complete!'
ALL_DONE = 'isrefreshingquests[]==0&&' + '&&'.join(
    f'(getvar[{v}]==1||getquestktprogress[{f}]>=getquestktrequired[{f}])' for f, v, _ in TASKS)

# Monsters that count for a task (names from a world scan and the kill messages).  The "Discorporate Rynthid of ..."
# types don't count for anything.  Rynthid Berserker is the Rare Boss task ("Rynthid Berserkers! Your task is
# complete!").
TARGETS = '^(Rynthid (Minion|Sorcerer|Cow|Crystal|Slayer|Rager|Berserker)|Raging Rynthid Sorcerer|Rift of |Empowered .*Wisp)'

# Flat distance checks in nav units (squared, like aFJamPillar.py).
_WE, _NS = 'coordinategetwe[getplayercoordinates[]]', 'coordinategetns[getplayercoordinates[]]'
def near(ew, ns, meters):
    return f'({_WE}-({ew}))*({_WE}-({ew}))+({_NS}-({ns}))*({_NS}-({ns}))<{(meters / 240) ** 2:.6f}'
AT_NPCS = near(-63.217, -61.588, 15)     # end of the getq route, next to the NPCs
AT_LOOP = near(-65.214, -62.011, 30)     # end of the gotoloop route, by RynthidHunt.nav's first point

# "Contract for Kill: ..." items the NPCs hand out.  The getq nav uses each one after its NPC, but a use sent while
# the character is busy is dropped silently, which left contracts in the pack.  Using one (even a duplicate of a contract
# already taken) deletes it.  No ^ anchor: UB expressions reject it.
HAS_CONTRACTS = 'getitemcountininventorybynamerx[Contract for Kill]>0'
CONTRACT_RETRY_SECONDS = 3
CONTRACT_DUE = (f'{HAS_CONTRACTS}&&ifthen[testvar[RKContractSW],'
                f'`stopwatchelapsedseconds[$RKContractSW]>={CONTRACT_RETRY_SECONDS}`,`1`]')

HUNT_TIMEOUT = f'ifthen[testvar[RKHuntSW],`stopwatchelapsedseconds[$RKHuntSW]>={HUNT_SECONDS}`,`0`]'
REFRESH_DUE = f'ifthen[testvar[RKQuestSW],`stopwatchelapsedseconds[$RKQuestSW]>={REFRESH_SECONDS}`,`0`]'

def nav(name):
    return m.embed_nav(os.path.join(VT, name))

def travel():
    return [m.opt('EnableCombat', False), m.opt('EnableLooting', False), m.opt('EnableNav', True)]

def kill_lines(state):
    """Mark each task done when the server says it's complete."""
    return [(state, m.chat_message(task_complete(names)), m.expr_a(f'setvar[{v},1]')) for _, v, names in TASKS]

def use_contracts(state):
    """Use leftover contracts one at a time, retrying every CONTRACT_RETRY_SECONDS until none are left."""
    return [(state, m.expr_c(CONTRACT_DUE),
             m.all_a(m.chat('/ub usepi Contract for Kill'),
                     m.expr_a('setvar[RKContractSW,stopwatchstart[stopwatchcreate[]]]')))]

def hunt_checks(state):
    """Rules shared by the hunting states: task completions, (optional) refresh, finish, give up."""
    refresh = [] if not REFRESH_SECONDS else [
        (state, m.expr_c(REFRESH_DUE),
         m.all_a(m.chat('/ub myquests'), m.expr_a('setvar[RKQuestSW,stopwatchstart[stopwatchcreate[]]]')))]
    return kill_lines(state) + refresh + [
        (state, m.all_c(m.seconds_in_state(5), m.expr_c(ALL_DONE)),
         m.all_a(*travel(), m.chat('/vt echo Rynthid: all tasks done, turning in'), m.set_state('RK6-turnin'))),
        (state, m.expr_c(HUNT_TIMEOUT),
         m.all_a(*travel(), m.chat(f'/vt echo Rynthid: {HUNT_SECONDS // 60} minute hunt limit reached, turning in'),
                 m.set_state('RK6-turnin'))),
        (state, m.died(), m.set_state('RK7-dead')),
    ]

rules = [
    ('Default', m.chat_message('^\\[Fellowship\\] (|).* (say|says), \\"!start\\"$'), m.set_state('RK1-start')),
    ('Default', m.seconds_in_state_persist(10), m.set_state('RK1-start')),

    ('RK1-start', m.always(), m.all_a(
        *travel(), m.opt('EnableBuffing', True), m.opt('IdlePeaceMode', False), m.opt('NavPriorityBoost', False),
        m.chat('/ub opt set VTank.PatchExpressionEngine True'), m.chat('/vt opt set attackdistance .1667'),
        m.chat('/vt setattackbar 0.1'),
        # Forget completions from an earlier run; the refresh after getting the tasks has the server's counts
        *[m.expr_a(f'clearvar[{v}]') for _, v, _ in TASKS])),
    ('RK1-start', m.seconds_in_state(3), m.set_state('RK2-getq')),

    # Gem, two portals, then each NPC.  Done at the end of the route, next to the NPCs.
    ('RK2-getq', m.always(), m.all_a(*travel(), nav('aKTRynthid-getq.nav'))),
    ('RK2-getq', m.all_c(m.seconds_in_state(20), m.nav_empty(), m.expr_c(AT_NPCS)),
     m.all_a(m.chat('/ub myquests'), m.expr_a('setvar[RKHuntSW,stopwatchstart[stopwatchcreate[]]]'),
             m.expr_a('setvar[RKQuestSW,stopwatchstart[stopwatchcreate[]]]'), m.set_state('RK3-gotohunt'))),
    ('RK2-getq', m.seconds_in_state(TURNIN_SECONDS),
     m.all_a(m.chat('/vt echo Rynthid: getting the tasks took too long, starting over'), m.set_state('RK1-start'))),

    # Walk down to the hunting loop
    ('RK3-gotohunt', m.always(), m.all_a(*travel(), nav('aKTRynthid-gotoloop.nav'))),
    *use_contracts('RK3-gotohunt'),
    ('RK3-gotohunt', m.any_c(m.expr_c(AT_LOOP), m.all_c(m.seconds_in_state(10), m.nav_empty())),
     m.all_a(m.expr_a('setvar[RKHuntSW,stopwatchstart[stopwatchcreate[]]]'), m.set_state('RK4-huntloop'))),
    ('RK3-gotohunt', m.died(), m.set_state('RK7-dead')),
    ('RK3-gotohunt', m.seconds_in_state(TURNIN_SECONDS),
     m.all_a(m.chat('/vt echo Rynthid: going to the hunt took too long, starting over'), m.set_state('RK1-start'))),
    # Every task on cooldown (no counters after the refresh RK2 sent): nothing to hunt or turn in
    ('RK3-gotohunt', m.all_c(m.seconds_in_state(8), m.expr_c(ALL_DONE)),
     m.all_a(m.opt('EnableNav', False), m.chat('/vt echo Rynthid: no open tasks'), m.set_state('** Chain **'))),

    # Patrol the loop with combat off; fight when a task monster is close
    ('RK4-huntloop', m.always(), m.all_a(*travel(), nav('RynthidHunt.nav'))),
    ('RK4-huntloop', m.monster_count(TARGETS, 1, TARGET_RANGE),
     m.all_a(m.opt('EnableCombat', True), m.set_state('RK5-stabbin'))),
    *hunt_checks('RK4-huntloop'),
    *use_contracts('RK4-huntloop'),

    ('RK5-stabbin', m.monster_count(TARGETS, 1, TARGET_RANGE), m.opt('EnableCombat', True)),
    ('RK5-stabbin', m.all_c(m.no_monsters_within(5), m.not_c(m.monster_count(TARGETS, 1, TARGET_RANGE))),
     m.all_a(m.opt('EnableCombat', False), m.set_state('RK4-huntloop'))),
    ('RK5-stabbin', m.seconds_in_state_persist(90), m.all_a(m.opt('EnableCombat', False), m.set_state('RK4-huntloop'))),
    *hunt_checks('RK5-stabbin'),

    # Turn in: the getq route again (the NPCs reward finished tasks).  Done once no contracts are left in the pack.
    ('RK6-turnin', m.always(), m.all_a(*travel(), nav('aKTRynthid-getq.nav'))),
    *use_contracts('RK6-turnin'),
    ('RK6-turnin', m.any_c(m.all_c(m.seconds_in_state(20), m.nav_empty(), m.expr_c(AT_NPCS),
                                   m.not_c(m.expr_c(HAS_CONTRACTS))),
                           m.seconds_in_state(TURNIN_SECONDS)),
     m.all_a(m.opt('EnableNav', False), m.chat('/ub myquests'),
             m.expr_a('setvar[RKQuestSW,stopwatchstart[stopwatchcreate[]]]'), m.chat('/b d'),
             m.set_state('** Chain **'))),

    # Died: wait for the lifestone, then start over (the NPCs just report progress for tasks already taken)
    ('RK7-dead', m.seconds_in_state(20), m.set_state('RK1-start')),

    ('** Chain **', m.always(),
     m.chat(f'/vt meta load {CHAIN}') if CHAIN else m.chat('/vt echo Rynthid: kill tasks finished')),
]

if __name__ == '__main__':
    out = os.path.join(VT, 'aKTRynthid.met')
    data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}')
