using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.VirindiInterop
{
    public static class VTOptionNames
    {
        public const string EnableLooting = "EnableLooting";
        public const string EnableNav = "EnableNav";
        public const string EnableBuffing = "EnableBuffing";
        public const string EnableCombat = "EnableCombat";
        public const string EnableMeta = "EnableMeta";

        public static readonly string[] AllStateOptions = new[] { EnableCombat, EnableNav, EnableBuffing, EnableLooting, EnableMeta };

        public const string AttackDistance = "AttackDistance";

        public const string NavCloseStopRange = "NavCloseStopRange";
        public const string NavFarStopRange = "NavFarStopRange";
        public const string NavPriorityBoost = "NavPriorityBoost";
        public const string UsePortalDistance = "UsePortalDistance";

        public const string SummonPets = "SummonPets";
        public const string PetRangeMode = "PetRangeMode";
        public const string PetCustomRange = "PetCustomRange";
        public const string PetMonsterDensity = "PetMonsterDensity";
        public const string PetRefillCountIdle = "PetRefillCount-Idle";
        public const string PetRefillCountNormal = "PetRefillCount-Normal";

        public const string DebuffEachFirst = "DebuffEachFirst";

        public const string AutoCram = "AutoCram";
    }

    public enum DebuffEachFirstMode
    {
        /// <summary>
        /// ebuffs one monster then attacks it
        /// </summary>
        One = 1,

        /// <summary>
        /// debuffs all monsters of the same priority then attacks
        /// </summary>
        Priority = 2,

        /// <summary>
        /// debuffs all monsters before attacking regardless of 
        /// </summary>
        All = 3
    }
}
