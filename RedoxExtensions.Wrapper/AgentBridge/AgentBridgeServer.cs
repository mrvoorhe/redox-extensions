using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// Loopback-only TCP server speaking newline-delimited JSON.  One thread per connection, capped, with small
/// stacks to keep the 32-bit client's address space cost low.
/// </summary>
public class AgentBridgeServer : IDisposable
{
    public const int MaxLineLength = 64 * 1024;
    private const int ThreadStackSize = 256 * 1024;
    private const int IdleTimeoutMs = 5 * 60 * 1000;

    private readonly Func<string, string> _processLine;
    private readonly Action<Exception> _logError;
    private readonly int _maxConnections;
    private readonly object _lock = new();
    private readonly List<TcpClient> _clients = new();

    private TcpListener _listener;
    private Thread _acceptThread;
    private volatile bool _stopping;

    public AgentBridgeServer(Func<string, string> processLine, Action<Exception> logError, int maxConnections = 2)
    {
        this._processLine = processLine;
        this._logError = logError ?? (_ => { });
        this._maxConnections = maxConnections;
    }

    public int Port { get; private set; }

    public int ConnectionCount
    {
        get
        {
            lock (this._lock)
                return this._clients.Count;
        }
    }

    public void Start()
    {
        this._listener = new TcpListener(IPAddress.Loopback, 0);
        this._listener.Start();
        this.Port = ((IPEndPoint)this._listener.LocalEndpoint).Port;

        this._acceptThread = new Thread(this.AcceptLoop, ThreadStackSize)
        {
            IsBackground = true,
            Name = "RE AgentBridge Accept"
        };
        this._acceptThread.Start();
    }

    public void Dispose()
    {
        this._stopping = true;

        this._listener?.Stop();

        lock (this._lock)
        {
            foreach (var client in this._clients)
                client.Close();
            this._clients.Clear();
        }
    }

    private void AcceptLoop()
    {
        while (!this._stopping)
        {
            TcpClient client;
            try
            {
                client = this._listener.AcceptTcpClient();
            }
            catch (Exception e)
            {
                if (!this._stopping)
                    this._logError(e);
                return;
            }

            lock (this._lock)
            {
                if (this._clients.Count >= this._maxConnections)
                {
                    RejectBusy(client);
                    continue;
                }
                this._clients.Add(client);
            }

            var thread = new Thread(() => this.ConnectionLoop(client), ThreadStackSize)
            {
                IsBackground = true,
                Name = "RE AgentBridge Connection"
            };
            thread.Start();
        }
    }

    private void ConnectionLoop(TcpClient client)
    {
        try
        {
            client.ReceiveTimeout = IdleTimeoutMs;
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n" };

            while (!this._stopping)
            {
                var line = ReadLine(reader, out var tooLong);
                if (line == null)
                    return;
                if (tooLong)
                {
                    writer.WriteLine("{\"ok\":false,\"error\":\"Request too long\"}");
                    writer.Flush();
                    return;
                }
                if (line.Trim().Length == 0)
                    continue;

                writer.WriteLine(this._processLine(line));
                writer.Flush();
            }
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception e)
        {
            if (!this._stopping)
                this._logError(e);
        }
        finally
        {
            lock (this._lock)
                this._clients.Remove(client);
            client.Close();
        }
    }

    private static string ReadLine(StreamReader reader, out bool tooLong)
    {
        tooLong = false;
        var sb = new StringBuilder();
        while (true)
        {
            var c = reader.Read();
            if (c == -1)
                return sb.Length > 0 ? sb.ToString() : null;
            if (c == '\n')
                return sb.ToString().TrimEnd('\r');
            if (sb.Length >= MaxLineLength)
            {
                tooLong = true;
                return string.Empty;
            }
            sb.Append((char)c);
        }
    }

    private static void RejectBusy(TcpClient client)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes("{\"ok\":false,\"error\":\"Too many connections\"}\n");
            client.GetStream().Write(bytes, 0, bytes.Length);
        }
        catch (Exception)
        {
        }
        finally
        {
            client.Close();
        }
    }
}
