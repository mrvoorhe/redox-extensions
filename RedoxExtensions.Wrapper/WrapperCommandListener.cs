using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Wrapper;

/// <summary>
/// Class responsible for listening for, and handling of, global commands.
/// 
/// Ex:  /rt do something
/// </summary>
internal class WrapperCommandListener : IDisposable
{
    private const string CommandPrefix = "/re";
    private const string CommandPrefix_Legacy = "/rew";

    internal WrapperCommandListener()
    {
        REWrapperPlugin.Instance.CoreManager.CommandLineText += DecalCore_CommandLineText;
    }

    public void Dispose()
    {
        REWrapperPlugin.Instance.CoreManager.CommandLineText -= DecalCore_CommandLineText;
    }

    #region Private Methods


    private bool HandleNormalCommand(string command, bool explicitWrapperCommand)
    {
        REWrapperPlugin.Instance.Debug.WriteLine($"Processing Command : {command}");

        var lowered = command.ToLower();
        if (lowered == "agent" || lowered.StartsWith("agent "))
        {
            HandleAgentCommand(lowered.Substring("agent".Length).Trim());
            return true;
        }

        switch (lowered)
        {
            case "unload":
                REWrapperPlugin.Instance.UnloadWrappedPlugin();
                return true;
            case "load":
                REWrapperPlugin.Instance.LoadWrappedPlugin();
                return true;
            case "reload":
                REWrapperPlugin.Instance.ReloadWrappedPlugin();
                return true;
            default:
                if (explicitWrapperCommand)
                {
                    REWrapperPlugin.Instance.Chat.WriteLine("Unknown Command : {0}", command);
                    return true;
                }

                return false;
        }
    }

    private static void HandleAgentCommand(string arg)
    {
        var plugin = REWrapperPlugin.Instance;

        if (arg.Length == 0 || arg == "status")
        {
            if (plugin.AgentMode == AgentMode.Off)
                plugin.Chat.WriteLine("Agent bridge is off.  Usage: /re agent on|rw|full|off");
            else
                plugin.Chat.WriteLine("Agent bridge is {0} on port {1} ({2} connected)", AgentPolicy.ModeName(plugin.AgentMode), plugin.AgentPort, plugin.AgentConnectionCount);
            return;
        }

        if (!AgentPolicy.TryParseMode(arg, out var mode))
        {
            plugin.Chat.WriteLine("Unknown agent mode '{0}'.  Usage: /re agent on|rw|full|off", arg);
            return;
        }

        plugin.SetAgentMode(mode);

        if (mode == AgentMode.Off)
            plugin.Chat.WriteLine("Agent bridge off");
        else
            plugin.Chat.WriteLine("Agent bridge {0} on port {1}", AgentPolicy.ModeName(mode), plugin.AgentPort);
    }

    #endregion

    #region Event Handlers

    private void DecalCore_CommandLineText(object sender, Decal.Adapter.ChatParserInterceptEventArgs e)
    {
        REWrapperPlugin.Instance.InvokeOperationSafely(() =>
        {
            if (e.Text.StartsWith(CommandPrefix_Legacy))
            {
                var command = e.Text.Substring(CommandPrefix_Legacy.Length + 1).Trim();

                e.Eat = this.HandleNormalCommand(command, true);
            }
            else if (e.Text.StartsWith(CommandPrefix))
            {
                var command = e.Text.Substring(CommandPrefix.Length + 1).Trim();

                e.Eat = this.HandleNormalCommand(command, false);
            }
        });
    }

    #endregion
}
