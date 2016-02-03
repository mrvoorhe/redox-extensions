using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class WrapperUtilitiesTests
    {
        [Test]
        public void TestDllFilePathToPdbFilePath()
        {
            var result = Wrapper.WrapperUtilities.DllFilePathToPdbFilePath(@"C:\some.dll");
            Assert.AreEqual(@"C:\some.pdb", result);
        }
    }
}
