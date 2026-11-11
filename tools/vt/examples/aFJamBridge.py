"""Example meta: travel to the bridge in Jam, then farm there.  Builds aFJamBridge.met in the VT folder.

    py tools/vt/examples/aFJamBridge.py [<VT folder>]

Tested on Rockdown 2026-11-05: from the Abandoned Mines start it loads the Farm profile, travels with combat off,
then holds the bridge with JamBridge.nav, nav and combat on and the attack bar at 0.3.

The trip is aFJam-goto.nav to the drop at the bottom of the shaft, where jamturnin.py hands in Strawberry Jam Jars to
the Factory Foreman, then aFJamBridge-tail.nav from the drop to the bridge.
"""
import os, sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import rectl
import vtmeta as m
import jamturnin

VT = sys.argv[1] if len(sys.argv) > 1 else rectl.vt_dir()
BRIDGE_CELL = 0x01A30153  # the farming spot (JamBridge.nav's only point)
JAM = jamturnin.JAM

rules = [
    # Load the character's "Farm" profile (--Rockdown_InfiniteLeaftide_Farm.usd).  It turns combat on, so wait for it
    # before the next state changes options.  It also turns nav on while the previous nav is still loaded, so turn nav
    # off again once it has loaded, or he walks toward that nav until the next state loads its own.
    ('Default', m.always(), m.all_a(m.opt('EnableNav', False), m.chat('/ub opt set VTank.PatchExpressionEngine true'),
                                    m.chat('/vt settings loadchar Farm'))),
    ('Default', m.seconds_in_state(1), m.opt('EnableNav', False)),
    ('Default', m.all_c(m.seconds_in_state(2), m.landcell(BRIDGE_CELL)), m.set_state('Farm')),
    ('Default', m.all_c(m.seconds_in_state(2), m.expr_c(jamturnin.AT_DROP)), m.set_state('JamAtDrop')),
    ('Default', m.all_c(m.seconds_in_state(2), m.not_c(m.landcell(BRIDGE_CELL)), m.not_c(m.expr_c(jamturnin.AT_DROP))),
     m.set_state('JamGoTo')),

    # Travel without fighting: to the drop, hand in jam jars (JamGoTo ... JamFace), then from the drop to the bridge
    *jamturnin.rules('GoToBridge'),
    ('GoToBridge', m.always(), m.all_a(m.opt('EnableCombat', False), m.opt('EnableNav', True),
                                       jamturnin.nav('aFJamBridge-tail.nav'))),
    ('GoToBridge', m.landcell(BRIDGE_CELL), m.set_state('Farm')),
    ('GoToBridge', m.died(), m.set_state('Dead')),

    # Hold the spot and fight
    ('Farm', m.always(), m.all_a(m.embed_nav(os.path.join(VT, 'JamBridge.nav')), m.opt('EnableNav', True),
                                 m.opt('EnableCombat', True), m.chat('/vt setattackbar 0.3'))),
    ('Farm', m.died(), m.set_state('Dead')),
    ('Farm', m.not_c(m.landblock(JAM)), m.set_state('Default')),  # left the dungeon: travel back

    ('Dead', m.seconds_in_state(20), m.set_state('Default')),
]

if __name__ == '__main__':
    out = os.path.join(VT, 'aFJamBridge.met')
    data = m.save(out, rules)
    print(f'wrote {len(rules)} rules, {len(data)} bytes to {out}')
