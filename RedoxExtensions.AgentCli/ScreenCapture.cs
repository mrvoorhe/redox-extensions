using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace RedoxExtensions.AgentCli;

/// <summary>
/// Captures a client's window with PrintWindow and saves it as PNG.  Runs in the CLI process, so acclient isn't touched.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class ScreenCapture
{
    private const uint PwClientOnly = 0x1;
    private const uint PwRenderFullContent = 0x2;
    private const uint DibRgbColors = 0;
    private const int BiRgb = 0;

    /// <summary>
    /// Captures the client area of <paramref name="pid"/>'s main window, shrunk by <paramref name="scale"/> (1 = full
    /// size).  Throws with a usable message.
    /// </summary>
    public static (int Width, int Height) CaptureToPng(int pid, string path, int scale = 1)
    {
        if (scale < 1)
            throw new ArgumentException("scale must be 1 or more");

        var hwnd = FindWindow(pid);
        if (IsIconic(hwnd))
            throw new InvalidOperationException("The client window is minimized, so there's nothing to capture.  Restore it first.");

        if (!GetClientRect(hwnd, out var rect))
            throw new InvalidOperationException($"GetClientRect failed ({Marshal.GetLastWin32Error()})");

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
            throw new InvalidOperationException($"The client window has no area ({width}x{height})");

        var bgra = Capture(hwnd, width, height);
        if (scale > 1)
            bgra = Shrink(bgra, ref width, ref height, scale);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        using (var file = File.Create(path))
            WritePng(file, width, height, bgra);

        return (width, height);
    }

    private static IntPtr FindWindow(int pid)
    {
        using var process = Process.GetProcessById(pid);
        var hwnd = process.MainWindowHandle;
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException($"Process {pid} has no main window");
        return hwnd;
    }

    /// <summary>Top-down rows of 32-bit BGRA pixels.</summary>
    private static byte[] Capture(IntPtr hwnd, int width, int height)
    {
        var windowDc = GetDC(hwnd);
        if (windowDc == IntPtr.Zero)
            throw new InvalidOperationException("GetDC failed");

        var memoryDc = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        var previous = IntPtr.Zero;
        try
        {
            memoryDc = CreateCompatibleDC(windowDc);
            bitmap = CreateCompatibleBitmap(windowDc, width, height);
            if (memoryDc == IntPtr.Zero || bitmap == IntPtr.Zero)
                throw new InvalidOperationException("Couldn't create a bitmap to capture into");

            previous = SelectObject(memoryDc, bitmap);
            if (!PrintWindow(hwnd, memoryDc, PwClientOnly | PwRenderFullContent))
                throw new InvalidOperationException($"PrintWindow failed ({Marshal.GetLastWin32Error()})");
            SelectObject(memoryDc, previous);
            previous = IntPtr.Zero;

            var info = new BitmapInfoHeader
            {
                Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                Width = width,
                Height = -height, // negative = top-down rows
                Planes = 1,
                BitCount = 32,
                Compression = BiRgb,
            };

            var pixels = new byte[width * height * 4];
            if (GetDIBits(memoryDc, bitmap, 0, (uint)height, pixels, ref info, DibRgbColors) != height)
                throw new InvalidOperationException("GetDIBits failed");

            return pixels;
        }
        finally
        {
            if (previous != IntPtr.Zero)
                SelectObject(memoryDc, previous);
            if (bitmap != IntPtr.Zero)
                DeleteObject(bitmap);
            if (memoryDc != IntPtr.Zero)
                DeleteDC(memoryDc);
            ReleaseDC(hwnd, windowDc);
        }
    }

    /// <summary>Averages each <paramref name="scale"/> x <paramref name="scale"/> block into one pixel.  Drops partial edge blocks.</summary>
    internal static byte[] Shrink(byte[] bgra, ref int width, ref int height, int scale)
    {
        var newWidth = width / scale;
        var newHeight = height / scale;
        if (newWidth == 0 || newHeight == 0)
            throw new ArgumentException($"scale {scale} is too large for a {width}x{height} window");

        var result = new byte[newWidth * newHeight * 4];
        var area = scale * scale;
        for (var y = 0; y < newHeight; y++)
        {
            for (var x = 0; x < newWidth; x++)
            {
                int b = 0, g = 0, r = 0;
                for (var dy = 0; dy < scale; dy++)
                {
                    var source = ((y * scale + dy) * width + x * scale) * 4;
                    for (var dx = 0; dx < scale; dx++, source += 4)
                    {
                        b += bgra[source];
                        g += bgra[source + 1];
                        r += bgra[source + 2];
                    }
                }

                var target = (y * newWidth + x) * 4;
                result[target] = (byte)(b / area);
                result[target + 1] = (byte)(g / area);
                result[target + 2] = (byte)(r / area);
            }
        }

        width = newWidth;
        height = newHeight;
        return result;
    }

    /// <summary>Writes 8-bit RGB PNG from top-down BGRA rows.</summary>
    internal static void WritePng(Stream output, int width, int height, byte[] bgra)
    {
        output.Write(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A });

        var header = new byte[13];
        WriteBigEndian(header, 0, (uint)width);
        WriteBigEndian(header, 4, (uint)height);
        header[8] = 8;  // bit depth
        header[9] = 2;  // color type: RGB
        header[10] = 0; // compression
        header[11] = 0; // filter
        header[12] = 0; // interlace
        WriteChunk(output, "IHDR", header);

        using (var compressed = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                var row = new byte[1 + width * 3]; // filter byte 0 (none), then RGB
                for (var y = 0; y < height; y++)
                {
                    var source = y * width * 4;
                    for (var x = 0; x < width; x++)
                    {
                        row[1 + x * 3] = bgra[source + x * 4 + 2];
                        row[2 + x * 3] = bgra[source + x * 4 + 1];
                        row[3 + x * 3] = bgra[source + x * 4];
                    }
                    zlib.Write(row);
                }
            }

            WriteChunk(output, "IDAT", compressed.ToArray());
        }

        WriteChunk(output, "IEND", Array.Empty<byte>());
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var length = new byte[4];
        WriteBigEndian(length, 0, (uint)data.Length);
        output.Write(length);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);

        var crc = Crc32.Update(Crc32.Update(0xFFFFFFFF, typeBytes), data) ^ 0xFFFFFFFF;
        var crcBytes = new byte[4];
        WriteBigEndian(crcBytes, 0, crc);
        output.Write(crcBytes);
    }

    private static void WriteBigEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    /// <summary>The CRC-32 PNG uses (ISO 3309, polynomial 0xEDB88320).</summary>
    internal static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        public static uint Update(uint crc, byte[] data)
        {
            foreach (var b in data)
                crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc;
        }

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }
    }

    #region Win32

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hwnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr bitmap, uint start, uint lines, byte[] bits, ref BitmapInfoHeader info, uint usage);

    #endregion
}
