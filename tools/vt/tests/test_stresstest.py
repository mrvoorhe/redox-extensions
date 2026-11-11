"""Tests for stresstest.  Run: py -m unittest discover -s tools/vt/tests"""
import os, sys, unittest
from datetime import datetime

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import stresstest

CYCLE = [(0, 'S1-arwic'), (10, 'S2-tn'), (40, 'S3-toutou-walk'), (80, 'S4-toutou-loop'), (190, 'TouTou lap done'),
         (190, 'S5-hoshino-gem'), (300, 'S8-hoshino-loop'), (600, 'Hoshino lap done'), (600, 'S9-lifestone')]


def feed(trk, events, offset=0):
    out = []
    for t, msg in events:
        for _ in range(4):  # /vt echo shows up about four times
            out += trk.feed(f'[VTank] Stress: {msg}', t + offset)
    return out


class StepTrackerTest(unittest.TestCase):
    def test_repeated_echo_counts_once(self):
        trk = stresstest.StepTracker()
        out = feed(trk, [(0, 'Default'), (3, 'S9-lifestone')])
        self.assertEqual([k for k, _ in out], ['step', 'step'])
        self.assertEqual(trk.state, 'S9-lifestone')

    def test_cycle_runs_arwic_to_arwic(self):
        trk = stresstest.StepTracker()
        out = feed(trk, CYCLE) + feed(trk, [(610, 'S1-arwic')])
        self.assertEqual(len(trk.cycles), 1)
        c = trk.cycles[0]
        self.assertEqual(c['seconds'], 610)
        self.assertEqual(c['steps']['S4-toutou-loop'], 110)
        self.assertEqual(c['steps']['S8-hoshino-loop'], 300)
        self.assertIn(('cycle', 'CYCLE 1 done in 610 s'), out)
        self.assertEqual(trk.current['n'], 2)

    def test_notes_are_not_steps(self):
        trk = stresstest.StepTracker()
        out = feed(trk, [(0, 'S1-arwic'), (5, 'GIVE UP S2-tn')])
        self.assertIn(('note', 'GIVE UP S2-tn'), out)
        self.assertEqual(trk.state, 'S1-arwic')
        self.assertEqual(trk.current['notes'], ['GIVE UP S2-tn'])

    def test_other_lines_ignored(self):
        trk = stresstest.StepTracker()
        self.assertEqual(trk.feed('[VTank] Meta executing action: Chat Command: /vt echo Stress: S2-tn', 0), [])
        self.assertIsNone(trk.state)

    def test_summary_table(self):
        trk = stresstest.StepTracker()
        feed(trk, CYCLE, 1e9)
        feed(trk, [(610, 'S1-arwic')], 1e9)
        text = stresstest.summary('Cowhead', datetime(2026, 11, 11, 10, 0), trk, [], '1 cycles done')
        self.assertIn('| 1 |', text)
        self.assertIn('| 610 s | 110 s | 300 s | 0 |', text)
        self.assertIn('| 2 |', text)  # the cycle in progress
        self.assertIn('None (no crash', text)


if __name__ == '__main__':
    unittest.main()
