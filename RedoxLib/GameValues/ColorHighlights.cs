using System;
using System.Collections.Generic;

namespace RedoxLib.GameValues
{
    public struct ColorHighlight
    {
        public ColorHighlight(string name, bool isGreen)
            : this()
        {
            this.Name = name;
            this.IsGreen = isGreen;
        }

        public string Name { get; private set; }

        public bool IsGreen { get; private set; }

        public string Color
        {
            get { return this.IsGreen ? "green" : "red"; }
        }

        public override string ToString()
        {
            return $"{this.Name}={this.Color}";
        }
    }

    /// <summary>
    /// Decodes an assessment highlight enable-mask + color-mask pair into per-field
    /// highlights. The color bit means 0=red (lowered), 1=green (raised).
    /// </summary>
    public static class ColorHighlights
    {
        public static IList<ColorHighlight> Decode<TEnum>(ushort enableMask, ushort colorMask) where TEnum : struct
        {
            var results = new List<ColorHighlight>();
            foreach (var value in Enum.GetValues(typeof(TEnum)))
            {
                var bit = Convert.ToUInt16(value);
                if (bit == 0)
                {
                    continue;
                }

                if ((enableMask & bit) == bit)
                {
                    var isGreen = (colorMask & bit) == bit;
                    results.Add(new ColorHighlight(value.ToString(), isGreen));
                }
            }

            return results;
        }
    }
}
