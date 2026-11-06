"""Rynthid kill tasks meta.  Builds aKTRynthid.met in the VT folder.

    py tools/vt/examples/aKTRynthid.py [<VT folder>]

Needs aKTRynthid-getq.nav and aKTRynthid-gotoloop.nav (built from the .spec files next to this script with
vtnav.py build) and RynthidHunt.nav (the user's hunting loop) in the VT folder.  They're embedded, so the meta
doesn't need them at runtime; rebuild the meta after changing a nav.

States (modelled on aKTTouTou / aKTHoshino):
  Default -> RK1-start -> RK2-getq (gem, portals, talk to the 9 NPCs, use their contracts)
  -> RK3-gotohunt -> RK4-huntloop <-> RK5-stabbin -> RK6-turnin (the getq route again) -> ** Chain **

The hunt ends when every task's kill counter is full (getquestktprogress >= getquestktrequired, refreshed with
/ub myquests every minute) or after HUNT_SECONDS.  A task that wasn't handed out (cooldown) has no counter, so its
required count is 0 and it doesn't hold the hunt up.  The Rare Boss task is left out: it needs a rare spawn.
Quest functions need VTank.PatchExpressionEngine; test them with /ub mexec (not /vt mexec).
"""
import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import rectl
import vtmeta as m

VT = sys.argv[1] if len(sys.argv) > 1 else rectl.vt_dir()
HUNT_SECONDS = 900     # give up hunting and turn in after this long
TURNIN_SECONDS = 240   # the getq route takes ~2 minutes
CHAIN = None           # meta to load after turn-in, e.g. 'aKTTouTou'; None stops there

# Kill counters from /myquests (diffed before/after talking to the NPCs).  Rares (killtaskrynthidrares0813) left out.
TASK_FLAGS = [
    'killtaskrynthidcrystals',        # Lohrn, 6 Rynthid Crystals
    'killtaskrynthidsorcerers0813',   # Wido, 15 (Raging) Rynthid Sorcerers
    'killtaskrynthidrifts0813',       # Rhys, 10 Rift of ...
    'killtaskrynthidragers0813',      # Ranulf, 15 Rynthid Ragers
    'killtaskrynthidslayers0813',     # Sylvanus, 15 Rynthid Slayers
    'killtaskrynthidminions0813',     # Marcus, 25 Rynthid Minions / Minions of Rage
    'rynthidcowkillcount',            # Royal Guard, 25 Rynthid Cows
    'killtaskrynthidwisps0813',       # Tillahan, 3 Empowered ... Wisps
]
ALL_DONE = 'isrefreshingquests[]==0&&' + '&&'.join(
    f'getquestktprogress[{f}]>=getquestktrequired[{f}]' for f in TASK_FLAGS)

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

HUNT_TIMEOUT = f'ifthen[testvar[RKHuntSW],`stopwatchelapsedseconds[$RKHuntSW]>={HUNT_SECONDS}`,`0`]'
REFRESH_DUE = 'ifthen[testvar[RKQuestSW],`stopwatchelapsedseconds[$RKQuestSW]>=60`,`0`]'

def nav(name):
    return m.embed_nav(os.path.join(VT, name))

def travel():
    return [m.opt('EnableCombat', False), m.opt('EnableLooting', False), m.opt('EnableNav', True)]

def hunt_checks(state):
    """Rules shared by the hunting states: refresh quest data, finish, give up."""
    return [
        (state, m.expr_c(REFRESH_DUE),
         m.all_a(m.chat('/ub myquests'), m.expr_a('setvar[RKQuestSW,stopwatchstart[stopwatchcreate[]]]'))),
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
        m.chat('/ub opt set VTank.PatchExpressionEngine True'), m.chat('/vt opt set attackdistance .12'),
        m.chat('/vt setattackbar 0.1'))),
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
    ('RK3-gotohunt', m.any_c(m.expr_c(AT_LOOP), m.all_c(m.seconds_in_state(10), m.nav_empty())),
     m.set_state('RK4-huntloop')),
    ('RK3-gotohunt', m.died(), m.set_state('RK7-dead')),
    # Every task on cooldown (no counters after the refresh RK2 sent): nothing to hunt or turn in
    ('RK3-gotohunt', m.all_c(m.seconds_in_state(8), m.expr_c(ALL_DONE)),
     m.all_a(m.opt('EnableNav', False), m.chat('/vt echo Rynthid: no open tasks'), m.set_state('** Chain **'))),

    # Patrol the loop with combat off; fight when a task monster is close
    ('RK4-huntloop', m.always(), m.all_a(*travel(), nav('RynthidHunt.nav'))),
    ('RK4-huntloop', m.monster_count(TARGETS, 1, 30),
     m.all_a(m.opt('EnableCombat', True), m.set_state('RK5-stabbin'))),
    *hunt_checks('RK4-huntloop'),

    ('RK5-stabbin', m.monster_count(TARGETS, 1, 30), m.opt('EnableCombat', True)),
    ('RK5-stabbin', m.all_c(m.no_monsters_within(5), m.not_c(m.monster_count(TARGETS, 1, 30))),
     m.all_a(m.opt('EnableCombat', False), m.set_state('RK4-huntloop'))),
    ('RK5-stabbin', m.seconds_in_state_persist(90), m.all_a(m.opt('EnableCombat', False), m.set_state('RK4-huntloop'))),
    *hunt_checks('RK5-stabbin'),

    # Turn in: the getq route again (the NPCs reward finished tasks)
    ('RK6-turnin', m.always(), m.all_a(*travel(), nav('aKTRynthid-getq.nav'))),
    ('RK6-turnin', m.any_c(m.all_c(m.seconds_in_state(20), m.nav_empty(), m.expr_c(AT_NPCS)),
                           m.seconds_in_state(TURNIN_SECONDS)),
     m.all_a(m.opt('EnableNav', False), m.chat('/ub myquests'), m.chat('/b d'), m.set_state('** Chain **'))),

    # Died: wait for the lifestone, then start over (the NPCs just report progress for tasks already taken)
    ('RK7-dead', m.seconds_in_state(20), m.set_state('RK1-start')),

    ('** Chain **', m.always(),
     m.chat(f'/vt meta load {CHAIN}') if CHAIN else m.chat('/vt echo Rynthid: kill tasks finished')),
]

if __name__ == '__main__':
    out = os.path.join(VT, 'aKTRynthid.met')
    data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}')
