namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// Chat from public channels that the bridge leaves out of the journal.  Other players' chatter there has nothing to
/// do with what the agent is watching, and can match its wait patterns by accident.
/// </summary>
internal static class ChatChannelFilter
{
    private static readonly string[] IgnoredPrefixes = { "[General]", "[Allegiance]", "[Trade]" };

    public static bool IsIgnored(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var prefix in IgnoredPrefixes)
        {
            if (text.StartsWith(prefix, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
