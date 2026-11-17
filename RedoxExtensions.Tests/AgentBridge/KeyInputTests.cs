using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using RedoxExtensions.AgentBridge;
using RedoxExtensions.AgentCli;

namespace RedoxExtensions.Tests.AgentBridge;

[TestFixture]
public class KeyInputTests
{
    [Test]
    public void NumpadKeyHasItsScanCodeAndNumpadVirtualKey()
    {
        Assert.That(KeyInput.TryParse("DIK_NUMPAD4", out var key), Is.True);

        Assert.That(key.Name, Is.EqualTo("NUMPAD4"));
        Assert.That(key.ScanCode, Is.EqualTo(0x4B));
        Assert.That(key.VirtualKey, Is.EqualTo(0x64));
        Assert.That(key.Extended, Is.False);
        Assert.That(key.LParam(up: false), Is.EqualTo(0x004B0001u));
        Assert.That(key.LParam(up: true), Is.EqualTo(0xC04B0001u));
    }

    [Test]
    public void ArrowKeyIsExtendedAndKnownByBothKeymapNames()
    {
        Assert.That(KeyInput.TryParse("left", out var left), Is.True);
        Assert.That(KeyInput.TryParse("DIK_RIGHTARROW", out var right), Is.True);

        Assert.That(left.ScanCode, Is.EqualTo(0x4B));
        Assert.That(left.Extended, Is.True);
        Assert.That(left.VirtualKey, Is.EqualTo(0x25));
        Assert.That(left.LParam(up: false), Is.EqualTo(0x014B0001u));
        Assert.That(right.Name, Is.EqualTo("RIGHT"));
    }

    [Test]
    public void LettersFollowTheKeyboardRows()
    {
        Assert.That(KeyInput.TryParse("W", out var w), Is.True);
        Assert.That(KeyInput.TryParse("z", out var z), Is.True);

        // The same scan codes MagTools posts for its jumps.
        Assert.That(w.ScanCode, Is.EqualTo(0x11));
        Assert.That(w.VirtualKey, Is.EqualTo('W'));
        Assert.That(z.ScanCode, Is.EqualTo(0x2C));
    }

    [Test]
    public void EveryKeyNameInTheDefaultKeymapIsKnownExceptAlt()
    {
        // Every DIK name in a stock Throne of Destiny keymap.  Alt keys are left out on purpose (WM_SYSKEYDOWN).
        var names = "0 1 2 3 4 5 6 7 8 9 A APOSTROPHE B BACK BACKSLASH C COMMA D DECIMAL DELETE DOWNARROW E END EQUALS ESCAPE " +
                    "F F1 F10 F11 F12 F2 F3 F4 F5 F6 F8 F9 G GRAVE H HOME I INSERT J K L LBRACKET LCONTROL LEFT LSHIFT " +
                    "LWIN M MINUS N NUMPAD0 NUMPAD2 NUMPAD4 NUMPAD5 NUMPAD6 NUMPAD8 NUMPADENTER NUMPADMINUS NUMPADPLUS " +
                    "NUMPADSLASH NUMPADSTAR O P PERIOD PGDN PGUP Q R RBRACKET RCONTROL RETURN RIGHTARROW RWIN S SEMICOLON " +
                    "SLASH SPACE T TAB U UPARROW V W X Y Z";

        var unknown = names.Split(' ').Where(n => !KeyInput.TryParse("DIK_" + n, out _)).ToList();

        Assert.That(unknown, Is.Empty);
        Assert.That(KeyInput.TryParse("DIK_LMENU", out _), Is.False);
    }

    [Test]
    public void UnknownKeyIsRejected()
    {
        Assert.That(KeyInput.TryParse("NUMPAD42", out _), Is.False);
        Assert.That(KeyInput.TryParse("", out _), Is.False);
    }

    private static readonly string[] KeymapLines =
    {
        "  CameraControls",
        "  [",
        "    CameraActivateAlternateMode [ \"\" [ 0 DIK_NUMPADSLASH ] ]",
        "    CameraInstantMouseLook [ \"\" [ 1 DIMOFS_BUTTON2 ] ]",
        "    CameraRotateLeft [ \"\" [ 0 DIK_NUMPAD4 ] ]",
        "    CameraMoveAway [ \"\" [ 0 DIK_EQUALS 0 0x00000001 ] ]",
        "    CameraMoveAway [ \"\" [ 0 DIK_NUMPADPLUS ] ]",
        "  ]",
        "",
        "  CameraAlternateControls",
        "  [",
        "    CameraRotateLeft [ \"\" [ 0 DIK_LEFT ] ]",
        "    CameraRotateUp [ \"\" [ 0 DIK_UPARROW ] ]",
        "  ]",
    };

    [Test]
    public void KeymapPrefersTheNamedSection()
    {
        Assert.That(Keymap.FindKey(KeymapLines, "CameraRotateLeft", "CameraControls"), Is.EqualTo("DIK_NUMPAD4"));

        // Alternate section first: the preferred one still wins.
        var alternateFirst = KeymapLines.Skip(9).Concat(new[] { "" }).Concat(KeymapLines.Take(9));
        Assert.That(Keymap.FindKey(alternateFirst, "CameraRotateLeft", "CameraControls"), Is.EqualTo("DIK_NUMPAD4"));
    }

    [Test]
    public void KeymapFallsBackToAnotherSection()
    {
        Assert.That(Keymap.FindKey(KeymapLines, "CameraRotateUp", "CameraControls"), Is.EqualTo("DIK_UPARROW"));
    }

    [Test]
    public void KeymapSkipsBindingsWithMetakeysAndOtherDevices()
    {
        Assert.That(Keymap.FindKey(KeymapLines, "CameraMoveAway", "CameraControls"), Is.EqualTo("DIK_NUMPADPLUS"));
        Assert.That(Keymap.FindKey(KeymapLines, "CameraInstantMouseLook", "CameraControls"), Is.Null);
        Assert.That(Keymap.FindKey(KeymapLines, "CameraViewMapMode", "CameraControls"), Is.Null);
    }

    [Test]
    public void EveryCameraMoveIsAnActionInTheStockKeymap()
    {
        var stock = new HashSet<string>
        {
            "CameraRotateLeft", "CameraRotateRight", "CameraRotateUp", "CameraRotateDown", "CameraMoveToward",
            "CameraMoveAway", "CameraViewDefault", "CameraViewFirstPerson", "CameraViewLookDown",
        };

        Assert.That(Cli.CameraActions.Values, Is.SubsetOf(stock));
    }
}
