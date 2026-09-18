using System;

namespace ZombieApocalypse.Inventory
{
    /// <summary>
    /// Holds reference to an ItemData asset and current slot stack quantity.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public ItemData itemData;
        public int quantity;

        public InventorySlot(ItemData item, int qty)
        {
            itemData = item;
            quantity = qty;
        }
    }
}
