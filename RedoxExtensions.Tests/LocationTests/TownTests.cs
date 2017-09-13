using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RedoxExtensions.Location;

namespace RedoxExtensions.Tests.LocationTests
{
    [TestFixture]
    public class TownTests
    {
        [Test]
        public static void ParseShoushi()
        {
            Town result;
            Assert.IsTrue(Town.TryParse("shoushi", out result));
            Assert.That(result.ToString(), Is.EqualTo("Shoushi"));
        }

        [Test]
        public static void ParseAyan()
        {
            Town result;
            Assert.IsTrue(Town.TryParse("ayan", out result));
            Assert.That(result.ToString(), Is.EqualTo("Ayan Baqur"));
        }

        [Test]
        public static void ParseAyanBaqur()
        {
            Town result;
            Assert.IsTrue(Town.TryParse("Ayan Baqur", out result));
            Assert.That(result.ToString(), Is.EqualTo("Ayan Baqur"));
        }

        [Test]
        public static void ParseNonTown()
        {
            Town result;
            Assert.IsFalse(Town.TryParse("Some Location", out result));
        }
    }
}
