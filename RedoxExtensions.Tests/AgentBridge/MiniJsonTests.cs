using System;
using System.Collections.Generic;

using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class MiniJsonTests
{
    [Test]
    public void ParsesNestedObject()
    {
        var parsed = (Dictionary<string, object>)MiniJson.Parse("{\"a\": 1, \"b\": [true, false, null, \"x\"], \"c\": {\"d\": -2.5e1}}");

        Assert.That(parsed["a"], Is.EqualTo(1.0));
        Assert.That((List<object>)parsed["b"], Is.EqualTo(new object[] { true, false, null, "x" }));
        Assert.That(((Dictionary<string, object>)parsed["c"])["d"], Is.EqualTo(-25.0));
    }

    [Test]
    public void RoundTripsEscapes()
    {
        var text = "quote\" backslash\\ newline\n tab\t control\u0001 unicodeé";

        var json = MiniJson.Serialize(text);

        Assert.That(MiniJson.Parse(json), Is.EqualTo(text));
        Assert.That(json, Does.Contain("\\u0001"));
    }

    [Test]
    public void SerializesPrimitivesAndCollections()
    {
        var value = new Dictionary<string, object>
        {
            ["int"] = 5,
            ["long"] = 1234567890123L,
            ["double"] = 1.5,
            ["bool"] = true,
            ["null"] = null,
            ["list"] = new List<string> { "a", "b" },
            ["enum"] = AgentMode.ReadWrite,
        };

        Assert.That(MiniJson.Serialize(value), Is.EqualTo("{\"int\":5,\"long\":1234567890123,\"double\":1.5,\"bool\":true,\"null\":null,\"list\":[\"a\",\"b\"],\"enum\":\"ReadWrite\"}"));
    }

    [TestCase("")]
    [TestCase("{")]
    [TestCase("{\"a\" 1}")]
    [TestCase("[1,]")]
    [TestCase("\"unterminated")]
    [TestCase("{} extra")]
    public void RejectsMalformedJson(string json)
    {
        Assert.That(() => MiniJson.Parse(json), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void GettersHandleMissingAndWrongTypes()
    {
        var obj = (Dictionary<string, object>)MiniJson.Parse("{\"n\": 42, \"s\": \"17\", \"list\": [\"a\", 1], \"single\": \"x\"}");

        Assert.That(MiniJson.GetLong(obj, "n"), Is.EqualTo(42));
        Assert.That(MiniJson.GetLong(obj, "s"), Is.EqualTo(17));
        Assert.That(MiniJson.GetLong(obj, "missing"), Is.Null);
        Assert.That(MiniJson.GetString(obj, "missing"), Is.Null);
        Assert.That(MiniJson.GetStringList(obj, "list"), Is.EqualTo(new[] { "a", "1" }));
        Assert.That(MiniJson.GetStringList(obj, "single"), Is.EqualTo(new[] { "x" }));
    }
}
