using System;
using System.Collections.Generic;
using System.Text;

namespace RedoxExtensions.Data
{
    public enum BusyStateValues
    {
        UnequipingItem = BusyStateConstants.UnequipingItem,
        EquipingItem = BusyStateConstants.EquipingItem,
        GivingItem = BusyStateConstants.GivingItem
    }

    public static class BusyStateConstants
    {
        public const int UnequipingItem = 5;
        public const int EquipingItem = 7;
        public const int GivingItem = 9;
    }
}
