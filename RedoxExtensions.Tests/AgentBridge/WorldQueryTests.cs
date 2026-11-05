using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using RedoxExtensions.AgentBridge;
using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class WorldQueryTests
{
    private static readonly WorldItem[] Landscape =
    {
        new() { Id = 10, Name = "Drudge Sneaker", ObjectClass = "Monster", Distance = 40 },
        new() { Id = 11, Name = "Drudge Ravener", ObjectClass = "Monster", Distance = 12.34 },
        new() { Id = 12, Name = "Town Network Portal", ObjectClass = "Portal", Distance = 3 },
        new() { Id = 13, Name = "Pack", ObjectClass = "Container", Distance = null },
    };

    private static readonly WorldItem[] Inventory =
    {
        new() { Id = 3, Name = "pyreal", ObjectClass = "Money", Container = 100, StackCount = 25000 },
        new() { Id = 1, Name = "Amulet", ObjectClass = "Jewelry", Container = 200 },
        new() { Id = 2, Name = "Bow", ObjectClass = "MissileWeapon", Wielder = 100 },
    };

    private static WorldQuery Parse(string json) => WorldQuery.Parse((IDictionary<string, object>)MiniJson.Parse(json));

    private static List<Dictionary<string, object>> Objects(Dictionary<string, object> result) =>
        ((List<object>)result["objects"]).Cast<Dictionary<string, object>>().ToList();

    [Test]
    public void DefaultsToLandscapeSortedByDistanceWithUnknownDistanceLast()
    {
        var result = Parse("{}").Apply(Landscape);

        Assert.That(result["scope"], Is.EqualTo("landscape"));
        Assert.That(result["total"], Is.EqualTo(4));
        Assert.That(Objects(result).Select(o => o["id"]), Is.EqualTo(new object[] { 12, 11, 10, 13 }));
        Assert.That(result.ContainsKey("nextOffset"), Is.False);
    }

    [Test]
    public void InventorySortsByNameIgnoringCase()
    {
        var result = Parse("{\"scope\":\"Inventory\"}").Apply(Inventory);

        Assert.That(Objects(result).Select(o => o["name"]), Is.EqualTo(new[] { "Amulet", "Bow", "pyreal" }));
    }

    [Test]
    public void FiltersByNameRegexClassAndDistance()
    {
        Assert.That(Objects(Parse("{\"name\":\"^drudge\"}").Apply(Landscape)).Count, Is.EqualTo(2));
        Assert.That(Objects(Parse("{\"class\":\"monster, portal\"}").Apply(Landscape)).Count, Is.EqualTo(3));
        Assert.That(Objects(Parse("{\"class\":[\"Portal\"]}").Apply(Landscape)).Select(o => o["id"]), Is.EqualTo(new object[] { 12 }));

        // Objects with no distance never match a distance limit.
        Assert.That(Objects(Parse("{\"maxDistance\":20}").Apply(Landscape)).Select(o => o["id"]), Is.EqualTo(new object[] { 12, 11 }));
        Assert.That(Objects(Parse("{\"maxDistance\":\"12.5\"}").Apply(Landscape)).Select(o => o["id"]), Is.EqualTo(new object[] { 12, 11 }));
    }

    [Test]
    public void FiltersByContainer()
    {
        var result = Parse("{\"scope\":\"inventory\",\"container\":100}").Apply(Inventory);

        Assert.That(Objects(result).Select(o => o["id"]), Is.EqualTo(new object[] { 3 }));
    }

    [Test]
    public void PagesWithOffsetAndLimitAndReportsNextOffset()
    {
        var first = Parse("{\"limit\":2}").Apply(Landscape);
        var second = Parse("{\"limit\":2,\"offset\":2}").Apply(Landscape);

        Assert.That(Objects(first).Select(o => o["id"]), Is.EqualTo(new object[] { 12, 11 }));
        Assert.That(first["nextOffset"], Is.EqualTo(2));
        Assert.That(Objects(second).Select(o => o["id"]), Is.EqualTo(new object[] { 10, 13 }));
        Assert.That(second.ContainsKey("nextOffset"), Is.False);
        Assert.That(second["total"], Is.EqualTo(4));
    }

    [Test]
    public void ClampsLimit()
    {
        Assert.That(Parse("{\"limit\":100000}").Limit, Is.EqualTo(WorldQuery.MaxLimit));
        Assert.That(Parse("{\"limit\":0}").Limit, Is.EqualTo(1));
        Assert.That(Parse("{\"offset\":-5}").Offset, Is.EqualTo(0));
    }

    [Test]
    public void ObjectJsonOmitsEmptyFieldsAndRoundsNumbers()
    {
        var json = new WorldItem { Id = 5, Name = "Bow", ObjectClass = "MissileWeapon", StackCount = 1, Distance = 12.3456, NorthSouth = 1.23456, EastWest = -2.5 }.ToJson();

        Assert.That(json.Keys, Is.EquivalentTo(new[] { "id", "name", "class", "distance", "ns", "ew" }));
        Assert.That(json["distance"], Is.EqualTo(12.3));
        Assert.That(json["ns"], Is.EqualTo(1.23));
    }

    [Test]
    public void RejectsBadInput()
    {
        Assert.That(() => Parse("{\"scope\":\"everything\"}"), Throws.ArgumentException.With.Message.Contains("Unknown scope"));
        Assert.That(() => Parse("{\"name\":\"(\"}"), Throws.ArgumentException.With.Message.Contains("Bad name regex"));
        Assert.That(() => Parse("{\"maxDistance\":\"far\"}"), Throws.ArgumentException.With.Message.Contains("Bad maxDistance"));
    }
}
