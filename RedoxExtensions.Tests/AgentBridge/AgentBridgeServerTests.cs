using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class AgentBridgeServerTests
{
    [Test]
    public void ServesMultipleRequestsPerConnection()
    {
        using var server = new AgentBridgeServer(line => "echo:" + line, null);
        server.Start();

        using var client = new TcpClient("127.0.0.1", server.Port);
        var (reader, writer) = Open(client);

        writer.WriteLine("one");
        Assert.That(reader.ReadLine(), Is.EqualTo("echo:one"));
        writer.WriteLine("two\r");
        Assert.That(reader.ReadLine(), Is.EqualTo("echo:two"));
    }

    [Test]
    public void SkipsBlankLines()
    {
        using var server = new AgentBridgeServer(line => "echo:" + line, null);
        server.Start();

        using var client = new TcpClient("127.0.0.1", server.Port);
        var (reader, writer) = Open(client);

        writer.WriteLine("");
        writer.WriteLine("x");
        Assert.That(reader.ReadLine(), Is.EqualTo("echo:x"));
    }

    [Test]
    public void RejectsConnectionsOverTheCap()
    {
        using var server = new AgentBridgeServer(line => "echo:" + line, null, maxConnections: 1);
        server.Start();

        using var first = new TcpClient("127.0.0.1", server.Port);
        var (firstReader, firstWriter) = Open(first);
        firstWriter.WriteLine("hold");
        Assert.That(firstReader.ReadLine(), Is.EqualTo("echo:hold"));

        using var second = new TcpClient("127.0.0.1", server.Port);
        var (secondReader, _) = Open(second);
        Assert.That(secondReader.ReadLine(), Does.Contain("Too many connections"));
    }

    [Test]
    public void FreesSlotWhenClientDisconnects()
    {
        using var server = new AgentBridgeServer(line => "echo:" + line, null, maxConnections: 1);
        server.Start();

        using (var first = new TcpClient("127.0.0.1", server.Port))
        {
            var (reader, writer) = Open(first);
            writer.WriteLine("a");
            Assert.That(reader.ReadLine(), Is.EqualTo("echo:a"));
        }

        WaitUntil(() => server.ConnectionCount == 0);

        using var second = new TcpClient("127.0.0.1", server.Port);
        var (secondReader, secondWriter) = Open(second);
        secondWriter.WriteLine("b");
        Assert.That(secondReader.ReadLine(), Is.EqualTo("echo:b"));
    }

    [Test]
    public void RejectsOverlongLines()
    {
        using var server = new AgentBridgeServer(line => "echo", null);
        server.Start();

        using var client = new TcpClient("127.0.0.1", server.Port);
        var (reader, writer) = Open(client);

        writer.WriteLine(new string('x', AgentBridgeServer.MaxLineLength + 10));
        Assert.That(reader.ReadLine(), Does.Contain("Request too long"));
    }

    [Test]
    public void DisposeStopsListening()
    {
        var server = new AgentBridgeServer(line => "echo", null);
        server.Start();
        var port = server.Port;

        server.Dispose();

        Assert.That(() => new TcpClient("127.0.0.1", port), Throws.InstanceOf<SocketException>());
    }

    private static (StreamReader, StreamWriter) Open(TcpClient client)
    {
        client.ReceiveTimeout = 5000;
        var stream = client.GetStream();
        return (new StreamReader(stream, new UTF8Encoding(false)), new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n", AutoFlush = true });
    }

    private static void WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        Assert.That(condition(), Is.True);
    }
}
