using System;
using System.Collections.Generic;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// Request handlers the wrapped plugin registers on load and removes on unload.  Handlers run on the game thread.
/// </summary>
public class AgentHandlerRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, AgentRequestHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string cmd, AgentRequestHandler handler)
    {
        lock (this._lock)
            this._handlers[cmd] = handler;
    }

    public void Unregister(string cmd)
    {
        lock (this._lock)
            this._handlers.Remove(cmd);
    }

    public bool TryGet(string cmd, out AgentRequestHandler handler)
    {
        lock (this._lock)
            return this._handlers.TryGetValue(cmd, out handler);
    }

    public List<string> Names()
    {
        lock (this._lock)
        {
            var names = new List<string>(this._handlers.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }
    }
}
