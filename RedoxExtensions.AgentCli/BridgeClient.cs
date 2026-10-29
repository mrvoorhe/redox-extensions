using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RedoxExtensions.AgentCli;

public class BridgeClient : IDisposable
{
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly string _token;
    private int _nextId = 1;

    public BridgeClient(int port, string token, int receiveTimeoutMs)
    {
        this._token = token;
        this._client = new TcpClient("127.0.0.1", port) { ReceiveTimeout = receiveTimeoutMs };
        var stream = this._client.GetStream();
        this._reader = new StreamReader(stream, new UTF8Encoding(false));
        this._writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n", AutoFlush = true };
    }

    public JsonObject Send(string cmd, IDictionary<string, JsonNode> fields = null)
    {
        var request = new JsonObject
        {
            ["token"] = this._token,
            ["cmd"] = cmd,
            ["id"] = this._nextId++,
        };
        if (fields != null)
        {
            foreach (var pair in fields)
                request[pair.Key] = pair.Value;
        }

        return this.SendRaw(request);
    }

    public JsonObject SendRaw(JsonObject request)
    {
        request["token"] = this._token;
        this._writer.WriteLine(request.ToJsonString());

        var line = this._reader.ReadLine() ?? throw new IOException("Connection closed by the client");
        return JsonNode.Parse(line) as JsonObject ?? throw new IOException("Response was not a JSON object: " + line);
    }

    public void Dispose()
    {
        this._client.Dispose();
    }
}
