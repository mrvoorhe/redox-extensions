using System;
using System.Collections.Generic;
using System.Linq;

namespace RedoxLib.AutoMule
{
    /// <summary>
    /// The give rules for a single mule character.  Loaded from a "&lt;MuleName&gt;.txt" file where
    /// each line is a case-insensitive substring matched against inventory item names.
    /// </summary>
    public class MuleConfig
    {
        public MuleConfig(string muleName, IEnumerable<string> substrings)
        {
            this.MuleName = muleName;
            this.Substrings = substrings.ToList().AsReadOnly();
        }

        public string MuleName { get; private set; }

        public IList<string> Substrings { get; private set; }

        /// <summary>
        /// Parses the raw lines of a mule file.  Blank lines and lines starting with '#' (comments)
        /// are ignored; each remaining line is trimmed and used as a substring to match.
        /// </summary>
        public static MuleConfig Parse(string muleName, IEnumerable<string> lines)
        {
            var substrings = new List<string>();

            foreach (var rawLine in lines)
            {
                if (rawLine == null)
                {
                    continue;
                }

                var line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                substrings.Add(line);
            }

            return new MuleConfig(muleName, substrings);
        }

        /// <summary>
        /// Returns true if the item name contains any of this mule's configured substrings
        /// (case-insensitive).
        /// </summary>
        public bool Matches(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return false;
            }

            return this.Substrings.Any(s => itemName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
