using System;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// Entry point the wrapped plugin and the debug writers use.  Lives in the wrapper so it survives /re reload.
/// </summary>
public static class AgentBridgeService
{
    public const int JournalCapacity = 5000;

    private static volatile EventJournal _journal;

    public static AgentHandlerRegistry Handlers { get; } = new();

    public static bool IsEnabled => _journal != null;

    public static void Publish(string kind, string text, int? color = null)
    {
        _journal?.Publish(kind, text, color);
    }

    internal static void SetJournal(EventJournal journal)
    {
        _journal = journal;
    }
}
