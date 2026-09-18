using System.Linq;
using NUnit.Framework;
using RedoxLib.GameValues;

namespace RedoxExtensions.Tests
{
    [TestFixture]
    public class ColorHighlightsTests
    {
        [Test]
        public void DecodeReturnsEmptyWhenNothingEnabled()
        {
            var result = ColorHighlights.Decode<ArmorHighlightMask>(0x0000, 0xFFFF);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DecodeReportsGreenWhenColorBitSet()
        {
            var result = ColorHighlights.Decode<ArmorHighlightMask>(
                (ushort)ArmorHighlightMask.Fire,
                (ushort)ArmorHighlightMask.Fire);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Fire", result[0].Name);
            Assert.IsTrue(result[0].IsGreen);
            Assert.AreEqual("Fire=green", result[0].ToString());
        }

        [Test]
        public void DecodeReportsRedWhenColorBitClear()
        {
            var result = ColorHighlights.Decode<ArmorHighlightMask>(
                (ushort)ArmorHighlightMask.Cold,
                0x0000);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Cold", result[0].Name);
            Assert.IsFalse(result[0].IsGreen);
            Assert.AreEqual("Cold=red", result[0].ToString());
        }

        [Test]
        public void DecodeHandlesMultipleBitsWithMixedColors()
        {
            ushort enable = (ushort)(ArmorHighlightMask.Fire | ArmorHighlightMask.Cold);
            ushort color = (ushort)ArmorHighlightMask.Fire; // Fire green, Cold red

            var result = ColorHighlights.Decode<ArmorHighlightMask>(enable, color);

            Assert.AreEqual(2, result.Count);
            var fire = result.Single(h => h.Name == "Fire");
            var cold = result.Single(h => h.Name == "Cold");
            Assert.IsTrue(fire.IsGreen);
            Assert.IsFalse(cold.IsGreen);
        }

        [Test]
        public void DecodeIgnoresColorBitsForDisabledFields()
        {
            // Only Fire enabled, but color mask also lights Cold's bit.
            var result = ColorHighlights.Decode<ArmorHighlightMask>(
                (ushort)ArmorHighlightMask.Fire,
                (ushort)(ArmorHighlightMask.Fire | ArmorHighlightMask.Cold));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Fire", result[0].Name);
        }

        [Test]
        public void DecodeWorksForAttributeMaskHighBit()
        {
            var result = ColorHighlights.Decode<AttributeHighlightMask>(
                (ushort)AttributeHighlightMask.Mana,
                (ushort)AttributeHighlightMask.Mana);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Mana", result[0].Name);
            Assert.IsTrue(result[0].IsGreen);
        }
    }
}
