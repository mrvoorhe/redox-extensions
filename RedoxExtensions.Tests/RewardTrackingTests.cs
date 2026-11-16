using System;
using System.Collections.Generic;

using NUnit.Framework;

using RedoxExtensions.Core.Utilities;
using RedoxExtensions.Listeners.Monitors;

namespace RedoxExtensions.Tests;

[TestFixture]
public class RewardTrackingTests
{
    [TestCase("Strawberry Jam Golem gives you Strawberry Jam Jar.", "Strawberry Jam Golem", 1, "Strawberry Jam Jar")]
    [TestCase("Royal Guard gives you 8 Enlightened Coins.", "Royal Guard", 8, "Enlightened Coins")]
    [TestCase("Gregoria, Gurog Destroyer gives you Aged Legendary Key.", "Gregoria, Gurog Destroyer", 1, "Aged Legendary Key")]
    [TestCase("Hunter gives you 20 Trade Notes (250,000).", "Hunter", 20, "Trade Notes (250,000)")]
    [TestCase("Royal Guard gives you Trade Note (250,000).", "Royal Guard", 1, "Trade Note (250,000)")]
    [TestCase("Someone gives you 1,200 Weakly Enlightened Coins.", "Someone", 1200, "Weakly Enlightened Coins")]
    public void TryParseGivesYou_ParsesRewardLines(string text, string giver, int count, string item)
    {
        var result = ChatParsingUtilities.TryParseGivesYou(text, out var actualGiver, out var actualCount, out var actualItem);

        Assert.That(result, Is.True);
        Assert.That(actualGiver, Is.EqualTo(giver));
        Assert.That(actualCount, Is.EqualTo(count));
        Assert.That(actualItem, Is.EqualTo(item));
    }

    [TestCase("Cowhead says, \"Hunter gives you 100 Enlightened Coins.\"")]
    [TestCase("Cowhead tells you, Hunter gives you 100 Enlightened Coins.")]
    [TestCase("[Fellowship] Cowhead says to your fellowship, Hunter gives you 100 Enlightened Coins.")]
    [TestCase("You give Hunter 10 Enlightened Coins.")]
    [TestCase("")]
    [TestCase(null)]
    public void TryParseGivesYou_RejectsOtherLines(string text)
    {
        Assert.That(ChatParsingUtilities.TryParseGivesYou(text, out _, out _, out _), Is.False);
    }

    private static IEnumerable<TestCaseData> CatalogCases()
    {
        yield return new TestCaseData("Enlightened Coin", RewardCatalog.EnlightenedCoin);
        yield return new TestCaseData("Enlightened Coins", RewardCatalog.EnlightenedCoin);
        yield return new TestCaseData("Weakly Enlightened Coins", RewardCatalog.WeaklyEnlightenedCoin);
        yield return new TestCaseData("Strawberry Jam Jar", RewardCatalog.StrawberryJamJar);
        yield return new TestCaseData("Expired Trade Note", RewardCatalog.ExpiredTradeNote);
        yield return new TestCaseData("Mythical Key", RewardCatalog.MythicalKey);
        yield return new TestCaseData("Aged Legendary Key", RewardCatalog.LegendaryKey);
        yield return new TestCaseData("Legendary Key", RewardCatalog.LegendaryKey);
        yield return new TestCaseData("Durable Legendary Key", RewardCatalog.DurableLegendaryKey);
        yield return new TestCaseData("Trade Note (250,000)", RewardCatalog.Mmd);
        yield return new TestCaseData("Trade Notes (250,000)", RewardCatalog.Mmd);
    }

    [TestCaseSource(nameof(CatalogCases))]
    public void RewardCatalog_MatchesTrackedItems(string itemName, RewardKind expected)
    {
        Assert.That(RewardCatalog.TryMatch(itemName, out var kind), Is.True);
        Assert.That(kind, Is.SameAs(expected));
    }

    [TestCase("Contract for Kill: Rynthid Sorcerers")]
    [TestCase("Trade Note (100,000)")]
    [TestCase("Shattered Legendary Key")]
    [TestCase("A'nekshay Token")]
    public void RewardCatalog_IgnoresOtherItems(string itemName)
    {
        Assert.That(RewardCatalog.TryMatch(itemName, out _), Is.False);
    }

    [Test]
    public void RewardTally_ValuesKeysInCoins()
    {
        var tally = new RewardTally();
        tally.Add(RewardCatalog.LegendaryKey, 20);
        tally.Add(RewardCatalog.DurableLegendaryKey, 1);

        Assert.That(tally.TotalCoins, Is.EqualTo(1.5).Within(1e-9));
    }

    [Test]
    public void RewardTally_WritesCountsRatesAndValueTable()
    {
        var tally = new RewardTally();
        tally.Add(RewardCatalog.StrawberryJamJar, 30);
        tally.Add(RewardCatalog.Mmd, 24);
        tally.Add(RewardCatalog.EnlightenedCoin, 8);

        var lines = new List<string>();
        tally.WriteReport(TimeSpan.FromHours(2), lines.Add);

        Assert.That(lines, Is.EqualTo(new[]
        {
            "[Rewards] Received",
            "Enlightened Coin: 8 (4/hr)",
            "Strawberry Jam Jar: 30 (15/hr)",
            "MMD: 24 (12/hr)",
            "[Rewards] Value in Enlightened Coins",
            "Enlightened Coin: 8.0 (4.0/hr)",
            "Strawberry Jam Jar: 3.0 (1.5/hr)",
            "MMD: 2.0 (1.0/hr)",
            "Total: 13.0 (6.5/hr)",
        }));
    }

    [Test]
    public void RewardTally_ResetClearsCounts()
    {
        var tally = new RewardTally();
        tally.Add(RewardCatalog.MythicalKey, 5);
        tally.Reset();

        var lines = new List<string>();
        tally.WriteReport(TimeSpan.FromHours(1), lines.Add);

        Assert.That(lines, Is.EqualTo(new[] { "[Rewards] None received." }));
    }
}
