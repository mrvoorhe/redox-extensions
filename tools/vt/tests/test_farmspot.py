"""Tests for farmspot.  Run: py -m unittest discover -s tools/vt/tests"""
import os, sys, tempfile, unittest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import farmspot
import vtmeta

LB = 0x57660000


def pos(t, x, y, cell='576602B8'):
    return {'t': t, 'cell': cell, 'x': x, 'y': y, 'z': 0.0, 'heading': 0.0}


def say(t, text):
    return {'t': t, 'kind': 'chat', 'text': f'[Fellowship] You say, "{text}"'}


class MarkerTests(unittest.TestCase):
    def test_parse_spot(self):
        self.assertEqual(farmspot.parse_spot('spot - left / right'), 'LR')
        self.assertEqual(farmspot.parse_spot('spot - let'), 'L')       # a typo seen in a real recording
        self.assertEqual(farmspot.parse_spot('spot - straight'), 'S')
        self.assertEqual(farmspot.parse_spot('spot fixed'), 'fixed')
        self.assertEqual(farmspot.parse_spot('spot - fixed'), 'fixed')
        self.assertIsNone(farmspot.parse_spot('spot - over there'))
        self.assertIsNone(farmspot.parse_spot('findspot - begin'))

    def test_markers_only_take_what_the_character_said(self):
        chat = [say(1, 'Spot - Left'), {'t': 2, 'text': 'Bob says, "spot - right"'}]
        self.assertEqual(farmspot.markers(chat), [(1, 'spot - left')])

    def test_heading_snaps_to_an_axis(self):
        samples = [pos(0, 10, 0), pos(1, 10.3, -3), pos(2, 10.4, -5)]
        self.assertEqual(farmspot.heading_into(samples, 2), (0, -1))

    def test_heading_off_axis_is_kept(self):
        samples = [pos(0, 0, 0), pos(1, 3, 3)]
        self.assertEqual(farmspot.heading_into(samples, 1), (0.71, 0.71))


class RouteTests(unittest.TestCase):
    def test_route_specs_from_a_marked_recording(self):
        positions = [pos(10 + i, 390, -80 + 5 * i) for i in range(8)]  # walking north from the entrance
        actions = [{'t': 12.5, 'action': 'use',
                    'object': {'id': 5, 'name': 'Door', 'classId': 26, 'nav': [-30.6, -20.5, 0.0]}}]
        chat = [say(9, 'findspot - begin'), say(13.2, 'spot - left'), say(13.6, 'spot - straight'),
                say(16.1, 'spot fixed'), say(17.5, 'findspot - end')]
        goto, route, notes = farmspot.route_specs(positions, actions, chat)
        self.assertEqual(goto, [])
        self.assertIn('no travel markers: write the goto spec by hand', notes)
        self.assertIn('door -30.6 -20.5 0', route)
        cps = [l for l in route if l.startswith('# checkpoint')]
        self.assertEqual(cps, ['# checkpoint 1 LS 0 1', '# checkpoint 2 fixed'])
        # Each checkpoint follows the point where the character was when the marker was sent
        self.assertEqual(route[route.index(cps[0]) - 1], 'pt 576602B8 390.00 -65.00 0.00')
        self.assertEqual(route[route.index(cps[1]) - 1], 'pt 576602B8 390.00 -50.00 0.00')

    def test_read_route_and_build(self):
        spec = '\n'.join([
            'type once', 'pt 576602B8 390.00 -80.00 0.00', 'pt 576602B8 390.00 -70.00 0.00',
            '# checkpoint 1 LR 0 1', 'door -30.6 -20.5 0', 'pt 576602B8 390.00 -60.00 0.00', '# checkpoint 2 fixed'])
        with tempfile.TemporaryDirectory() as here:
            for part, text in (('route', spec), ('goto', 'type once\npt 576602B8 390.00 -80.00 0.00')):
                with open(os.path.join(here, f'aT-{part}.spec'), 'w') as f:
                    f.write(text)
            cps = farmspot.read_route(os.path.join(here, 'aT-route.spec'))
            self.assertEqual([(c['kind'], c['fwd']) for c in cps], [('LR', (0.0, 1.0)), ('fixed', None)])
            self.assertEqual(cps[1]['leg'][0], 'pt 576602B8 390.00 -70.00 0.00')  # starts at checkpoint 1
            d = farmspot.Dungeon(name='aT', landblock=LB, here=here)
            # Walking north, left is west
            left = farmspot.spots_of(d, cps[0])[0]
            self.assertEqual((left['name'], left['x'], left['y']), ('01L', 384.0, -70.0))
            with tempfile.TemporaryDirectory() as navdir:
                rules = farmspot.build_rules(d, navdir)
                out = os.path.join(navdir, 'aT.met')
                vtmeta.save(out, rules)
                states = {r[0] for r in rules}
            for s in ('Default', 'GoTo', 'Walk01', 'Check01', 'Go01L', 'Farm01L', 'Farm01R', 'Walk02', 'Farm02',
                      'Recall', 'Dead'):
                self.assertIn(s, states)

    def test_measured_door_moves_the_spot(self):
        cell = 0x576602B8
        ew, ns, z = farmspot.vtnav.nav_coord(cell, 395.0, -70.2, 0)  # straight ahead (east), 5 m out
        far = farmspot.vtnav.nav_coord(cell, 390.0, -77.0, 0)       # behind the character: not this spot's
        d = farmspot.Dungeon(name='aT', landblock=LB, here='.', doors=[(ew, ns, z), far])
        cp = {'k': 3, 'kind': 'SR', 'fwd': (1.0, 0.0), 'pt': (cell, 390.0, -70.0, 0.0)}
        s, r = farmspot.spots_of(d, cp)
        self.assertEqual(s['door'], f'door {ew!r} {ns!r} {z!r}')
        self.assertAlmostEqual(s['x'], 395.0 + d.side_offset - d.side_door)  # same distance past the door
        self.assertAlmostEqual(r['y'], -76.0)  # no measured door south within side_offset: the estimate
        self.assertIn('-30.', r['door'])

    def test_checkpoint_without_direction_is_an_error(self):
        with tempfile.TemporaryDirectory() as here:
            p = os.path.join(here, 'r.spec')
            with open(p, 'w') as f:
                f.write('pt 576602B8 390.00 -80.00 0.00\n# checkpoint 1 LR\n')
            with self.assertRaises(ValueError):
                farmspot.read_route(p)


if __name__ == '__main__':
    unittest.main()
