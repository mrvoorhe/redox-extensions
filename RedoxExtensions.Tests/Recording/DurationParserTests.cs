using System;
using System.Globalization;
using System.Threading;

using NUnit.Framework;

using RedoxExtensions.Recording;

namespace RedoxExtensions.Tests.Recording;

[TestFixture]
public class DurationParserTests
{
    [TestCase("30s", 30.0)]
    [TestCase("5m", 300.0)]
    [TestCase("4.5h", 16200.0)]
    [TestCase(".5h", 1800.0)]
    [TestCase("5M", 300.0)]
    [TestCase("2 h", 7200.0)]
    public void Valid(string text, double seconds)
    {
        Assert.That(DurationParser.TryParse(text, out var value), Is.True);
        Assert.That(value, Is.EqualTo(TimeSpan.FromSeconds(seconds)));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("5")]
    [TestCase("0m")]
    [TestCase("0.0s")]
    [TestCase("-5m")]
    [TestCase("5d")]
    [TestCase("1h30m")]
    [TestCase("4,5h")]
    [TestCase("1e3s")]
    [TestCase("NaNh")]
    [TestCase("99999999999h")]
    public void Rejected(string text)
    {
        Assert.That(DurationParser.TryParse(text, out var value), Is.False);
        Assert.That(value, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void UsesInvariantCulture()
    {
        var saved = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            Assert.That(DurationParser.TryParse("4.5h", out var value), Is.True);
            Assert.That(value, Is.EqualTo(TimeSpan.FromMinutes(270)));
            Assert.That(DurationParser.TryParse("4,5h", out _), Is.False);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = saved;
        }
    }
}
