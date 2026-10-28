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
}
