using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using NUnit.Framework;

using RedoxExtensions.Wrapper.AgentBridge;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
[NonParallelizable]
public class ChatTextCaptureTests
{
    private const int Slots = 16;
    private const int Slot = 11;

    private IntPtr _vtable;
    private ChatTextCapture.ChatTextFn _original;
    private readonly List<(IntPtr Self, string Text, int Color, int Target)> _originalCalls = new();

    [SetUp]
    public void SetUp()
    {
        this._originalCalls.Clear();
        this._vtable = Marshal.AllocHGlobal(Slots * IntPtr.Size);
        for (var i = 0; i < Slots; i++)
            Marshal.WriteIntPtr(this._vtable, i * IntPtr.Size, IntPtr.Zero);

        this._original = (self, text, color, target) =>
        {
            this._originalCalls.Add((self, Marshal.PtrToStringBSTR(text), color, target));
            return 0x1234;
        };
        Marshal.WriteIntPtr(this._vtable, Slot * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(this._original));
    }

    [TearDown]
    public void TearDown()
    {
        ChatTextCapture.Listener = null;
        Marshal.FreeHGlobal(this._vtable);
    }

    [Test]
    public void PatchReplacesOnlyTheSlotAndKeepsTheOriginal()
    {
        var before = Marshal.ReadIntPtr(this._vtable, Slot * IntPtr.Size);

        var hook = new ChatTextCapture.Hook(this._vtable, Slot);

        Assert.That(hook.Patch.OriginalPointer, Is.EqualTo(before));
        Assert.That(hook.Patch.IsInPlace, Is.True);
        Assert.That(Marshal.ReadIntPtr(this._vtable, Slot * IntPtr.Size), Is.EqualTo(hook.Patch.ReplacementPointer));
        Assert.That(Marshal.ReadIntPtr(this._vtable, (Slot - 1) * IntPtr.Size), Is.EqualTo(IntPtr.Zero));
        Assert.That(Marshal.ReadIntPtr(this._vtable, (Slot + 1) * IntPtr.Size), Is.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void CallsThroughTheSlotReachTheListenerAndThenTheOriginal()
    {
        var seen = new List<(string Text, int Color, int Target)>();
        ChatTextCapture.Listener = (text, color, target) => seen.Add((text, color, target));
        new ChatTextCapture.Hook(this._vtable, Slot);

        var result = this.CallSlot(new IntPtr(42), "[VTank] Macro started.\n", 5, 1);

        Assert.That(result, Is.EqualTo(0x1234));
        Assert.That(seen, Is.EqualTo(new[] { ("[VTank] Macro started.", 5, 1) }));
        Assert.That(this._originalCalls, Is.EqualTo(new[] { (new IntPtr(42), "[VTank] Macro started.\n", 5, 1) }));
    }

    [Test]
    public void WithoutAListenerTheCallIsOnlyForwarded()
    {
        new ChatTextCapture.Hook(this._vtable, Slot);

        Assert.That(this.CallSlot(IntPtr.Zero, "hello", 0, 0), Is.EqualTo(0x1234));
        Assert.That(this._originalCalls, Has.Count.EqualTo(1));
    }

    [Test]
    public void AThrowingListenerStillForwardsTheCall()
    {
        ChatTextCapture.Listener = (_, _, _) => throw new InvalidOperationException("boom");
        new ChatTextCapture.Hook(this._vtable, Slot);

        Assert.That(this.CallSlot(IntPtr.Zero, "hello", 0, 0), Is.EqualTo(0x1234));
        Assert.That(this._originalCalls, Has.Count.EqualTo(1));
    }

    [Test]
    public void ChatWrittenFromTheListenerIsNotReportedAgain()
    {
        var seen = new List<string>();
        new ChatTextCapture.Hook(this._vtable, Slot);
        ChatTextCapture.Listener = (text, _, _) =>
        {
            seen.Add(text);
            this.CallSlot(IntPtr.Zero, "from listener", 0, 0);
        };

        this.CallSlot(IntPtr.Zero, "outer", 0, 0);

        Assert.That(seen, Is.EqualTo(new[] { "outer" }));
        Assert.That(this._originalCalls, Has.Count.EqualTo(2));
    }

    [Test]
    [Platform("Win")]
    public void DecalHooksInterfaceHasTheExpectedChatTextSlots()
    {
        // The wrapper reads these slots from Decal's real interop type at runtime.  This pins the numbers it should get.
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "External", "Decal", "bin", "Decal.Interop.Core.DLL");
        var hooksInterface = Assembly.LoadFrom(Path.GetFullPath(path)).GetType("Decal.Interop.Core.IACHooks", true);

        Assert.That(ChatTextCapture.SlotOf(hooksInterface, "AddChatText"), Is.EqualTo(11));
        Assert.That(ChatTextCapture.SlotOf(hooksInterface, "AddChatTextRaw"), Is.EqualTo(12));
    }

    private int CallSlot(IntPtr self, string text, int color, int target)
    {
        var pointer = Marshal.ReadIntPtr(this._vtable, Slot * IntPtr.Size);
        var fn = Marshal.GetDelegateForFunctionPointer<ChatTextCapture.ChatTextFn>(pointer);
        var bstr = Marshal.StringToBSTR(text);
        try
        {
            return fn(self, bstr, color, target);
        }
        finally
        {
            Marshal.FreeBSTR(bstr);
        }
    }
}
