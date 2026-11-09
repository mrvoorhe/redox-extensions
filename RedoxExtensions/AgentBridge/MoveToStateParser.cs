using System;

namespace RedoxExtensions.AgentBridge;

/// <summary>
/// Reads the movement intent out of the client's MoveToState game action (F7B1 / F61C), which the client sends every
/// time the movement keys change, including when VT drives them.  Read from the raw bytes: a RawMotionState is a packed
/// flags DWORD followed by one DWORD per flag present, in flag order.
/// </summary>
internal static class MoveToStateParser
{
    public const int GameActionType = 0xF7B1;
    public const int MoveToStateAction = 0xF61C;

    private const uint CurrentHoldKey = 0x001;
    private const uint CurrentStyle = 0x002;
    private const uint ForwardCommand = 0x004;
    private const uint SidestepCommand = 0x020;
    private const uint TurnCommand = 0x100;

    private const uint HoldKeyRun = 2;
    private const uint WalkForward = 0x05;
    private const uint WalkBackward = 0x06;
    private const uint RunForward = 0x07;

    /// <param name="raw">Decal's <c>Message.RawData</c>: type, sequence, action, payload.  Also accepted without the type.</param>
    public static bool TryParse(byte[] raw, out MotionIntent intent)
    {
        intent = default;
        if (raw == null || raw.Length < 12)
            return false;

        int offset;
        if (ReadUInt(raw, 0) == GameActionType && raw.Length >= 16 && ReadUInt(raw, 8) == MoveToStateAction)
            offset = 12;
        else if (ReadUInt(raw, 4) == MoveToStateAction)
            offset = 8;
        else
            return false;

        var flags = ReadUInt(raw, offset) & 0x7FF;
        offset += 4;

        uint holdKey = 0;
        if ((flags & CurrentHoldKey) != 0)
        {
            if (raw.Length < offset + 4)
                return false;
            holdKey = ReadUInt(raw, offset);
            offset += 4;
        }

        if ((flags & CurrentStyle) != 0)
            offset += 4;

        if ((flags & ForwardCommand) != 0)
        {
            if (raw.Length < offset + 4)
                return false;
            switch (ReadUInt(raw, offset) & 0xFFFF)
            {
                case WalkForward:
                case RunForward:
                    intent.Forward = true;
                    break;
                case WalkBackward:
                    intent.Backward = true;
                    break;
            }
        }

        intent.Sidestep = (flags & SidestepCommand) != 0;
        intent.Turn = (flags & TurnCommand) != 0;
        intent.Run = holdKey == HoldKeyRun;
        return true;
    }

    private static uint ReadUInt(byte[] raw, int offset) => BitConverter.ToUInt32(raw, offset);
}
