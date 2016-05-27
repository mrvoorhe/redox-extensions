using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

using RedoxLib.GameValues;

using RedoxExtensions.Data;
using RedoxLib.Objects;
using RedoxLib.Objects.Extensions;

namespace RedoxExtensions.Core.Extensions
{
    public static class WorldObjectExtensions
    {
        #region Logging & Debug

        public static string ToShortSummary(this WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return "NULL";
            }

            return string.Format("[{0}] {1}, {2}", worldObject.ObjectClass, worldObject.Name, worldObject.Id);
        }

        public static string ToShortSummary(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return "NULL";
            }

            return string.Format("[{0}] {1}, {2}", worldObject.ObjectClass, worldObject.Name, worldObject.Id);
        }

        public static void WriteToDebug(this WorldObject worldObject, string message)
        {
            WriteToDebug(worldObject.Wrap(), message);
        }

        public static void WriteToDebug(this IWorldObject worldObject, string message)
        {
            REPlugin.Instance.Debug.WriteLine(string.Format("{0} : {1}", message, worldObject.ToShortSummary()));
        }

        #endregion

        #region Basic Is and Has Shortcuts

        public static bool IsWandStaffOrb(this WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.WandStaffOrb;
        }

        public static bool IsWandStaffOrb(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.WandStaffOrb;
        }

        public static bool IsMissileWeapon(this WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.MissileWeapon;
        }

        public static bool IsMissileWeapon(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.MissileWeapon;
        }

        public static bool IsMeleeWeapon(this WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.MeleeWeapon;
        }

        public static bool IsMeleeWeapon(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.MeleeWeapon;
        }

        public static bool IsJewelry(this WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.Jewelry;
        }

        public static bool IsJewelry(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.Jewelry;
        }

        public static bool IsNpc(this WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.Npc;
        }

        public static bool IsNpc(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.ObjectClass == ObjectClass.Npc;
        }

        public static bool IsSelf(this WorldObject worldObject)
        {
            if(worldObject == null)
            {
                return false;
            }

            return worldObject.Id == REPlugin.Instance.CharacterFilter.Id;
        }

        public static bool IsSelf(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.Id == REPlugin.Instance.CharacterFilter.Id;
        }

        public static bool IsKey(this WorldObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj.Name.Contains("Key");
        }

        public static bool IsKey(this IWorldObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj.Name.Contains("Key");
        }

        public static bool IsLegendaryKey(this WorldObject obj)
        {
            return IsLegendaryKey(obj.Wrap());
        }

        public static bool IsLegendaryKey(this IWorldObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj.Name.Contains("Legendary Key");
        }

        public static bool IsLevel8SpellComponent(this WorldObject obj)
        {
            return IsLevel8SpellComponent(obj.Wrap());
        }

        public static bool IsLevel8SpellComponent(this IWorldObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.ObjectClass != ObjectClass.Misc && obj.ObjectClass != ObjectClass.CraftedAlchemy)
            {
                return false;
            }

            if (obj.Name.StartsWith("Quill of") || obj.Name.StartsWith("Quills of"))
            {
                return true;
            }

            if (obj.Name.StartsWith("Ink of") || obj.Name.EndsWith("Ink"))
            {
                return true;
            }

            if (obj.Name.StartsWith("Glyph of"))
            {
                return true;
            }

            return false;
        }

        public static bool IsFullSalvage(this WorldObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.ObjectClass == ObjectClass.Salvage)
            {
                var usesRemaining = obj.Values(LongValueKey.UsesRemaining);
                return usesRemaining == 100;
            }

            return false;
        }

        public static bool IsMaterial(this WorldObject wo, int materialId)
        {
            var woMaterialId = wo.Values(LongValueKey.Material);

            // object has no material property
            if (woMaterialId == 0)
            {
                return false;
            }

            return woMaterialId == materialId;
        }

        public static bool IsSet(this WorldObject wo, int setId)
        {
            var woSetId = wo.Values(LongValueKey.ArmorSet, 0);

            if (woSetId == 0)
            {
                return false;
            }

            return woSetId == setId;
        }

        public static bool IsCloak(this WorldObject wo)
        {
            var woSetId = wo.Values(LongValueKey.ArmorSet, 0);

            return woSetId >= Sets.CloakFirstSetId && woSetId <= Sets.CloakLastSetId;
        }

        public static bool IsSigil(this WorldObject wo)
        {
            var woSetId = wo.Values(LongValueKey.ArmorSet, 0);

            return woSetId >= Sets.SigilFirstSetId && woSetId <= Sets.SigilLastSetId;
        }

        #endregion

        #region Other Shortcuts

        public static void Give(this WorldObject obj, WorldObject targetObj)
        {
            REPlugin.Instance.Actions.GiveItem(obj.Id, targetObj.Id);
        }

        public static void Give(this WorldObject obj, int targetId)
        {
            REPlugin.Instance.Actions.GiveItem(obj.Id, targetId);
        }

        #endregion

        #region Enhanced Functionality

        /// <summary>
        /// Copied from MagTools
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsEquippedByMe(this WorldObject wo)
        {
            if (wo == null)
            {
                return false;
            }

            return wo.Wrap().IsEquippedByMe();
        }

        /// <summary>
        /// Copied from MagTools
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsEquippedByMe(this IWorldObject wo)
        {
            if (wo == null)
            {
                return false;
            }

            if (wo.Values(LongValueKey.EquippedSlots) <= 0)
                return false;

            // Weapons are in the -1 slot
            if (wo.Values(LongValueKey.Slot, -1) == -1)
                return (wo.Container == REPlugin.Instance.CharacterFilter.Id);

            return true;
        }

        /// <summary>
        /// True if an item is equipped or in your inventory
        /// </summary>
        /// <param name="worldObject"></param>
        /// <returns></returns>
        public static bool InMyPossession(this WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.Wrap().InMyPossession();
        }

        /// <summary>
        /// True if an item is equipped or in your inventory
        /// </summary>
        /// <param name="worldObject"></param>
        /// <returns></returns>
        public static bool InMyPossession(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.IsEquippedByMe() || worldObject.IsInInventory();
        }

        /// <summary>
        /// Returns true if an item is in your inventory
        /// </summary>
        /// <param name="worldObject"></param>
        /// <returns></returns>
        public static bool IsInInventory(this WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            return worldObject.Wrap().IsInInventory();
        }

        /// <summary>
        /// Returns true if an item is in your inventory
        /// </summary>
        /// <param name="worldObject"></param>
        /// <returns></returns>
        public static bool IsInInventory(this IWorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            foreach (WorldObject invetoryWorldObject in REPlugin.Instance.CoreManager.WorldFilter.GetInventory())
            {
                if (worldObject.Id == invetoryWorldObject.Id)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsUngiveable(this WorldObject worldObject)
        {
            throw new NotImplementedException();
        }

        public static Ratings.Information GetRatings(this IWorldObject wo)
        {
            foreach (var rating in Ratings.AllRatingsIntValueKeys)
            {
                int value;
                if (wo.TryGetValue(rating, out value) && value > 0)
                {
                    return new Ratings.Information(value, (Ratings.Type)rating);
                }
            }

            return new Ratings.Information(0, Ratings.Type.None);
        }

        public static bool HasRatings(this IWorldObject wo)
        {
            return wo.GetRatings().Type != Ratings.Type.None;
        }

        public static bool IsLegendary(this IWorldObject wo)
        {
            throw new NotImplementedException();
        }

        public static bool RequiresIdentification(this WorldObject wo)
        {
            return Identification.Required(wo);
        }

        #endregion

        #region WorldObject Conversions

        public static IWorldObject Capture(this IWorldObject wo)
        {
            return wo.Capture(REPlugin.Instance);
        }

        #endregion


        #region WorldObject as Integer

        public static IWorldObject ToWorldObject(this int objectId)
        {
            return objectId.ToWorldObject(REPlugin.Instance).Wrap();
        }

        public static bool IsSelf(this int objectId)
        {
            return objectId == REPlugin.Instance.CharacterFilter.Id;
        }

        #endregion

        #region Collection Helpers

        public static ReadOnlyCollection<WorldObject> GetLevel8SpellComponents(this WorldObjectCollection collection)
        {
            return collection.Where(w => w.IsLevel8SpellComponent()).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetLegendaryKeys(this WorldObjectCollection collection)
        {
            return collection.Where(w => w.IsLegendaryKey()).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetAllFullSalvage(this WorldObjectCollection collection)
        {
            return collection.Where(w => w.IsFullSalvage()).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetFullSalvage(this WorldObjectCollection collection, int materialId)
        {
            return collection.GetAllFullSalvage().Where(w => w.IsMaterial(materialId)).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetSalvage(this WorldObjectCollection collection, int materialId)
        {
            throw new NotImplementedException();
        }

        public static ReadOnlyCollection<WorldObject> GetItemsByName(this WorldObjectCollection collection, string name)
        {
            return collection.Where(w => w.Name == name).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetInventoryItemsByName(string name)
        {
            throw new NotImplementedException();
        }

        public static ReadOnlyCollection<WorldObject> GetMatchInventoryItems(Func<WorldObject, bool> predicate)
        {
            return REPlugin.Instance.WorldFilter.GetInventory().Where(predicate).ToList().AsReadOnly();
        }

        #region Sets

        public static ReadOnlyCollection<WorldObject> GetArmorSetItems(this WorldObjectCollection collection, Sets.Armor value)
        {
            return collection.Where(w => w.IsSet((int)value)).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetCloaks(this WorldObjectCollection collection)
        {
            return collection.Where(w => w.IsCloak()).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetSigils(this WorldObjectCollection collection)
        {
            return collection.Where(w => w.IsSigil()).ToList().AsReadOnly();
        }

        #endregion

        #region Weapons

        public static ReadOnlyCollection<WorldObject> GetMissileWeapons(this WorldObjectCollection collection)
        {
            return collection.Where(w => w.IsMissileWeapon()).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetMeleeWeapons(this WorldObjectCollection collection)
        {
            return collection.Where(w => w.IsMeleeWeapon()).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetMagicWeapons(this WorldObjectCollection collection)
        {
            return collection.Where(w => w.IsWandStaffOrb()).ToList().AsReadOnly();
        }

        #endregion

        #region Jewlery & Clothing

        public static ReadOnlyCollection<WorldObject> GetJewelryWith(this WorldObjectCollection collection, Func<IWorldObject, bool> predicate)
        {
            return collection.Where(w => w.IsJewelry() && predicate(w.Wrap())).ToList().AsReadOnly();
        }

        public static ReadOnlyCollection<WorldObject> GetJewelryWithRatings(this WorldObjectCollection collection)
        {
            return collection.GetJewelryWith(wo => wo.HasRatings());
        }

        public static ReadOnlyCollection<WorldObject> GetJewelryWithMaxRatings(this WorldObjectCollection collection)
        {
            return collection.GetJewelryWith(wo => wo.GetRatings().Value == 3);
        }

        public static ReadOnlyCollection<WorldObject> GetJewelryWithLegendaryAndRatings(this WorldObjectCollection collection)
        {
            return collection.GetJewelryWith(wo => wo.HasRatings() && wo.IsLegendary());
        }

        public static ReadOnlyCollection<WorldObject> GetJewelryWithLegendaryAndNoRatings(this WorldObjectCollection collection)
        {
            return collection.GetJewelryWith(wo => !wo.HasRatings() && wo.IsLegendary());
        }

        #endregion

        #endregion
    }
}
