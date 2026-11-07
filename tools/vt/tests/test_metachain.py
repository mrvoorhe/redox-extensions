import datetime as dt, os, re, sys, unittest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))
import metachain  # noqa: E402


def chat(t, text, kind='chat'):
    return {'kind': kind, 'time': t.astimezone(dt.timezone.utc).isoformat().replace('+00:00', 'Z'), 'text': text}


class StartPlanTests(unittest.TestCase):
    def test_staggered(self):
        first = dt.datetime(2026, 11, 8, 7, 0)
        plan = metachain.plan_starts(['A', 'B', 'C'], first, 15)
        self.assertEqual([t.strftime('%H:%M') for _, t in plan], ['07:00', '07:15', '07:30'])
        self.assertEqual([c for c, _ in plan], ['A', 'B', 'C'])

    def test_clock_time_is_next_occurrence(self):
        t = metachain.parse_when('00:00')
        self.assertTrue(dt.timedelta(0) < t - dt.datetime.now() + dt.timedelta(minutes=1) <= dt.timedelta(days=1))
        self.assertEqual(metachain.parse_when('2026-11-08T06:30'), dt.datetime(2026, 11, 8, 6, 30))


class AnalyzeTests(unittest.TestCase):
    def test_tasks_grouped_and_timeout(self):
        info = {'aKTX': {'limit': 600, 'hunt': True, 'flags': set(),
                         'triggers': [(re.compile('Clay Golem', re.I), 'XGolem')]}}
        t0 = dt.datetime(2026, 11, 7, 7, 0).astimezone()
        s = lambda sec: t0 + dt.timedelta(seconds=sec)
        recs = [
            chat(s(0), '[VTank] Meta executing action: ChatCommand /vt meta load aKTX', 'plugin'),
            chat(s(5), "[VTank] Meta executing action: Expression {e='setvar[XHuntSW,stopwatchstart[stopwatchcreate[]]]'}", 'plugin'),
            chat(s(60), 'You have killed 1 Clay Golems! You must kill 3 to complete your task.'),
            chat(s(70), 'You have killed 2 Bronze Golems! You must kill 3 to complete your task.'),  # same counter
            chat(s(80), 'You have killed 3 Bronze Golems! Your task is complete!'),
            chat(s(90), 'You have killed 4 Sand Cows! You must kill 25 to complete your task.'),
            chat(s(605), 'X: 10 minute hunt limit reached, turning in', 'plugin'),
        ]
        run = metachain.analyze(recs, info=info)['runs'][0]
        self.assertEqual(run['limit'], 600)
        self.assertIsNotNone(run['timeout'])
        self.assertEqual(set(run['tasks']), {'XGolem', 'Sand Cow'})
        self.assertIsNotNone(run['tasks']['XGolem']['done'])
        self.assertEqual(run['tasks']['XGolem']['mobs'], {'Clay Golems', 'Bronze Golems'})
        self.assertIsNone(run['tasks']['Sand Cow']['done'])
        self.assertIn('Tasks with kills but not complete at end of hunt: 1', metachain.report(
            {'runs': [run], 'metas': [], 'incidents': [], 'positions': [], 'warns': []}))


if __name__ == '__main__':
    unittest.main()
