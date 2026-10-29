using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RedoxExtensions.Wrapper.AgentBridge;

public delegate object AgentRequestHandler(IDictionary<string, object> request);

public interface IAgentBridgeHost
{
    object InvokeOnGameThread(Func<object> func);

    /// <summary>
    /// Called on the game thread
    /// </summary>
    void Exec(string commandText);

    /// <summary>
    /// Called on the game thread
    /// </summary>
    void Reload();

    Dictionary<string, object> Describe();
}

/// <summary>
/// Turns one request line into one response line.  Runs on a connection thread.
/// </summary>
public class AgentRequestProcessor
{
    public const int DefaultEventsMax = 500;
    public const int MaxEventsMax = 2000;
    public const int DefaultWaitTimeoutMs = 10000;
    public const int MaxWaitTimeoutMs = 120000;

    private readonly IAgentBridgeHost _host;
    private readonly AgentPolicy _policy;
    private readonly EventJournal _journal;
    private readonly AgentHandlerRegistry _handlers;
    private readonly string _token;

    public AgentRequestProcessor(IAgentBridgeHost host, AgentPolicy policy, EventJournal journal, AgentHandlerRegistry handlers, string token)
    {
        this._host = host;
        this._policy = policy;
        this._journal = journal;
        this._handlers = handlers;
        this._token = token;
    }

    public string Process(string line)
    {
        object id = null;
        try
        {
            if (MiniJson.Parse(line) is not Dictionary<string, object> request)
                return Error(null, "Request must be a JSON object");

            request.TryGetValue("id", out id);

            if (MiniJson.GetString(request, "token") != this._token)
                return Error(id, "Bad token");

            var cmd = MiniJson.GetString(request, "cmd");
            if (string.IsNullOrEmpty(cmd))
                return Error(id, "Missing cmd");

            if (!this._policy.CanRead(out var error))
                return Error(id, error);

            return cmd switch
            {
                "ping" => Ok(id, this.Ping()),
                "events" => Ok(id, this.Events(request)),
                "wait" => Ok(id, this.Wait(request)),
                "exec" => this.Exec(id, request),
                "reload" => this.Reload(id),
                _ => this.Dispatch(id, cmd, request)
            };
        }
        catch (FormatException e)
        {
            return Error(id, "Bad JSON: " + e.Message);
        }
        catch (Exception e)
        {
            return Error(id, e.GetType().Name + ": " + e.Message);
        }
    }

    private Dictionary<string, object> Ping()
    {
        var result = this._host.Describe();
        result["mode"] = AgentPolicy.ModeName(this._policy.Mode);
        result["seq"] = this._journal.NextSeq;
        result["handlers"] = this._handlers.Names();
        return result;
    }

    private Dictionary<string, object> Events(IDictionary<string, object> request)
    {
        var since = MiniJson.GetLong(request, "since") ?? 0;
        var max = (int)Math.Min(Math.Max(MiniJson.GetLong(request, "max") ?? DefaultEventsMax, 1), MaxEventsMax);
        var events = this._journal.GetSince(since, MiniJson.GetStringList(request, "kinds"), max);

        var list = new List<object>(events.Count);
        foreach (var evt in events)
            list.Add(evt.ToJson());

        var oldestRetained = Math.Max(1, this._journal.NextSeq - this._journal.Capacity);
        return new Dictionary<string, object>
        {
            ["events"] = list,
            ["next"] = events.Count > 0 ? events[events.Count - 1].Seq + 1 : Math.Max(since, this._journal.NextSeq),
            ["truncated"] = since > 0 && since < oldestRetained,
        };
    }

    private Dictionary<string, object> Wait(IDictionary<string, object> request)
    {
        var since = MiniJson.GetLong(request, "since") ?? this._journal.NextSeq;
        var timeoutMs = Math.Min(Math.Max(MiniJson.GetLong(request, "timeoutMs") ?? DefaultWaitTimeoutMs, 0), MaxWaitTimeoutMs);
        var regexText = MiniJson.GetString(request, "regex");
        var pattern = string.IsNullOrEmpty(regexText) ? null : new Regex(regexText, RegexOptions.IgnoreCase);

        var evt = this._journal.WaitFor(since, MiniJson.GetStringList(request, "kinds"), pattern, TimeSpan.FromMilliseconds(timeoutMs));

        var result = new Dictionary<string, object> { ["matched"] = evt != null };
        if (evt != null)
            result["event"] = evt.ToJson();
        return result;
    }

    private string Exec(object id, IDictionary<string, object> request)
    {
        var text = MiniJson.GetString(request, "text");
        if (!this._policy.CanExec(text, out var error))
            return Error(id, error);

        var seq = this._journal.NextSeq;
        this._journal.Publish(EventJournal.KindCommand, text);
        this._host.InvokeOnGameThread(() =>
        {
            this._host.Exec(text);
            return null;
        });
        return Ok(id, new Dictionary<string, object> { ["seq"] = seq });
    }

    private string Reload(object id)
    {
        if (!this._policy.CanReload(out var error))
            return Error(id, error);

        var seq = this._journal.NextSeq;
        this._host.InvokeOnGameThread(() =>
        {
            this._host.Reload();
            return null;
        });
        return Ok(id, new Dictionary<string, object> { ["seq"] = seq });
    }

    private string Dispatch(object id, string cmd, IDictionary<string, object> request)
    {
        if (!this._handlers.TryGet(cmd, out var handler))
            return Error(id, $"Unknown cmd '{cmd}'.  Available: ping, events, wait, exec, reload, {string.Join(", ", this._handlers.Names().ToArray())} (plugin handlers are missing while the plugin is unloaded)");

        return Ok(id, this._host.InvokeOnGameThread(() => handler(request)));
    }

    private static string Ok(object id, object result)
    {
        var response = new Dictionary<string, object> { ["ok"] = true };
        if (id != null)
            response["id"] = id;
        response["result"] = result;
        return MiniJson.Serialize(response);
    }

    private static string Error(object id, string error)
    {
        var response = new Dictionary<string, object> { ["ok"] = false };
        if (id != null)
            response["id"] = id;
        response["error"] = error;
        return MiniJson.Serialize(response);
    }
}
