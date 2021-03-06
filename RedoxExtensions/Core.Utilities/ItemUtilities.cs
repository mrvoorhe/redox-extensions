﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Decal.Adapter.Wrappers;

using RedoxLib.GameValues;

using RedoxExtensions.Core.Extensions;
using RedoxExtensions.Data;

namespace RedoxExtensions.Core.Utilities
{
    public static class ItemUtilities
    {
        #region Keywords

        public static ReadOnlyCollection<WorldObject> TryGetInventoryItemsForKeyword(string keyword, string secondOption)
        {
            return TryGetItemsForKeyword(REPlugin.Instance.WorldFilter.GetInventory(), keyword, secondOption);
        }

        public static ReadOnlyCollection<WorldObject> TryGetItemsForKeyword(WorldObjectCollection woCollection, string keyword, string secondOption)
        {
            var loweredKeyWord = keyword.ToLower();
            switch (loweredKeyWord)
            {
                case "keys":
                    return woCollection.GetLegendaryKeys();
                case "lvl8":
                    return woCollection.GetLevel8SpellComponents();
                case "set":
                case "sets":
                    if (string.IsNullOrEmpty(secondOption))
                    {
                        return null;
                    }

                    Sets.Armor armor;
                    if (Sets.TryParseArmorShortName(secondOption, out armor))
                    {
                        return woCollection.GetArmorSetItems(armor);
                    }

                    return null;
                case "cloak":
                case "cloaks":
                    return woCollection.GetCloaks();
                case "sigil":
                case "sigils":
                    return woCollection.GetSigils();
                case "weapon":
                case "weapons":
                    if (string.IsNullOrEmpty(secondOption))
                    {
                        return null;
                    }

                    switch (secondOption)
                    {
                        case "missile":
                            return woCollection.GetMissileWeapons();
                        case "melee":
                            return woCollection.GetMeleeWeapons();
                        case "magic":
                            return woCollection.GetMagicWeapons();
                    }

                    return null;
                case "jewelry":
                case "jew":
                    if (string.IsNullOrEmpty(secondOption))
                    {
                        return null;
                    }

                    switch (secondOption)
                    {
                        case "legendary":
                        case "leg":
                            return woCollection.GetJewelryWithLegendaryAndNoRatings();
                        case "legendary+ratings":
                        case "leg+ratings":
                        case "leg+":
                            return woCollection.GetJewelryWithLegendaryAndRatings();
                        case "ratings":
                            return woCollection.GetJewelryWithRatings();
                        case "max":
                            return woCollection.GetJewelryWithMaxRatings();
                    }

                    return null;

                default:
                    // See if it's one of the material short cut keywords
                    int possibleMaterialId;
                    if (Constants.MaterialShortCutNamesToIdTable.TryGetValue(loweredKeyWord, out possibleMaterialId))
                    {
                        return woCollection.GetFullSalvage(possibleMaterialId);
                    }

                    // It's not a known keyword
                    return null;
            }
        }

        #endregion
    }
}
