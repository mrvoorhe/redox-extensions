"""Tests for vtnav and vtrecord.  Run: py -m unittest discover -s tools/vt/tests"""
import os, sys, unittest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import vtnav, vtrecord

# A nav with every node type seen in real files, written the way VT writes it
SAMPLE_NAV = '\r\n'.join([
    'uTank2 NAV 1.2', '4', '7',
    '4', '0', '0', '0', '0', '/ub usei Town Network Sending Gem',
    '3', '0', '0', '0', '0', '5000',
    '0', '-101.640410709381', '-96.6705167134603', '2.08333134651184E-05', '0',
    '6', '-100.942167504629', '28.2436187426249', '-0.374979146321615', '0',
    'Mayoi Portal', '14', 'True', '-101.491666666667', '-96.5723725001017', '-0.000262499845121056',
    '9', '-100.942090304693', '28.2198474884033', '-0.374979146321615', '0', '180', 'False', '500.00003',
    '2', '95.349081103007', '-29.2172978719076', '0.0833541631698608', '0', '2645',
    '5', '0', '0', '0', '0', '1234', 'Shopkeeper Renald',
]) + '\r\n'


class NavTests(unittest.TestCase):
    def test_nav_coord_matches_jam_bridge_nav(self):
        # JamBridge.nav's only point, recorded in VT at 01A30153 (50.21, -64.52, -101.99)
        ew, ns, z = vtnav.nav_coord(0x01A30153, 50.21, -64.52, -101.99)
        self.assertAlmostEqual(ew, -100.940903981527, places=3)
        self.assertAlmostEqual(ns, 28.181179523468, places=3)
        self.assertAlmostEqual(z, -0.424979146321615, places=3)

    def test_parse_and_serialize_is_lossless(self):
        route = vtnav.parse(SAMPLE_NAV)
        self.assertEqual([n.type for n in route.nodes], [4, 3, 0, 6, 9, 2, 5])
        self.assertEqual(route.serialize(), SAMPLE_NAV)

    def test_build_spec(self):
        route = vtnav.build_spec('\n'.join([
            'type circular',
            '# comment',
            'chat /ub usei Town Network Sending Gem',
            'pause 5000',
            'pt 01A30153 50.21 -64.52 -101.99',
            'portal 81.8789291381836 -61.3628124872843 0.116895832121372 Help Wanted',
            'jump 01A3017E 49.79 -55.24 -89.99 180 False 500',
        ]))
        self.assertEqual(route.route_type, 1)
        text = route.serialize()
        self.assertTrue(text.startswith('uTank2 NAV 1.2\r\n1\r\n5\r\n4\r\n0\r\n0\r\n0\r\n0\r\n/ub usei Town Network Sending Gem\r\n'))
        self.assertIn('\r\nHelp Wanted\r\n14\r\nTrue\r\n81.8789291381836\r\n-61.3628124872843\r\n0.116895832121372\r\n', text)
        self.assertTrue(text.endswith('\r\n0\r\n180\r\nFalse\r\n500\r\n'))
        self.assertEqual(vtnav.parse(text).serialize(), text)

    def test_numbers_are_written_like_dotnet(self):
        self.assertEqual(vtnav.fmt(500.0), '500')
        self.assertEqual(vtnav.fmt(2.08333134651184e-05), '2.08333134651184E-05')


def pos(t, cell, x, y, z, heading=0):
    return {'t': t, 'cell': f'{cell:08X}', 'x': x, 'y': y, 'z': z, 'heading': heading}


class RecordSpecTests(unittest.TestCase):
    def test_merges_uses_and_jumps_by_time(self):
        gem = {'id': 1, 'name': 'Town Network Sending Gem', 'container': 99}
        portal = {'id': 2, 'name': 'Help Wanted', 'classId': 14, 'nav': [81.87, -61.36, 0.11]}
        positions = [
            pos(0, 0x01C9022D, 72.9, -30.2, 0),
            pos(2, 0x00070145, 70, -80, 0),                 # teleported by the gem
            pos(3, 0x00070145, 80, -80, 0),
            pos(4, 0x00070145, 90, -80, 0),
            pos(5, 0x00070145, 95, -80, 0),                 # walk-up after the portal use: skipped
            pos(8, 0x01A30305, 49.88, -49.53, 0),           # teleported by the portal
            pos(9, 0x01A3017D, 49.88, -49.53, -40),         # falling: collapses with the next
            pos(10, 0x01A3017D, 49.88, -49.53, -89.99),
            pos(11, 0x01A3017D, 49.86, -54.44, -89.99, 180),
            pos(12, 0x01A3017E, 49.86, -59.71, -85.88),     # in the air: skipped
            pos(13, 0x01A30153, 49.86, -62.40, -101.99),    # landed
            pos(14, 0x01A30153, 49.86, -62.40, -101.99),
        ]
        actions = [
            {'t': 1, 'action': 'use', 'object': gem},
            {'t': 4.5, 'action': 'use', 'object': portal},
            {'t': 6, 'action': 'use', 'object': portal},    # second click before the teleport: dropped
            {'t': 11.5, 'action': 'jump'},
        ]
        lines = [l.split('    #')[0] for l in vtrecord.build_spec(positions, actions, min_move=4)]
        self.assertEqual(lines, [
            'pt 01C9022D 72.90 -30.20 0.00',
            'chat /ub usei Town Network Sending Gem',
            'pause 5000',
            'pt 00070145 70.00 -80.00 0.00',
            'pt 00070145 80.00 -80.00 0.00',
            'pt 00070145 90.00 -80.00 0.00',
            'portal 81.87 -61.36 0.11 Help Wanted',
            'pause 5000',
            'pt 01A30305 49.88 -49.53 0.00',
            'pt 01A3017D 49.88 -49.53 -89.99',
            'pt 01A3017D 49.86 -54.44 -89.99',
            'jump 01A3017D 49.86 -54.44 -89.99 180 False 500',
            'pt 01A30153 49.86 -62.40 -101.99',
        ])

    def test_event_time(self):
        self.assertAlmostEqual(vtrecord.event_time('2026-11-05T23:57:46.4852512Z'), 1793923066.485251, places=5)


if __name__ == '__main__':
    unittest.main()
