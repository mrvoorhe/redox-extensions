"""Patch Attributes4.0.met so the Fiun turn-in gives up after 3 passes (MetaAnalysis/2026-11-11/rockdown-attributes-loop.md).

Each attribute has its own 7-day cooldown.  If every Fiun elder refuses ("You have improved your X too recently"),
rule '7.7 Vaelesh - Vit' -> '7.1 Bayaas - Coord' loops until one cooldown ends.  On 2026-11-11 that took Rockdown
2 h 13 min.

Changes (everything else, including all embedded navs, is kept byte for byte):
  * '7.0 Fiuns Go To' resets the pass counter when entered.
  * Vaelesh's refusal (the end of a pass) adds 1 to the counter and goes to '7.7a Pass Check'.
  * '7.7a Pass Check': fewer than 3 passes goes back to 7.1.  3 passes echoes, then goes to '** Chain **'.
    The gem stays in inventory, so the next run's '6.5 Attributes Riddle' goes straight to the Fiuns.

Usage: py tools/vt/examples/fix_attributes.py <in.met> <out.met>
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import vtmeta as m  # noqa: E402

PASSES = 3
VAR = 'FiunPasses'
GO_TO, FIRST, LAST, CHECK = '7.0 Fiuns Go To', '7.1 Bayaas - Coord', '7.7 Vaelesh - Vit', '7.7a Pass Check'


def rules_of(path):
    _, table, _ = m.load(path)
    return [(r[4][1], (int(r[0][1]), r[2]), (int(r[1][1]), r[3])) for r in table.rows]


def main(src, dst):
    rules = rules_of(src)
    states = {s for s, _, _ in rules}
    assert GO_TO in states and LAST in states and CHECK not in states, 'unexpected meta (already patched?)'

    def is_loop_rule(s, c, a):
        return (s == LAST and m.COND.get(c[0]) == 'ChatMessage' and 'Vaelesh' in m.describe(c[1])
                and m.ACT.get(a[0]) == 'SetState' and FIRST in m.describe(a[1]))

    assert sum(is_loop_rule(*r) for r in rules) == 1
    nav_none = None
    for s, c, a in rules:  # the meta's own empty nav, from '7.8 Attributes End'
        if s == '7.8 Attributes End' and m.ACT.get(a[0]) == 'All':
            nav_none = next((int(k[1]), v) for k, v in a[1][1].rows if int(k[1]) == 4)
    assert nav_none

    out = []
    for s, c, a in rules:
        if s == GO_TO and not any(o[0] == GO_TO for o in out):
            out.append((GO_TO, m.always(), m.expr_a(f'setvar[{VAR},0]')))
        if is_loop_rule(s, c, a):
            out.append((s, c, m.all_a(m.expr_a(f'setvar[{VAR},getvar[{VAR}]+1]'), m.set_state(CHECK))))
            out.append((CHECK, m.expr_c(f'getvar[{VAR}]<{PASSES}'), m.set_state(FIRST)))
            out.append((CHECK, m.expr_c(f'getvar[{VAR}]>={PASSES}'), m.all_a(
                nav_none,
                m.chat(f'/vt echo Attributes: no Fiun accepted the gem after {PASSES} passes '
                       '(all attributes on weekly cooldown), skipping to next meta'),
                m.set_state('** Chain **'))))
            continue
        out.append((s, c, a))
    m.save(dst, out)


if __name__ == '__main__':
    main(*sys.argv[1:3])
