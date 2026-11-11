"""Example meta: travel to the spot under the pillar in Jam, then farm there.  Builds aFJamPillar.met in the VT folder.

    py tools/vt/examples/aFJamPillar.py [<VT folder>]

A variation of aFJamBridge.py: the same trip (aFJam-goto.nav, to the drop at the bottom of the shaft; jamturnin.py
hands in Strawberry Jam Jars to the Factory Foreman there), then aFJamPillar-tail.nav (a jump at 135 from the drop) and
JamUnderPillar.nav to hold the spot.
"""
import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import rectl
import vtmeta as m
import jamturnin

VT = sys.argv[1] if len(sys.argv) > 1 else rectl.vt_dir()
JAM = jamturnin.JAM

# "At the pillar": within 4 m (flat) of JamUnderPillar.nav's point (28.2383N, 100.9380W) and down on its floor.
# Moved 3.4 m NW from (28.2285N, 100.9278W), which was too far from the spawn for the attack range.
# The spot is on a cell boundary, so a LandCellE check missed it (the route ended 1.9 m away, in the next cell).
# Squared distance in nav units because sqrt isn't available, and "z + 0.4 < 0" because the parser rejects "<-".
# z is in nav units: the pillar floor is -0.45 (-108 m), the bottom of the shaft -0.375 (-90 m).
# UB functions need VTank.PatchExpressionEngine, which Default turns on.
_WE, _NS = 'coordinategetwe[getplayercoordinates[]]', 'coordinategetns[getplayercoordinates[]]'
AT_PILLAR = (f'({_WE}+100.9380)*({_WE}+100.9380)+({_NS}-28.2383)*({_NS}-28.2383)<{(4 / 240) ** 2:.6f}'
             '&&coordinategetz[getplayercoordinates[]]+0.4<0')

rules = [
    # Load the character's "Farm" profile.  It turns combat on, so wait for it before the next state changes options,
    # and keep nav off meanwhile so he doesn't walk toward the previously loaded nav.
    ('Default', m.always(), m.all_a(m.opt('EnableNav', False), m.chat('/ub opt set VTank.PatchExpressionEngine true'),
                                    m.chat('/vt settings loadchar Farm'))),
    ('Default', m.seconds_in_state(1), m.opt('EnableNav', False)),
    ('Default', m.all_c(m.seconds_in_state(2), m.expr_c(AT_PILLAR)), m.set_state('Farm')),
    ('Default', m.all_c(m.seconds_in_state(2), m.expr_c(jamturnin.AT_DROP)), m.set_state('JamAtDrop')),
    ('Default', m.all_c(m.seconds_in_state(2), m.not_c(m.expr_c(AT_PILLAR)), m.not_c(m.expr_c(jamturnin.AT_DROP))),
     m.set_state('JamGoTo')),

    # Travel without fighting: to the drop, hand in jam jars (JamGoTo ... JamFace), then from the drop to the pillar
    *jamturnin.rules('GoToPillar'),
    ('GoToPillar', m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True),
                                       jamturnin.nav('aFJamPillar-tail.nav'))),
    ('GoToPillar', m.expr_c(AT_PILLAR), m.set_state('Farm')),
    ('GoToPillar', m.died(), m.set_state('Dead')),

    # Hold the spot and fight
    ('Farm', m.always(), m.all_a(m.embed_nav(os.path.join(VT, 'JamUnderPillar.nav')), m.opt('EnableNav', True),
                                 m.opt('EnableCombat', True), m.chat('/vt setattackbar 0.3'))),
    ('Farm', m.died(), m.set_state('Dead')),
    ('Farm', m.not_c(m.landblock(JAM)), m.set_state('Default')),  # left the dungeon: travel back

    ('Dead', m.seconds_in_state(20), m.set_state('Default')),
]

if __name__ == '__main__':
    out = os.path.join(VT, 'aFJamPillar.met')
    data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}')
