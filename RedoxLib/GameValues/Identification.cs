using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Decal.Adapter.Wrappers;

namespace RedoxLib.GameValues
{
    public static class Identification
    {
        public static bool Required(WorldObject wo)
        {
            return Required(wo.ObjectClass, wo.Name);
        }

        public static bool Required(ObjectClass objectClass, string name)
        {
            if (objectClass == ObjectClass.Armor || objectClass == ObjectClass.Clothing ||
                objectClass == ObjectClass.MeleeWeapon || objectClass == ObjectClass.MissileWeapon || objectClass == ObjectClass.WandStaffOrb ||
                objectClass == ObjectClass.Jewelry ||
                (objectClass == ObjectClass.Gem && !String.IsNullOrEmpty(name) && name.Contains("Aetheria")) || // Aetheria are Gems
                (objectClass == ObjectClass.Misc && !String.IsNullOrEmpty(name) && name.Contains("Essence"))) // Essences (Summoning Gems) are Misc
                return true;

            return false;
        }
    }
}
