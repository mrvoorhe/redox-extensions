using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Legacy;

using RedoxExtensions.Core;
using RedoxExtensions.Data;
using RedoxExtensions.Data.Events;
using RedoxExtensions.Tests.Fakes;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class JumpRecorderTests
    {
        [Test]
        public void SimpleStartToEndScenarioTest()
        {

            // Setup Fakes

            var fakeRtEvents = new FakeRTEvents();
            var fakeDecalEvents = new FakeDecalEventsProxy();

            Queue<Location.Location> locationResults = new Queue<Location.Location>();
            locationResults.Enqueue(new Location.Location(0, 0, 1, 0, 0));
            locationResults.Enqueue(new Location.Location(0, 0, 2, 0, 0));
            locationResults.Enqueue(new Location.Location(0, 0, 3, 0, 0));
            locationResults.Enqueue(new Location.Location(0, 0, 4, 0, 0));

            for (int i = 0; i < JumpRecorder.NumberOfConsecutiveZCoordsSameToSingleLand; i++)
            {
                locationResults.Enqueue(new Location.Location(0, 0, 5, 0, 0));
            }

            List<SelfJumpCompleteEventArgs> cachedCompleteCallValues = new List<SelfJumpCompleteEventArgs>();

            // Workflow to Test

            JumpRecorder recorder = new JumpRecorder(fakeRtEvents, fakeDecalEvents, e => cachedCompleteCallValues.Add(e), () => locationResults.Dequeue());

            var initialJumpData = new JumpData(new Location.Location(0, 0, 0, 0, 0), 0.0, 0.0);

            ClassicAssert.IsFalse(recorder.IsRecording);

            fakeRtEvents.FireSelfJump(new JumpEventArgs(0, initialJumpData, 0, 0));

            ClassicAssert.IsTrue(recorder.IsRecording);

            fakeDecalEvents.FireRenderFrame(new EventArgs());
            fakeDecalEvents.FireRenderFrame(new EventArgs());
            fakeDecalEvents.FireRenderFrame(new EventArgs());

            // We should not have landed yet
            ClassicAssert.IsTrue(recorder.IsRecording);
            ClassicAssert.AreEqual(0, cachedCompleteCallValues.Count);

            fakeDecalEvents.FireRenderFrame(new EventArgs());

            for (int i = 0; i < JumpRecorder.NumberOfConsecutiveZCoordsSameToSingleLand; i++)
            {
                fakeDecalEvents.FireRenderFrame(new EventArgs());
            }

            // Now we should have landed
            ClassicAssert.IsFalse(recorder.IsRecording);
            ClassicAssert.AreEqual(1, cachedCompleteCallValues.Count);

            ClassicAssert.AreEqual(initialJumpData, cachedCompleteCallValues[0].JumpData);

            ClassicAssert.AreEqual(4 + JumpRecorder.NumberOfConsecutiveZCoordsSameToSingleLand, cachedCompleteCallValues[0].Trajectory);
            ClassicAssert.AreEqual(1, cachedCompleteCallValues[0].Trajectory[0].Z);
            ClassicAssert.AreEqual(5, cachedCompleteCallValues[0].Trajectory.Last().Z);

            ClassicAssert.AreEqual(5, cachedCompleteCallValues[0].LandingLocation.Z);
        }
    }
}
