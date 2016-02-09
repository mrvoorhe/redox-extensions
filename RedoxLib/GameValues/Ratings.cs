using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxLib.GameValues
{
    public static class Ratings
    {
        public enum Type
        {
            None = 0,
            DamRating = 370,
            DamResistRating = 371,
            CritRating = 372,
            CritResistRating = 373,
            CritDamRating = 374,
            CritDamResistRating = 375,

            HealBoostRating = 376,
            VitalityRating = 379
        }

        public struct Information
        {
            public readonly Type Type;
            public readonly int Value;

            public Information(int value, Type type)
            {
                Value = value;
                Type = type;
            }
        }

        public static readonly IntValueKey[] AllRatingsIntValueKeys = new[]
        {
            IntValueKey.DamRating,
            IntValueKey.DamResistRating,
            IntValueKey.CritRating,
            IntValueKey.CritResistRating,
            IntValueKey.CritDamRating,
            IntValueKey.DamResistRating,
            IntValueKey.HealBoostRating,
            IntValueKey.VitalityRating
        };
    }
}
