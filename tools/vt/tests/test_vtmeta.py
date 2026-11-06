"""Tests for vtmeta.  Run: py -m unittest discover -s tools/vt/tests"""
import os, sys, tempfile, unittest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import vtnav, vtmeta


class MetaTests(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.mkdtemp()
        self.nav = os.path.join(self.dir, 'Spot.nav')
        vtnav.Route(1).point_at(0x01A30153, 50.21, -64.52, -101.99).save(self.nav)

    def rules(self):
        m = vtmeta
        return [
            ('Default', m.always(), m.chat('/vt settings loadchar Farm')),
            ('Default', m.all_c(m.seconds_in_state(2), m.not_c(m.landcell(0xE5320036))), m.set_state('Go')),
            ('Go', m.always(), m.all_a(m.opt('EnableCombat', False), m.embed_nav(self.nav))),
            ('Go', m.not_c(m.landblock(0x01A30153)), m.set_state('Default')),
        ]

    def test_build_round_trips_and_dumps(self):
        data = vtmeta.build(self.rules())
        self.assertEqual(vtmeta.write_bytes(*vtmeta.read_bytes(data)), data)
        path = os.path.join(self.dir, 'x.met')
        with open(path, 'wb') as f:
            f.write(data)
        dump = vtmeta.dump(path)
        self.assertEqual(dump[0], "[Default] IF Always 0  DO ChatCommand '/vt settings loadchar Farm'")
        self.assertEqual(dump[1], "[Default] IF All {SecondsInStateGE 2; Not {LandCellE -449707978}}  DO SetState 'Go'")
        self.assertIn("SetVTOption {o='EnableCombat', v='False'}", dump[2])
        self.assertIn("<nav 'Spot.nav', 1 nodes", dump[2])
        self.assertEqual(dump[3], "[Go] IF Not {LandBlockE 27459584}  DO SetState 'Default'")

    def test_embedded_nav_is_name_count_then_file(self):
        _, (tag, blob) = vtmeta.embed_nav(self.nav)
        with open(self.nav, 'rb') as f:
            nav = f.read()
        self.assertEqual(tag, 'ba')
        self.assertEqual(blob, b'Spot.nav\r\n1\r\n' + nav)

    def test_file_starts_like_vt_metas(self):
        data = vtmeta.build(self.rules())
        self.assertTrue(data.startswith(b'1\r\nCondAct\r\n5\r\nCType\r\nAType\r\nCData\r\nAData\r\nState\r\n'
                                        b'n\r\nn\r\nn\r\nn\r\nn\r\n4\r\ni\r\n1\r\ni\r\n2\r\ni\r\n0\r\n'))

if __name__ == '__main__':
    unittest.main()
