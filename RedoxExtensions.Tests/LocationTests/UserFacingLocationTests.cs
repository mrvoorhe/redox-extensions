using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RedoxExtensions.Location;

namespace RedoxExtensions.Tests.LocationTests;

[TestFixture]
public class UserFacingLocationTests
{
    [Test]
    public void TryParseWithSpaceOnly()
    {
        var ns = new UserFacingCoord(10.5, Direction.N);
        var ew = new UserFacingCoord(90.3, Direction.E);
        Assert.IsTrue(UserFacingLocation.TryParse($"{ns} {ew}", out var result));
        Assert.That(result.NorthSouth, Is.EqualTo(ns));
        Assert.That(result.EastWest, Is.EqualTo(ew));
    }

    [Test]
    public void TryParseWithSpaceAndComma()
    {
        var ns = new UserFacingCoord(10.5, Direction.N);
        var ew = new UserFacingCoord(90.3, Direction.E);
        Assert.IsTrue(UserFacingLocation.TryParse($"{ns}, {ew}", out var result));
        Assert.That(result.NorthSouth, Is.EqualTo(ns));
        Assert.That(result.EastWest, Is.EqualTo(ew));
    }

    [Test]
    public void TryParseWithCommaOnly()
    {
        var ns = new UserFacingCoord(10.5, Direction.N);
        var ew = new UserFacingCoord(90.3, Direction.E);
        Assert.IsTrue(UserFacingLocation.TryParse($"{ns},{ew}", out var result));
        Assert.That(result.NorthSouth, Is.EqualTo(ns));
        Assert.That(result.EastWest, Is.EqualTo(ew));
    }

    [Test]
    public void TryParseWithSpaceAndCommaAndExtraSpaces()
    {
        var ns = new UserFacingCoord(10.5, Direction.N);
        var ew = new UserFacingCoord(90.3, Direction.E);
        Assert.IsTrue(UserFacingLocation.TryParse($"  {ns}   {ew}   ", out var result));
        Assert.That(result.NorthSouth, Is.EqualTo(ns));
        Assert.That(result.EastWest, Is.EqualTo(ew));
    }

    /// <summary>
    /// Special case that is nice to handle since this can happen all the time when
    /// copy pasting from the wiki
    /// </summary>
    [Test]
    public void TryParseWithSpaceAndCommaAndTrailingDot()
    {
        var ns = new UserFacingCoord(10.5, Direction.N);
        var ew = new UserFacingCoord(90.3, Direction.E);
        Assert.IsTrue(UserFacingLocation.TryParse($"{ns}, {ew}.", out var result));
        Assert.That(result.NorthSouth, Is.EqualTo(ns));
        Assert.That(result.EastWest, Is.EqualTo(ew));
    }
}
