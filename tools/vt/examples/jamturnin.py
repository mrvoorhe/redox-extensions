"""Shared part of aFJamBridge.py and aFJamPillar.py: travel to the drop in Jam, hand Strawberry Jam Jars to the Factory
Foreman there, then go back to the drop and on to the farming spot.

States (prefix "Jam"):
  JamGoTo       aFJam-goto.nav: Town Network gem ... Help Wanted portal, ends at the drop (bottom of the shaft)
  JamAtDrop     100+ jars: JamToForeman, else the meta's own tail state
  JamToForeman  aFJam-foreman.nav, then JamGive1
  JamGive1/2    one give each on entry, handing over to each other every 2 s while there are 100+ jars
  JamDeposit    /b d (deposits the Enlightened Coins he pays too: "Deposited all currencies!")
  JamBack       aFJam-drop.nav, then face 180 (the heading you land with) and go to the tail state

The foreman takes 100 jars per give (the one given plus 99 more) and pays 10 Enlightened Coins (Cowhead, 2026-11-11).
Gives stop below 100 jars, so he is never asked when you don't have enough.  Tested from the foreman on Cowhead: 14
gives, deposit, back to the drop, then aFJamPillar's tail to the pillar.
"""
import os

import vtmeta as m

HERE = os.path.dirname(os.path.abspath(__file__))
JAM = 0x01A30000         # the Jam dungeon landblock
FOREMAN = 694200123      # Factory Foreman's template type
JAR = 'Strawberry Jam Jar'
PER_GIVE = 100           # jars the foreman takes per give; skip the turn-in below this
GIVE_TRIES = 40          # safety cap on gives per turn-in (4000 jars)
DROP_HEADING = 180       # heading on landing at the drop

_P = 'getplayercoordinates[]'
_WE, _NS, _Z = f'coordinategetwe[{_P}]', f'coordinategetns[{_P}]', f'coordinategetz[{_P}]'


def near(ew, ns, meters):
    """Squared flat distance in nav units (sqrt isn't available)."""
    return f'({_WE}-({ew}))*({_WE}-({ew}))+({_NS}-({ns}))*({_NS}-({ns}))<{(meters / 240) ** 2:.8f}'


# At the drop: within 8 m of where you land and on the shaft floor (z -0.375 = -90 m; the pillar floor is -0.45 and
# the bridge -0.425).  "+0.36<0" because the parser rejects "<-".
AT_DROP = f'{near(-100.942167, 28.243625, 8)}&&{_Z}+0.36<0&&{_Z}+0.40>0'
AT_FOREMAN = near(-100.9523, 28.2246, 4)
ENOUGH_JARS = f'getitemcountininventorybyname[{JAR}]>={PER_GIVE}'


def nav(name):
    return m.embed_nav(os.path.join(HERE, name))


def echo(text):
    return m.chat(f'/vt echo Jam: {text}')


def rules(tail_state):
    """The Jam* states.  The meta goes to JamGoTo to travel (or JamAtDrop when already at the drop), and they end in
    tail_state, standing on the drop facing 180, where the meta loads its own tail nav."""
    give = m.all_a(m.expr_a(f'actiontrygiveitem[wobjectfindininventorybyname[{JAR}],'
                            f'wobjectfindnearestbytemplatetype[{FOREMAN}]]'),
                   m.expr_a('setvar[JamGiveN,getvar[JamGiveN]+1]'))
    r = [
        # Travel without fighting.  The nav ends with the Help Wanted portal; landing at the drop ends the state.  If
        # the drop check misses (landed off it), the nav runs out in Jam: go on anyway, the next navs start at the drop.
        ('JamGoTo', m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True), nav('aFJam-goto.nav'))),
        ('JamGoTo', m.expr_c(AT_DROP), m.set_state('JamAtDrop')),
        ('JamGoTo', m.all_c(m.seconds_in_state(10), m.nav_empty(), m.landblock(JAM)), m.set_state('JamAtDrop')),

        ('JamAtDrop', m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', False))),
        ('JamAtDrop', m.all_c(m.seconds_in_state(1), m.expr_c(ENOUGH_JARS)), m.set_state('JamToForeman')),
        ('JamAtDrop', m.all_c(m.seconds_in_state(1), m.not_c(m.expr_c(ENOUGH_JARS))), m.set_state(tail_state)),

        ('JamToForeman', m.always(), m.all_a(m.expr_a('setvar[JamGiveN,0]'), m.opt('EnableNav', True),
                                             nav('aFJam-foreman.nav'), m.chat('/ub combatstate peace'))),
        ('JamToForeman', m.all_c(m.seconds_in_state(2), m.nav_empty(), m.expr_c(AT_FOREMAN)), m.set_state('JamGive1')),
        ('JamToForeman', m.seconds_in_state(30), m.all_a(echo('could not get to the Factory Foreman'),
                                                         m.set_state('JamBack'))),

        ('JamDeposit', m.always(), m.chat('/b d')),
        ('JamDeposit', m.seconds_in_state(3), m.set_state('JamBack')),

        ('JamBack', m.always(), m.all_a(m.opt('EnableNav', True), nav('aFJam-drop.nav'))),
        ('JamBack', m.all_c(m.seconds_in_state(2), m.nav_empty()), m.set_state('JamFace')),
        ('JamBack', m.seconds_in_state(30), m.set_state('JamFace')),  # the tail nav starts at the drop anyway

        ('JamFace', m.always(), m.chat(f'/ub face {DROP_HEADING}')),
        ('JamFace', m.seconds_in_state(2), m.set_state(tail_state)),
    ]
    # Like aKTViridian's essence gives: two states that hand over to each other, one give on entry, and decide only at
    # the 2 s handover (while a give is going through the count can read low)
    for a, b in (('JamGive1', 'JamGive2'), ('JamGive2', 'JamGive1')):
        r += [(a, m.always(), give),
              (a, m.all_c(m.seconds_in_state(2), m.expr_c(ENOUGH_JARS)), m.set_state(b)),
              (a, m.all_c(m.seconds_in_state(2), m.not_c(m.expr_c(ENOUGH_JARS))), m.set_state('JamDeposit')),
              (a, m.expr_c(f'getvar[JamGiveN]>={GIVE_TRIES}'),
               m.all_a(echo('gave the most allowed, stopping'), m.set_state('JamDeposit')))]
    for s in ('JamGoTo', 'JamAtDrop', 'JamToForeman', 'JamGive1', 'JamGive2', 'JamDeposit', 'JamBack', 'JamFace'):
        r.append((s, m.died(), m.set_state('Dead')))
    return r
