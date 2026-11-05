using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using RedoxExtensions.AgentBridge;
using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
[NonParallelizable]
public class NetCaptureTests
{
    [TearDown]
    public void TearDown() => Configure("{\"off\":true}");

    private static Dictionary<string, object> Configure(string json) =>
        (Dictionary<string, object>)NetCapture.Configure((IDictionary<string, object>)MiniJson.Parse(json));

    [Test]
    public void CapturesNothingUntilConfigured()
    {
        Assert.That(NetCapture.IsCapturing(true, 0xF7B0), Is.False);
        Assert.That(NetCapture.IsCapturing(false, 0xF7B1), Is.False);
    }

    [Test]
    public void SelectsTypesPerDirection()
    {
        var result = Configure("{\"server\":\"F7B0, 0xf74e\",\"client\":[\"F7B1\"]}");

        Assert.That(NetCapture.IsCapturing(true, 0xF7B0), Is.True);
        Assert.That(NetCapture.IsCapturing(true, 0xF74E), Is.True);
        Assert.That(NetCapture.IsCapturing(true, 0xF7B1), Is.False);
        Assert.That(NetCapture.IsCapturing(false, 0xF7B1), Is.True);
        Assert.That(NetCapture.IsCapturing(false, 0xF7B0), Is.False);
        Assert.That(result["server"], Is.EqualTo(new object[] { "F74E", "F7B0" }));
        Assert.That(result["client"], Is.EqualTo(new object[] { "F7B1" }));
    }

    [Test]
    public void SettingOneDirectionKeepsTheOtherAndOffClearsBoth()
    {
        Configure("{\"server\":\"F7B0\",\"client\":\"F7B1\"}");
        Configure("{\"server\":\"all\"}");

        Assert.That(NetCapture.IsCapturing(true, 0x1234), Is.True);
        Assert.That(NetCapture.IsCapturing(false, 0xF7B1), Is.True);

        var result = Configure("{\"off\":true}");

        Assert.That(NetCapture.IsCapturing(true, 0x1234), Is.False);
        Assert.That(NetCapture.IsCapturing(false, 0xF7B1), Is.False);
        Assert.That(result["server"], Is.Empty);
    }

    [Test]
    public void RejectsBadTypes()
    {
        Assert.That(() => Configure("{\"server\":\"F7B0,nope\"}"), Throws.ArgumentException.With.Message.Contains("Bad message type 'nope'"));
    }

    [Test]
    public void FormatsFieldsAndNestedStructs()
    {
        var fields = new FakeFields(
            ("character", 1342207843),
            ("event", 0x01C7),
            ("position", new FakeFields(("x", 1.5), ("y", -2.25))));

        Assert.That(NetCapture.Format(true, 0xF7B0, fields), Is.EqualTo("S F7B0 character=1342207843 event=455 position={ x=1.5 y=-2.25 }"));
        Assert.That(NetCapture.Format(false, 0x36, new FakeFields()), Is.EqualTo("C 0036"));
    }

    [Test]
    public void LimitsDepthSkipsUnreadableFieldsAndCutsLongText()
    {
        var deep = new FakeFields(("a", new FakeFields(("b", new FakeFields(("c", new FakeFields(("d", 1))))))));
        Assert.That(NetCapture.Format(true, 1, deep), Is.EqualTo("S 0001 a={ b={ c={...} } }"));

        var unreadable = new FakeFields(("ok", 1), ("bad", new Func<object>(() => throw new InvalidOperationException())), ("also", 2));
        Assert.That(NetCapture.Format(true, 1, unreadable), Is.EqualTo("S 0001 ok=1 also=2"));

        var long_ = new FakeFields(Enumerable.Range(0, 200).Select(i => ($"field{i}", (object)i)).ToArray());
        var text = NetCapture.Format(true, 1, long_);
        Assert.That(text.Length, Is.EqualTo(NetCapture.MaxTextLength));
        Assert.That(text, Does.EndWith("..."));
    }

    private sealed class FakeFields : INetFields
    {
        private readonly (string Name, object Value)[] _fields;

        public FakeFields(params (string Name, object Value)[] fields) => this._fields = fields;

        public int Count => this._fields.Length;

        public string Name(int index) => this._fields[index].Name;

        public object Value(int index) => this._fields[index].Value is Func<object> read ? read() : this._fields[index].Value;
    }
}
