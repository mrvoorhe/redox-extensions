using NUnit.Framework;

using RedoxExtensions.VirindiInterop;

namespace RedoxExtensions.Tests;

[TestFixture]
public class VTActionsTests
{
    [TestCase("Foo", "Foo")]
    [TestCase("Foo.met", "Foo")]
    [TestCase("Foo.MET", "Foo")]
    [TestCase(" Foo ", "Foo")]
    [TestCase(" Foo.met ", "Foo")]
    public void TestNormalizeMetaName(string input, string expected)
    {
        Assert.That(VTActions.NormalizeMetaName(input), Is.EqualTo(expected));
    }

    private static readonly string[] AvailableMetaNames = { "aKTHoshino", "aKTHoshinoOld", "zFarmGraveyard", "Graveyard" };

    [TestCase("aKTHoshino")]
    [TestCase("akthoshino")]
    [TestCase("aKTHoshino.met")]
    public void TestMatchMetaNamesPrefersExactMatch(string input)
    {
        Assert.That(VTActions.MatchMetaNames(AvailableMetaNames, input), Is.EqualTo(new[] { "aKTHoshino" }));
    }

    [TestCase("Old", "aKTHoshinoOld")]
    [TestCase("farm", "zFarmGraveyard")]
    public void TestMatchMetaNamesSinglePartialMatch(string input, string expected)
    {
        Assert.That(VTActions.MatchMetaNames(AvailableMetaNames, input), Is.EqualTo(new[] { expected }));
    }

    [Test]
    public void TestMatchMetaNamesMultiplePartialMatches()
    {
        Assert.That(VTActions.MatchMetaNames(AvailableMetaNames, "Hosh"), Is.EqualTo(new[] { "aKTHoshino", "aKTHoshinoOld" }));
    }

    [Test]
    public void TestMatchMetaNamesExactMatchWinsOverPartialMatches()
    {
        Assert.That(VTActions.MatchMetaNames(AvailableMetaNames, "Graveyard"), Is.EqualTo(new[] { "Graveyard" }));
    }

    [Test]
    public void TestMatchMetaNamesNoMatch()
    {
        Assert.That(VTActions.MatchMetaNames(AvailableMetaNames, "Nope"), Is.Empty);
    }
}
