using System;
using System.Collections.Generic;
using System.Globalization;

namespace RedoxExtensions.Listeners.Monitors;

/// <summary>
/// Counts rewards received and writes the /re report tables: the counts with their hourly rate, then their value in
/// Enlightened Coins.
/// </summary>
public class RewardTally
{
    private readonly Dictionary<RewardKind, long> _counts = new Dictionary<RewardKind, long>();

    public void Reset() => this._counts.Clear();

    public void Add(RewardKind kind, long count)
    {
        if (kind == null || count <= 0)
        {
            return;
        }

        this._counts.TryGetValue(kind, out var current);
        this._counts[kind] = current + count;
    }

    public long GetCount(RewardKind kind)
    {
        this._counts.TryGetValue(kind, out var count);
        return count;
    }

    public double TotalCoins
    {
        get
        {
            double total = 0;
            foreach (var kvp in this._counts)
            {
                total += kvp.Key.ToCoins(kvp.Value);
            }

            return total;
        }
    }

    public void WriteReport(TimeSpan elapsed, Action<string> writer)
    {
        if (writer == null)
        {
            return;
        }

        var received = new List<RewardKind>();
        foreach (var kind in RewardCatalog.All)
        {
            if (GetCount(kind) > 0)
            {
                received.Add(kind);
            }
        }

        if (received.Count == 0)
        {
            writer("[Rewards] None received.");
            return;
        }

        double hours = elapsed.TotalHours;

        writer("[Rewards] Received");
        foreach (var kind in received)
        {
            long count = GetCount(kind);
            writer(string.Format(CultureInfo.InvariantCulture, "{0}: {1:N0} ({2:N0}/hr)", kind.Name, count, PerHour(count, hours)));
        }

        writer("[Rewards] Value in Enlightened Coins");
        foreach (var kind in received)
        {
            double coins = kind.ToCoins(GetCount(kind));
            writer(string.Format(CultureInfo.InvariantCulture, "{0}: {1:N1} ({2:N1}/hr)", kind.Name, coins, PerHour(coins, hours)));
        }

        double total = TotalCoins;
        writer(string.Format(CultureInfo.InvariantCulture, "Total: {0:N1} ({1:N1}/hr)", total, PerHour(total, hours)));
    }

    private static double PerHour(double amount, double hours) => hours > 0 ? amount / hours : 0;
}
