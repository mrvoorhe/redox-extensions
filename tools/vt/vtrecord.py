"""Record what a character does through the agent bridge, then turn it into a vtnav spec.

    py tools/vt/vtrecord.py record <character> <dir> [--interval 0.7]
    py tools/vt/vtrecord.py spec <dir> [--min-move 4] [--out <dir>/route.spec]

record: run it in the background and let the user play the route.  Stop it by creating the file <dir>/STOP
(it exits within a second and turns net capture off).  If it is killed instead, run `rectl -c <char> net off`.
It only appends to logs, so nothing is lost either way:

    <dir>/positions.jsonl  position samples: t (epoch seconds), cell (hex), x, y, z, heading
    <dir>/actions.jsonl    uses and jumps: t, action (use / useWithTarget / jump), object (as `rectl object` saw it)
    <dir>/chat.jsonl       chat and plugin text, for context (portal messages, NPC replies, etc.)

How: polls `rectl state` for position, and turns on net capture of client game actions (F7B1) to see UseItem (0x36)
with its object id, which it looks up with `rectl object` at once, while the object still exists.  Jumps are the
client's jump action (0xF61B).

spec: merges positions and actions by time and writes a draft spec for `vtnav.py build`.  Events reach the journal a
little after they happen, so the merge (not the order they were seen) decides where each use or jump goes:

  * waypoints every --min-move meters, at each new landblock (arrival after a teleport) and where the character
    stopped; samples that only change height (falling) collapse into one
  * inventory item use (gems) -> "chat /ub usei <name>" + pause; no waypoint needed
  * portal/NPC/other object use -> waypoint where the character stood, then a use node with the object's exact nav
    coordinates; a repeat use of the same object before any teleport is dropped (VT and players often click twice).
    After a portal use, samples are skipped until the teleport (the character keeps walking up to the portal, and
    VT would try to walk back to those points from the other side)
  * jump -> waypoint at the last position before the jump, then a jump node with that heading, then the landing
    spot (samples in the air are skipped).  Shift and milliseconds
    aren't in the network message: the draft uses False / 500, which worked for a running jump down a ledge.

Always read the draft before building.
"""
import argparse, datetime, json, math, os, sys, time

sys.path.insert(0, os.path.dirname(__file__))
import rectl
from vtnav import nav_coord, fmt

USE_ITEM, USE_WITH_TARGET, JUMP = 0x36, 0x35, 0xF61B
PORTAL_PAUSE_MS = 5000
CLASS_KINDS = {14: 'portal', 37: 'npc'}
TELEPORT_METERS = 30      # a bigger horizontal jump between two samples is a teleport (falls are vertical)
PORTAL_WAIT_SECONDS = 20  # stop waiting for the teleport after a portal use (it failed or never happened)


# ---------------------------------------------------------------- record

def journal_end(char):
    """Seq after the newest journal event.  ("truncated" in an events reply means older events were dropped, not that
    there are more pages, so page until a short page.)"""
    since = 0
    while True:
        r = rectl.run(char, 'events', '--since', since, '--max', 500)
        since = r.get('next', since)
        if len(r.get('events', [])) < 500:
            return since


def event_time(iso):
    """Bridge event time (UTC, 7 fractional digits, Z) to epoch seconds."""
    iso = iso.rstrip('Z')
    if '.' in iso:
        head, frac = iso.split('.')
        iso = f'{head}.{frac[:6]}'
    return datetime.datetime.fromisoformat(iso).replace(tzinfo=datetime.timezone.utc).timestamp()


def record(char, out_dir, interval):
    os.makedirs(out_dir, exist_ok=True)
    stop_file = os.path.join(out_dir, 'STOP')
    if os.path.exists(stop_file):
        os.remove(stop_file)
    logs = {k: open(os.path.join(out_dir, f'{k}.jsonl'), 'a', encoding='utf-8') for k in ('positions', 'actions', 'chat')}

    def log(kind, obj):
        logs[kind].write(json.dumps(obj) + '\n')
        logs[kind].flush()

    previous_net = rectl.run(char, 'net')
    rectl.run(char, 'net', '--client', 'F7B1')
    seq = journal_end(char)
    print(f'recording {char} into {out_dir}; create {stop_file} to stop', flush=True)
    try:
        while not os.path.exists(stop_file):
            t0 = time.time()
            s = rectl.state(char)
            l = s['location']
            log('positions', {'t': (t0 + time.time()) / 2, 'cell': f'{rectl.landcell(s):08X}', 'x': l['x'],
                              'y': l['y'], 'z': l['z'], 'heading': l['heading']})

            r = rectl.run(char, 'events', '--since', seq, '--max', 500)
            for e in r.get('events', []):
                if e['seq'] < seq:
                    continue
                if e['kind'] in ('chat', 'plugin'):
                    log('chat', {'t': event_time(e['time']), 'kind': e['kind'], 'text': e['text']})
                elif e['kind'] == 'net' and e['text'].startswith('C F7B1'):
                    fields = dict(f.split('=', 1) for f in e['text'].split()[2:] if '=' in f)
                    action = int(fields.get('action', -1))
                    if action == JUMP:
                        log('actions', {'t': event_time(e['time']), 'action': 'jump'})
                    elif action in (USE_ITEM, USE_WITH_TARGET):
                        oid = int(fields['object'])
                        obj = rectl.run(char, 'object', oid, check=False) or {'id': oid}
                        log('actions', {'t': event_time(e['time']), 'action': 'use' if action == USE_ITEM else 'useWithTarget',
                                        'object': obj})
                        print('use', obj.get('name', oid), flush=True)
            seq = r.get('next', seq)
            time.sleep(max(0.0, interval - (time.time() - t0)))
    finally:
        restore = []
        if previous_net.get('server'):
            restore += ['--server', ','.join(previous_net['server'])]
        if previous_net.get('client'):
            restore += ['--client', ','.join(previous_net['client'])]
        rectl.run(char, 'net', *(restore or ['off']), check=False)
        print('stopped', flush=True)


# ---------------------------------------------------------------- spec

def read_jsonl(path):
    if not os.path.exists(path):
        return []
    with open(path, encoding='utf-8') as f:
        return [json.loads(line) for line in f if line.strip()]


def meters(a, b):
    """Distance between two (cell, x, y, z) positions, and the horizontal part alone."""
    ea, na, za = nav_coord(*a)
    eb, nb, zb = nav_coord(*b)
    return math.dist((ea, na, za), (eb, nb, zb)) * 240, math.hypot(ea - eb, na - nb) * 240


def build_spec(positions, actions, min_move=4.0):
    timeline = sorted([(p['t'], 0, p) for p in positions] + [(a['t'], 1, a) for a in actions], key=lambda x: (x[0], x[1]))
    lines = []           # [text, comment, pos or None]
    last_pt = None       # position of the last waypoint line
    last_sample = None   # latest position sample
    prev_sample = None
    last_heading = 0.0
    last_use = None      # (object id, landblock) of the last world-object use

    def pt(pos, comment=None):
        nonlocal last_pt
        lines.append([f'pt {pos[0]:08X} {pos[1]:.2f} {pos[2]:.2f} {pos[3]:.2f}', comment, pos])
        last_pt = pos

    skip_until_teleport = None   # time of a portal use; samples are the walk-up until the character teleports
    airborne_since = None        # time of a jump; samples are in the air until the height settles

    for t, kind, item in timeline:
        if kind == 0:
            pos = (int(item['cell'], 16), item['x'], item['y'], item['z'])
            prev_sample, last_sample, last_heading = last_sample, pos, item['heading']
            teleported = prev_sample is not None and (prev_sample[0] >> 16 != pos[0] >> 16 or
                                                      meters(prev_sample, pos)[1] > TELEPORT_METERS)
            if skip_until_teleport is not None:
                if teleported or t - skip_until_teleport > PORTAL_WAIT_SECONDS:
                    skip_until_teleport = None
                    if teleported:
                        pt(pos, 'arrived')
                continue
            if airborne_since is not None:
                if t - airborne_since < 0.5 or abs(pos[3] - prev_sample[3]) > 0.05:
                    continue
                airborne_since = None
                pt(pos, 'landed')
                continue
            if last_pt is None:
                pt(pos, 'start')
            elif teleported:
                pt(pos, 'arrived')
            else:
                total, horizontal = meters(last_pt, pos)
                stopped = prev_sample is not None and meters(prev_sample, pos)[0] < 0.2 and total >= 0.5
                if horizontal < 0.5 and total >= 0.5 and lines and lines[-1][2] == last_pt and lines[-1][1] is None:
                    lines[-1] = [f'pt {pos[0]:08X} {pos[1]:.2f} {pos[2]:.2f} {pos[3]:.2f}', None, pos]  # falling
                    last_pt = pos
                elif total >= min_move or stopped:
                    pt(pos)
            continue

        action = item['action']
        obj = item.get('object') or {}
        name = obj.get('name')
        if action == 'jump':
            if last_sample:
                if last_pt != last_sample:
                    pt(last_sample, 'jump start')
                p = last_sample
                lines.append([f'jump {p[0]:08X} {p[1]:.2f} {p[2]:.2f} {p[3]:.2f} {last_heading:.0f} False 500',
                              'check shift and ms', None])
                airborne_since = item['t']
        elif not name:
            lines.append([f'# used object {obj.get("id")}, which was gone before it could be looked up', None, None])
        elif action == 'useWithTarget':
            lines.append([f'# used {name} on a target: not converted, add it by hand', None, None])
        elif obj.get('container'):
            lines.append([f'chat /ub usei {name}', None, None])
            lines.append([f'pause {PORTAL_PAUSE_MS}', None, None])
        elif obj.get('nav'):
            key = (obj.get('id'), last_sample[0] >> 16 if last_sample else None)
            if key == last_use:
                continue
            last_use = key
            if last_sample and last_pt != last_sample:
                pt(last_sample, f'before {name}')
            ew, ns, z = obj['nav']
            k = CLASS_KINDS.get(obj.get('classId'))
            lines.append([f'{k or "use " + str(obj.get("classId"))} {fmt(ew)} {fmt(ns)} {fmt(z)} {name}', None, None])
            if k == 'portal':
                lines.append([f'pause {PORTAL_PAUSE_MS}', None, None])
                skip_until_teleport = item['t']
        else:
            lines.append([f'# used {name} ({obj.get("class")}) but it has no position: add it by hand', None, None])

    return [text + (f'    # {comment}' if comment else '') for text, comment, _ in lines]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    r = sub.add_parser('record')
    r.add_argument('character')
    r.add_argument('dir')
    r.add_argument('--interval', type=float, default=0.7)
    s = sub.add_parser('spec')
    s.add_argument('dir')
    s.add_argument('--min-move', type=float, default=4.0, help='meters between waypoints (default 4)')
    s.add_argument('--out')
    a = ap.parse_args()

    if a.cmd == 'record':
        record(a.character, a.dir, a.interval)
    else:
        lines = build_spec(read_jsonl(os.path.join(a.dir, 'positions.jsonl')),
                           read_jsonl(os.path.join(a.dir, 'actions.jsonl')), a.min_move)
        out = a.out or os.path.join(a.dir, 'route.spec')
        with open(out, 'w', encoding='utf-8') as f:
            f.write('type once\n' + '\n'.join(lines) + '\n')
        print(f'wrote {len(lines)} lines to {out}')


if __name__ == '__main__':
    main()
