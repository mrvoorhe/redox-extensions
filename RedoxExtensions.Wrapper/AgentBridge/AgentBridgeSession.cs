using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Decal.Adapter;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// The bridge while it's turned on in game.  Created by '/re agent on', disposed by '/re agent off' or logoff.
/// </summary>
internal class AgentBridgeSession : IAgentBridgeHost, IDisposable
{
    private static readonly TimeSpan GameThreadTimeout = TimeSpan.FromSeconds(10);

    private readonly REWrapperPlugin _plugin;
    private readonly AgentPolicy _policy = new();
    private readonly EventJournal _journal = new(AgentBridgeService.JournalCapacity);
    private readonly GameThreadQueue _gameThreadQueue = new(GameThreadTimeout);
    private readonly AgentBridgeServer _bridgeServer;
    private readonly string _token = Guid.NewGuid().ToString("N");
    private readonly string _discoveryDirectory = AgentDiscovery.DefaultDirectory;
    private readonly int _pid = Process.GetCurrentProcess().Id;
    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private readonly string _character;
    private readonly string _server;

    public AgentBridgeSession(REWrapperPlugin plugin, AgentMode mode)
    {
        this._plugin = plugin;
        this._policy.Mode = mode;
        this._character = plugin.CoreManager.CharacterFilter.Name;
        this._server = plugin.CoreManager.CharacterFilter.Server;

        var processor = new AgentRequestProcessor(this, this._policy, this._journal, AgentBridgeService.Handlers, this._token);
        this._bridgeServer = new AgentBridgeServer(processor.Process, e => this._plugin.Error.LogError(e));
        this._bridgeServer.Start();

        try
        {
            this.WriteDiscovery();
        }
        catch (Exception)
        {
            this._bridgeServer.Dispose();
            throw;
        }

        this._plugin.CoreManager.RenderFrame += this.CoreManager_RenderFrame;
        this._plugin.CoreManager.ChatBoxMessage += this.CoreManager_ChatBoxMessage;

        AgentBridgeService.SetJournal(this._journal);
    }

    public AgentMode Mode
    {
        get => this._policy.Mode;
        set
        {
            this._policy.Mode = value;
            this.WriteDiscovery();
        }
    }

    public int Port => this._bridgeServer.Port;

    public int ConnectionCount => this._bridgeServer.ConnectionCount;

    public void Dispose()
    {
        AgentBridgeService.SetJournal(null);

        this._plugin.CoreManager.RenderFrame -= this.CoreManager_RenderFrame;
        this._plugin.CoreManager.ChatBoxMessage -= this.CoreManager_ChatBoxMessage;

        this._policy.Mode = AgentMode.Off;
        this._bridgeServer.Dispose();
        this._gameThreadQueue.FailAll("Agent bridge turned off");

        try
        {
            AgentDiscovery.Delete(this._discoveryDirectory, this._pid);
        }
        catch (Exception e)
        {
            this._plugin.Error.LogError(e);
        }
    }

    #region IAgentBridgeHost

    public object InvokeOnGameThread(Func<object> func) => this._gameThreadQueue.Invoke(func);

    public void Exec(string commandText)
    {
        if (!DispatchOnChatCommandWithPluginIntercept(commandText))
            this._plugin.PluginHost.Actions.InvokeChatParser(commandText);
    }

    public void Reload() => this._plugin.ReloadWrappedPlugin();

    public Dictionary<string, object> Describe()
    {
        return new Dictionary<string, object>
        {
            ["pid"] = this._pid,
            ["character"] = this._character,
            ["server"] = this._server,
            ["pluginLoaded"] = this._plugin.IsWrappedPluginLoaded,
            ["startedUtc"] = this._startedUtc,
        };
    }

    #endregion

    private void WriteDiscovery()
    {
        AgentDiscovery.Write(
            this._discoveryDirectory,
            this._pid,
            this._character,
            this._server,
            this._bridgeServer.Port,
            this._token,
            this._policy.Mode,
            this._startedUtc);
    }

    private void CoreManager_RenderFrame(object sender, EventArgs e)
    {
        this._plugin.InvokeOperationSafely(this._gameThreadQueue.Drain);
    }

    private void CoreManager_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
    {
        this._plugin.InvokeOperationSafely(() => this._journal.Publish(EventJournal.KindChat, e.Text?.TrimEnd('\n', '\r'), e.Color));
    }

    #region Decal Chat Dispatch (same approach as RedoxExtensions.Core.DecalProxy, copied from MagTools)

    [DllImport("Decal.dll")]
    private static extern int DispatchOnChatCommand(ref IntPtr str, [MarshalAs(UnmanagedType.U4)] int target);

    private static bool DispatchOnChatCommandWithPluginIntercept(string cmd)
    {
        var bstr = Marshal.StringToBSTR(cmd);
        try
        {
            return (DispatchOnChatCommand(ref bstr, 1) & 0x1) > 0;
        }
        finally
        {
            Marshal.FreeBSTR(bstr);
        }
    }

    #endregion
}
