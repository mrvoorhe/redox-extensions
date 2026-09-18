using System.Linq;
using NUnit.Framework;
using RedoxLib.AutoMule;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class MuleConfigTests
    {
        [Test]
        public void ParseKeepsMuleName()
        {
            var config = MuleConfig.Parse("Foo", new[] { "Salvage" });
            Assert.AreEqual("Foo", config.MuleName);
        }

        [Test]
        public void ParseTrimsWhitespaceAndSkipsBlankAndCommentLines()
        {
            var config = MuleConfig.Parse("Foo", new[]
            {
                "  Salvage  ",
                "",
                "   ",
                "# a comment",
                "Gem",
            });

            CollectionAssert.AreEqual(new[] { "Salvage", "Gem" }, config.Substrings.ToArray());
        }

        [Test]
        public void MatchesIsCaseInsensitiveContains()
        {
            var config = MuleConfig.Parse("Foo", new[] { "Salvage" });

            Assert.IsTrue(config.Matches("Iron Salvage"));
            Assert.IsTrue(config.Matches("iron salvage (100)"));
        }

        [Test]
        public void MatchesReturnsFalseWhenNoSubstringPresent()
        {
            var config = MuleConfig.Parse("Foo", new[] { "Salvage" });

            Assert.IsFalse(config.Matches("Prismatic Taper"));
        }

        [Test]
        public void MatchesReturnsFalseForNullOrEmptyName()
        {
            var config = MuleConfig.Parse("Foo", new[] { "Salvage" });

            Assert.IsFalse(config.Matches(null));
            Assert.IsFalse(config.Matches(string.Empty));
        }

        [Test]
        public void MatchesAnyOfMultipleSubstrings()
        {
            var config = MuleConfig.Parse("Foo", new[] { "Salvage", "Key" });

            Assert.IsTrue(config.Matches("Legendary Key"));
            Assert.IsTrue(config.Matches("Iron Salvage"));
            Assert.IsFalse(config.Matches("Health Potion"));
        }

        [Test]
        public void EmptyConfigMatchesNothing()
        {
            var config = MuleConfig.Parse("Foo", new string[0]);

            Assert.AreEqual(0, config.Substrings.Count);
            Assert.IsFalse(config.Matches("Iron Salvage"));
        }
    }
}
