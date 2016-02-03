using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

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

            Queue<Location> locationResults = new Queue<Location>();
            locationResults.Enqueue(new Location(0, 0, 1, 0, 0));
            locationResults.Enqueue(new Location(0, 0, 2, 0, 0));
            locationResults.Enqueue(new Location(0, 0, 3, 0, 0));
            locationResults.Enqueue(new Location(0, 0, 4, 0, 0));

            for (int i = 0; i < JumpRecorder.NumberOfConsecutiveZCoordsSameToSingleLand; i++)
            {
                locationResults.Enqueue(new Location(0, 0, 5, 0, 0));
            }

            List<SelfJumpCompleteEventArgs> cachedCompleteCallValues = new List<SelfJumpCompleteEventArgs>();

            // Workflow to Test

            JumpRecorder recorder = new JumpRecorder(fakeRtEvents, fakeDecalEvents, e => cachedCompleteCallValues.Add(e), () => locationResults.Dequeue());

            var initialJumpData = new JumpData(new Location(0, 0, 0, 0, 0), 0.0, 0.0);

            Assert.IsFalse(recorder.IsRecording);

            fakeRtEvents.FireSelfJump(new JumpEventArgs(0, initialJumpData, 0, 0));

            Assert.IsTrue(recorder.IsRecording);

            fakeDecalEvents.FireRenderFrame(new EventArgs());
            fakeDecalEvents.FireRenderFrame(new EventArgs());
            fakeDecalEvents.FireRenderFrame(new EventArgs());

            // We should not have landed yet
            Assert.IsTrue(recorder.IsRecording);
            Assert.AreEqual(0, cachedCompleteCallValues.Count);

            fakeDecalEvents.FireRenderFrame(new EventArgs());

            for (int i = 0; i < JumpRecorder.NumberOfConsecutiveZCoordsSameToSingleLand; i++)
            {
                fakeDecalEvents.FireRenderFrame(new EventArgs());
            }

            // Now we should have landed
            Assert.IsFalse(recorder.IsRecording);
            Assert.AreEqual(1, cachedCompleteCallValues.Count);

            Assert.AreEqual(initialJumpData, cachedCompleteCallValues[0].JumpData);

            Assert.AreEqual(4 + JumpRecorder.NumberOfConsecutiveZCoordsSameToSingleLand, cachedCompleteCallValues[0].Trajectory);
            Assert.AreEqual(1, cachedCompleteCallValues[0].Trajectory[0].Z);
            Assert.AreEqual(5, cachedCompleteCallValues[0].Trajectory.Last().Z);

            Assert.AreEqual(5, cachedCompleteCallValues[0].LandingLocation.Z);
        }
    }
}
