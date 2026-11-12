using System.Text.RegularExpressions;

namespace RedoxExtensions.MetaChainWatch;

/// <summary>
/// Spots VT meta loads in command line text.
/// </summary>
internal static class MetaLoadCommand
{
    private static readonly Regex Pattern = new(@"^\s*/vt\s+meta\s+load\s+(?<name>\S.*?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// True when <paramref name="text"/> is <c>/vt meta load &lt;name&gt;</c> (any case, any spacing).
    /// <paramref name="metaName"/> is the rest of the line, normalized with <see cref="MetaChainWatchRules.NormalizeName"/>.
    /// </summary>
    public static bool TryParse(string text, out string metaName)
    {
        metaName = null;
        if (string.IsNullOrEmpty(text))
            return false;

        var match = Pattern.Match(text);
        if (!match.Success)
            return false;

        var name = MetaChainWatchRules.NormalizeName(match.Groups["name"].Value);
        if (name.Length == 0)
            return false;

        metaName = name;
        return true;
    }
}
