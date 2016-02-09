using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Decal.Adapter.Wrappers;

namespace RedoxLib.GameValues
{
    public static class Sets
    {
        public const LongValueKey WorldObjectPropertyValue = LongValueKey.ArmorSet;

        public const int ValueSoldier = 13;
        public const int ValueAdept = 14;
        public const int ValueArcher = 15;
        public const int ValueDefender = 16;
        public const int ValueTinker = 17;
        public const int ValueCrafter = 18;
        public const int ValueHearty = 19;
        public const int ValueDexterous = 20;
        public const int ValueWise = 21;

        public enum Armor
        {
            Invalid = 0,
            Soldier = ValueSoldier,
            Adept = ValueAdept,
            Aracher = ValueArcher,
            Defender = ValueDefender,
            Tinker = ValueTinker,
            Crafter = ValueCrafter,
            Hearty = ValueHearty,
            Dexterous = ValueDexterous,
            Wise = ValueWise
        }

        private static readonly Dictionary<string, int> _armorShortCutNameToIdTable;

        static Sets()
        {
            _armorShortCutNameToIdTable = new Dictionary<string, int>();

            InitializeArmorShortCutTable();
        }

        public static Dictionary<string, int> ArmorShortCutNameToIdTable
        {
            get
            {
                return _armorShortCutNameToIdTable;
            }
        }

        public static bool TryParseArmorShortName(string shortName, out Armor armor)
        {
            int tmp;
            if (_armorShortCutNameToIdTable.TryGetValue(shortName, out tmp))
            {
                armor = (Armor)tmp;
                return true;
            }

            armor = Armor.Invalid;
            return false;
        }

        #region IdToNameTable

        /// <summary>
        /// Returns a dictionary of attribute set ids vs names
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, string> IdToNameTable = new Dictionary<int, string> {
            // This list was taken from Virindi Tank Loot Editor
            // 01
            // 02
            // 03
            // 04
            { 05, "Noble Relic Set" },
            { 06, "Ancient Relic Set" },
            { 07, "Relic Alduressa Set" },
            { 08, "Shou-jen Set" },
            { 09, "Empyrean Rings Set" },
            { 10, "Arm, Mind, Heart Set" },
            { 11, "Coat of the Perfect Light Set" },
            { 12, "Leggings of Perfect Light Set" },
            { 13, "Soldier's Set" },
            { 14, "Adept's Set" },
            { 15, "Archer's Set" },
            { 16, "Defender's Set" },
            { 17, "Tinker's Set" },
            { 18, "Crafter's Set" },
            { 19, "Hearty Set" },
            { 20, "Dexterous Set" },
            { 21, "Wise Set" },
            { 22, "Swift Set" },
            { 23, "Hardenend Set" },
            { 24, "Reinforced Set" },
            { 25, "Interlocking Set" },
            { 26, "Flame Proof Set" },
            { 27, "Acid Proof Set" },
            { 28, "Cold Proof Set" },
            { 29, "Lightning Proof Set" },
            { 30, "Dedication Set" },
            { 31, "Gladiatorial Clothing Set" },
            { 32, "Protective Clothing Set" },
            // 33
            // 34
            { 35, "Sigil of Defense" },
            { 36, "Sigil of Destruction" },
            { 37, "Sigil of Fury" },
            { 38, "Sigil of Growth" },
            { 39, "Sigil of Vigor" },
            { 40, "Heroic Protector Set" },
            { 41, "Heroic Destroyer Set" },
            // 42
            // 43
            // 44
            // 45
            // 46
            { 47, "Upgraded Ancient Relic Set" },
            // 48
            { 49, "Weave of Alchemy" },
            { 50, "Weave of Arcane Lore" },
            { 51, "Weave of Armor Tinkering" },
            { 52, "Weave of Assess Person" },
            { 53, "Weave of Light Weapons" },
            { 54, "Weave of Missile Weapons" },
            { 55, "Weave of Cooking" },
            { 56, "Weave of Creature Enchantment" },
            { 57, "Weave of Missile Weapons" },
            { 58, "Weave of Finesse" },
            { 59, "Weave of Deception" },
            { 60, "Weave of Fletching" },
            { 61, "Weave of Healing" },
            { 62, "Weave of Item Enchantment" },
            { 63, "Weave of Item Tinkering" },
            { 64, "Weave of Leadership" },
            { 65, "Weave of Life Magic" },
            { 66, "Weave of Loyalty" },
            { 67, "Weave of Light Weapons" },
            { 68, "Weave of Magic Defense" },
            { 69, "Weave of Magic Item Tinkering" },
            { 70, "Weave of Mana Conversion" },
            { 71, "Weave of Melee Defense" },
            { 72, "Weave of Missile Defense" },
            { 73, "Weave of Salvaging" },
            { 74, "Weave of Light Weapons" },
            { 75, "Weave of Light Weapons" },
            { 76, "Weave of Heavy Weapons" },
            { 77, "Weave of Missile Weapons" },
            { 78, "Weave of Two Handed Combat" },
            { 79, "Weave of Light Weapons" },
            { 80, "Weave of Void Magic" },
            { 81, "Weave of War Magic" },
            { 82, "Weave of Weapon Tinkering" },
            { 83, "Weave of Assess Creature " },
            { 84, "Weave of Dirty Fighting" },
            { 85, "Weave of Dual Wield" },
            { 86, "Weave of Recklessness" },
            { 87, "Weave of Shield" },
            { 88, "Weave of Sneak Attack" },
            // 89
            { 90, "Weave of Summoning" },
        };

        #endregion

        #region Private Static Methods

        private static void InitializeArmorShortCutTable()
        {
            _armorShortCutNameToIdTable.Add("soldier", ValueSoldier);
            _armorShortCutNameToIdTable.Add("sol", ValueSoldier);

            _armorShortCutNameToIdTable.Add("adept", ValueAdept);
            _armorShortCutNameToIdTable.Add("ade", ValueAdept);

            _armorShortCutNameToIdTable.Add("archer", ValueArcher);
            _armorShortCutNameToIdTable.Add("arc", ValueArcher);

            _armorShortCutNameToIdTable.Add("defender", ValueDefender);
            _armorShortCutNameToIdTable.Add("def", ValueDefender);

            _armorShortCutNameToIdTable.Add("tinker", ValueTinker);
            _armorShortCutNameToIdTable.Add("tink", ValueTinker);
            _armorShortCutNameToIdTable.Add("tin", ValueTinker);

            _armorShortCutNameToIdTable.Add("crafter", ValueCrafter);
            _armorShortCutNameToIdTable.Add("craft", ValueCrafter);
            _armorShortCutNameToIdTable.Add("cra", ValueCrafter);

            _armorShortCutNameToIdTable.Add("hearty", ValueHearty);
            _armorShortCutNameToIdTable.Add("hea", ValueHearty);

            _armorShortCutNameToIdTable.Add("dexterous", ValueDexterous);
            _armorShortCutNameToIdTable.Add("dex", ValueDexterous);

            _armorShortCutNameToIdTable.Add("wise", ValueWise);
            _armorShortCutNameToIdTable.Add("wis", ValueWise);

        }

        #endregion
    }
}
