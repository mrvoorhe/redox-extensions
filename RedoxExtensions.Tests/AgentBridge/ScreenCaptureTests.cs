using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using NUnit.Framework;

using RedoxExtensions.AgentCli;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class ScreenCaptureTests
{
    [Test]
    public void Crc32MatchesTheKnownValueForIend()
    {
        var crc = ScreenCapture.Crc32.Update(0xFFFFFFFF, Encoding.ASCII.GetBytes("IEND")) ^ 0xFFFFFFFF;

        Assert.That(crc, Is.EqualTo(0xAE426082));
    }

    [Test]
    public void WritesAValidRgbPngFromBgraRows()
    {
        // 2x2, top-down BGRA: red, green / blue, white.  Alpha is ignored.
        var bgra = new byte[]
        {
            0, 0, 255, 0,     0, 255, 0, 0,
            255, 0, 0, 0,     255, 255, 255, 0,
        };

        using var stream = new MemoryStream();
        ScreenCapture.WritePng(stream, 2, 2, bgra);
        var png = stream.ToArray();

        Assert.That(png.Take(8), Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));

        var chunks = ReadChunks(png);
        Assert.That(chunks.Select(c => c.Type), Is.EqualTo(new[] { "IHDR", "IDAT", "IEND" }));

        var header = chunks[0].Data;
        Assert.That(BigEndian(header, 0), Is.EqualTo(2));
        Assert.That(BigEndian(header, 4), Is.EqualTo(2));
        Assert.That(header[8], Is.EqualTo(8));
        Assert.That(header[9], Is.EqualTo(2));

        using var inflated = new MemoryStream();
        using (var zlib = new ZLibStream(new MemoryStream(chunks[1].Data), CompressionMode.Decompress))
            zlib.CopyTo(inflated);

        Assert.That(inflated.ToArray(), Is.EqualTo(new byte[]
        {
            0, 255, 0, 0,   0, 255, 0,
            0, 0, 0, 255,   255, 255, 255,
        }));
    }

    [Test]
    public void ShrinkAveragesBlocksAndDropsPartialEdges()
    {
        // 3x2 BGRA; scale 2 keeps the left 2x2 block and drops the third column.
        var bgra = new byte[]
        {
            0, 0, 0, 0,       100, 0, 0, 0,     9, 9, 9, 0,
            200, 40, 0, 0,    100, 40, 8, 0,    9, 9, 9, 0,
        };
        int width = 3, height = 2;

        var result = ScreenCapture.Shrink(bgra, ref width, ref height, 2);

        Assert.That((width, height), Is.EqualTo((1, 1)));
        Assert.That(result.Take(3), Is.EqualTo(new byte[] { 100, 20, 2 }));
        Assert.That(() => ScreenCapture.Shrink(bgra, ref width, ref height, 5), Throws.ArgumentException);
    }

    private static (string Type, byte[] Data)[] ReadChunks(byte[] png)
    {
        var chunks = new System.Collections.Generic.List<(string, byte[])>();
        var offset = 8;
        while (offset < png.Length)
        {
            var length = (int)BigEndian(png, offset);
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var data = png.Skip(offset + 8).Take(length).ToArray();

            var expectedCrc = ScreenCapture.Crc32.Update(ScreenCapture.Crc32.Update(0xFFFFFFFF, Encoding.ASCII.GetBytes(type)), data) ^ 0xFFFFFFFF;
            Assert.That(BigEndian(png, offset + 8 + length), Is.EqualTo(expectedCrc), $"CRC of {type}");

            chunks.Add((type, data));
            offset += 12 + length;
        }
        return chunks.ToArray();
    }

    private static uint BigEndian(byte[] buffer, int offset) =>
        (uint)(buffer[offset] << 24 | buffer[offset + 1] << 16 | buffer[offset + 2] << 8 | buffer[offset + 3]);
}
