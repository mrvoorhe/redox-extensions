using System;
using System.Globalization;
using System.Text.RegularExpressions;

// Free of plugin dependencies so rectl can link it later.
namespace RedoxExtensions.Recording;

public static class DurationParser
{
    private static readonly Regex Pattern = new Regex(@"^(?<n>[0-9]+(\.[0-9]+)?|\.[0-9]+)\s*(?<u>[smh])\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool TryParse(string text, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        if (text == null)
            return false;

        var match = Pattern.Match(text.Trim());
        if (!match.Success)
            return false;

        double number;
        if (!double.TryParse(match.Groups["n"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number))
            return false;

        double unitTicks;
        switch (char.ToLowerInvariant(match.Groups["u"].Value[0]))
        {
            case 's': unitTicks = TimeSpan.TicksPerSecond; break;
            case 'm': unitTicks = TimeSpan.TicksPerMinute; break;
            default: unitTicks = TimeSpan.TicksPerHour; break;
        }

        var ticks = Math.Round(number * unitTicks);
        if (double.IsNaN(ticks) || double.IsInfinity(ticks) || ticks <= 0 || ticks >= long.MaxValue)
            return false;

        value = TimeSpan.FromTicks((long)ticks);
        return true;
    }
}
