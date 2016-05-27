using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RedoxLib.Objects;

namespace RedoxLib.Tests
{
    [TestFixture]
    public class LocationTests
    {
        [Test]
        public void TestHeadingMatches_WhenVeryClose()
        {
            double h1 = 271.719787457302;
            double h2 = 271.719791617596;
            Assert.IsTrue(Location.HeadingMatches(h1, h2));
        }

        [Test]
        public void TestHeadingMatches_WhenVeryClose2()
        {
            double h1 = 242.14591887813;
            double h2 = 242.145908179008;
            Assert.IsTrue(Location.HeadingMatches(h1, h2));
        }

        [Test]
        public void TestHeadingMatches_WhenVeryClose3()
        {
            double h1 = 252.634903121656;
            double h2 = 252.634907781011;
            Assert.IsTrue(Location.HeadingMatches(h1, h2));
        }

        [Test]
        public void TestHeadingMatches_WhenVeryCloseRollOver()
        {
            double h1 = 271.999999999999;
            double h2 = 272.000000000000;
            Assert.IsTrue(Location.HeadingMatches(h1, h2));
        }
    }
}
