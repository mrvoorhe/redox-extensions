using System;

namespace RedoxLib.GameValues
{
    [Flags]
    public enum ArmorHighlightMask : ushort
    {
        ArmorLevel = 0x0001,
        Slashing = 0x0002,
        Piercing = 0x0004,
        Bludgeoning = 0x0008,
        Cold = 0x0010,
        Fire = 0x0020,
        Acid = 0x0040,
        Electrical = 0x0080,
    }

    [Flags]
    public enum AttributeHighlightMask : ushort
    {
        Strength = 0x0001,
        Endurance = 0x0002,
        Quickness = 0x0004,
        Coordination = 0x0008,
        Focus = 0x0010,
        Self = 0x0020,
        Health = 0x0040,
        Stamina = 0x0080,
        Mana = 0x0100,
    }

    [Flags]
    public enum WeaponHighlightMask : ushort
    {
        AttackSkill = 0x0001,
        MeleeDefense = 0x0002,
        Speed = 0x0004,
        Damage = 0x0008,
    }

    [Flags]
    public enum WandHighlightMask : ushort
    {
        ManaConversionBonus = 0x1000,
    }
}
