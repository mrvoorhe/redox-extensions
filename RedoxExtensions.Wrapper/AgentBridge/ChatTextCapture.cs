using System;
using System.Reflection;
using System.Runtime.InteropServices;

using Decal.Adapter.Wrappers;

using RedoxExtensions.Wrapper.Diagnostics;

namespace RedoxExtensions.Wrapper.AgentBridge;

/// <summary>
/// Sees the text every plugin writes to chat.
///
/// Plugins write with Decal's IACHooks.AddChatText / AddChatTextRaw, which draws straight into the chat UI without raising
/// ChatBoxMessage, so Decal events never show it.  All plugins share Decal's one hooks object, so this patches those two
/// slots of its COM vtable to report the text here and then call the original.  Calls made late-bound through
/// IDispatch::Invoke don't go through the vtable and aren't seen.
///
/// Installed once per process, on first use, and never removed.  Another plugin may patch the same slots later and chain
/// to our pointer, so putting the original back could cut it out.  With no listener it only forwards the call.
/// </summary>
internal static class ChatTextCapture
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int ChatTextFn(IntPtr self, IntPtr text, int color, int target);

    private static readonly object InstallLock = new();

    private static volatile Action<string, int, int> _listener;

    [ThreadStatic]
    private static bool _inListener;

    private static Hook _chatText;
    private static Hook _chatTextRaw;

    public static bool IsInstalled => _chatText != null;

    /// <summary>Called with (text, color, target window) for every captured line.  Null to stop.</summary>
    public static Action<string, int, int> Listener
    {
        get => _listener;
        set => _listener = value;
    }

    /// <summary>
    /// Patches the hooks object behind <paramref name="actions"/>.  Does nothing if already installed.  Throws if the
    /// interface doesn't look like the one this was written against, without patching anything.
    /// </summary>
    public static void EnsureInstalled(HooksWrapper actions)
    {
        lock (InstallLock)
        {
            if (IsInstalled)
                return;

            // Through reflection so the wrapper doesn't need a compile-time reference to Decal.Interop.Core.
            var underlyingProperty = typeof(HooksWrapper).GetProperty("Underlying")
                ?? throw new InvalidOperationException("HooksWrapper.Underlying not found");
            var hooks = underlyingProperty.GetValue(actions, null)
                ?? throw new InvalidOperationException("HooksWrapper.Underlying is null");
            var hooksInterface = underlyingProperty.PropertyType;

            var chatTextSlot = SlotOf(hooksInterface, "AddChatText");
            var chatTextRawSlot = SlotOf(hooksInterface, "AddChatTextRaw");

            // IUnknown + IDispatch take slots 0-6.  The two methods are declared next to each other.
            if (chatTextSlot < 7 || chatTextRawSlot != chatTextSlot + 1)
                throw new InvalidOperationException($"Unexpected IACHooks layout (AddChatText={chatTextSlot}, AddChatTextRaw={chatTextRawSlot})");

            VerifySlot(hooksInterface, "AddChatText", chatTextSlot);
            VerifySlot(hooksInterface, "AddChatTextRaw", chatTextRawSlot);

            var interfacePointer = Marshal.GetComInterfaceForObject(hooks, hooksInterface);
            IntPtr vtable;
            try
            {
                vtable = VTableSlotPatch.ReadVTable(interfacePointer);
            }
            finally
            {
                Marshal.Release(interfacePointer);
            }

            _chatText = new Hook(vtable, chatTextSlot);
            _chatTextRaw = new Hook(vtable, chatTextRawSlot);
        }
    }

    /// <summary>
    /// The vtable slot of a method on a ComImport interface: after IUnknown (3 slots), or after IDispatch (7) for a
    /// dual or dispatch interface, in metadata declaration order.  Same rule the CLR uses to lay out the interface.
    /// </summary>
    internal static int SlotOf(Type comInterface, string methodName)
    {
        if (comInterface.GetInterfaces().Length != 0)
            throw new InvalidOperationException($"{comInterface.Name} inherits other interfaces; slot math doesn't cover that");

        var methods = comInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Array.Sort(methods, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));

        var index = Array.FindIndex(methods, m => m.Name == methodName);
        if (index < 0)
            throw new InvalidOperationException($"{comInterface.Name}.{methodName} not found");

        var interfaceType = comInterface.GetCustomAttributes(typeof(InterfaceTypeAttribute), false) is { Length: > 0 } attributes
            ? ((InterfaceTypeAttribute)attributes[0]).Value
            : ComInterfaceType.InterfaceIsDual;

        return (interfaceType == ComInterfaceType.InterfaceIsIUnknown ? 3 : 7) + index;
    }

    /// <summary>Cross-checks <see cref="SlotOf"/> against the runtime, where the runtime still has the API (.NET Framework).</summary>
    private static void VerifySlot(Type comInterface, string methodName, int slot)
    {
        var runtimeSlot = Marshal.GetComSlotForMethodInfo(comInterface.GetMethod(methodName));
        if (runtimeSlot != slot)
            throw new InvalidOperationException($"{comInterface.Name}.{methodName}: computed slot {slot}, runtime says {runtimeSlot}");
    }

    internal static void Report(IntPtr text, int color, int target)
    {
        var listener = _listener;
        if (listener == null || _inListener)
            return;

        _inListener = true;
        try
        {
            var value = text == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(text);
            listener(value.TrimEnd('\n', '\r'), color, target);
        }
        catch (Exception e)
        {
            // Never let anything escape into native code.  Not to chat either, since that would come back here.
            try
            {
                WrapperDebugWriter.WriteToStaticLog("REW", $"ChatTextCapture listener failed: {e}");
            }
            catch
            {
            }
        }
        finally
        {
            _inListener = false;
        }
    }

    /// <summary>One patched slot.  Reports, then forwards to whatever was in the slot before.</summary>
    internal sealed class Hook
    {
        private readonly ChatTextFn _original;

        public Hook(IntPtr vtable, int slot)
        {
            // Set up the call to the original before patching, so a call that arrives right after the patch can forward.
            var originalPointer = Marshal.ReadIntPtr(VTableSlotPatch.SlotAddressOf(vtable, slot));
            this._original = (ChatTextFn)Marshal.GetDelegateForFunctionPointer(originalPointer, typeof(ChatTextFn));

            ChatTextFn replacement = this.Invoke;
            this.Patch = VTableSlotPatch.Install(vtable, slot, replacement);
        }

        public VTableSlotPatch Patch { get; }

        private int Invoke(IntPtr self, IntPtr text, int color, int target)
        {
            Report(text, color, target);
            return this._original(self, text, color, target);
        }
    }
}
