"""Run the aStressTest meta (client stability stress test) on a character, watch it, and report.

    py tools/vt/stresstest.py run <Char> <outdir> [--cycles N] [--then <meta>] [--no-unstick] [--no-start]

aStressTest.met (built by tools/vt/examples/aStressTest.py) loops with combat off through the usual crash points:
Arwic -> Town Network -> TouTou hunt loop -> Hoshino hunt loop -> Lifestone Sending Gem -> Arwic.  This script:

* checks the character has the three gems and records where it starts;
* stops the macro, goes to peace, loads aStressTest and starts it (unless --no-start: the meta is already running);
* polls the bridge every 5 s.  It prints and logs one line per step, cycle, stall, death, give-up and crash (pid
  gone), and HANG? when the pid is alive but the bridge doesn't answer for 60 s.  After a crash it waits for the
  character to come back with the bridge on;
* unstick (on by default): on a walking/loop step, if the character moves less than 2 m in 20 s (usually a monster
  standing in the path), it turns EnableCombat on for 30 s, then off again;
* after --cycles full cycles (Arwic to Arwic), it stops the macro.  With --then <meta>, it loads that meta and
  starts it (e.g. the farming meta the character was on before);
* writes <outdir>/stress.log, events.jsonl and SUMMARY.md (a table per cycle).  Create <outdir>/STOP to end early.
  SUMMARY.md is written then too, but the macro is left running.

Exit code 0 when the cycles finished, 1 on a crash before then (it keeps waiting for the client, so this only
happens with STOP), 2 on a bad setup (a gem missing, the character not found).
"""
import argparse, json, math, os, subprocess, sys, time
from datetime import datetime

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rectl
import vtnav

META = 'aStressTest'
GEMS = ['Town Network Sending Gem', 'Halls of Introduction Gem', 'Lifestone Sending Gem']
WALK_STEPS = {'S3-toutou-walk', 'S4-toutou-loop', 'S6-hoshino-walk', 'S7-hoshino-goto', 'S8-hoshino-loop'}
ECHO = '[VTank] Stress: '
POLL, DOWN_S = 5, 60
STILL_S, MIN_MOVE_M, COMBAT_S = 20, 2.0, 30


class StepTracker:
    """Turns "[VTank] Stress: X" lines into steps and cycles.  /vt echo lines show up ~4 times: repeats within 5 s
    are dropped.  A message without spaces is a state name; anything else ("TouTou lap done", "GIVE UP ...") is a note.
    A cycle runs from one S1-arwic to the next."""

    def __init__(self):
        self.state, self.since = None, None
        self.last_msg, self.last_t = None, -1e9
        self.cycles = []          # finished: {'n', 'start', 'seconds', 'steps': {state: s}, 'notes': [...], 'stalls'}
        self.current = None

    def feed(self, text, t):
        """Returns a list of (kind, message) for things worth printing."""
        if not text.startswith(ECHO):
            return []
        msg = text[len(ECHO):].strip()
        if msg == self.last_msg and t - self.last_t < 5:
            return []
        self.last_msg, self.last_t = msg, t
        if ' ' in msg:
            if self.current is not None:
                self.current['notes'].append(msg)
            return [('note', msg)]
        out = []
        if self.state and self.current is not None:
            self.current['steps'][self.state] = self.current['steps'].get(self.state, 0) + round(t - self.since)
        if msg == 'S1-arwic':
            if self.current is not None:
                self.current['seconds'] = round(t - self.current['start'])
                self.cycles.append(self.current)
                out.append(('cycle', f"CYCLE {self.current['n']} done in {self.current['seconds']} s"))
            self.current = {'n': len(self.cycles) + 1, 'start': t, 'seconds': None, 'steps': {}, 'notes': [],
                            'stalls': 0}
        out.append(('step', f'step {msg}' + (f' (prev {self.state} {round(t - self.since)} s)' if self.state else '')))
        self.state, self.since = msg, t
        return out


def summary(char, started, trk, incidents, ending):
    lines = [f'# Stress test, {char}, {started:%Y-%m-%d %H:%M}', '',
             f'Meta {META}.  {len(trk.cycles)} full cycle(s).  Ended: {ending}.', '',
             '| Cycle | Start | Whole cycle | TouTou lap | Hoshino lap | Stalls | Notes |',
             '|---|---|---|---|---|---|---|']
    for c in trk.cycles + ([trk.current] if trk.current else []):
        notes = [n for n in c['notes'] if 'lap done' not in n]
        whole = f"{c['seconds']} s" if c['seconds'] is not None else 'unfinished'
        lap = lambda s: f"{c['steps'][s]} s" if s in c['steps'] else ''
        lines.append(f"| {c['n']} | {datetime.fromtimestamp(c['start']):%H:%M:%S} | {whole} | {lap('S4-toutou-loop')} "
                     f"| {lap('S8-hoshino-loop')} | {c['stalls']} | {'; '.join(notes)} |")
    lines += ['', '## Incidents', ''] + ([f'* {i}' for i in incidents] or ['None (no crash, hang, death or give-up).'])
    lines += ['', 'Times are to the 5 s poll.  Logs: stress.log, events.jsonl.', '']
    return '\n'.join(lines)


def clients():
    rectl.ensure_built()
    p = subprocess.run(['dotnet', rectl.DLL, 'list'], capture_output=True, text=True)
    try:
        return json.loads(p.stdout.lstrip('﻿')) if p.stdout.strip() else []
    except ValueError:
        return []


def pid_alive(pid):
    p = subprocess.run(['tasklist', '/FI', f'PID eq {pid}', '/NH'], capture_output=True, text=True)
    return str(pid) in p.stdout


def journal_end(char):
    """events returns the oldest first: page to the end."""
    seq = 0
    while True:
        r = rectl.run(char, 'events', '--since', seq, '--kinds', 'debug', '--max', '5000')
        if r['next'] == seq:
            return seq
        seq = r['next']


def flat_pos(char):
    loc = rectl.state(char)['location']
    ew, ns, _ = vtnav.nav_coord(loc['landcell'] & 0xFFFFFFFF, loc['x'], loc['y'], loc['z'])
    return ew * 240, ns * 240, loc['landcell'] & 0xFFFFFFFF


def run(args):
    char, out = args.char, args.outdir
    os.makedirs(out, exist_ok=True)
    stop_file = os.path.join(out, 'STOP')
    if os.path.exists(stop_file):
        os.remove(stop_file)
    log = open(os.path.join(out, 'stress.log'), 'a', encoding='utf-8')
    jsonl = open(os.path.join(out, 'events.jsonl'), 'a', encoding='utf-8')

    def say(msg):
        line = f'{datetime.now():%H:%M:%S} {msg}'
        print(line, flush=True)
        log.write(line + '\n'); log.flush()

    def rec(kind, **kw):
        jsonl.write(json.dumps({'t': datetime.now().isoformat(), 'kind': kind, **kw}) + '\n'); jsonl.flush()

    def cmd(text, wait='.'):
        r = rectl.exec_wait(char, text, wait, timeout_ms=4000)
        rec('command', text=text, reply=r)
        time.sleep(1)
        return r

    mine = [c for c in clients() if c.get('character', '').lower() == char.lower()]
    if not mine:
        print(f'{char} has no client with the bridge on (rectl list)', file=sys.stderr); return 2
    pid = mine[0]['pid']
    inv = rectl.run(char, 'inventory', 'Gem', '--limit', '200')
    names = {o['name'] for o in inv.get('objects', [])}
    missing = [g for g in GEMS if g not in names]
    if missing:
        print(f'{char} is missing {", ".join(missing)}', file=sys.stderr); return 2
    s = rectl.state(char)
    say(f'start: {char} pid {pid}, cell {rectl.landcell(s):08X}, cycles {args.cycles or "unlimited"}, '
        f'then {args.then or "stop"}, unstick {"off" if args.no_unstick else "on"}')
    rec('start', char=char, pid=pid, cell=rectl.landcell(s), cycles=args.cycles, then=args.then)

    seq = journal_end(char)
    if not args.no_start:
        cmd('/vt stop', 'Macro stopped')
        cmd('/ub combatstate peace')
        cmd(f'/vt meta load {META}')
        cmd('/vt opt set EnableMeta True', 'EnableMeta')
        cmd('/vt start', 'Macro started')

    started = datetime.now()
    trk = StepTracker()
    incidents = []
    down_since = None
    hist, combat_until = [], None
    finished = False

    while not os.path.exists(stop_file):
        time.sleep(POLL)
        now = time.time()
        if pid is None:
            mine = [c for c in clients() if c.get('character', '').lower() == char.lower()]
            if mine:
                pid = mine[0]['pid']
                seq = journal_end(char)
                say(f'client back: pid {pid} (the meta starts over from Default when VT starts)')
                rec('pid', pid=pid)
            continue
        if not pid_alive(pid):
            msg = f'CRASH: client pid {pid} gone, step {trk.state}'
            say(msg); incidents.append(f'{datetime.now():%H:%M:%S} {msg}')
            rec('crash', pid=pid, state=trk.state)
            pid, combat_until, hist = None, None, []
            continue
        try:
            r = rectl.run(char, 'events', '--since', seq, '--kinds', 'plugin,chat', '--max', '1000')
            x, y, cell = flat_pos(char)
            if down_since:
                say(f'bridge answering again after {round(now - down_since)} s'); down_since = None
        except Exception as e:
            if down_since is None:
                down_since = now
            elif now - down_since > DOWN_S:
                msg = f'HANG?: pid {pid} alive, bridge not answering for {round(now - down_since)} s'
                say(msg); incidents.append(f'{datetime.now():%H:%M:%S} {msg}'); rec('bridge-down', error=str(e))
                down_since = now
            continue
        seq = r['next']

        for ev in r['events']:
            text = ev['text']
            if text.startswith(ECHO) or 'Meta transitioning' in text:
                rec('event', seq=ev['seq'], time=ev['time'], text=text)
            for kind, msg in trk.feed(text, now):
                say(msg)
                if 'GIVE UP' in msg or 'DIED' in msg:
                    incidents.append(f'{datetime.now():%H:%M:%S} {msg}')
                if kind == 'cycle':
                    rec('cycle', **{k: v for k, v in trk.cycles[-1].items()})
            if 'tells you' in text and text.startswith(('Royal', 'Umbral', 'Sergeant', 'Lieutenant')):
                say(f'chat: {text}')

        if args.cycles and len(trk.cycles) >= args.cycles:
            finished = True
            break

        # Unstick
        if combat_until and now >= combat_until:
            cmd('/vt opt set EnableCombat False', 'EnableCombat')
            say(f'unstick: combat off, moved {math.dist(stall_pos, (x, y)):.1f} m while it was on')
            combat_until, hist = None, []
        elif not args.no_unstick and combat_until is None:
            if trk.state not in WALK_STEPS:
                hist = []
            else:
                hist = [h for h in hist + [(now, x, y)] if now - h[0] <= STILL_S + POLL]
                if now - hist[0][0] >= STILL_S:
                    moved = max(math.dist(h[1:], (x, y)) for h in hist)
                    if moved < MIN_MOVE_M:
                        if trk.current is not None:
                            trk.current['stalls'] += 1
                        say(f'STALL: {trk.state}, moved {moved:.1f} m in {round(now - hist[0][0])} s, cell {cell:08X}; '
                            f'combat on for {COMBAT_S} s')
                        rec('stall', state=trk.state, cell=cell, moved=moved)
                        cmd('/vt opt set EnableCombat True', 'EnableCombat')
                        combat_until, stall_pos = now + COMBAT_S, (x, y)

    if finished:
        cmd('/vt stop', 'Macro stopped')
        if args.then:
            cmd(f'/vt meta load {args.then}')
            cmd('/vt opt set EnableMeta True', 'EnableMeta')
            cmd('/vt start', 'Macro started')
        ending = f'{args.cycles} cycles done, ' + (f'{args.then} loaded and started' if args.then else 'macro stopped')
    else:
        ending = 'STOP file (macro left running)'
    say(f'done: {ending}')
    with open(os.path.join(out, 'SUMMARY.md'), 'w', encoding='utf-8') as f:
        f.write(summary(char, started, trk, incidents, ending))
    return 0 if finished else 1


def main(argv):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    r = sub.add_parser('run')
    r.add_argument('char')
    r.add_argument('outdir')
    r.add_argument('--cycles', type=int, default=0, help='stop after this many full cycles (0: until STOP)')
    r.add_argument('--then', help='meta to load and start after the cycles')
    r.add_argument('--no-unstick', action='store_true')
    r.add_argument('--no-start', action='store_true', help="aStressTest is already running; just watch")
    return run(ap.parse_args(argv))


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
