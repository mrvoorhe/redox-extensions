using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace RedoxExtensions.Tests;

[TestFixture]
public class WrapperUtilitiesTests
{
    [Test]
    public void TestDllFilePathToPdbFilePath()
    {
        var result = Wrapper.WrapperUtilities.DllFilePathToPdbFilePath(@"C:\some.dll");
        ClassicAssert.AreEqual(@"C:\some.pdb", result);
    }
}
