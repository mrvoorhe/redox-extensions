using NUnit.Framework;

using RedoxExtensions.Recording;

namespace RedoxExtensions.Tests.Recording;

[TestFixture]
public class AfterStopArgsTests
{
    [Test]
    public void ExpandsAllPlaceholdersIncludingPathsWithSpaces()
    {
        var args = "analyze \"{file}\" --md \"{dir}\\{name}.analysis.md\" --who {char} --day {date}";
        var result = AfterStopArgs.Expand(args, @"E:\Meta Analysis\2026-11-21\Cobble-2.jsonl", @"E:\Meta Analysis\2026-11-21", "Cobble-2", "Cobble", "2026-11-21");

        Assert.That(result, Is.EqualTo("analyze \"E:\\Meta Analysis\\2026-11-21\\Cobble-2.jsonl\" --md \"E:\\Meta Analysis\\2026-11-21\\Cobble-2.analysis.md\" --who Cobble --day 2026-11-21"));
    }

    [Test]
    public void ExpandsRepeatedPlaceholdersAndLeavesUnknownOnes()
    {
        Assert.That(AfterStopArgs.Expand("{name} {name} {other}", "f", "d", "n", "c", "t"), Is.EqualTo("n n {other}"));
    }

    [Test]
    public void ValuesAreNotExpandedAgain()
    {
        Assert.That(AfterStopArgs.Expand("{char}", "f", "d", "n", "{date}", "2026-11-21"), Is.EqualTo("{date}"));
    }

    [TestCase(null)]
    [TestCase("")]
    public void NoArgsGivesEmpty(string args)
    {
        Assert.That(AfterStopArgs.Expand(args, "f", "d", "n", "c", "t"), Is.EqualTo(string.Empty));
    }
}
