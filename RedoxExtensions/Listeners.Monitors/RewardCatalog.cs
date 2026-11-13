using System;
using System.Collections.Generic;

namespace RedoxExtensions.Listeners.Monitors;

/// <summary>
/// One kind of reward that /re track counts, and its value in Enlightened Coins.
/// </summary>
public class RewardKind
{
    public RewardKind(string name, double itemsPerCoin, params string[] itemNames)
    {
        this.Name = name;
        this.ItemsPerCoin = itemsPerCoin;
        this.ItemNames = itemNames;
    }

    /// <summary>The name shown in the report.</summary>
    public string Name { get; }

    /// <summary>How many of this item are worth one Enlightened Coin.</summary>
    public double ItemsPerCoin { get; }

    /// <summary>The item names as they appear in "X gives you ..." lines, singular and plural.</summary>
    public string[] ItemNames { get; }

    public double ToCoins(long count) => count / this.ItemsPerCoin;
}

/// <summary>
/// The rewards /re track counts from "X gives you ..." chat lines.  See docs/design/Feature-RewardTracking.md.
/// </summary>
public static class RewardCatalog
{
    public static readonly RewardKind EnlightenedCoin =
        new RewardKind("Enlightened Coin", 1, "Enlightened Coin", "Enlightened Coins");

    public static readonly RewardKind WeaklyEnlightenedCoin =
        new RewardKind("Weakly Enlightened Coin", 10, "Weakly Enlightened Coin", "Weakly Enlightened Coins");

    public static readonly RewardKind StrawberryJamJar =
        new RewardKind("Strawberry Jam Jar", 10, "Strawberry Jam Jar", "Strawberry Jam Jars");

    public static readonly RewardKind ExpiredTradeNote =
        new RewardKind("Expired Trade Note", 1, "Expired Trade Note", "Expired Trade Notes");

    public static readonly RewardKind MythicalKey =
        new RewardKind("Mythical Key", 10, "Mythical Key", "Mythical Keys");

    // Aged Legendary Keys are what kill tasks hand out.  The plain "Legendary Key" is rare and counted the same.
    public static readonly RewardKind LegendaryKey =
        new RewardKind("Legendary Key", 20, "Aged Legendary Key", "Aged Legendary Keys", "Legendary Key", "Legendary Keys");

    // A Durable Legendary Key is worth 10 Aged Legendary Keys.
    public static readonly RewardKind DurableLegendaryKey =
        new RewardKind("Durable Legendary Key", 2, "Durable Legendary Key", "Durable Legendary Keys");

    // MMD = the 250,000 pyreal trade note.
    public static readonly RewardKind Mmd =
        new RewardKind("MMD", 12, "Trade Note (250,000)", "Trade Notes (250,000)");

    /// <summary>All kinds, in report order.</summary>
    public static readonly IList<RewardKind> All = new List<RewardKind>
    {
        EnlightenedCoin,
        WeaklyEnlightenedCoin,
        StrawberryJamJar,
        ExpiredTradeNote,
        MythicalKey,
        LegendaryKey,
        DurableLegendaryKey,
        Mmd,
    }.AsReadOnly();

    private static readonly Dictionary<string, RewardKind> ByItemName = BuildLookup();

    public static bool TryMatch(string itemName, out RewardKind kind)
    {
        kind = null;
        return itemName != null && ByItemName.TryGetValue(itemName.Trim(), out kind);
    }

    private static Dictionary<string, RewardKind> BuildLookup()
    {
        var lookup = new Dictionary<string, RewardKind>(StringComparer.OrdinalIgnoreCase);
        foreach (var kind in All)
        {
            foreach (var name in kind.ItemNames)
            {
                lookup[name] = kind;
            }
        }

        return lookup;
    }
}
