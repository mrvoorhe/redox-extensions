"""Hoshino kill tasks meta.  Builds aKTHoshino.met in the VT folder.

    py tools/vt/examples/aKTHoshino.py [<VT folder>]

The hunt, kill tracking and resume rules are the original hand-made aKTHoshino.met (2026-11-06), copied rule for
rule.  What's new is the travel, which the original did as one state with one-shot uses.  On 2026-11-10 Cowhead
used the gem while VT was still casting buffs.  The use was dropped silently, nothing tried it again, and the
character stood still for 2.8 hours.

Travel (to get the tasks, and again to turn them in) is now one state per step.  Each step ends at a teleport,
so it is done when the character is in the destination landblock, wherever it started:

  gem   /ub usei Halls of Introduction Gem   (from anywhere)   -> Halls of Introduction  0x012F
  halls walk, /ub uselp Daily Portals                          -> Daily Portals hub      0x6444
  tent  walk, /ub uselp Lord Brem's War Tent                   -> Hoshino NPC area       0x49E4
  npcs  walk the NPC route (talk to each)                      -> hunt entrance cell 0x49E40032

A step re-embeds its nav (which does the use again) at RETRY_SECONDS.  If it isn't in the destination landblock by
GIVE_UP_SECONDS, it echoes, clears the run's pvars and goes to ** Chain **.  A step that finds the character
already in a later landblock (e.g. resuming after a relog) skips ahead.  The uses are UB commands, not /mt,
because on 2026-11-10 Mag-Tools was broken in Virmar's client and every /mt command did nothing.

The navs are next to this script, not in the VT folder: they're embedded, so the meta doesn't need them at
runtime.  Rebuild the meta after changing one.
  aKTHoshino-gem/halls/tent.nav   built from the .spec files of the same name:
                                  py tools/vt/vtnav.py build tools/vt/examples/aKTHoshino-gem.spec tools/vt/examples/aKTHoshino-gem.nav
  aKTHoshino-getquest/gotoloop/T1..T5.nav   exported unchanged from the original meta's embedded navs
"""
import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import rectl
import vtmeta as m

VT = sys.argv[1] if len(sys.argv) > 1 else rectl.vt_dir()
NAVS = os.path.dirname(os.path.abspath(__file__))
RETRY_SECONDS = (15, 30, 90)   # do a travel step's use again at these times
GIVE_UP_SECONDS = 120          # not at the step's destination by then: go to the next meta
HUNT_SECONDS = 720
CHAIN = 'aKTRynthid'

HALLS, HUB, NPC_AREA = 0x012F0000, 0x64440000, 0x49E40000   # landblocks
HUNT_ENTRANCE = 0x49E40032      # landcell at the end of the NPC route
LOOP_START = 0x4BE3002B

ALL_TASKS = ['HKCow', 'HKArcher', 'HKMage', 'HKSamurai', 'HKGolem', 'HKSarco', 'HKNanjou', 'HKClaw', 'HKBushi',
             'HKMinion']
ALL_DONE = '&&'.join(f'@{t}==1' for t in ALL_TASKS)
ANY_OPEN = '||'.join(f'@{t}<1' for t in ALL_TASKS)

HUNT_TIMEOUT = f'ifthen[testvar[HoshinoHuntSW],`stopwatchelapsedseconds[$HoshinoHuntSW]>={HUNT_SECONDS}`,`0`]'
START_SW = 'setvar[HoshinoHuntSW,stopwatchstart[stopwatchcreate[]]]'

# NPC tell -> the task variable it marks as handed out
NPC_TASKS = [('Sergeant Freng', 'HKSarco'), ('Royal Guard', 'HKCow'), ('Lieutenant Aurin', 'HKGolem'),
             ('Sergeant Trebuus', 'HKArcher'), ("Hanamoto Aki'ko", 'HKNanjou'), ('Lord Eorlinde', 'HKMage'),
             ('Lieutenant Takamaki', 'HKSamurai'), ('Griffon', 'HKClaw'), ('Corporal Irashi', 'HKBushi'),
             ('Aun Kirtal', 'HKMinion')]

# Kill line prefix -> task variable to set
KILL_LINES = [('25 Chrysoberyl', 'HKCow'), ('5 Spectral Blade Adept', 'HKClaw'), ('15 Spectral Minion', 'HKMinion'),
              ('10 Spectral Bushi', 'HKBushi'), ('5 Spectral Blade Master', 'HKClaw'),
              ('5 Spectral Claw Adept', 'HKClaw'), ('5 Spectral Nanjou Shou-jen', 'HKNanjou'),
              ('5 Iron Golem', 'HKGolem'), ('5 Bronze Golem', 'HKGolem'), ('4 Sarcoph', 'HKSarco'),
              ('5 Clay Golem', 'HKGolem'), ('10 Spectral Samurai', 'HKSamurai'),
              ('5 Spectral Bloodmage', 'HKMage'), ('5 Spectral Voidmage', 'HKMage'),
              ('15 Spectral Archer', 'HKArcher')]

TARGETS = ['Sarcophagus', 'Spectral Nanjou Shou-jen', 'Chrysoberyl Cow', 'Spectral Archer']


def nav(name):
    return m.embed_nav(os.path.join(NAVS, name))


def setup(*extra):
    """Options and commands set when a run starts or resumes."""
    return [m.opt('EnableCombat', False), m.opt('EnableNav', True), m.opt('EnableBuffing', True),
            m.opt('EnableLooting', False), m.opt('NavPriorityBoost', False), m.opt('IdlePeaceMode', False),
            m.chat('/ub opt set VTank.PatchExpressionEngine True'), m.chat('/vt opt set attackdistance .12'),
            m.chat('/vt setattackbar 0.1'), *extra]


def hunt_timeout(state):
    return (state, m.expr_c(HUNT_TIMEOUT),
            m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True),
                    m.chat(f'/vt echo Hoshino: {HUNT_SECONDS // 60} minute hunt limit reached, turning in'),
                    m.set_state('Hoshino6-turnin')))


def kill_lines(state):
    return [(state, m.chat_message(f'^You have killed {text}'), m.expr_a(f'@{var}=1')) for text, var in KILL_LINES]


def travel(states, purpose):
    """The gem / halls / tent steps.  states: names for (gem, halls, tent, npcs)."""
    gem, halls, tent, npcs = states
    steps = [(gem, 'aKTHoshino-gem.nav', 'the Halls of Introduction', [(HALLS, halls), (HUB, tent), (NPC_AREA, npcs)]),
             (halls, 'aKTHoshino-halls.nav', 'the Daily Portals hub', [(HUB, tent), (NPC_AREA, npcs)]),
             (tent, 'aKTHoshino-tent.nav', 'the Hoshino NPCs', [(NPC_AREA, npcs)])]
    rules = []
    for state, nav_name, where, ahead in steps:
        rules.append((state, m.always(), nav(nav_name)))
        rules += [(state, m.landblock(block), m.set_state(to)) for block, to in ahead]
        rules += [(state, m.seconds_in_state(s), nav(nav_name)) for s in RETRY_SECONDS]
        rules.append((state, m.seconds_in_state(GIVE_UP_SECONDS),
                      m.all_a(m.opt('EnableNav', False),
                              m.chat(f'/vt echo Hoshino: could not get to {where} {purpose}, skipping to the next meta'),
                              m.expr_a('clearallpvars[]'), m.set_state('** Chain **'))))
    return rules


GOTO = ('Hoshino2-goto', 'Hoshino2a-halls', 'Hoshino2b-tent', 'Hoshino2c-npcs')
TURNIN = ('Hoshino6-turnin', 'Hoshino6a-halls', 'Hoshino6b-tent', 'Hoshino6c-npcs')

rules = [
    ('Default', m.chat_message('^\\[Fellowship\\] (|).* (say|says), \\"!start\\"$'), m.set_state('Hoshino1-start')),
    ('Default', m.all_c(m.seconds_in_state(5), m.expr_c('testpvar[HoshinoRun]==1')),
     m.all_a(*setup(m.chat('/vt echo Hoshino: resuming after relog'), m.set_state('Hoshino0-resume')))),
    ('Default', m.all_c(m.seconds_in_state_persist(10), m.expr_c('testpvar[HoshinoRun]==0')),
     m.set_state('Hoshino1-start')),

    ('Hoshino0-resume', m.expr_c(ALL_DONE), m.set_state('Hoshino6-turnin')),
    ('Hoshino0-resume', m.expr_c(ANY_OPEN), m.set_state('Hoshino2-goto')),

    ('Hoshino1-start', m.always(),
     m.all_a(*setup(m.expr_a('clearallpvars[]'), m.expr_a('setpvar[HoshinoRun,1]')))),
    ('Hoshino1-start', m.any_c(m.expr_c(
        'getqueststatus[ChrysoberylCowKillCount]==1||getqueststatus[SarcophagusKillCount]==1||'
        'getqueststatus[KillTaskSpectralArchers0812]==1||getqueststatus[KillTaskSpectralBloodMages0812]==1||'
        'getqueststatus[KillTaskSpectralBushi0812]==1||getqueststatus[KillTaskSpectralClawsBlades0812]==1||'
        'getqueststatus[KillTaskSpectralMinions0812]==1||getqueststatus[KillTaskSpectralNinjas0812]==1||'
        'getqueststatus[KillTaskSpectralSamurai0812]==1||getqueststatus[KillTaskGolemSamurai0812]==1')),
     m.expr_a('false')),
    ('Hoshino1-start', m.seconds_in_state(5), m.set_state('Hoshino2-goto')),

    # Get the tasks
    *travel(GOTO, 'to get the tasks'),
    ('Hoshino2c-npcs', m.always(), nav('aKTHoshino-getquest.nav')),
    *[('Hoshino2c-npcs', m.chat_message(f'{npc} tells you'), m.all_a(m.expr_a(f'touchpvar[{var}]')))
      for npc, var in NPC_TASKS],
    ('Hoshino2c-npcs', m.landcell(HUNT_ENTRANCE), m.all_a(m.expr_a(START_SW), m.set_state('Hoshino3-huntstart'))),

    # Hunt
    ('Hoshino3-huntstart', m.always(), nav('aKTHoshino-gotoloop.nav')),
    ('Hoshino3-huntstart', m.landcell(LOOP_START), m.set_state('Hoshino4-huntloop')),
    hunt_timeout('Hoshino3-huntstart'),

    ('Hoshino4-huntloop', m.landcell(0x4BE3002B), nav('aKTHoshino-T1.nav')),
    ('Hoshino4-huntloop', m.landcell(0x4CE20026), nav('aKTHoshino-T2.nav')),
    ('Hoshino4-huntloop', m.landcell(0x4BE00030), nav('aKTHoshino-T3.nav')),
    ('Hoshino4-huntloop', m.landcell(0x4BE10039), nav('aKTHoshino-T4.nav')),
    ('Hoshino4-huntloop', m.landcell(0x4CE20034), nav('aKTHoshino-T5.nav')),
    *[('Hoshino4-huntloop', m.monster_count(t, 1, 15), m.set_state('Hoshino5-getstabbin')) for t in TARGETS],
    ('Hoshino4-huntloop', m.seconds_in_state(5), m.chat('/ub listpvars')),
    ('Hoshino4-huntloop', m.all_c(m.any_c(m.expr_c(ALL_DONE))),
     m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True), m.set_state('Hoshino6-turnin'))),
    ('Hoshino4-huntloop', m.landcell(0x4BE3002C), nav('aKTHoshino-T1.nav')),
    *kill_lines('Hoshino4-huntloop'),
    hunt_timeout('Hoshino4-huntloop'),

    *kill_lines('Hoshino5-getstabbin'),
    ('Hoshino5-getstabbin', m.monster_count('Spectral Archer', 1, 15), m.opt('enablecombat', True)),
    ('Hoshino5-getstabbin', m.monster_count('Chrysoberyl Cow', 1, 15), m.opt('EnableCombat', True)),
    ('Hoshino5-getstabbin', m.monster_count('Spectral Nanjou Shou-jen', 1, 15), m.opt('EnableCombat', True)),
    ('Hoshino5-getstabbin', m.monster_count('Sarcophagus', 1, 15), m.opt('EnableCombat', True)),
    ('Hoshino5-getstabbin',
     m.all_c(m.no_monsters_within(5), *[m.not_c(m.monster_count(t, 1, 15)) for t in
                                        ['Spectral Archer', 'Chrysoberyl Cow', 'Spectral Nanjou Shou-jen',
                                         'Sarcophagus']]),
     m.all_a(m.opt('EnableCombat', False), m.set_state('Hoshino4-huntloop'))),
    ('Hoshino5-getstabbin', m.seconds_in_state_persist(90),
     m.all_a(m.opt('EnableCombat', False), m.set_state('Hoshino4-huntloop'))),
    hunt_timeout('Hoshino5-getstabbin'),

    # Turn in: the same route; the NPCs reward finished tasks
    *travel(TURNIN, 'to turn in'),
    ('Hoshino6c-npcs', m.always(), nav('aKTHoshino-getquest.nav')),
    ('Hoshino6c-npcs', m.landcell(HUNT_ENTRANCE),
     m.all_a(m.opt('EnableNav', False), m.chat('/b d'), m.expr_a('clearallpvars[]'), m.set_state('** Chain **'))),

    ('** Chain **', m.always(), m.chat(f'/vt meta load {CHAIN}')),
]

if __name__ == '__main__':
    out = os.path.join(VT, 'aKTHoshino.met')
    data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}')
