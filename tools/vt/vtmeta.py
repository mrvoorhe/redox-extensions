"""Read, write, dump and build VTank meta files (.met).

    py tools/vt/vtmeta.py dump <file.met>          print the rules as  [State] IF <condition>  DO <action>
    py tools/vt/vtmeta.py check <glob>             check read + write gives the same bytes for every match

Building a meta from Python:

    import os, rectl, vtmeta as m
    vt = rectl.vt_dir()                    # the user's VirindiTank folder
    rules = [
        ('Default', m.always(), m.chat('/vt settings loadchar Farm')),
        ('Default', m.all_c(m.seconds_in_state(2), m.landcell(0x01A30153)), m.set_state('Farm')),
        ('Farm', m.always(), m.all_a(m.embed_nav(os.path.join(vt, 'JamBridge.nav')), m.opt('EnableCombat', True))),
    ]
    m.save(os.path.join(vt, 'MyMeta.met'), rules)

The file format is VT's typed table: each value is a type line ("i", "s", "d", "b", "ba", "TABLE") then its data.
The top table has columns CType, AType, CData, AData, State, one row per rule.  Encodings below were taken from the
user's own metas (every .met in the VT folder round-trips byte for byte).
"""
import glob, os, sys

COND = {0: 'Never', 1: 'Always', 2: 'All', 3: 'Any', 4: 'ChatMessage', 5: 'MainPackSlotsLE', 6: 'SecondsInStateGE',
        7: 'NavRouteEmpty', 8: 'Died', 9: 'VendorOpen', 10: 'VendorClosed', 11: 'ItemCountLE', 12: 'ItemCountGE',
        13: 'MonsterCountWithinDistance', 14: 'MonstersWithPriorityWithinDistance', 15: 'NeedToBuff',
        16: 'NoMonstersWithinDistance', 17: 'LandBlockE', 18: 'LandCellE', 19: 'PortalspaceEnter',
        20: 'PortalspaceExit', 21: 'Not', 22: 'SecondsInStatePersistGE', 23: 'TimeLeftOnSpellGE',
        24: 'BurdenPercentGE', 25: 'DistAnyRoutePtGE', 26: 'Expression', 27: 'ClientDialogPopup',
        28: 'ChatMessageCapture'}
ACT = {0: 'None', 1: 'SetState', 2: 'ChatCommand', 3: 'All', 4: 'EmbedNav', 5: 'CallState', 6: 'ReturnFromCall',
       7: 'Expression', 8: 'ChatExpression', 9: 'SetWatchdog', 10: 'ClearWatchdog', 11: 'GetVTOption',
       12: 'SetVTOption', 13: 'CreateView', 14: 'DestroyView', 15: 'DestroyAllViews'}


# ---------------------------------------------------------------- lossless file format

class Table:
    def __init__(self, cols, rows, flags=None):
        self.cols, self.rows = cols, rows
        self.flags = flags or ['n'] * len(cols)


def read_bytes(data):
    """Returns (header lines, top Table, trailing bytes).  Values are (tag, payload) tuples; payloads stay text."""
    p = 0

    def line():
        nonlocal p
        e = data.index(b'\r\n', p)
        s = data[p:e].decode('latin-1')
        p = e + 2
        return s

    def value():
        nonlocal p
        t = line()
        if t in ('i', 's', 'd', 'b'):
            return (t, line())
        if t == 'ba':
            n = int(line())
            raw = data[p:p + n]
            p += n
            return (t, raw)
        if t == 'TABLE':
            return (t, table())
        raise ValueError(f'unknown value type {t!r} at byte {p}')

    def table():
        n = int(line())
        cols = [line() for _ in range(n)]
        flags = [line() for _ in range(n)]
        rows = [[value() for _ in cols] for _ in range(int(line()))]
        return Table(cols, rows, flags)

    header = [line(), line()]
    return header, table(), data[p:]


def write_bytes(header, table, tail=b''):
    out = []

    def line(s):
        out.append(s.encode('latin-1') + b'\r\n')

    def value(v):
        tag, x = v
        line(tag)
        if tag == 'ba':
            line(str(len(x)))
            out.append(x)
        elif tag == 'TABLE':
            write_table(x)
        else:
            line(x)

    def write_table(t):
        line(str(len(t.cols)))
        for c in t.cols:
            line(c)
        for f in t.flags:
            line(f)
        line(str(len(t.rows)))
        for r in t.rows:
            for v in r:
                value(v)

    for h in header:
        line(h)
    write_table(table)
    return b''.join(out) + tail


def load(path):
    with open(path, 'rb') as f:
        return read_bytes(f.read())


# ---------------------------------------------------------------- builders

def _i(n):
    return ('i', str(n - 2**32 if n >= 2**31 else n))  # landcells above 0x7FFFFFFF are stored signed


def _s(x):
    return ('s', x)


def _d(x):
    return ('d', repr(float(x)).replace('e', 'E').removesuffix('.0'))


def _kv(*pairs):  # option/expression tables use lowercase k/v
    return ('TABLE', Table(['k', 'v'], [[_s(k), v] for k, v in pairs]))


def _KV(items):  # lists of child conditions/actions use uppercase K/V: K is the type, V its data
    return ('TABLE', Table(['K', 'V'], [[_i(t), d] for t, d in items]))


# Conditions: (type, data)
def never(): return (0, _i(0))
def always(): return (1, _i(0))
def all_c(*conds): return (2, _KV(conds))
def any_c(*conds): return (3, _KV(conds))
def chat_message(regex): return (4, _s(regex))
def seconds_in_state(n): return (6, _i(n))
def nav_empty(): return (7, _i(0))
def died(): return (8, _i(0))
def monster_count(name_regex, count, distance): return (13, _kv(('n', _s(name_regex)), ('c', _i(count)), ('r', _d(distance))))
def no_monsters_within(distance): return (16, _kv(('r', _d(distance)),))
def landblock(lb): return (17, _i(lb & 0xFFFF0000))  # landblock in the high word, e.g. 0x01A30000
def landcell(cell): return (18, _i(cell))
def portal_enter(): return (19, _i(0))
def portal_exit(): return (20, _i(0))
def not_c(cond): return (21, _KV([cond]))
def seconds_in_state_persist(n): return (22, _i(n))
def expr_c(expression): return (26, _kv(('e', _s(expression)),))


# Actions: (type, data)
def none(): return (0, _i(0))
def set_state(state): return (1, _s(state))
def chat(text): return (2, _s(text))
def all_a(*acts): return (3, _KV(acts))
def call_state(state, return_to): return (5, _kv(('st', _s(state)), ('ret', _s(return_to))))
def return_from_call(): return (6, _i(0))
def expr_a(expression): return (7, _kv(('e', _s(expression)),))
def watchdog(state, distance, seconds): return (9, _kv(('s', _s(state)), ('r', _d(distance)), ('t', _d(seconds))))
def clear_watchdog(): return (10, _kv())  # VT writes an empty k/v table (aQD9ELerRhan)


def opt(name, value):
    """SetVTOption.  Booleans are written True/False, as VT writes them."""
    if isinstance(value, bool):
        value = 'True' if value else 'False'
    return (12, _kv(('o', _s(name)), ('v', _s(str(value)))))


def embed_nav(nav_path, name=None):
    """EmbedNav: the nav's name, its node count, then the whole nav file."""
    with open(nav_path, 'rb') as f:
        nav = f.read()
    count = nav.split(b'\r\n')[2]
    name = name or os.path.basename(nav_path)
    return (4, ('ba', name.encode('latin-1') + b'\r\n' + count + b'\r\n' + nav))


def build(rules):
    """rules: list of (state, condition, action).  Returns the file bytes."""
    rows = [[_i(c[0]), _i(a[0]), c[1], a[1], _s(state)] for state, c, a in rules]
    data = write_bytes(['1', 'CondAct'], Table(['CType', 'AType', 'CData', 'AData', 'State'], rows))
    assert write_bytes(*read_bytes(data)) == data
    return data


def save(path, rules):
    data = build(rules)
    with open(path, 'wb') as f:
        f.write(data)
    return data


# ---------------------------------------------------------------- dump

def describe(v, names=None):
    tag, x = v
    if tag == 'TABLE':
        if x.cols == ['K', 'V']:
            return '{' + '; '.join(describe_typed(int(k[1]), d, names) for k, d in x.rows) + '}'
        return '{' + ', '.join(f'{k[1]}={describe(val)}' for k, val in x.rows) + '}'
    if tag == 'ba':
        first = x.split(b'\r\n', 2)
        return f'<nav {first[0].decode("latin-1")!r}, {first[1].decode()} nodes, {len(x)} bytes>'
    return repr(x) if tag == 's' else x


def describe_typed(t, data, names):
    return f'{names.get(t, t)} {describe(data, names)}'


def dump(path):
    _, t, tail = load(path)
    lines = []
    for ct, at, cd, ad, st in t.rows:
        lines.append(f'[{st[1]}] IF {describe_typed(int(ct[1]), cd, COND)}  DO {describe_typed(int(at[1]), ad, ACT)}')
    if tail:
        lines.append(f'(+{len(tail)} trailing bytes)')
    return lines


def main(argv):
    if len(argv) == 2 and argv[0] == 'dump':
        print('\n'.join(dump(argv[1])))
    elif len(argv) == 2 and argv[0] == 'check':
        ok = bad = 0
        for f in glob.glob(argv[1]):
            with open(f, 'rb') as fh:
                data = fh.read()
            try:
                same = write_bytes(*read_bytes(data)) == data
            except Exception as e:
                print('ERROR', f, e)
                same = False
            ok += same
            bad += not same
            if not same:
                print('DIFF', f)
        print(f'identical {ok}, different {bad}')
        return 1 if bad else 0
    else:
        print(__doc__)
        return 2
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
