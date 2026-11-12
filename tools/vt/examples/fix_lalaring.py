"""Patch LaLaRing.met so the guardian fight can't hang (see MetaAnalysis/2026-11-11/rockdown-lalaring-stuck.md).

On 2026-11-11, Rockdown's VT combat went idle in '1.3 Guardian Fight' with mobs attacking it.  Combat held the nav, so
the corpse-open nav (`/mt uselp Corpse of Decay`) stopped.  The state has no timeout, and the user had to toggle nav,
combat and loot by hand.

Changes (everything else, including all embedded navs, is kept byte for byte):
  1. Kick: after 25 s in 1.3, go to '1.3a Fight Kick'.  This turns nav off and combat off, turns combat back on after
     1 s, then returns to 1.3 after 15 s (re-embedding the 3-point corpse nav).  It does what the user did by hand.
  2. Loot: 1.3 turns looting on, so the ring is picked up even when combat holds the nav.  The exits turn it off.
  3. Attack distance .05 in 1.3 (the user's other quest-fight metas use .03-.06).  The exits restore .0101.
  4. Hard timeout: a stopwatch starts on entering 1.3 from the 1.2 nav.  After 180 s in 1.3/1.3a, echo, then leave
     through 'Portal Gem Travel' to '** Chain **', not to '2.0 Turn In', which would wait forever without the ring.
  5. Nav on: 1.3's ring/quest-done exits, 'Portal Gem Travel' and '2.0 Turn In' turn enablenav on when entered.

Usage: py tools/vt/examples/fix_lalaring.py <in.met> <out.met>
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import vtmeta as m  # noqa: E402

RING = 'Ancient Mummified Emerald Ring'
FIGHT, KICK = '1.3 Guardian Fight', '1.3a Fight Kick'
SW = 'LaLaFightSW'
TIMEOUT_S, KICK_AFTER_S, KICK_HOLD_S = 180, 25, 15


def rules_of(path):
    _, table, _ = m.load(path)
    return [(r[4][1], (int(r[0][1]), r[2]), (int(r[1][1]), r[3])) for r in table.rows]


def children(act):
    """Child (type, data) list of an All/Any condition or action, given as (type, ('TABLE', Table))."""
    return [(int(k[1]), v) for k, v in act[1][1].rows]


def cond_name(c):
    return m.COND.get(c[0])


def is_ring_cond(c):
    return cond_name(c) == 'ItemCountGE' and RING in m.describe(c[1])


def main(src, dst):
    rules = rules_of(src)
    states = {s for s, _, _ in rules}
    assert FIGHT in states and KICK not in states, 'unexpected meta (already patched?)'

    fight_rules = [r for r in rules if r[0] == FIGHT]
    entry = next(r for r in fight_rules if cond_name(r[1]) == 'Always')
    corpse_nav = next(a for a in children(entry[2]) if a[0] == 4)          # the 3-point circular uselp nav
    exit_rules = [r for r in fight_rules if r is not entry]                 # ring / "may complete again" exits
    assert len(exit_rules) == 2

    sw_running = m.expr_c(f'stopwatchelapsedseconds[${SW}]>={TIMEOUT_S}')
    restore = [m.opt('enablecombat', 'false'), m.opt('enablelooting', 'false'), m.opt('attackdistance', '.0101'),
               m.opt('enablenav', 'true')]

    def exit_action(old_action):
        # Old: All {combat false; uselp; EmbedNav none; CallState Portal Gem Travel -> 2.0 Turn In}
        acts = children(old_action)
        return m.all_a(*restore, *[a for a in acts if not (a[0] == 12)])

    fight = [
        (FIGHT, m.always(), m.all_a(m.opt('enablenav', 'true'), m.opt('enablecombat', 'true'),
                                    m.opt('enablelooting', 'true'), m.opt('attackdistance', '.05'), corpse_nav)),
        *[(FIGHT, c, exit_action(a)) for _, c, a in exit_rules],
        (FIGHT, m.seconds_in_state(KICK_AFTER_S), m.set_state(KICK)),
    ]
    timeout_action = m.all_a(
        m.chat(f'/vt echo LaLaRing: no ring after {TIMEOUT_S // 60} minutes at the guardian, skipping to the next meta'),
        *restore, next(a for a in children(exit_rules[0][2]) if a[0] == 4),  # the meta's own empty nav
        m.call_state('Portal Gem Travel', '** Chain **'))
    ring_exit = next(r for r in exit_rules if is_ring_cond(r[1]))
    kick = [
        (KICK, m.always(), m.all_a(m.opt('enablenav', 'false'), m.opt('enablecombat', 'false'),
                                   m.opt('enablelooting', 'true'))),
        (KICK, m.seconds_in_state(1), m.opt('enablecombat', 'true')),
        (KICK, ring_exit[1], exit_action(ring_exit[2])),
        (KICK, m.seconds_in_state(KICK_HOLD_S), m.set_state(FIGHT)),
        (KICK, sw_running, timeout_action),
    ]
    fight.append((FIGHT, sw_running, timeout_action))

    out = []
    for r in rules:
        s, c, a = r
        if s == FIGHT:
            if r is fight_rules[0]:
                out += fight + kick
            continue
        if s == '1.2 Samsur to Ring' and cond_name(c) == 'NavRouteEmpty':
            # Start the fight stopwatch when the walk to the guardian ends.
            a = m.all_a(m.expr_a(f'setvar[{SW},stopwatchstart[stopwatchcreate[]]]'), *children(a))
        out.append((s, c, a))
        if s in ('Portal Gem Travel', '2.0 Turn In') and not any(o[0] == s and cond_name(o[1]) == 'Always'
                                                                 for o in out[:-1]):
            out.insert(len(out) - 1, (s, m.always(), m.opt('enablenav', 'true')))
    m.save(dst, out)


if __name__ == '__main__':
    main(*sys.argv[1:3])
