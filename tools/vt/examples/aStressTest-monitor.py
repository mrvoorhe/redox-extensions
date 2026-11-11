"""Watch a character running aStressTest.met; log steps, cycles, anomalies and client crashes.

    py tools/vt/examples/aStressTest-monitor.py <repo> <archive dir> [character]

Writes <archive dir>/stress.log (one line per step, cycle, NOTE, STUCK?, HANG? or CRASH) and events.jsonl, and prints
the same lines (for Claude's Monitor tool).  Stops when <archive dir>/STOP exists.  A crash is the client's pid
disappearing; when the character logs back in with the bridge on, the new pid is picked up.
The meta's "/vt echo Stress: <state>" lines show up about four times each in the journal, so repeats are dropped.
"""
import json, os, subprocess, sys, time
from datetime import datetime

REPO, OUT = sys.argv[1], sys.argv[2]
CHAR = sys.argv[3] if len(sys.argv) > 3 else 'Rockdown'
sys.path.insert(0, os.path.join(REPO, 'tools', 'vt'))
import rectl

os.makedirs(OUT, exist_ok=True)
JSONL = open(os.path.join(OUT, 'events.jsonl'), 'a', encoding='utf-8')
LOG = open(os.path.join(OUT, 'stress.log'), 'a', encoding='utf-8')
POLL, STUCK_S, DOWN_S = 10, 180, 60


def now():
    return datetime.now().strftime('%H:%M:%S')


def say(msg):
    line = f'{now()} {msg}'
    print(line, flush=True)
    LOG.write(line + '\n'); LOG.flush()


def rec(kind, **kw):
    JSONL.write(json.dumps({'t': datetime.now().isoformat(), 'kind': kind, **kw}) + '\n'); JSONL.flush()


def clients():
    rectl.ensure_built()
    p = subprocess.run(['dotnet', rectl.DLL, 'list'], capture_output=True, text=True)
    try:
        return json.loads(p.stdout) if p.stdout.strip() else []
    except ValueError:
        return []


def pid_alive(pid):
    p = subprocess.run(['tasklist', '/FI', f'PID eq {pid}', '/NH'], capture_output=True, text=True)
    return str(pid) in p.stdout


pid = None
seq = None
last_state = None
state_since = time.time()
cycle = 0
cycle_start = None
last_cell, cell_since = None, time.time()
down_since = None
crashed = False
stuck_flagged = False
last_msg, last_msg_t = None, 0

say(f'monitor started for {CHAR}')
while not os.path.exists(os.path.join(OUT, 'STOP')):
    try:
        mine = [c for c in clients() if c.get('character', '').lower() == CHAR.lower()]
        if pid is None and mine:
            pid = mine[0]['pid']
            say(f'client pid {pid}')
            rec('pid', pid=pid)
        if pid is not None and not pid_alive(pid):
            say(f'CRASH: client pid {pid} is gone. last state {last_state} ({int(time.time() - state_since)} s in it), '
                f'cell {last_cell:08X}' if last_cell else f'CRASH: client pid {pid} is gone. last state {last_state}')
            rec('crash', pid=pid, state=last_state, cell=last_cell, cycle=cycle)
            pid, seq, crashed = None, None, True
            continue
        if pid is None:
            time.sleep(POLL); continue

        try:
            if seq is None:
                r = rectl.run(CHAR, 'events', '--max', '1')
                seq = r['next']
                if crashed:
                    say('client back, bridge answering'); crashed = False
            r = rectl.run(CHAR, 'events', '--since', seq, '--kinds', 'plugin,chat', '--max', '500')
            st = rectl.state(CHAR)
            if down_since:
                say(f'bridge answering again after {int(time.time() - down_since)} s'); down_since = None
        except Exception as e:  # bridge not answering but process alive
            if down_since is None:
                down_since = time.time()
            elif time.time() - down_since > DOWN_S:
                say(f'HANG?: pid {pid} alive but bridge not answering for {int(time.time() - down_since)} s ({e})')
                rec('bridge-down', pid=pid, state=last_state)
                down_since = time.time()
            time.sleep(POLL); continue

        seq = r['next']
        for ev in r['events']:
            text = ev['text']
            if 'Stress:' in text or 'Meta transitioning to state' in text or 'Meta transitioning' in text:
                rec('event', seq=ev['seq'], time=ev['time'], text=text)
            if text.startswith('[VTank] Stress: '):
                msg = text[len('[VTank] Stress: '):].strip()
                if msg == last_msg and time.time() - last_msg_t < 5:
                    continue
                last_msg, last_msg_t = msg, time.time()
                if ' ' not in msg:
                    dur = int(time.time() - state_since)
                    if last_state:
                        rec('step', state=last_state, seconds=dur, cycle=cycle)
                    if msg == 'S1-arwic':
                        if cycle_start:
                            say(f'CYCLE {cycle} done in {int(time.time() - cycle_start)} s')
                            rec('cycle', cycle=cycle, seconds=int(time.time() - cycle_start))
                        cycle += 1; cycle_start = time.time()
                    say(f'step {msg} (prev {last_state} {dur} s)')
                    last_state, state_since = msg, time.time()
                    stuck_flagged = False
                else:
                    say(f'NOTE: {msg}')
            elif any(k in text for k in ('tells you', 'You have been killed', 'died', 'Exception', 'exception')):
                say(f'chat: {text}')
                rec('notable', text=text)

        cell = rectl.landcell(st)
        if cell != last_cell:
            last_cell, cell_since, stuck_flagged = cell, time.time(), False
        elif time.time() - cell_since > STUCK_S and not stuck_flagged:
            say(f'STUCK?: in cell {cell:08X} for {int(time.time() - cell_since)} s, state {last_state}')
            rec('stuck', cell=cell, state=last_state)
            stuck_flagged = True
    except Exception as e:
        say(f'monitor error: {e!r}')
    time.sleep(POLL)
say('monitor stopped')
