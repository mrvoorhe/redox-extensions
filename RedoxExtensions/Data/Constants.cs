using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedoxExtensions.Data
{
    public static class Constants
    {
        private static readonly Dictionary<string, int> _materialShortCutNamesToMaterialId;

        static Constants()
        {
            _materialShortCutNamesToMaterialId = new Dictionary<string, int>();

            InitializeMaterialShortCutTable();
        }

        public static Dictionary<string, int> MaterialShortCutNamesToIdTable
        {
            get
            {
                return _materialShortCutNamesToMaterialId;
            }
        }

        private static void InitializeMaterialShortCutTable()
        {
            foreach (var pair in Mag.Shared.Constants.Dictionaries.MaterialInfo)
            {
                foreach (var validName in GetValidNamesForMaterial(pair.Value))
                {
                    // Double check that the valid name scheme doesn't result in any ambiguous names
                    if (_materialShortCutNamesToMaterialId.ContainsKey(validName))
                    {
                        // Throwing in a static constructor isn't great, but this is a once and done thing.
                        // So if I see a crash, I know it's this.  If not, then this is okay
                        throw new InvalidOperationException(string.Format("Short material name already registered : {0}", validName));
                    }

                    _materialShortCutNamesToMaterialId.Add(validName, pair.Key);
                }
            }
        }

        private static IEnumerable<string> GetValidNamesForMaterial(string fullMaterialName)
        {
            // Example (1) :
            //    Material : Steel
            //    Valid Names :
            //         (1) steel
            //
            // Example (2) :
            //    Material : Green Garnet
            //    Valid names :
            //         (1) green garnet
            //         (2) greengarent
            //         (3) gg

            var lowered = fullMaterialName.ToLower();

            yield return lowered;

            var splitValue = lowered.Split(' ');
            if (splitValue.Length > 1)
            {
                // It's a multi-word material
                var oneWord = splitValue.Aggregate(string.Empty, (accum, next) => string.Format("{0}{1}", accum, next));
                var firstLetters = splitValue.Aggregate(string.Empty, (accum, next) => string.Format("{0}{1}", accum, next[0]));

                yield return oneWord;
                yield return firstLetters;
            }
        }
    }
}
