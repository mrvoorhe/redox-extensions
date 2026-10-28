using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Legacy;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests;

[TestFixture]
public class VTUtilitiesTests
{
    [Test]
    public void TestTryParseOptionOutputWhenNotVTOptionOutput()
    {
        bool result = VTUtilities.TryParseOptionOutput("Hello World", out var keyValue);

        ClassicAssert.IsFalse(result);
    }

    [Test]
    public void TestTryParseOptionOutputWhen()
    {
        bool result = VTUtilities.TryParseOptionOutput("[VTank] Option EnableLooting = False", out var keyValue);

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
