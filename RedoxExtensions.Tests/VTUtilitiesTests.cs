using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Legacy;

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

            ClassicAssert.IsFalse(result);
        }

        [Test]
        public void TestTryParseOptionOutputWhen()
        {
            KeyValuePair<string, string> keyValue;
            bool result = VTUtilities.TryParseOptionOutput("[VTank] Option EnableLooting = False", out keyValue);

            ClassicAssert.IsTrue(result);

            ClassicAssert.AreEqual(VTOptionNames.EnableLooting, keyValue.Key);
            ClassicAssert.AreEqual("False", keyValue.Value);
        }

        [Test]
        public void TestHumanRangeToVTRange()
        {
            var result = VTUtilities.HumanRangeToVTRange(10);

            ClassicAssert.Greater(result, 0);
            ClassicAssert.Less(result, 1);
        }
    }
}
