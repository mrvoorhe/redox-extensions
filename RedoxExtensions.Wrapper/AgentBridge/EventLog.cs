using System;
using System.Diagnostics;

using Decal.Adapter;

using RedoxExtensions.Wrapper.Diagnostics;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// The event journal and what feeds it (chat, plugin and debug output).  Runs from login to logoff,
/// independent of the agent bridge.
/// </summary>
internal class EventLog : IDisposable
{
    private readonly REWrapperPlugin _plugin;

    public EventLog(REWrapperPlugin plugin)
    {
        this._plugin = plugin;
        this.Journal = new EventJournal(
            AgentBridgeService.JournalCapacity,
            $"{Process.GetCurrentProcess().Id}-{DateTime.Now:yyyy-MM-ddTHH:mm:ss}");

        this._plugin.CoreManager.ChatBoxMessage += this.CoreManager_ChatBoxMessage;
        AgentBridgeService.SetJournal(this.Journal);
        this.StartChatTextCapture();
    }

    public EventJournal Journal { get; }

    public void Dispose()
    {
        ChatTextCapture.Listener = null;
        AgentBridgeService.SetJournal(null);
        this._plugin.CoreManager.ChatBoxMessage -= this.CoreManager_ChatBoxMessage;
    }

    private void StartChatTextCapture()
    {
        try
        {
            ChatTextCapture.EnsureInstalled(this._plugin.PluginHost.Actions);
        }
        catch (Exception e)
        {
            // The log still works, it just only sees our own chat output (see REWrapperPlugin.WriteToChat).
            this._plugin.Error.LogError(e);
            this.Journal.Publish(EventJournal.KindDebug, $"Chat text capture unavailable: {e.Message}");
            return;
        }

        var journal = this.Journal;
        ChatTextCapture.Listener = (text, color, target) =>
            journal.Publish(target == WrapperDebugWriter.DEBUG_CHAT ? EventJournal.KindDebug : EventJournal.KindPlugin, text, color);
    }

    private void CoreManager_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
    {
        this._plugin.InvokeOperationSafely(() =>
        {
            if (!ChatChannelFilter.IsIgnored(e.Text))
                this.Journal.Publish(EventJournal.KindChat, e.Text?.TrimEnd('\n', '\r'), e.Color);
        });
    }
}
