using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Decal.Adapter;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.AgentBridge;

/// <summary>The fields of a decoded message (or one of its nested structs).</summary>
internal interface INetFields
{
    int Count { get; }
    string Name(int index);

    /// <summary>A scalar, or another <see cref="INetFields"/> for a nested struct.</summary>
    object Value(int index);
}

/// <summary>
/// Journals decoded network messages as "net" events, for the message types an agent asked for with the 'net'
/// request.  Captures nothing until then.  The selection is plugin state, so it resets on /re reload.
/// </summary>
internal static class NetCapture
{
    public const string Kind = "net";

    /// <summary>Longer messages are cut, so one big message can't crowd out the journal.</summary>
    public const int MaxTextLength = 400;

    private const int MaxDepth = 2;

    private static readonly object Lock = new();
    private static HashSet<int> _server = new();
    private static HashSet<int> _client = new();
    private static bool _allServer;
    private static bool _allClient;

    public static bool IsCapturing(bool fromServer, int type)
    {
        lock (Lock)
        {
            return fromServer
                ? _allServer || _server.Contains(type)
                : _allClient || _client.Contains(type);
        }
    }

    /// <summary>
    /// 'net' request.  "server" / "client": message types to capture (hex, with or without 0x, as a list or
    /// comma-separated, or "all"), replacing the current selection for that direction.  "off": stop capturing both.
    /// Returns the selection.
    /// </summary>
    public static object Configure(IDictionary<string, object> request)
    {
        lock (Lock)
        {
            if (MiniJson.GetString(request, "off") is { } off && off.ToLowerInvariant() is "true" or "1")
            {
                _server = new HashSet<int>();
                _client = new HashSet<int>();
                _allServer = _allClient = false;
            }

            // Parse both before changing anything, so a bad type leaves the selection as it was.
            HashSet<int> server = null, client = null;
            bool allServer = false, allClient = false;
            if (MiniJson.GetStringList(request, "server") is { } serverValues)
                server = ParseTypes(serverValues, out allServer);
            if (MiniJson.GetStringList(request, "client") is { } clientValues)
                client = ParseTypes(clientValues, out allClient);

            if (server != null)
            {
                _server = server;
                _allServer = allServer;
            }

            if (client != null)
            {
                _client = client;
                _allClient = allClient;
            }

            return Describe();
        }
    }

    internal static HashSet<int> ParseTypes(IEnumerable<string> values, out bool all)
    {
        var types = new HashSet<int>();
        all = false;

        foreach (var raw in values.SelectMany(v => v.Split(',')).Select(v => v.Trim()).Where(v => v.Length > 0))
        {
            if (raw.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                all = true;
                continue;
            }

            var hex = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? raw.Substring(2) : raw;
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var type))
                throw new ArgumentException($"Bad message type '{raw}'.  Use hex, e.g. F7B0.");
            types.Add(type);
        }

        return types;
    }

    private static Dictionary<string, object> Describe()
    {
        static object Selection(HashSet<int> types, bool all) =>
            all ? "all" : types.OrderBy(t => t).Select(t => (object)TypeName(t)).ToList();

        return new Dictionary<string, object>
        {
            ["server"] = Selection(_server, _allServer),
            ["client"] = Selection(_client, _allClient),
        };
    }

    public static void OnServerDispatch(NetworkMessageEventArgs e) => Publish(true, e);

    public static void OnClientDispatch(NetworkMessageEventArgs e) => Publish(false, e);

    private static void Publish(bool fromServer, NetworkMessageEventArgs e)
    {
        if (!AgentBridgeService.IsEnabled)
            return;

        var message = e.Message;
        var type = message.Type;
        if (!IsCapturing(fromServer, type))
            return;

        AgentBridgeService.Publish(Kind, Format(fromServer, type, new MessageFields(message)));
    }

    /// <summary>"S F7B0 character=1 sequence=2 event=0x01C7", nested structs in braces, cut at MaxTextLength.</summary>
    internal static string Format(bool fromServer, int type, INetFields fields)
    {
        var text = new StringBuilder(128);
        text.Append(fromServer ? "S " : "C ").Append(TypeName(type));
        AppendFields(text, fields, 0);

        if (text.Length > MaxTextLength)
        {
            text.Length = MaxTextLength - 3;
            text.Append("...");
        }

        return text.ToString();
    }

    internal static string TypeName(int type) => type.ToString("X4", CultureInfo.InvariantCulture);

    private static void AppendFields(StringBuilder text, INetFields fields, int depth)
    {
        for (var i = 0; i < fields.Count && text.Length <= MaxTextLength; i++)
        {
            object value;
            try
            {
                value = fields.Value(i);
            }
            catch (Exception)
            {
                // Some fields can't be read back as a value.  Skip them rather than drop the message.
                continue;
            }

            text.Append(' ').Append(fields.Name(i)).Append('=');

            if (value is INetFields nested)
            {
                if (depth >= MaxDepth)
                {
                    text.Append("{...}");
                    continue;
                }

                text.Append('{');
                AppendFields(text, nested, depth + 1);
                text.Append(" }");
            }
            else
            {
                text.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }
    }

    private sealed class MessageFields : INetFields
    {
        private readonly Message _message;

        public MessageFields(Message message) => this._message = message;

        public int Count => this._message.Count;
        public string Name(int index) => this._message.Name(index);
        public object Value(int index) => Wrap(this._message[index]);
    }

    private sealed class StructFields : INetFields
    {
        private readonly MessageStruct _struct;

        public StructFields(MessageStruct messageStruct) => this._struct = messageStruct;

        public int Count => this._struct.Count;
        public string Name(int index) => this._struct.Name(index);
        public object Value(int index) => Wrap(this._struct[index]);
    }

    private static object Wrap(object value) => value is MessageStruct nested ? new StructFields(nested) : value;
}
