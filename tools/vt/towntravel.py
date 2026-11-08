"""Send a character to a town through Town Network with the aTownNetwork meta.

    py tools/vt/towntravel.py <character> <town> [--restore <meta>] [--timeout 150]

Loads aTownNetwork (VT runs one meta at a time), turns EnableMeta on, starts VT, and has the character itself say
"/f !<town>".  Waits until it has been in Town Network (landblock 0x0007) and left it again, then waits for the meta to
go back to Default.  Prints the meta log and where the character ended up.  --restore loads that meta afterwards
(the user's farm meta, say) and leaves VT running.  Exit code 0 = arrived, 1 = didn't.

Town names: `py tools/vt/examples/aTownNetwork.py --list`.  Build the meta first if it isn't in the VT folder:
`py tools/vt/examples/aTownNetwork.py`.
"""
import argparse, os, sys, time

sys.path.insert(0, os.path.dirname(__file__))
import rectl

TN = 0x0007


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('character')
    ap.add_argument('town')
    ap.add_argument('--restore', help='meta to load when done')
    ap.add_argument('--timeout', type=float, default=150)
    a = ap.parse_args()
    c = a.character

    rectl.run(c, 'exec', '/vt meta load aTownNetwork')
    rectl.run(c, 'exec', '/vt opt set EnableMeta True')
    if not rectl.exec_wait(c, '/vt start', 'Macro started', 'plugin'):
        print('VT did not start')
    time.sleep(1)
    since = rectl.run(c, 'exec', f'/f !{a.town}')['seq']
    if not rectl.run(c, 'wait', 'Meta transitioning to state: G_', '--since', since, '--kinds', 'plugin',
                     '--timeout', 5000, check=False).get('matched'):
        print(f'no meta reaction to "!{a.town}": check the name with aTownNetwork.py --list')
        return 1

    t0, was_in_tn, arrived = time.time(), False, False
    while time.time() - t0 < a.timeout:
        lb = rectl.landcell(rectl.state(c)) >> 16
        was_in_tn = was_in_tn or lb == TN
        if was_in_tn and lb != TN:
            arrived = True
            break
        time.sleep(1.5)
    if arrived:  # the meta goes back to Default (nav off) once it sees the new landblock
        rectl.run(c, 'wait', 'Meta transitioning to state: Default', '--since', since, '--kinds', 'plugin',
                  '--timeout', 10000, check=False)

    for e in rectl.run(c, 'events', '--since', since, '--kinds', 'plugin', '--max', '300')['events']:
        if 'Meta' in e['text'] or '[UB]' in e['text']:
            print('   ', e['text'][:140])
    s = rectl.state(c)
    l = s['location']
    print(f'{"ARRIVED" if arrived else "FAILED"} after {time.time() - t0:.0f}s at {rectl.landcell(s):08X} '
          f'({l["x"]:.1f}, {l["y"]:.1f}, {l["z"]:.1f})')

    if a.restore:
        rectl.run(c, 'exec', f'/vt meta load {a.restore}')
        rectl.exec_wait(c, '/vt start', 'Macro started', 'plugin')
        print(f'loaded {a.restore} and started VT')
    return 0 if arrived else 1


if __name__ == '__main__':
    sys.exit(main())
