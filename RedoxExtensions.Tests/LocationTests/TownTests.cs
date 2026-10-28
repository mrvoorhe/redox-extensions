using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using RedoxExtensions.Location;

namespace RedoxExtensions.Tests.LocationTests;

[TestFixture]
public class TownTests
{
    [Test]
    public static void ParseShoushi()
    {
        ClassicAssert.IsTrue(Town.TryParse("shoushi", out var result));
        Assert.That(result.ToString(), Is.EqualTo("Shoushi"));
    }

    [Test]
    public static void ParseAyan()
    {
        ClassicAssert.IsTrue(Town.TryParse("ayan", out var result));
        Assert.That(result.ToString(), Is.EqualTo("Ayan Baqur"));
    }

    [Test]
    public static void ParseAyanBaqur()
    {
        ClassicAssert.IsTrue(Town.TryParse("Ayan Baqur", out var result));
        Assert.That(result.ToString(), Is.EqualTo("Ayan Baqur"));
    }

    [Test]
    public static void ParseNonTown()
    {
        ClassicAssert.IsFalse(Town.TryParse("Some Location", out var result));
    }
}
