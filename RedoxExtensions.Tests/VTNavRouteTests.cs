using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests;

[TestFixture]
public class VTNavRouteTests
{
    [Test]
    public void TestSerializeEmptyRoute()
    {
        var route = new VTNavRoute(VTNavRouteType.Circular);

        Assert.AreEqual("uTank2 NAV 1.2\r\n1\r\n0\r\n", route.Serialize());
    }

    [Test]
    public void TestSerializeRouteTypes()
    {
        Assert.IsTrue(new VTNavRoute(VTNavRouteType.Linear).Serialize().StartsWith("uTank2 NAV 1.2\r\n2\r\n"));
        Assert.IsTrue(new VTNavRoute(VTNavRouteType.Once).Serialize().StartsWith("uTank2 NAV 1.2\r\n4\r\n"));
    }

    [Test]
    public void TestSerializePoints()
    {
        var route = new VTNavRoute(VTNavRouteType.Once);
        route.AddPoint(new VTNavCoord(33.5, 42.25, 0.125));
        route.AddPoint(new VTNavCoord(-10.75, -3.5, 2));

        var expected =
            "uTank2 NAV 1.2\r\n4\r\n2\r\n" +
            "0\r\n33.5\r\n42.25\r\n0.125\r\n0\r\n" +
            "0\r\n-10.75\r\n-3.5\r\n2\r\n0\r\n";

        Assert.AreEqual(2, route.Count);
        Assert.AreEqual(expected, route.Serialize());
    }

    [Test]
    public void TestSerializePortalNpc()
    {
        var route = new VTNavRoute(VTNavRouteType.Once);
        route.AddPortalNpc(new VTNavCoord(33.5, 42.25, 0.125), "Town Crier", 37, new VTNavCoord(33.75, 42.5, 0.25));

        var expected =
            "uTank2 NAV 1.2\r\n4\r\n1\r\n" +
            "6\r\n33.5\r\n42.25\r\n0.125\r\n0\r\n" +
            "Town Crier\r\n37\r\nTrue\r\n33.75\r\n42.5\r\n0.25\r\n";

        Assert.AreEqual(expected, route.Serialize());
    }

    [Test]
    public void TestSerializeOpenVendor()
    {
        var route = new VTNavRoute(VTNavRouteType.Once);
        route.AddOpenVendor(1234, "Shopkeeper Renald");

        var expected =
            "uTank2 NAV 1.2\r\n4\r\n1\r\n" +
            "5\r\n0\r\n0\r\n0\r\n0\r\n" +
            "1234\r\nShopkeeper Renald\r\n";

        Assert.AreEqual(expected, route.Serialize());
    }

    [Test]
    public void TestSerializeChatCommandAndPause()
    {
        var route = new VTNavRoute(VTNavRouteType.Once);
        route.AddChatCommand("/ub usei Town Network Sending Gem");
        route.AddPause(5000);

        var expected =
            "uTank2 NAV 1.2\r\n4\r\n2\r\n" +
            "4\r\n0\r\n0\r\n0\r\n0\r\n/ub usei Town Network Sending Gem\r\n" +
            "3\r\n0\r\n0\r\n0\r\n0\r\n5000\r\n";

        Assert.AreEqual(expected, route.Serialize());
    }

    [Test]
    public void TestFromLandcell()
    {
        var coord = VTNavCoord.FromLandcell(unchecked((int)0xA9B40019), 84, 84, 240);

        Assert.AreEqual(33.6, coord.EastWest, 0.0001);
        Assert.AreEqual(42.4, coord.NorthSouth, 0.0001);
        Assert.AreEqual(1.0, coord.Z, 0.0001);
    }

    [Test]
    public void TestFromLandcellSouthWest()
    {
        var coord = VTNavCoord.FromLandcell(0x7F7F0001, 0, 0, 0);

        Assert.AreEqual(-0.35, coord.EastWest, 0.0001);
        Assert.AreEqual(-0.35, coord.NorthSouth, 0.0001);
    }

    [Test]
    public void TestNormalizeName()
    {
        Assert.AreEqual("myroute", VTNavRoute.NormalizeName("myroute"));
        Assert.AreEqual("myroute", VTNavRoute.NormalizeName(" myroute.nav "));
        Assert.AreEqual("myroute", VTNavRoute.NormalizeName("myroute.NAV"));
    }
}
