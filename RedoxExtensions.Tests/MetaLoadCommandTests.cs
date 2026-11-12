using NUnit.Framework;

using RedoxExtensions.MetaChainWatch;

namespace RedoxExtensions.Tests;

[TestFixture]
public class MetaLoadCommandTests
{
    [TestCase("/vt meta load Daily", "Daily")]
    [TestCase("/VT Meta LOAD Daily", "Daily")]
    [TestCase("  /vt   meta    load   Daily  ", "Daily")]
    [TestCase("/vt\tmeta\tload\tDaily", "Daily")]
    [TestCase("/vt meta load Daily.met", "Daily")]
    [TestCase("/vt meta load Attributes4.0", "Attributes4.0")]
    [TestCase("/vt meta load Some Meta Name", "Some Meta Name")]
    [TestCase("/vt meta load zzMCW_Start\r\n", "zzMCW_Start")]
    public void TryParse_MetaLoad_ReturnsName(string text, string expected)
    {
        Assert.That(MetaLoadCommand.TryParse(text, out var name), Is.True);
        Assert.That(name, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("/vt meta load")]
    [TestCase("/vt meta load   ")]
    [TestCase("/vt meta load .met")]
    [TestCase("/vt meta loadDaily")]
    [TestCase("/vt metaload Daily")]
    [TestCase("/vt nav load Daily")]
    [TestCase("/vt setmetastate Default")]
    [TestCase("/vtx meta load Daily")]
    [TestCase("say /vt meta load Daily")]
    [TestCase("/re sm Daily")]
    public void TryParse_NotMetaLoad_ReturnsFalse(string text)
    {
        Assert.That(MetaLoadCommand.TryParse(text, out var name), Is.False);
        Assert.That(name, Is.Null);
    }
}
