using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class VTUtilitiesTests
    {
        [Test]
        public void TestTryParseOptionOutputWhenNotVTOptionOutput()
        {
            KeyValuePair<string, string> keyValue;
            bool result = VTUtilities.TryParseOptionOutput("Hello World", out keyValue);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestTryParseOptionOutputWhen()
        {
            KeyValuePair<string, string> keyValue;
            bool result = VTUtilities.TryParseOptionOutput("[VTank] Option EnableLooting = False", out keyValue);

            Assert.IsTrue(result);

            Assert.AreEqual(VTOptionNames.EnableLooting, keyValue.Key);
            Assert.AreEqual("False", keyValue.Value);
        }

        [Test]
        public void TestHumanRangeToVTRange()
        {
            var result = VTUtilities.HumanRangeToVTRange(10);

            Assert.Greater(result, 0);
            Assert.Less(result, 1);
        }
    }
}
