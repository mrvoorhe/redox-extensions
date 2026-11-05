using System;
using System.Runtime.InteropServices;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// Replaces one function pointer in a COM vtable and keeps the original so the replacement can chain to it.
/// </summary>
internal sealed class VTableSlotPatch
{
    private const uint PageExecuteReadWrite = 0x40;

    private readonly Delegate _replacement;

    private VTableSlotPatch(IntPtr slotAddress, IntPtr originalPointer, Delegate replacement, IntPtr replacementPointer)
    {
        this.SlotAddress = slotAddress;
        this.OriginalPointer = originalPointer;
        this.ReplacementPointer = replacementPointer;

        // The vtable now points at a thunk for this delegate, so it has to stay reachable.
        this._replacement = replacement;
    }

    public IntPtr SlotAddress { get; }

    public IntPtr OriginalPointer { get; }

    public IntPtr ReplacementPointer { get; }

    public bool IsInPlace => Marshal.ReadIntPtr(this.SlotAddress) == this.ReplacementPointer;

    public static IntPtr ReadVTable(IntPtr interfacePointer) => Marshal.ReadIntPtr(interfacePointer);

    public static IntPtr SlotAddressOf(IntPtr vtable, int slot) => new(vtable.ToInt64() + (long)slot * IntPtr.Size);

    /// <summary>
    /// Points <paramref name="slot"/> of <paramref name="vtable"/> at <paramref name="replacement"/>.
    /// <paramref name="replacement"/> must be a delegate with an <see cref="UnmanagedFunctionPointerAttribute"/> whose
    /// signature matches the slot, including the leading 'this' pointer.
    /// </summary>
    public static VTableSlotPatch Install(IntPtr vtable, int slot, Delegate replacement)
    {
        var slotAddress = SlotAddressOf(vtable, slot);
        var original = Marshal.ReadIntPtr(slotAddress);
        var replacementPointer = Marshal.GetFunctionPointerForDelegate(replacement);

        WritePointer(slotAddress, replacementPointer);

        return new VTableSlotPatch(slotAddress, original, replacement, replacementPointer);
    }

    private static void WritePointer(IntPtr address, IntPtr value)
    {
        // Vtables live in read-only sections.  Off Windows (unit tests) the memory is our own heap block.
        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        uint oldProtect = 0;
        var size = new UIntPtr((uint)IntPtr.Size);

        if (isWindows && !VirtualProtect(address, size, PageExecuteReadWrite, out oldProtect))
            throw new InvalidOperationException($"VirtualProtect failed ({Marshal.GetLastWin32Error()})");

        try
        {
            Marshal.WriteIntPtr(address, value);
        }
        finally
        {
            if (isWindows)
                VirtualProtect(address, size, oldProtect, out _);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint newProtect, out uint oldProtect);
}
