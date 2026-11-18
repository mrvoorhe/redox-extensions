using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace RedoxExtensions.AgentBridge;

/// <summary>
/// One key, by the DirectInput name AC's keymap uses (<c>DIK_NUMPAD4</c>), with the scan code and virtual key code a
/// WM_KEYDOWN / WM_KEYUP message needs.
/// </summary>
public sealed class KeyCode
{
    public KeyCode(string name, int dik, int virtualKey)
    {
        this.Name = name;
        this.Dik = dik;
        this.VirtualKey = virtualKey;
    }

    /// <summary>The keymap name without the DIK_ prefix, e.g. NUMPAD4.</summary>
    public string Name { get; }

    /// <summary>The DirectInput key code.  It's the keyboard scan code, plus 0x80 for extended keys.</summary>
    public int Dik { get; }

    public int VirtualKey { get; }

    public int ScanCode => this.Dik & 0x7F;

    public bool Extended => (this.Dik & 0x80) != 0;

    /// <summary>
    /// The lParam of a WM_KEYDOWN (repeat count 1) or WM_KEYUP (previous state down, transition up) for this key.
    /// </summary>
    public uint LParam(bool up)
    {
        var value = 1u | ((uint)this.ScanCode << 16);
        if (this.Extended)
            value |= 1u << 24;
        if (up)
            value |= (1u << 30) | (1u << 31);
        return value;
    }
}

/// <summary>
/// Presses keys in the game by posting WM_KEYDOWN, then WM_KEYUP after the hold time, to the client's window, the way
/// MagTools' PostMessageTools does for <c>/mt send</c> and its jumps.  The window doesn't need focus.  Alt keys are
/// left out: they go through WM_SYSKEYDOWN.
/// </summary>
public static class KeyInput
{
    public const int MaxHoldMs = 5000;

    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;

    private static readonly Dictionary<string, KeyCode> Keys = BuildKeys();

    private static readonly object PendingLock = new();
    private static readonly List<Pending> PendingReleases = new();

    /// <summary>All the key names <see cref="TryParse"/> knows, without the DIK_ prefix.</summary>
    public static IEnumerable<string> Names => Keys.Keys;

    /// <summary>Looks up a key by its DirectInput name, with or without the DIK_ prefix, ignoring case.</summary>
    public static bool TryParse(string name, out KeyCode key)
    {
        key = null;
        if (string.IsNullOrEmpty(name))
            return false;

        var upper = name.Trim().ToUpperInvariant();
        if (upper.StartsWith("DIK_"))
            upper = upper.Substring(4);
        return Keys.TryGetValue(upper, out key);
    }

    /// <summary>
    /// Sends one window message.  Tests swap it out to watch the key downs and ups without a client window.
    /// </summary>
    internal static Action<IntPtr, uint, KeyCode, bool> Poster = PostToWindow;

    /// <summary>
    /// Posts the key down now and the key up after <paramref name="holdMs"/> on a timer, so the caller (the game
    /// thread) never waits.  PostMessage can be called from any thread.  Pressing a key that is still held from an
    /// earlier press doesn't press it again: it moves that key's release to <paramref name="holdMs"/> from now, so
    /// rectl can hold a key for long by renewing it, and the key still lets go on its own if rectl stops renewing.
    /// </summary>
    public static void Press(IntPtr hwnd, KeyCode key, int holdMs)
    {
        if (holdMs < 0 || holdMs > MaxHoldMs)
            throw new ArgumentException($"Hold time must be 0 to {MaxHoldMs} ms, not {holdMs}");

        Pending pending;
        lock (PendingLock)
        {
            pending = PendingReleases.Find(p => p.Hwnd == hwnd && p.Key.Name == key.Name);
            if (pending != null && holdMs > 0)
            {
                pending.Timer.Change(holdMs, Timeout.Infinite);
                return;
            }
        }

        if (pending != null)
        {
            Release(pending);
            return;
        }

        Poster(hwnd, WmKeyDown, key, false);
        if (holdMs == 0)
        {
            Poster(hwnd, WmKeyUp, key, true);
            return;
        }

        pending = new Pending(hwnd, key);
        lock (PendingLock)
        {
            PendingReleases.Add(pending);
            pending.Timer = new Timer(_ => Release(pending), null, holdMs, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Lets go of a key held by <see cref="Press"/> now, before its timer.  Returns false if it wasn't held.
    /// </summary>
    public static bool Release(IntPtr hwnd, KeyCode key)
    {
        Pending pending;
        lock (PendingLock)
            pending = PendingReleases.Find(p => p.Hwnd == hwnd && p.Key.Name == key.Name);
        return pending != null && Release(pending);
    }

    /// <summary>Whether a key pressed by <see cref="Press"/> is still held.</summary>
    public static bool IsHeld(IntPtr hwnd, KeyCode key)
    {
        lock (PendingLock)
            return PendingReleases.Exists(p => p.Hwnd == hwnd && p.Key.Name == key.Name);
    }

    /// <summary>Lets go of every key still held.  Called when the plugin unloads so no key stays down.</summary>
    public static void ReleaseAll()
    {
        List<Pending> all;
        lock (PendingLock)
            all = new List<Pending>(PendingReleases);
        foreach (var pending in all)
            Release(pending);
    }

    private static bool Release(Pending pending)
    {
        lock (PendingLock)
        {
            if (!PendingReleases.Remove(pending))
                return false;
            pending.Timer?.Dispose();
        }

        Poster(pending.Hwnd, WmKeyUp, pending.Key, true);
        return true;
    }

    private static void PostToWindow(IntPtr hwnd, uint message, KeyCode key, bool up)
    {
        if (!PostMessage(hwnd, message, (IntPtr)key.VirtualKey, (UIntPtr)key.LParam(up)))
            throw new InvalidOperationException($"PostMessage failed (error {Marshal.GetLastWin32Error()})");
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wparam, UIntPtr lparam);

    private sealed class Pending
    {
        public Pending(IntPtr hwnd, KeyCode key)
        {
            this.Hwnd = hwnd;
            this.Key = key;
        }

        public IntPtr Hwnd { get; }
        public KeyCode Key { get; }
        public Timer Timer { get; set; }
    }

    private static Dictionary<string, KeyCode> BuildKeys()
    {
        var keys = new Dictionary<string, KeyCode>();

        void Add(int dik, int vk, params string[] names)
        {
            foreach (var name in names)
                keys[name] = new KeyCode(names[0], dik, vk);
        }

        // Letters, in keyboard rows (DIK codes follow the scan codes, not the alphabet).
        // (No tuples: net35 has no ValueTuple.)
        var rows = new[] { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
        var firsts = new[] { 0x10, 0x1E, 0x2C };
        for (var row = 0; row < rows.Length; row++)
        {
            for (var i = 0; i < rows[row].Length; i++)
                Add(firsts[row] + i, rows[row][i], rows[row][i].ToString());
        }

        // 1-9 are 0x02-0x0A and 0 is 0x0B.
        for (var digit = 1; digit <= 9; digit++)
            Add(0x01 + digit, '0' + digit, digit.ToString());
        Add(0x0B, '0', "0");

        // F1-F10 are 0x3B-0x44; F11 and F12 come later.
        for (var f = 1; f <= 10; f++)
            Add(0x3A + f, 0x6F + f, "F" + f);
        Add(0x57, 0x7A, "F11");
        Add(0x58, 0x7B, "F12");

        Add(0x01, 0x1B, "ESCAPE");
        Add(0x0C, 0xBD, "MINUS");
        Add(0x0D, 0xBB, "EQUALS");
        Add(0x0E, 0x08, "BACK", "BACKSPACE");
        Add(0x0F, 0x09, "TAB");
        Add(0x1A, 0xDB, "LBRACKET");
        Add(0x1B, 0xDD, "RBRACKET");
        Add(0x1C, 0x0D, "RETURN", "ENTER");
        Add(0x1D, 0x11, "LCONTROL");
        Add(0x27, 0xBA, "SEMICOLON");
        Add(0x28, 0xDE, "APOSTROPHE");
        Add(0x29, 0xC0, "GRAVE");
        Add(0x2A, 0x10, "LSHIFT");
        Add(0x2B, 0xDC, "BACKSLASH");
        Add(0x33, 0xBC, "COMMA");
        Add(0x34, 0xBE, "PERIOD");
        Add(0x35, 0xBF, "SLASH");
        Add(0x36, 0x10, "RSHIFT");
        Add(0x39, 0x20, "SPACE");
        Add(0x3A, 0x14, "CAPITAL", "CAPSLOCK");

        // Numpad, with numlock on.
        Add(0x37, 0x6A, "MULTIPLY", "NUMPADSTAR");
        Add(0x45, 0x90, "NUMLOCK");
        Add(0x46, 0x91, "SCROLL");
        Add(0x47, 0x67, "NUMPAD7");
        Add(0x48, 0x68, "NUMPAD8");
        Add(0x49, 0x69, "NUMPAD9");
        Add(0x4A, 0x6D, "SUBTRACT", "NUMPADMINUS");
        Add(0x4B, 0x64, "NUMPAD4");
        Add(0x4C, 0x65, "NUMPAD5");
        Add(0x4D, 0x66, "NUMPAD6");
        Add(0x4E, 0x6B, "ADD", "NUMPADPLUS");
        Add(0x4F, 0x61, "NUMPAD1");
        Add(0x50, 0x62, "NUMPAD2");
        Add(0x51, 0x63, "NUMPAD3");
        Add(0x52, 0x60, "NUMPAD0");
        Add(0x53, 0x6E, "DECIMAL", "NUMPADPERIOD");
        Add(0x9C, 0x0D, "NUMPADENTER");
        Add(0xB5, 0x6F, "DIVIDE", "NUMPADSLASH");

        // Extended keys.
        Add(0x9D, 0x11, "RCONTROL");
        Add(0xC7, 0x24, "HOME");
        Add(0xC8, 0x26, "UP", "UPARROW");
        Add(0xC9, 0x21, "PRIOR", "PGUP");
        Add(0xCB, 0x25, "LEFT", "LEFTARROW");
        Add(0xCD, 0x27, "RIGHT", "RIGHTARROW");
        Add(0xCF, 0x23, "END");
        Add(0xD0, 0x28, "DOWN", "DOWNARROW");
        Add(0xD1, 0x22, "NEXT", "PGDN");
        Add(0xD2, 0x2D, "INSERT");
        Add(0xD3, 0x2E, "DELETE");
        Add(0xDB, 0x5B, "LWIN");
        Add(0xDC, 0x5C, "RWIN");

        return keys;
    }
}
