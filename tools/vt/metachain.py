"""Start a meta chain on several characters at set times, record what happens, and turn the recording into data.

    py tools/vt/metachain.py start <chain> <Char> [<Char> ...] [--at HH:MM|YYYY-MM-DDTHH:MM] [--stagger 15] [--log f]
                                   [--record <outdir> [--record-hours 3]]
    py tools/vt/metachain.py record <Char> <outdir> [--start HH:MM|ISO] [--stop HH:MM|ISO | --hours 8]
    py tools/vt/metachain.py analyze <outdir>/<Char>.jsonl [--since ISO] [--json out.json]

start: sends `/re startmeta <chain>` once to each character, in the order given.  The first one goes at --at (default
07:00, the next time that comes round), each later one --stagger minutes after the one before it.  If a character's
client can't be reached at its time, it retries every 30 s for up to 15 minutes.  Once the bridge has accepted the
command it is never sent again, so a chain starts at most once per character.  Needs the bridge in `rw` mode.
With --record, it also runs one `record` per character, from a minute before that character's start for
--record-hours, and exits only when they have all finished.

record: read-only, never sends a game command.  From --start (default now) to --stop, it polls `rectl events` every
3 s and `rectl state` every 30 s and appends everything to <outdir>/<Char>.jsonl.  It follows the character across
client restarts and writes `kind: meta` marker records for missing clients, new instances and journal gaps.  Create
<outdir>/STOP to end it early.

record also dumps every installed meta to <outdir>/metas/ when it starts, so a later analyze reads the metas as they
were that day.

analyze: reads one recording and prints a markdown summary: the chain timeline, and for each hunt meta (one that
starts a `<X>HuntSW` stopwatch) the hunt limit, whether it timed out, every kill task's progress and finish time,
time per state, stationary periods, NPC lines, deaths and warnings.  Tasks are grouped with the meta's own
"You have killed" triggers (read from the installed .met files), because the server counts per task, not per mob
(Clay Golem Samurais 1 is followed by Bronze Golem Samurais 2).
"""
import argparse, datetime as dt, json, os, re, subprocess, sys, time
from collections import OrderedDict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rectl, vtmeta  # noqa: E402


# ---------------------------------------------------------------- shared

def now():
    return dt.datetime.now()


def parse_when(s, default_time=None):
    """HH:MM -> the next time that clock time comes round (a minute of grace); ISO -> as given."""
    if s is None:
        if default_time is None:
            return now()
        s = default_time
    if re.fullmatch(r'\d{1,2}:\d{2}', s):
        t = dt.datetime.combine(dt.date.today(), dt.datetime.strptime(s, '%H:%M').time())
        return t + dt.timedelta(days=1) if t < now() - dt.timedelta(minutes=1) else t
    return dt.datetime.fromisoformat(s)


def cli(*args, timeout=30):
    """Runs rectl with raw args (no -c), returns (json or None, error or None)."""
    rectl.ensure_built()
    try:
        p = subprocess.run(['dotnet', rectl.DLL, *map(str, args)], capture_output=True, text=True, timeout=timeout)
    except subprocess.TimeoutExpired:
        return None, 'timeout'
    if p.returncode != 0:
        return None, (p.stderr.strip() or p.stdout.strip())[:300]
    try:
        return (json.loads(p.stdout) if p.stdout.strip() else None), None
    except ValueError as e:
        return None, f'bad json: {e}'


class JsonlLog:
    def __init__(self, path, echo=False):
        self.f = open(path, 'a', encoding='utf-8') if path else None
        self.echo = echo

    def write(self, rec):
        rec.setdefault('local', now().isoformat(timespec='seconds'))
        line = json.dumps(rec, ensure_ascii=False)
        if self.f:
            self.f.write(line + '\n')
            self.f.flush()
        if self.echo:
            print(line, flush=True)


# ---------------------------------------------------------------- start

def plan_starts(chars, first, stagger_min):
    return [(c, first + dt.timedelta(minutes=stagger_min * i)) for i, c in enumerate(chars)]


def cmd_start(a):
    command = f'/re startmeta {a.chain}'
    plan = plan_starts(a.chars, parse_when(a.at, '07:00'), a.stagger)
    log = JsonlLog(a.log, echo=True)
    recs = record_plan(plan, a.record_hours) if a.record else []
    log.write({'what': 'armed', 'command': command, 'plan': [(c, t.isoformat(timespec='seconds')) for c, t in plan],
               **({'record': a.record, 'recorders': recs} if a.record else {})})
    if a.dry_run:
        return 0
    rectl.ensure_built()
    recorders = [subprocess.Popen([sys.executable, os.path.abspath(__file__), 'record', c, a.record,
                                   '--start', b, '--stop', e]) for c, b, e in recs]
    for char, when in plan:
        while now() < when:
            time.sleep(min(20, max(0.2, (when - now()).total_seconds())))
        deadline = when + dt.timedelta(minutes=15)
        while True:
            p = subprocess.run(['dotnet', rectl.DLL, '-c', char, '--timeout', '30000', 'exec', command,
                                '--wait', 'StartMeta|startmeta|Meta transitioning', '--kinds', 'plugin,chat'],
                               capture_output=True, text=True)
            try:
                r = json.loads(p.stdout) if p.stdout.strip() else None
            except ValueError:
                r = None
            accepted = isinstance(r, dict) and r.get('exec') is not None  # sent; the wait may still have missed
            log.write({'what': 'exec', 'character': char, 'rc': p.returncode, 'accepted': accepted, 'result': r,
                       'stderr': p.stderr.strip()[:500]})
            if accepted or now() > deadline:
                break
            time.sleep(30)
    log.write({'what': 'done'})
    for p in recorders:  # stay alive until the recordings are complete, so whoever runs this knows when to analyze
        p.wait()
    if recorders:
        log.write({'what': 'recorders-done', 'record': a.record})
    return 0


def record_plan(plan, hours):
    """One recorder per character, from a minute before its start (to catch the StartMeta line) for `hours`."""
    return [(c, (t - dt.timedelta(minutes=1)).isoformat(timespec='seconds'),
             (t + dt.timedelta(hours=hours)).isoformat(timespec='seconds')) for c, t in plan]


# ---------------------------------------------------------------- record

def snapshot_metas(dest):
    """Dumps every installed .met to dest/<name>.txt once, so analyze sees the metas as they were on the day."""
    if os.path.isdir(dest):
        return
    os.makedirs(dest, exist_ok=True)  # recorders started together may race here
    vt = rectl.vt_dir()
    for f in sorted(os.listdir(vt)):
        if f.lower().endswith('.met'):
            try:
                lines = vtmeta.dump(os.path.join(vt, f))
            except Exception as e:  # a few third-party metas may not parse; they aren't needed
                lines = [f'(dump failed: {e})']
            with open(os.path.join(dest, f[:-4] + '.txt'), 'w', encoding='utf-8') as fh:
                fh.write('\n'.join(lines) + '\n')


def cmd_record(a):
    char, outdir = a.char, a.outdir
    os.makedirs(outdir, exist_ok=True)
    stop_file = os.path.join(outdir, 'STOP')
    log = JsonlLog(os.path.join(outdir, f'{char}.jsonl'))
    start = parse_when(a.start)
    stop = parse_when(a.stop) if a.stop else start + dt.timedelta(hours=a.hours)
    log.write({'kind': 'meta', 'what': 'collector-armed', 'start': start.isoformat(), 'stop': stop.isoformat()})
    while now() < start and not os.path.exists(stop_file):
        time.sleep(min(30, max(1, (start - now()).total_seconds())))
    log.write({'kind': 'meta', 'what': 'collector-started'})
    snapshot_metas(os.path.join(outdir, 'metas'))

    def find_pid():
        r, _ = cli('list')
        pids = [c['pid'] for c in (r or []) if c.get('character', '').lower() == char.lower()]
        return pids[-1] if pids else None

    pid, last_pid, since, last_state, last_list, down_since = None, None, 0, 0.0, 0.0, None
    while now() < stop and not os.path.exists(stop_file):
        t = time.time()
        if pid is None or t - last_list > 30:
            last_list = t
            p = find_pid()
            if p is None:
                if down_since is None:
                    down_since = now()
                    log.write({'kind': 'meta', 'what': 'client-missing', 'pid': pid or last_pid})
                if pid:
                    last_pid = pid
                pid = None
            elif p != pid:
                same = p == last_pid  # a failed list call, not a new client: keep our place in the journal
                log.write({'kind': 'meta', 'what': 'client-found', 'pid': p, 'oldPid': last_pid, 'sameInstance': same,
                           'downSince': down_since.isoformat(timespec='seconds') if down_since else None})
                if not same:
                    since = 0
                pid, last_pid, down_since = p, p, None
        if pid is None:
            time.sleep(5)
            continue
        r, err = cli('--pid', pid, 'events', '--since', since, '--max', 500)
        if r is None:
            log.write({'kind': 'meta', 'what': 'events-error', 'pid': pid, 'error': err})
            last_list = 0
            time.sleep(5)
            continue
        evs = r.get('events', [])
        if evs and since and evs[0]['seq'] > since:
            log.write({'kind': 'meta', 'what': 'journal-gap', 'pid': pid, 'expected': since, 'got': evs[0]['seq']})
        for e in evs:
            e['pid'] = pid
            log.write(e)
        since = r.get('next', since)  # --since is inclusive and `next` is the first unseen seq
        if t - last_state > 30:
            last_state = t
            s, err = cli('--pid', pid, 'state')
            log.write({'kind': 'state', 'pid': pid, 'state': s, 'error': err})
        if not r.get('truncated'):
            time.sleep(3)
    log.write({'kind': 'meta', 'what': 'collector-stopped'})
    return 0


# ---------------------------------------------------------------- analyze

KILL_RE = re.compile(r"^You have killed (\d+) (.+?)! (?:You must kill (\d+)|Your task is complete)")
OUTOF_RE = re.compile(r"^You've killed (\d+) out of (\d+) (.+?)\.?$")
TRANS_RE = re.compile(r'Meta transitioning to state: (.+)$')
# A name built at run time (`/vt meta load zFarm`+wobjectgetname[...]) is not a meta name, so it doesn't match.
LOAD_RE = re.compile(r'/vt meta load ([^,;}\]\'`]+?)\s*(?:[,;}\]\']|`(?!\+)|$)')
START_RE = re.compile(r'\[RE\] StartMeta : (\S+)')
HUNTSW_RE = re.compile(r'setvar\[(\w*HuntSW),stopwatchstart')
LIMIT_MSG_RE = re.compile(r'(\w+): (\d+) minute hunt limit reached')
COIN_RE = re.compile(r'^(.+?) gives you ([\d,]+) ((?:Weakly )?Enlightened Coins?)\.?$')
MYQ_RE = re.compile(r'^(\w+) - (\d+) solves \(\d+\) "[^"]*" (\d+) 0$')
DEATH_RE = re.compile(r"(You have died|You died|killed you|You have been killed|You've been killed)", re.I)
WARN_RE = re.compile(r'(Exception|\[VTank\].*(error|warning|fail|could not|unable|bugged)|took too long|'
                     r'starting over|unhittable|Could not find)', re.I)
TRIGGER_RE = re.compile(r"ChatMessage '\^You have killed (?:\d+|\[0-9\]\+) (.+?)(?:!.*)?'\s+DO (.*)$")
SETS_RE = re.compile(r"(?:@(\w+)\s*=|setvar\[(\w+),|setpvar\[(\w+),)")


def meta_info(names, dump_dir=None):
    """meta -> {'limit': seconds or None, 'hunt': bool, 'triggers': [(mob regex, task key)]}.

    Reads dump_dir/<name>.txt (the snapshot `record` takes) when it exists, else the installed .met."""
    vt, info = None, {}
    for name in names:
        snap = os.path.join(dump_dir, name + '.txt') if dump_dir else None
        if snap and os.path.exists(snap):
            lines = open(snap, encoding='utf-8').read().splitlines()
        else:
            vt = vt or rectl.vt_dir()
            path = os.path.join(vt, name + '.met')
            if not os.path.exists(path):
                continue
            lines = vtmeta.dump(path)
        text = '\n'.join(lines)
        limit = re.search(r'stopwatchelapsedseconds\[\$\w*HuntSW\]>=(\d+)', text)
        trig = []
        for line in lines:
            m = TRIGGER_RE.search(line)
            if not m:
                continue
            s = SETS_RE.search(m.group(2))
            key = next(g for g in s.groups() if g) if s else m.group(1)
            try:
                trig.append((re.compile(m.group(1), re.I), key))
            except re.error:
                trig.append((re.compile(re.escape(m.group(1)), re.I), key))
        flags = set(re.findall(r'getquestkt(?:progress|required)\[(\w+)\]', text))
        info[name] = {'limit': int(limit.group(1)) if limit else None, 'triggers': trig, 'flags': flags,
                      'hunt': bool(HUNTSW_RE.search(text))}
    return info


def task_key(triggers, mob):
    best = None
    for rx, key in triggers:
        m = rx.match(mob)
        if m and (not best or m.end() > best[0]):
            best = (m.end(), key)
    return best[1] if best else None


def untracked_key(tasks, mob, n, req):
    """Task key for a mob no meta trigger names (a variant the meta forgot, or a task the meta doesn't track).

    The server keeps one counter per task, not per mob, so a kill line whose count carries on from a task's last
    count (and has the same requirement) belongs to that task.  Otherwise it is its own task, named after the mob."""
    name = re.sub(r's$', '', mob)
    if name in tasks:
        return name
    for key, k in tasks.items():
        if k['seen'] == n - 1 and (req is None or k['required'] == int(req)) and (req or k['required'] <= n):
            k['joined'] = True
            return key
    return name


def ev_time(e):
    if 'time' in e:
        t = dt.datetime.fromisoformat(e['time'].rstrip('Z')[:26]).replace(tzinfo=dt.timezone.utc)
        return t.astimezone().replace(tzinfo=None)
    return dt.datetime.fromisoformat(e['local'])


def read_log(path):
    out = []
    for line in open(path, encoding='utf-8'):
        if line.strip():
            try:
                out.append(json.loads(line))
            except ValueError:
                pass
    return out


def analyze(recs, since=None, info=None, dump_dir=None):
    if info is None:
        names = set()
        for r in recs:
            for m in LOAD_RE.finditer(r.get('text') or ''):
                names.add(m.group(1).strip().strip('`'))
        info = meta_info(sorted(names), dump_dir)
    meta, state = None, None
    runs, cur, metas_seen, incidents, positions, warns, coins = [], None, [], [], [], [], []
    for r in recs:
        t = ev_time(r)
        if since and t < since:
            continue
        kind = r.get('kind')
        if kind == 'meta':
            if r['what'] in ('client-missing', 'client-found', 'journal-gap', 'events-error'):
                incidents.append({'time': t, 'meta': meta, 'state': state, 'what': r['what'],
                                  'text': json.dumps({k: v for k, v in r.items() if k not in ('kind', 'local', 'what')})})
            continue
        if kind == 'state':
            loc = (r.get('state') or {}).get('location')
            if loc:
                positions.append((t, meta, state, loc['landcell'], loc['x'], loc['y'], loc['z']))
            continue
        text = r.get('text') or ''
        m = START_RE.search(text)
        if m and not metas_seen:
            meta = m.group(1)
            metas_seen.append((t, meta))
        m = LOAD_RE.search(text)
        if m and 'executing action' in text:
            if cur:
                cur['end'] = t
                cur = None
            meta = m.group(1).strip().strip('`')
            metas_seen.append((t, meta))
            if info.get(meta, {}).get('hunt'):
                cur = {'meta': meta, 'limit': info[meta]['limit'], 'loaded': t, 'huntStart': None, 'timeout': None,
                       'turnin': None, 'end': None, 'tasks': OrderedDict(), 'states': [], 'deaths': [], 'npc': [],
                       'notes': [], 'quests': OrderedDict()}
                runs.append(cur)
        m = TRANS_RE.search(text)
        if m:
            state = m.group(1).strip()
            if cur:
                cur['states'].append((t, state))
                if cur['huntStart'] and not cur['turnin'] and re.search(r'turnin', state, re.I):
                    cur['turnin'] = t
                if 'dead' in state.lower():
                    cur['deaths'].append((t, state))
        if kind in ('plugin', 'debug', 'chat') and WARN_RE.search(text):
            warns.append((t, meta, state, text[:200]))
        if kind == 'chat' and DEATH_RE.search(text):
            incidents.append({'time': t, 'meta': meta, 'state': state, 'what': 'death?', 'text': text[:200]})
        m = COIN_RE.search(text) if kind == 'chat' else None
        if m:  # "<NPC or mob> gives you N Enlightened Coins.", from turn-ins and kill rewards
            coins.append((t, meta, m.group(1), int(m.group(2).replace(',', '')), m.group(3).rstrip('s') + 's'))
        if cur is None:
            continue
        if HUNTSW_RE.search(text) and cur['huntStart'] is None and 'executing action' in text:
            cur['huntStart'] = t
        m = LIMIT_MSG_RE.search(text)
        if m and 'executing action' not in text and cur['timeout'] is None:
            cur['timeout'], cur['limit'] = t, int(m.group(2)) * 60  # what the meta itself said on the day
        if re.search(r'took too long|starting over|no open tasks|all tasks done', text):
            if not cur['notes'] or cur['notes'][-1][1] != text:
                cur['notes'].append((t, text))
        if kind != 'chat':
            continue
        m = KILL_RE.search(text)
        if m:
            n, mob, req = int(m.group(1)), m.group(2), m.group(3)
            key = task_key(info[cur['meta']]['triggers'], mob)
            untracked = key is None
            if untracked:
                key = untracked_key(cur['tasks'], mob, n, req)
            k = cur['tasks'].setdefault(key, {'mobs': set(), 'first': t, 'firstCount': n, 'count': 0,
                                             'required': None, 'done': None, 'last': t, 'untracked': untracked,
                                             'seen': 0})
            k['mobs'].add(mob)
            k['seen'] = max(k['seen'], n)
            if req:
                k['required'] = int(req)
                if n > k['count']:
                    k['count'], k['last'] = n, t
            else:  # "Your task is complete!"; it repeats on every later kill, which isn't progress
                k['required'] = k['required'] or n
                if not k['done']:
                    k['done'], k['count'] = t, max(k['count'], n)
            continue
        m = MYQ_RE.search(text)
        if m:
            if not (cur['timeout'] or cur['turnin']) and m.group(1) in info[cur['meta']]['flags']:
                cur['quests'][m.group(1)] = (int(m.group(2)), int(m.group(3)))
            continue
        if OUTOF_RE.search(text):
            cur['notes'].append((t, text))
        elif ' tells you, "' in text and (not cur['npc'] or cur['npc'][-1][1] != text[:220]):
            cur['npc'].append((t, text[:220]))
    return {'runs': runs, 'metas': metas_seen, 'incidents': incidents, 'positions': positions, 'warns': warns,
            'coins': coins}


def coin_totals(coins):
    """(currency, meta, giver) -> [times given, total], plus currency -> total."""
    by, tot = OrderedDict(), OrderedDict()
    for _, meta, giver, n, cur in coins:
        k = by.setdefault((cur, meta, giver), [0, 0])
        k[0], k[1] = k[0] + 1, k[1] + n
        tot[cur] = tot.get(cur, 0) + n
    return by, tot


def mins(a, b):
    return None if not a or not b else round((b - a).total_seconds() / 60, 1)


def stationary(positions, start, end, min_minutes=2.5, tol=3.0):
    pts = [p for p in positions if start <= p[0] <= (end or dt.datetime.max)]
    out, i = [], 0
    while i < len(pts):
        j = i
        while (j + 1 < len(pts) and pts[j + 1][3] == pts[i][3] and abs(pts[j + 1][4] - pts[i][4]) < tol
               and abs(pts[j + 1][5] - pts[i][5]) < tol):
            j += 1
        if mins(pts[i][0], pts[j][0]) >= min_minutes:
            out.append((pts[i][0], pts[j][0], pts[i][2], pts[j][2], pts[i][3]))
        i = j + 1
    return out


def report(res):
    out, ms = ['## Meta chain', '', '| meta | loaded | minutes |', '|---|---|---|'], res['metas']
    for i, (t, m) in enumerate(ms):
        nxt = ms[i + 1][0] if i + 1 < len(ms) else None
        out.append(f"| {m} | {t:%H:%M:%S} | {mins(t, nxt) if nxt else '(last seen)'} |")
    out.append('')
    for r in res['runs']:
        hs, lim = r['huntStart'], r['limit']
        hend = r['timeout'] or r['turnin']
        out.append(f"### {r['meta']} (loaded {r['loaded']:%H:%M:%S}, hunt limit "
                   f"{f'{lim / 60:g} min' if lim else 'unknown'})")
        out.append(f"* hunt stopwatch start: {hs:%H:%M:%S}" if hs else '* hunt stopwatch never started')
        out.append(f"* timeout: {'YES at ' + format(r['timeout'], '%H:%M:%S') if r['timeout'] else 'no'}; "
                   f"turn-in state: {format(r['turnin'], '%H:%M:%S') if r['turnin'] else '-'}")
        out.append(f"* hunt minutes: {mins(hs, hend)}; meta total {mins(r['loaded'], r['end'])}; "
                   f"before hunt {mins(r['loaded'], hs)}; after hunt {mins(hend, r['end'])}")
        if r['deaths']:
            out.append(f"* death states: {', '.join(f'{t:%H:%M:%S} {s}' for t, s in r['deaths'])}")
        out += ['', '| task | mobs | first count seen | killed | required | done (min into hunt) | kills/min |',
                '|---|---|---|---|---|---|---|']
        open_ = []
        for key, k in r['tasks'].items():
            span = mins(k['first'], k['done'] or k['last'])
            rate = round((k['count'] - k['firstCount'] + 1) / span, 2) if span else None
            if not k['done']:
                open_.append(f"{key} {k['count']}/{k['required']}")
            out.append(f"| {key} | {', '.join(sorted(k['mobs']))} | {k['firstCount']} | {k['count']} | "
                       f"{k['required']} | {mins(hs, k['done']) if k['done'] else 'NOT DONE'} | {rate} |")
        out += ['', f"**Tasks with kills but not complete at end of hunt: {len(open_)}** {', '.join(open_)}"]
        loose = [key for key, k in r['tasks'].items() if k['untracked']]
        if loose:
            out.append(f"* tasks with no meta trigger: {', '.join(loose)}")
        joined = [f"{key} ({', '.join(sorted(k['mobs']))})" for key, k in r['tasks'].items()
                  if k.get('joined') and not k['untracked']]
        if joined:
            out.append(f"* mobs the meta's triggers miss, joined to their task by kill count: {'; '.join(joined)}")
        if r['quests']:
            out.append('* last /myquests counters before hunt end: ' + ', '.join(
                f"{q} {n}/{rq}{'' if n >= rq else ' OPEN'}" for q, (n, rq) in r['quests'].items()))
        agg = OrderedDict()
        for i, (t, st) in enumerate(r['states']):
            e2 = r['states'][i + 1][0] if i + 1 < len(r['states']) else r['end']
            if e2:
                agg[st] = agg.get(st, 0) + (e2 - t).total_seconds() / 60
        out.append('* minutes per state: ' + '; '.join(f'{k} {v:.1f}' for k, v in agg.items()))
        for a, b, s1, s2, cell in stationary(res['positions'], hs or r['loaded'], r['end']):
            out.append(f"* stationary {a:%H:%M:%S}-{b:%H:%M:%S} ({mins(a, b)} min), state {s1} -> {s2}, cell {cell}")
        for t, x in r['notes']:
            out.append(f"* note {t:%H:%M:%S} {x[:160]}")
        seen = set()
        for t, x in r['npc']:
            if x not in seen:
                seen.add(x)
                out.append(f"* NPC {t:%H:%M:%S} {x}")
        out.append('')
    by, tot = coin_totals(res.get('coins', []))
    out.append('### Coins received')
    out.append('* totals: ' + ('; '.join(f'{c} {n:,}' for c, n in tot.items()) if tot else 'none'))
    if by:
        out += ['', '| currency | meta | from | times | total |', '|---|---|---|---|---|']
        out += [f'| {c} | {m} | {g} | {k} | {n:,} |' for (c, m, g), (k, n) in by.items()]
    out.append('')
    out.append('### Incidents')
    for i in res['incidents']:
        out.append(f"* {i['time']:%H:%M:%S} {i['what']} meta={i['meta']} state={i['state']} {i.get('text', '')}")
    out.append('')
    out.append('### Warnings and errors (deduplicated)')
    seen = {}
    for t, m, s, x in res['warns']:
        seen.setdefault(re.sub(r'\d+', '#', x[:90]), [t, m, s, x, 0])[4] += 1
    for t, m, s, x, n in seen.values():
        out.append(f"* x{n}, first {t:%H:%M:%S} [{m}/{s}] {x[:160]}")
    return '\n'.join(out)


def cmd_analyze(a):
    dumps = a.metas or os.path.join(os.path.dirname(os.path.abspath(a.log)), 'metas')
    if not os.path.isdir(dumps):
        print(f'(no meta snapshot in {dumps}: using the installed metas, which may have changed since)', file=sys.stderr)
        dumps = None
    res = analyze(read_log(a.log), dt.datetime.fromisoformat(a.since) if a.since else None, dump_dir=dumps)
    print(report(res))
    if a.json:
        with open(a.json, 'w', encoding='utf-8') as f:
            json.dump(res, f, default=str, indent=1)
    return 0


def main(argv):
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0], formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    s = sub.add_parser('start')
    s.add_argument('chain')
    s.add_argument('chars', nargs='+')
    s.add_argument('--at', help='HH:MM (next occurrence) or YYYY-MM-DDTHH:MM; default 07:00')
    s.add_argument('--stagger', type=float, default=15, help='minutes between characters (default 15)')
    s.add_argument('--log', help='append a JSON line per step to this file')
    s.add_argument('--dry-run', action='store_true', help='print the plan and exit')
    s.add_argument('--record', metavar='OUTDIR', help='also record each character into OUTDIR, from its start')
    s.add_argument('--record-hours', type=float, default=3, help='how long each recorder runs (default 3)')
    s = sub.add_parser('record')
    s.add_argument('char')
    s.add_argument('outdir')
    s.add_argument('--start')
    s.add_argument('--stop')
    s.add_argument('--hours', type=float, default=8)
    s = sub.add_parser('analyze')
    s.add_argument('log')
    s.add_argument('--since')
    s.add_argument('--json')
    s.add_argument('--metas', help='meta dump folder (default: <log folder>/metas, written by record)')
    a = ap.parse_args(argv)
    return {'start': cmd_start, 'record': cmd_record, 'analyze': cmd_analyze}[a.cmd](a)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
