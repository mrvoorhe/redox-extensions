using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

using RedoxExtensions.Wrapper.Compatibility;

namespace RedoxExtensions.Tests;

[TestFixture]
public class LargeAddressAlignmentScanTests
{
    private const int TextVa = 0x1000;

    // and eax, 0x80000003 ; jns +5 ; dec eax ; or eax, -4 ; inc eax ; je +0x18
    private static readonly byte[] EaxSite = { 0x25, 0x03, 0x00, 0x00, 0x80, 0x79, 0x05, 0x48, 0x83, 0xC8, 0xFC, 0x40, 0x74, 0x18 };

    [Test]
    public void MatchesTheEaxShortForm()
    {
        var sites = Scan(EaxSite);

        Assert.That(sites, Has.Count.EqualTo(1));
        Assert.That(sites[0].AndVa, Is.EqualTo(TextVa));
        Assert.That(sites[0].ImmHighByteVa, Is.EqualTo(TextVa + 4));
        Assert.That(sites[0].AlreadyPatched, Is.False);
    }

    [Test]
    public void MatchesTheLongFormWithAGapAndAPop()
    {
        // and ecx, 0x80000003 ; mov [esp+0xC], eax ; jns ; dec ecx ; or ecx, -4 ; inc ecx ; pop ebx ; je
        byte[] code = { 0x81, 0xE1, 0x03, 0x00, 0x00, 0x80, 0x89, 0x44, 0x24, 0x0C, 0x79, 0x05, 0x49, 0x83, 0xC9, 0xFC, 0x41, 0x5B, 0x74, 0x10 };

        var sites = Scan(code);

        Assert.That(sites, Has.Count.EqualTo(1));
        Assert.That(sites[0].Register, Is.EqualTo(1));
        Assert.That(sites[0].ImmHighByteVa, Is.EqualTo(TextVa + 5));
    }

    [TestCase(12, 0x66, TestName = "No trailing je")]
    [TestCase(7, 0x49, TestName = "Fix-up on another register")]
    [TestCase(1, 0x07, TestName = "and 0x80000007")]
    public void RefusesLookAlikes(int index, byte value)
    {
        var code = (byte[])EaxSite.Clone();
        code[index] = value;

        Assert.That(Scan(code), Is.Empty);
    }

    [Test]
    public void ReportsAndEax3AsAlreadyPatched()
    {
        var code = (byte[])EaxSite.Clone();
        code[4] = LargeAddressAlignmentScan.PatchedHighByte;

        var sites = Scan(code);

        Assert.That(sites, Has.Count.EqualTo(1));
        Assert.That(sites[0].AlreadyPatched, Is.True);
    }

    [TestCase(0x2FDC98E1u)]
    [TestCase(0x2FDC98E3u)]
    [TestCase(0x80438035u)]
    [TestCase(0x80438036u)]
    [TestCase(0x8043A4C3u)]
    [TestCase(0xFFFFFFFDu)]
    public void PatchedPaddingAlignsAtAnyAddress(uint cursor)
    {
        int stock = StockPad(cursor), patched = PatchedPad(cursor);

        Assert.That(patched, Is.LessThan(4));
        Assert.That((cursor + (uint)patched) % 4, Is.EqualTo(0));
        Assert.That(stock, Is.EqualTo(cursor < 0x80000000 ? patched : patched + 4));
    }

    [Test]
    public void FindsTheRetailClientsPaddingSites()
    {
        var path = Environment.GetEnvironmentVariable("ACCLIENT_EXE") ?? @"C:\Turbine\Asheron's Call\acclient.exe";
        if (!File.Exists(path))
            Assert.Ignore($"acclient.exe not found at {path} (set ACCLIENT_EXE)");

        Assert.That(TryReadText(File.ReadAllBytes(path), out int textVa, out byte[] text), Is.True, ".text section read");

        var sites = LargeAddressAlignmentScan.Find(text, textVa);

        Assert.That(sites, Has.Count.EqualTo(190));
        Assert.That(sites.All(s => !s.AlreadyPatched), Is.True, "none patched on disk");
        Assert.That(sites.Select(s => s.AndVa), Is.SupersetOf(new[] { 0x005271A6, 0x00534B48, 0x00534C96, 0x00532E78, 0x004FD1B6, 0x006B394E }));
        Assert.That(sites.Select(s => s.AndVa), Has.No.Member(0x00547029), "16-bit ring index left alone");

        foreach (var s in sites)
            text[s.ImmHighByteVa - textVa] = LargeAddressAlignmentScan.PatchedHighByte;
        var again = LargeAddressAlignmentScan.Find(text, textVa);

        Assert.That(again.Select(s => s.AndVa), Is.EqualTo(sites.Select(s => s.AndVa)));
        Assert.That(again.All(s => s.AlreadyPatched), Is.True);
    }

    private static int StockPad(uint cursor)
    {
        int r = (int)cursor % 4;
        return r == 0 ? 0 : 4 - r;
    }

    private static int PatchedPad(uint cursor)
    {
        int r = (int)(cursor & 3);
        return r == 0 ? 0 : 4 - r;
    }

    private static List<AlignmentSite> Scan(byte[] code)
    {
        var padded = new byte[code.Length + 64];
        code.CopyTo(padded, 0);
        return LargeAddressAlignmentScan.Find(padded, TextVa);
    }

    private static bool TryReadText(byte[] image, out int textVa, out byte[] text)
    {
        textVa = 0;
        text = Array.Empty<byte>();
        int pe = BitConverter.ToInt32(image, 0x3C);
        int sections = BitConverter.ToUInt16(image, pe + 6);
        int optSize = BitConverter.ToUInt16(image, pe + 20);
        int imageBase = BitConverter.ToInt32(image, pe + 24 + 28);
        for (int i = 0; i < sections; i++)
        {
            int s = pe + 24 + optSize + i * 40;
            if (Encoding.ASCII.GetString(image, s, 8).TrimEnd('\0') != ".text")
                continue;
            int virtualSize = BitConverter.ToInt32(image, s + 8);
            int rva = BitConverter.ToInt32(image, s + 12);
            int rawOffset = BitConverter.ToInt32(image, s + 20);
            textVa = imageBase + rva;
            text = image.Skip(rawOffset).Take(virtualSize).ToArray();
            return true;
        }
        return false;
    }
}

[TestFixture]
public class CompatibilitySettingsTests
{
    [TestCase("{ \"UserSettingsFilePath\": \"mysettings.json\" }", true, TestName = "Missing key leaves the fix on")]
    [TestCase("{ \"EnableLargeAddressAlignmentFix\": true }", true, TestName = "true leaves the fix on")]
    [TestCase("{ \"EnableLargeAddressAlignmentFix\": false }", false, TestName = "false turns the fix off")]
    [TestCase("﻿{ \"UserSettingsFilePath\": \"x\", \"EnableLargeAddressAlignmentFix\": false }", false, TestName = "BOM is ignored")]
    [TestCase("{ \"EnableLargeAddressAlignmentFix\": \"false\" }", true, TestName = "Only a JSON false turns the fix off")]
    public void ReadsTheLargeAddressAlignmentSwitch(string json, bool expected)
    {
        Assert.That(CompatibilitySettings.IsLargeAddressAlignmentFixEnabled(json), Is.EqualTo(expected));
    }
}
