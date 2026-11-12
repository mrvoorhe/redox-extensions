using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RedoxExtensions.Wrapper.Compatibility;

/// <summary>
/// Rewrites acclient's signed "pointer % 4" padding (<see cref="LargeAddressAlignmentScan"/>)
/// to <c>and r, 3</c> in memory, so pack/unpack of buffers above 2 GB pads 0..3 bytes like
/// it does below 2 GB. Without it, a large-address-aware client corrupts dat and network
/// unpacks whenever the heap hands AC a buffer above 0x80000000 (long sessions).
///
/// The patch is never undone, and sites already patched (by RynthCore or an earlier load)
/// are counted, not rewritten. acclient.exe on disk is not touched.
/// </summary>
internal static class LargeAddressAlignmentPatch
{
    private const uint PageExecuteReadWrite = 0x40;
    private const ushort ImageDosSignature = 0x5A4D;
    private const uint ImageNtSignature = 0x00004550;
    private const int SectionHeaderSize = 40;

    private static bool _applied;

    public static string StatusMessage { get; private set; } = "Not applied yet.";

    public static int PatchedCount { get; private set; }

    public static int AlreadyPatchedCount { get; private set; }

    public static void Apply(bool enabled)
    {
        if (_applied)
            return;
        _applied = true;

        if (!enabled)
        {
            StatusMessage = $"Disabled via settings.json ({CompatibilitySettings.EnableLargeAddressAlignmentFixKey}=false).";
            return;
        }

        if (Environment.OSVersion.Platform != PlatformID.Win32NT || IntPtr.Size != 4)
        {
            StatusMessage = "Skipped: not a 32-bit Windows process.";
            return;
        }

        IntPtr moduleBase = GetModuleHandleW("acclient.exe");
        if (moduleBase == IntPtr.Zero)
            moduleBase = GetModuleHandleW(null);
        if (moduleBase == IntPtr.Zero || !TryGetTextSection(moduleBase, out int textRva, out int textSize))
        {
            StatusMessage = "Skipped: acclient .text section not found.";
            return;
        }

        IntPtr textBase = Offset(moduleBase, textRva);
        byte[] text = new byte[textSize];
        Marshal.Copy(textBase, text, 0, textSize);
        List<AlignmentSite> sites = LargeAddressAlignmentScan.Find(text, textBase.ToInt32());

        int patched = 0, already = 0, failed = 0;
        foreach (AlignmentSite site in sites)
        {
            if (site.AlreadyPatched)
                already++;
            else if (TryWriteHighByte(new IntPtr(site.ImmHighByteVa)))
                patched++;
            else
                failed++;
        }

        if (patched > 0)
            FlushInstructionCache(GetCurrentProcess(), textBase, new UIntPtr((uint)textSize));

        PatchedCount = patched;
        AlreadyPatchedCount = already;
        StatusMessage = $"{patched} site(s) patched, {already} already patched, {failed} failed ({sites.Count} found).";
    }

    public static void Fail(Exception e) => StatusMessage = $"Failed: {e.GetType().Name}: {e.Message}";

    // One byte, protection restored per site: other hooks may hold different protections on
    // neighbouring .text pages, so the whole section is never flipped at once.
    private static bool TryWriteHighByte(IntPtr address)
    {
        var size = new UIntPtr(1u);
        if (!VirtualProtect(address, size, PageExecuteReadWrite, out uint oldProtect))
            return false;

        try
        {
            // Re-check the live byte: the scan worked on a copy.
            if (Marshal.ReadByte(address) != LargeAddressAlignmentScan.StockHighByte)
                return false;
            Marshal.WriteByte(address, LargeAddressAlignmentScan.PatchedHighByte);
            return true;
        }
        finally
        {
            VirtualProtect(address, size, oldProtect, out _);
        }
    }

    private static bool TryGetTextSection(IntPtr moduleBase, out int rva, out int size)
    {
        rva = 0;
        size = 0;
        if ((ushort)Marshal.ReadInt16(moduleBase) != ImageDosSignature)
            return false;
        int peOffset = Marshal.ReadInt32(moduleBase, 0x3C);
        if (peOffset <= 0)
            return false;
        IntPtr nt = Offset(moduleBase, peOffset);
        if ((uint)Marshal.ReadInt32(nt) != ImageNtSignature)
            return false;

        int sectionCount = (ushort)Marshal.ReadInt16(nt, 6);
        int optionalHeaderSize = (ushort)Marshal.ReadInt16(nt, 20);
        IntPtr section = Offset(nt, 24 + optionalHeaderSize);
        for (int i = 0; i < sectionCount; i++, section = Offset(section, SectionHeaderSize))
        {
            if (Marshal.PtrToStringAnsi(section, 8).TrimEnd('\0') != ".text")
                continue;
            size = Marshal.ReadInt32(section, 8);
            rva = Marshal.ReadInt32(section, 12);
            return size > 0 && rva > 0;
        }
        return false;
    }

    private static IntPtr Offset(IntPtr pointer, int offset) => new(pointer.ToInt64() + offset);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandleW(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);
}
