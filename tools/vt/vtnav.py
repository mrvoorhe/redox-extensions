"""Read, write and build VTank nav routes ("uTank2 NAV 1.2").

    py tools/vt/vtnav.py dump <file.nav>                 print the nodes
    py tools/vt/vtnav.py build <spec.txt> <out.nav>      build a route from a spec (format below)
    py tools/vt/vtnav.py coord <landcell hex> <x> <y> <z>  nav coordinates for a game position

Spec format, one node per line (blank lines and lines starting with # are ignored).  The first line that isn't a
node may be "type circular|linear|once" (default once):

    pt <landcell hex> <x> <y> <z>                       waypoint at a game position (rectl state / positions log)
    navpt <ew> <ns> <z>                                 waypoint already in nav coordinates
    chat <text>                                         chat command, e.g. chat /ub usei Town Network Sending Gem
    pause <ms>
    portal <ew> <ns> <z> <name>                         use a portal (object class 14); ew/ns/z are the OBJECT's nav coords
    npc <ew> <ns> <z> <name>                            use an NPC (object class 37)
    use <class> <ew> <ns> <z> <name>                    use any object with an explicit ObjectClass number
    vendor <id> <name>                                  open a vendor
    jump <landcell hex> <x> <y> <z> <heading> <shift True|False> <ms>   jump from a game position
"""
import sys

HEADER = 'uTank2 NAV 1.2'
ROUTE_TYPES = {'circular': 1, 'linear': 2, 'once': 4}
NODE_NAMES = {0: 'point', 2: 'recall', 3: 'pause', 4: 'chat', 5: 'vendor', 6: 'use', 7: 'type7', 9: 'jump'}
# Lines after the 5 common ones (type, ew, ns, z, 0), by node type.  Only types seen in real files are listed.
# Type 2 holds one number, probably a spell id (2645 in ZTouTou.nav; named recall here, not verified).  Type 7 has the same layout as 6 (seen once, on an NPC); what VT does differently with it isn't known.
EXTRA_LINES = {0: 0, 2: 1, 3: 1, 4: 1, 5: 2, 6: 6, 7: 6, 9: 3}
PORTAL_CLASS, NPC_CLASS = 14, 37


def nav_coord(landcell, x, y, z):
    """Game position (landcell + local x/y/z) to nav (ew, ns, z).  Same as VTNavCoord.FromLandcell."""
    lbx = (landcell >> 24) & 0xFF
    lby = (landcell >> 16) & 0xFF
    return ((lbx - 127) * 192.0 + x - 84.0) / 240.0, ((lby - 127) * 192.0 + y - 84.0) / 240.0, z / 240.0


def fmt(v):
    """Numbers the way VT and VTNavRoute write them (.NET invariant "R": no trailing .0, uppercase E).
    Strings (values read from a file) are written back unchanged, so parse + serialize is lossless."""
    if isinstance(v, float):
        if v == int(v) and abs(v) < 1e15:
            return str(int(v))
        return repr(v).replace('e', 'E')
    return str(v)


class Node:
    def __init__(self, type_, ew=0.0, ns=0.0, z=0.0, extra=()):
        self.type, self.ew, self.ns, self.z, self.extra = type_, ew, ns, z, list(extra)

    def position(self):
        return float(self.ew), float(self.ns), float(self.z)

    def lines(self):
        return [str(self.type), fmt(self.ew), fmt(self.ns), fmt(self.z), '0'] + [fmt(x) for x in self.extra]

    def __repr__(self):
        name = NODE_NAMES.get(self.type, self.type)
        pos = '({:.4f}, {:.4f}, {:.4f})'.format(*self.position())
        return f'{name} {pos} {self.extra}' if self.extra else f'{name} {pos}'


class Route:
    def __init__(self, route_type=4, nodes=None):
        self.route_type, self.nodes = route_type, nodes or []

    # Builders
    def point(self, ew, ns, z): self.nodes.append(Node(0, ew, ns, z)); return self
    def point_at(self, landcell, x, y, z): return self.point(*nav_coord(landcell, x, y, z))
    def pause(self, ms): self.nodes.append(Node(3, extra=[int(ms)])); return self
    def chat(self, text): self.nodes.append(Node(4, extra=[text])); return self
    def vendor(self, vendor_id, name): self.nodes.append(Node(5, extra=[int(vendor_id), name])); return self

    def use(self, name, object_class, ew, ns, z):
        """Walk to and use a portal/NPC.  VT finds the object by name, class and these coordinates."""
        self.nodes.append(Node(6, extra=[name, int(object_class), 'True', float(ew), float(ns), float(z)]))
        return self

    def jump(self, ew, ns, z, heading, shift=False, ms=500):
        """Same layout /vt addnavjump writes: position, heading, shift (walk), milliseconds of charge."""
        self.nodes.append(Node(9, ew, ns, z, [float(heading), 'True' if shift else 'False', float(ms)]))
        return self

    def serialize(self):
        out = [HEADER, str(self.route_type), str(len(self.nodes))]
        for n in self.nodes:
            out += n.lines()
        return '\r\n'.join(out) + '\r\n'

    def save(self, path):
        with open(path, 'w', encoding='latin-1', newline='') as f:
            f.write(self.serialize())


def parse(text):
    lines = text.replace('\r\n', '\n').split('\n')
    if lines[0] != HEADER:
        raise ValueError(f'not a nav file (first line {lines[0]!r})')
    route_type = int(lines[1])
    if route_type not in ROUTE_TYPES.values():
        raise ValueError(f'route type {route_type} (follow routes etc.) is not supported')
    count, p, nodes = int(lines[2]), 3, []
    for _ in range(count):
        t = int(lines[p])
        if t not in EXTRA_LINES:
            raise ValueError(f'unknown node type {t} at line {p + 1}')
        ew, ns, z = lines[p + 1:p + 4]
        n = EXTRA_LINES[t]
        nodes.append(Node(t, ew, ns, z, lines[p + 5:p + 5 + n]))
        p += 5 + n
    return Route(route_type, nodes)


def load(path):
    with open(path, encoding='latin-1', newline='') as f:
        return parse(f.read())


def build_spec(text):
    route = Route()
    for raw in text.splitlines():
        line = raw.strip()
        if not line or line.startswith('#'):
            continue
        f = line.split()
        op, a = f[0].lower(), f[1:]
        if op == 'type':
            route.route_type = ROUTE_TYPES[a[0].lower()]
        elif op == 'pt':
            route.point_at(int(a[0], 16), float(a[1]), float(a[2]), float(a[3]))
        elif op == 'navpt':
            route.point(float(a[0]), float(a[1]), float(a[2]))
        elif op == 'chat':
            route.chat(line.split(None, 1)[1])
        elif op == 'pause':
            route.pause(int(a[0]))
        elif op in ('portal', 'npc'):
            route.use(' '.join(a[3:]), PORTAL_CLASS if op == 'portal' else NPC_CLASS, *map(float, a[:3]))
        elif op == 'use':
            route.use(' '.join(a[4:]), int(a[0]), *map(float, a[1:4]))
        elif op == 'vendor':
            route.vendor(int(a[0]), ' '.join(a[1:]))
        elif op == 'jump':
            ew, ns, z = nav_coord(int(a[0], 16), float(a[1]), float(a[2]), float(a[3]))
            route.jump(ew, ns, z, float(a[4]), a[5].lower() == 'true', float(a[6]))
        else:
            raise ValueError(f'unknown spec line: {raw}')
    return route


def main(argv):
    if len(argv) >= 2 and argv[0] == 'dump':
        r = load(argv[1])
        names = {v: k for k, v in ROUTE_TYPES.items()}
        print(f'{names[r.route_type]} route, {len(r.nodes)} nodes')
        for i, n in enumerate(r.nodes):
            print(f'{i:3} {n!r}')
    elif len(argv) == 3 and argv[0] == 'build':
        with open(argv[1], encoding='utf-8') as f:
            r = build_spec(f.read())
        r.save(argv[2])
        print(f'wrote {len(r.nodes)} nodes to {argv[2]}')
    elif len(argv) == 5 and argv[0] == 'coord':
        print(*[fmt(v) for v in nav_coord(int(argv[1], 16), *map(float, argv[2:]))])
    else:
        print(__doc__)
        return 2
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
