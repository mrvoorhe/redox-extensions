using NUnit.Framework;
using RedoxExtensions.Dispatching;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using RedoxExtensions.Diagnostics;
using RedoxExtensions.Tests.Fakes;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class PipelineDispatcherTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Debug.SetTemporaryWriter(new NullWriteToChat());
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            Debug.RestoreDefaultWriter();
        }

        [Test]
        public void VerifyQueueActionWillQueueAndRunOnceRenderFrameFired()
        {
            var fakeDecalEventsProxy = new Fakes.FakeDecalEventsProxy();
            using (var fakePipelineAction = new Fakes.FakePipelineAction())
            {
                using (var dispatcher = new PipelineDispatcher(fakeDecalEventsProxy))
                {
                    dispatcher.EnqueueAction(fakePipelineAction);

                    Assert.AreEqual(1, dispatcher.QueueCount);
                    Assert.IsFalse(dispatcher.HasPendingAction);

                    // Nothing should be called as a result of queueing
                    Assert.AreEqual(0, fakePipelineAction.InitCallCount);
                    Assert.AreEqual(0, fakePipelineAction.BeginInvokeCallCount);
                    Assert.AreEqual(0, fakePipelineAction.EndInvokeCallCount);
                    Assert.IsFalse(fakePipelineAction.IsComplete);

                    // Fire the render event
                    fakeDecalEventsProxy.FireRenderFrame(new EventArgs());

                    // Step 1 is to Init.  Verify that happened.
                    Assert.IsTrue(dispatcher.HasPendingAction);

                    Assert.AreEqual(1, fakePipelineAction.InitCallCount);
                    Assert.AreEqual(0, fakePipelineAction.BeginInvokeCallCount);
                    Assert.AreEqual(0, fakePipelineAction.EndInvokeCallCount);
                    Assert.IsFalse(fakePipelineAction.IsComplete);

                    // Fire the render event
                    fakeDecalEventsProxy.FireRenderFrame(new EventArgs());

                    // Step 2 is to BeginInvoke & Wait in the Background
                    Assert.IsTrue(dispatcher.HasPendingAction);

                    Assert.AreEqual(1, fakePipelineAction.InitCallCount);
                    Assert.AreEqual(1, fakePipelineAction.BeginInvokeCallCount);
                    Assert.AreEqual(0, fakePipelineAction.EndInvokeCallCount);
                    Assert.IsFalse(fakePipelineAction.IsComplete);

                    // Now tell the background wait to release.
                    fakePipelineAction.ForTesting_SetComplete(false);

                    // The action should report complete, but the dispatcher shouldn't have processed it yet
                    // since we haven't fired render frame yet.
                    Assert.AreEqual(1, fakePipelineAction.InitCallCount);
                    Assert.AreEqual(1, fakePipelineAction.BeginInvokeCallCount);
                    Assert.AreEqual(0, fakePipelineAction.EndInvokeCallCount);
                    Assert.IsTrue(fakePipelineAction.IsComplete);

                    // Fire the render event
                    fakeDecalEventsProxy.FireRenderFrame(new EventArgs());
                    Assert.IsFalse(dispatcher.HasPendingAction);

                    // Now the action should have been fully processed
                    Assert.AreEqual(1, fakePipelineAction.InitCallCount);
                    Assert.AreEqual(1, fakePipelineAction.BeginInvokeCallCount);
                    Assert.AreEqual(1, fakePipelineAction.EndInvokeCallCount);
                    Assert.IsTrue(fakePipelineAction.IsComplete);

                    Assert.AreEqual(0, dispatcher.QueueCount);

                    // Check the thread id's that everything was invoked on to verify they were correct.
                    // Doing this at the end to simplying timing of caching this info
                    Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.InitThreadId);
                    Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.BeginInvokeThreadId);
                    Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.EndInvokeThreadId);

                    Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.WaitForCompleteThreadId);
                }
            }
        }

        [Test]
        public void VerifyQueueActionCanRetry()
        {
            var fakeDecalEventsProxy = new Fakes.FakeDecalEventsProxy();
            using (var fakePipelineAction = new Fakes.FakePipelineAction())
            {
                using (var dispatcher = new PipelineDispatcher(fakeDecalEventsProxy))
                {
                    dispatcher.EnqueueAction(fakePipelineAction);

                    // Fire the render event - Gets us init'ed
                    fakeDecalEventsProxy.FireRenderFrame(new EventArgs());

                    // Fire the render event again - bets us begin invoked
                    fakeDecalEventsProxy.FireRenderFrame(new EventArgs());

                    // Now tell the background wait to release and request a retry
                    fakePipelineAction.ForTesting_SetComplete(true);

                    // Now we are into the situation this unit test is focused on.  Start making assertions.
                    Assert.IsTrue(dispatcher.HasPendingAction);

                    // Fire the render event - This should trip the Retry check and Reset
                    fakeDecalEventsProxy.FireRenderFrame(new EventArgs());

                    // Init should not be called again, so make sure that count is still 1.
                    Assert.AreEqual(1, fakePipelineAction.InitCallCount);
                    // Begin invoke shouldn't have been called again yet.  So the count should still be 1.
                    Assert.AreEqual(1, fakePipelineAction.BeginInvokeCallCount);
                    // We are retrying, not completing, so no EndInvoke.
                    Assert.AreEqual(0, fakePipelineAction.EndInvokeCallCount);
                    
                    // Now, what should have changed due to the lastest firing.
                    Assert.AreEqual(1, fakePipelineAction.RetryCallCount);
                    Assert.AreEqual(1, fakePipelineAction.ResetForRetryCallCount);

                    Assert.IsTrue(dispatcher.HasPendingAction);

                    // Fire the render event - Now begin invoke will be called again.
                    fakeDecalEventsProxy.FireRenderFrame(new EventArgs());

                    // Init should never be called again, so make sure that count is still 1.
                    Assert.AreEqual(1, fakePipelineAction.InitCallCount);
                    // Begin invoke will have been called again
                    Assert.AreEqual(2, fakePipelineAction.BeginInvokeCallCount);
                    // We are retrying, not completing, so no EndInvoke.
                    Assert.AreEqual(0, fakePipelineAction.EndInvokeCallCount);

                    // These are unchanged
                    Assert.AreEqual(1, fakePipelineAction.RetryCallCount);
                    Assert.AreEqual(1, fakePipelineAction.ResetForRetryCallCount);

                    // Now complete the action for real
                    fakePipelineAction.ForTesting_SetComplete(false);

                    // Fire the render event - This should cause everything to complete.
                    fakeDecalEventsProxy.FireRenderFrame(new EventArgs());


                    // Now the action should have been fully processed
                    Assert.AreEqual(1, fakePipelineAction.InitCallCount);
                    Assert.AreEqual(2, fakePipelineAction.BeginInvokeCallCount);
                    Assert.AreEqual(1, fakePipelineAction.EndInvokeCallCount);
                    Assert.IsTrue(fakePipelineAction.IsComplete);
                    Assert.AreEqual(2, fakePipelineAction.RetryCallCount);
                    Assert.AreEqual(1, fakePipelineAction.ResetForRetryCallCount);

                    Assert.AreEqual(0, dispatcher.QueueCount);

                    // Check the thread id's that everything was invoked on to verify they were correct.
                    // Doing this at the end to simplying timing of caching this info
                    Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.InitThreadId);
                    Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.BeginInvokeThreadId);
                    Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.EndInvokeThreadId);
                    Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.ResetForRetryThreadId);

                    Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, fakePipelineAction.WaitForCompleteThreadId);
                }
            }
        }
    }
}
