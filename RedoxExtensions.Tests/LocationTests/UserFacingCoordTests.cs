using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RedoxExtensions.Location;

namespace RedoxExtensions.Tests.LocationTests
{
    [TestFixture]
    public class UserFacingCoordTests
    {
        [Test]
        public void ParseTwoDigit()
        {
            Assert.IsTrue(UserFacingCoord.TryParse("1.5N", out var result));
            Assert.That(result.Value, Is.EqualTo(1.5));
            Assert.That(result.Direction, Is.EqualTo(Direction.N));
            Assert.That(result.ToString(), Is.EqualTo("1.5N"));
        }

        [Test]
        public void ParseThreeDigit()
        {
            Assert.IsTrue(UserFacingCoord.TryParse("10.5N", out var result));
            Assert.That(result.Value, Is.EqualTo(10.5));
            Assert.That(result.Direction, Is.EqualTo(Direction.N));
            Assert.That(result.ToString(), Is.EqualTo("10.5N"));
        }

        /// <summary>
        /// When copy-pasting from the wiki it's common to have a trailing '.' at the end
        /// for easy of use, let's handle this
        /// </summary>
        [Test]
        public void ParseWithTrailingDotAndWhitespace()
        {
            Assert.IsTrue(UserFacingCoord.TryParse("1.5N.  ", out var result));
            Assert.That(result.Value, Is.EqualTo(1.5));
            Assert.That(result.Direction, Is.EqualTo(Direction.N));
            Assert.That(result.ToString(), Is.EqualTo("1.5N"));
        }

        /// <summary>
        /// When copy-pasting from the wiki it's common to have a trailing ',' at the end
        /// for easy of use, let's handle this
        /// </summary>
        [Test]
        public void ParseWithTrailingComma()
        {
            Assert.IsTrue(UserFacingCoord.TryParse("1.5N,", out var result));
            Assert.That(result.Value, Is.EqualTo(1.5));
            Assert.That(result.Direction, Is.EqualTo(Direction.N));
            Assert.That(result.ToString(), Is.EqualTo("1.5N"));
        }
    }
}
