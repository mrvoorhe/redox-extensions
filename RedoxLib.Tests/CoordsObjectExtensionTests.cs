using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Decal.Adapter.Wrappers;
using NUnit.Framework;
using RedoxLib.Objects.Extensions;

namespace RedoxLib.Tests
{
    [TestFixture]
    public class CoordsObjectExtensionTests
    {
        [Test]
        public void TestEqualsWithinMarginOfError_WhenSame()
        {
            var coords = new CoordsObject(1.000000, 2.00000);

            Assert.IsTrue(coords.EqualsWithinMarginOfError(coords));
        }

        [Test]
        public void TestEqualsWithinMarginOfError_WhenWayOff()
        {
            var coords1 = new CoordsObject(1.000000, 1.00000);
            var coords2 = new CoordsObject(2.000000, 2.00000);

            Assert.IsFalse(coords1.EqualsWithinMarginOfError(coords2));
        }

        [Test]
        public void TestEqualsWithinMarginOfError_WhenDifferenceAfter5thSignificantDigit()
        {
            var coords1 = new CoordsObject(42.2217361450195, -51.3048895835876);
            var coords2 = new CoordsObject(coords1.NorthSouth + 0.000001, coords1.EastWest - 0.000001);

            Assert.IsTrue(coords1.EqualsWithinMarginOfError(coords2));
        }
    }
}
