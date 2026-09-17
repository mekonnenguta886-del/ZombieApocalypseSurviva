using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieApocalypse.Inventory
{
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

    /// <summary>
    /// Manages player inventory slots, item pickup, drop, and usage.
    /// 
    /// ATTACH TO: Player prefab or Persistent Inventory Manager GameObject.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public event Action OnInventoryUpdated;

        [Header("Capacity")]
        [SerializeField] private int maxSlots = 20;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        public IReadOnlyList<InventorySlot> Slots => slots;
        public int MaxSlots => maxSlots;

        public bool AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            // Stack handling logic
            if (item.isStackable)
            {
                foreach (var slot in slots)
                {
                    if (slot.itemData == item && slot.quantity < item.maxStackSize)
                    {
                        int addable = Mathf.Min(amount, item.maxStackSize - slot.quantity);
                        slot.quantity += addable;
                        amount -= addable;

                        if (amount <= 0)
                        {
                            OnInventoryUpdated?.Invoke();
                            return true;
                        }
                    }
                }
            }

            // Add new slot
            if (slots.Count < maxSlots)
            {
                slots.Add(new InventorySlot(item, amount));
                OnInventoryUpdated?.Invoke();
                return true;
            }

            Debug.LogWarning("[InventoryManager] Inventory full!");
            return false;
        }
    }
}
