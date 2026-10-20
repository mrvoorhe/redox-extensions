using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Legacy;

using RedoxExtensions.Dispatching;
using RedoxExtensions.Dispatching.Legacy;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class GameThreadDispatcherTests
    {
        [Test]
        public void VerifyQueueActionWillQueueAndRunOnceRenderFrameFired()
        {
            var fakeDecalEventsProxy = new Fakes.FakeDecalEventsProxy();

            bool actionCalled = false;

            using (var dispatcher = new GameThreadDispatcher(fakeDecalEventsProxy))
            {
                dispatcher.QueueAction(() =>
                {
                    actionCalled = true;
                });

                ClassicAssert.AreEqual(1, dispatcher.QueueCount);

                // Fire the render event to simulate what decal would do
                fakeDecalEventsProxy.FireRenderFrame(new EventArgs());

                ClassicAssert.IsTrue(actionCalled);
                ClassicAssert.AreEqual(0, dispatcher.QueueCount);
            }
        }
    }
}
