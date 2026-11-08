"""Town Network travel meta: say "!<town>" in fellowship chat and every member running this meta uses the Town Network
Sending Gem, walks to that town's portal and goes through it.  Builds aTownNetwork.met in the VT folder.

    py tools/vt/examples/aTownNetwork.py [<VT folder>]          build the meta
    py tools/vt/examples/aTownNetwork.py --list                 print the commands and routes

Matches "[Fellowship] <name> says, "!cragstone"" from other members and "[Fellowship] You say, "!cragstone"" from
yourself, case-insensitive, with or without spaces, hyphens or apostrophes in the name ("!fort tethana",
"!forttethana", "!qalabar").

Portal positions are in TownNetworkPortals.json (from `rectl world --class Portal` inside Town Network, 2026-11-08).
The Sending Gem always lands at the Annex (70, -80).  The building is a cross: a north wing (Aluvian towns), a south
wing (Facility Hub / Marketplace crossbar at y -100, then the big room), a west wing (Gharu'ndim) and an east wing
(Sho).  Every wing has furniture in the middle (desks, a bar), so routes go along the walls, about 3 m out from the
portals.  All approach points were walked on Woodford 2026-11-08.

States: Default waits for a command.  G_<key> uses the gem (again at 15 s and 30 s if nothing happened, gives up at
45 s) and waits to be in the Annex landing cell.  N_<key> walks the embedded route and uses the portal; leaving the Town Network landblock ends it, and so does
a 90 s timeout.
"""
import json, os, re, sys, tempfile

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import vtmeta as m
import vtnav

HERE = os.path.dirname(os.path.abspath(__file__))
TN = 0x00070000
LB = 0x0007_0100        # any cell in the Town Network landblock; nav coords only use the landblock
GEM = 'Town Network Sending Gem'
ANNEX = 0x00070145      # the cell the gem lands in, (70, -80)

# Portal name -> command names.  Spaces, hyphens and apostrophes are optional when matching.
NAMES = {
    'Al-Jalima Portal': ['al-jalima', 'jalima', 'jal'],
    'Tufa Portal': ['tufa'],
    'Uziz Portal': ['uziz'],
    'Portal to Zaikhal': ['zaikhal', 'zai'],
    'Portal to Khayyaban': ['khayyaban', 'khay'],
    'Portal to Xarabydun': ['xarabydun', 'xara'],
    'Portal to Al-Arqas': ['al-arqas', 'arqas', 'arq'],
    "Qalaba'r Portal": ["qalaba'r", 'qal'],
    'Portal to Yaraq': ['yaraq'],
    'Portal to Samsur': ['samsur'],
    'Portal to Cragstone': ['cragstone', 'crag'],
    'Lytelthorpe Portal': ['lytelthorpe', 'lyt'],
    'Glenden Wood Portal': ['glenden wood', 'glenden', 'gw'],
    'Portal to Holtburg': ['holtburg', 'holt'],
    'Plateau Portal': ['plateau', 'plat'],
    'Fort Tethana Portal': ['fort tethana', 'tethana', 'teth'],
    'Stonehold Portal': ['stonehold', 'stone'],
    'Portal to Rithwic': ['rithwic', 'rith'],
    'Portal to Dryreach': ['dryreach', 'dry'],
    'Eastham Portal': ['eastham', 'ham'],
    'Portal to Arwic': ['arwic'],
    'Portal to Facility Hub': ['facility hub', 'fh', 'hub'],
    'The Marketplace of Dereth': ['marketplace', 'mp', 'market'],
    'Portal to Bluespire': ['bluespire', 'blue'],
    'Portal to Greenspire': ['greenspire', 'green'],
    'Ahurenga Portal': ['ahurenga', 'ahu'],
    'Portal to Neydisa Castle': ['neydisa castle', 'neydisa', 'ney'],
    'Portal to Silyun': ['silyun'],
    'Fiun Outpost': ['fiun outpost', 'fiun'],
    'Base of the Timaru Plateau Portal': ['timaru plateau', 'timaru'],
    'Abandoned Mine': ['abandoned mine', 'mine', 'am'],
    "Oolutanga's Refuge": ["oolutanga's refuge", 'oolutanga', 'oolu'],
    'Portal to Sanamar': ['sanamar', 'sana'],
    'Portal to Redspire': ['redspire', 'red'],
    'Portal to Westwatch': ['westwatch', 'ww'],
    'Portal to Linvak Tukal': ['linvak tukal', 'tukal', 'linvak'],
    "Danby's Outpost": ["danby's outpost", "danby's", 'danby'],
    'Portal to Eastwatch': ['eastwatch', 'ew'],
    'Portal to Tou-Tou': ['tou-tou'],
    'Portal to Shoushi': ['shoushi', 'shou'],
    'Portal to Nanto': ['nanto'],
    'Mayoi Portal': ['mayoi'],
    'Baishi Portal': ['baishi'],
    'Lin Portal': ['lin'],
    'Portal to Yanshi': ['yanshi'],
    'Portal to Hebian-To': ['hebian-to', 'hebian', 'heb'],
    'Sawato Portal': ['sawato'],
    'Portal to Kara': ['kara'],
    'Portal to Kryst': ['kryst'],
    'Freehold Portal': ['freehold', 'free'],
}
# Not included: "Quest Explorer Portal Week46" (changes weekly), and Linvak Extas and Korway, which are on a lower
# floor under the south room that a nav couldn't get down to.


def local(ew, ns):
    """Object nav coords -> local x, y in the Town Network landblock."""
    return ew * 240 + 84 - (0 - 127) * 192, ns * 240 + 84 - (7 - 127) * 192


def route_points(x, y):
    """Waypoints (local x, y) from the Annex landing to about 3 m in front of the portal at (x, y)."""
    if -45 < y:                                   # north wing
        base = [(70, -60), (70, -45)]
        if y > -6:                                # end wall: Plateau, Fort Tethana (west side), Stonehold (east side)
            if x < 75:
                return base + [(57, -42), (57, -10), (60, -6.5)] + ([(x, -6.5)] if x > 62 else [])
            return base + [(83, -42), (83, -10), (x, -6.5)]
        side = 57 if x < 70 else 83
        return base + [(side, -42), (side, y)]
    if -105 < y < -95:                            # Facility Hub / Marketplace crossbar
        return [(70, -100), (57 if x < 70 else 83, -100)]
    if y < -105:                                  # south room
        base = [(70, -100), (70, -113)]
        if y < -174:                              # end wall
            if x < 75:
                return base + [(57, -120), (57, -170), (60, -173)] + ([(x, -173)] if x > 62 else [])
            return base + [(83, -120), (83, -170), (x, -173)]
        side = 57 if x < 70 else 83
        return base + [(side, -120), (side, y)]
    if x < 50:                                    # west wing
        if x < 6:                                 # end wall
            if y > -70:
                return [(60, -70), (45, -70), (42, -70), (40, -57), (10, -57), (7.5, y)]
            return [(60, -70), (45, -70), (45, -83), (10, -83), (7.5, y)]
        if y > -70:
            return [(60, -70), (45, -70), (42, -70), (40, -57), (x, -57)]
        return [(60, -70), (45, -70), (45, -83), (x, -83)]
    if x > 95:                                    # east wing
        base = [(80, -70), (95, -70), (99, -69)]
        if x > 145:                               # end wall
            if y > -70:
                return base + [(99, -57), (140, -57), (143, y)]
            return base + [(100, -83), (140, -83), (143, y)]
        if y > -70:
            return base + [(99, -57), (x, -57)]
        return base + [(100, -83), (x, -83)]
    raise ValueError(f'no route for ({x:.1f}, {y:.1f})')


def key_of(portal):
    return re.sub(r'[^A-Za-z]', '', NAMES[portal][0].title())


def name_regex(names):
    alts = []
    for n in names:
        parts = re.split(r"[ '\-]+", n)
        alts.append("[ '\\-]?".join(re.escape(p) for p in parts))
    return '^\\[Fellowship\\] .*says?, "!(?i:' + '|'.join(alts) + ')"$'


def nav_for(portal, ew, ns, z):
    r = vtnav.Route(4).pause(2000)
    for (px, py) in [(70, -80)] + route_points(*local(ew, ns)):
        r.point_at(LB, px, py, 0)
    r.use(portal, vtnav.PORTAL_CLASS, ew, ns, z)
    return r


def rules(portals):
    out = [
        ('Default', m.always(), m.opt('EnableNav', False)),
    ]
    tmp = tempfile.mkdtemp(prefix='aTownNetwork-')
    for portal in NAMES:
        ew, ns, z = portals[portal]
        key = key_of(portal)
        nav_path = os.path.join(tmp, f'TN_{key}.nav')
        nav_for(portal, ew, ns, z).save(nav_path)
        g, n = f'G_{key}', f'N_{key}'
        out += [
            ('Default', m.chat_message(name_regex(NAMES[portal])), m.set_state(g)),
            # Use the gem.  It fails silently when busy (fighting, casting), so try again.
            (g, m.always(), m.all_a(m.opt('EnableNav', False), m.chat(f'/ub usei {GEM}'))),
            (g, m.seconds_in_state(15), m.chat(f'/ub usei {GEM}')),
            (g, m.seconds_in_state(30), m.chat(f'/ub usei {GEM}')),
            (g, m.seconds_in_state(45), m.set_state('Default')),
            # Arrived at the Annex landing.  (PortalspaceExit isn't usable here: it fired a second after the gem use,
            # before the teleport.)
            (g, m.all_c(m.landcell(ANNEX), m.seconds_in_state(2)), m.set_state(n)),
            # Walk to the portal and use it
            (n, m.always(), m.all_a(m.embed_nav(nav_path, f'TN_{key}.nav'), m.opt('EnableNav', True))),
            (n, m.not_c(m.landblock(TN)), m.set_state('Default')),
            (n, m.seconds_in_state(90), m.set_state('Default')),
        ]
    return out


def main(argv):
    portals = json.load(open(os.path.join(HERE, 'TownNetworkPortals.json')))
    missing = [p for p in NAMES if p not in portals]
    if missing:
        sys.exit(f'not in TownNetworkPortals.json: {missing}')
    seen = {}
    for p, names in NAMES.items():
        for n in names:
            norm = re.sub(r"[ '\-]", '', n.lower())
            if seen.setdefault(norm, p) != p:
                sys.exit(f'"{n}" is used by both {seen[norm]} and {p}')
    if argv[:1] == ['--list']:
        for p in NAMES:
            x, y = local(*portals[p][:2])
            print(f'!{NAMES[p][0]:<20} {p:<34} ({x:6.1f},{y:6.1f})  via {route_points(x, y)}')
        return
    if argv:
        vt = argv[0]
    else:
        import rectl
        vt = rectl.vt_dir()
    out = os.path.join(vt, 'aTownNetwork.met')
    r = rules(portals)
    data = m.save(out, r)
    print(f'wrote {len(r)} rules for {len(NAMES)} portals, {len(data)} bytes to {out}')


if __name__ == '__main__':
    main(sys.argv[1:])
