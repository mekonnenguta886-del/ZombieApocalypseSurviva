using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Player;
using ZombieApocalypse.Weapons;

namespace ZombieApocalypse.Inventory
{
    /// <summary>
    /// UI-independent central inventory manager supporting partial stack handling,
    /// configurable slot capacity (default 20), consumable usage, ammo replenishment,
    /// item dropping, and death state safety.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        public event Action OnInventoryUpdated;
        public event Action<string> OnInventoryNotification; // e.g. "+ Bandage x2", "Inventory Full"
        public event Action<ItemData, int> OnItemAdded;

        [Header("Inventory Capacity")]
        [SerializeField] private int maxSlots = 20;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        // System References
        private PlayerHealth playerHealth;
        private PlayerSurvivalStats survivalStats;
        private WeaponController weaponController;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public int MaxSlots => maxSlots;
        public int OccupiedSlots => slots.Count;
        public bool IsFull => slots.Count >= maxSlots;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            survivalStats = GetComponent<PlayerSurvivalStats>();
            weaponController = GetComponent<WeaponController>();
        }

        /// <summary>
        /// Attempts to add quantity of item to inventory. Returns actual amount added (0 if full or invalid).
        /// Handles partial stack filling without losing remaining items.
        /// </summary>
        public int AddItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return 0;
            if (playerHealth != null && playerHealth.IsDead) return 0;

            int initialAmount = amount;
            int addedCount = 0;

            // Step 1: Add to existing stackable slots
            if (item.isStackable)
            {
                foreach (var slot in slots)
                {
                    if (slot.itemData == item && slot.quantity < item.maxStackSize)
                    {
                        int addable = Mathf.Min(amount - addedCount, item.maxStackSize - slot.quantity);
                        slot.quantity += addable;
                        addedCount += addable;

                        if (addedCount >= amount)
                        {
                            OnInventoryNotification?.Invoke($"+ {item.itemName} x{addedCount}");
                            OnItemAdded?.Invoke(item, addedCount);
                            OnInventoryUpdated?.Invoke();
                            return addedCount;
                        }
                    }
                }
            }

            // Step 2: Add to new inventory slots if capacity remains
            while (addedCount < amount && slots.Count < maxSlots)
            {
                int spaceRemaining = amount - addedCount;
                int qtyForNewSlot = item.isStackable ? Mathf.Min(spaceRemaining, item.maxStackSize) : 1;
                slots.Add(new InventorySlot(item, qtyForNewSlot));
                addedCount += qtyForNewSlot;

                if (addedCount >= amount)
                {
                    OnInventoryNotification?.Invoke($"+ {item.itemName} x{addedCount}");
                    OnItemAdded?.Invoke(item, addedCount);
                    OnInventoryUpdated?.Invoke();
                    return addedCount;
                }
            }

            if (addedCount > 0)
            {
                OnInventoryNotification?.Invoke($"+ {item.itemName} x{addedCount}");
                OnItemAdded?.Invoke(item, addedCount);
                OnInventoryUpdated?.Invoke();
            }

            if (addedCount < initialAmount)
            {
                OnInventoryNotification?.Invoke("Inventory Full");
                Debug.LogWarning($"[InventorySystem] Partial add: added {addedCount}/{initialAmount} of {item.itemName}. Inventory full.");
            }

            return addedCount;
        }

        /// <summary>
        /// Checks if inventory contains at least specified quantity of item.
        /// </summary>
        public bool HasItem(ItemData item, int qty = 1)
        {
            if (item == null) return false;
            return GetItemQuantity(item) >= qty;
        }

        /// <summary>
        /// Returns total count of an item in the inventory across all slots.
        /// </summary>
        public int GetItemQuantity(ItemData item)
        {
            if (item == null) return 0;
            int total = 0;
            foreach (var slot in slots)
            {
                if (slot != null && slot.itemData == item)
                {
                    total += slot.quantity;
                }
            }
            return total;
        }

        /// <summary>
        /// Uses 1 item from the specified inventory slot.
        /// </summary>
        public bool UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            if (playerHealth != null && playerHealth.IsDead) return false;

            InventorySlot slot = slots[slotIndex];
            if (slot == null || slot.itemData == null || slot.quantity <= 0) return false;

            ItemData data = slot.itemData;
            bool success = false;

            switch (data.category)
            {
                case ItemCategory.Medical:
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(data.healthRestore);
                        success = true;
                    }
                    break;

                case ItemCategory.Food:
                    if (survivalStats != null)
                    {
                        survivalStats.RestoreHunger(data.hungerRestore);
                        success = true;
                    }
                    break;

                case ItemCategory.Water:
                    if (survivalStats != null)
                    {
                        survivalStats.RestoreThirst(data.thirstRestore);
                        success = true;
                    }
                    break;

                case ItemCategory.Ammunition:
                    if (weaponController != null)
                    {
                        success = weaponController.AddReserveAmmo(data.ammoType, data.ammoAmount);
                    }
                    break;

                case ItemCategory.Miscellaneous:
                    Debug.Log($"[InventorySystem] Used miscellaneous item: {data.itemName}");
                    success = true;
                    break;
            }

            if (success && data.isConsumable)
            {
                slot.quantity--;
                if (slot.quantity <= 0)
                {
                    slots.RemoveAt(slotIndex);
                }
                OnInventoryUpdated?.Invoke();
            }

            return success;
        }

        /// <summary>
        /// Removes a quantity of items from a slot index.
        /// </summary>
        public bool RemoveItemAt(int slotIndex, int qty = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;

            InventorySlot slot = slots[slotIndex];
            if (slot == null) return false;

            slot.quantity -= qty;
            if (slot.quantity <= 0)
            {
                slots.RemoveAt(slotIndex);
            }

            OnInventoryUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes quantity of specified item across matching slots.
        /// </summary>
        public bool RemoveItem(ItemData item, int qty = 1)
        {
            if (item == null || qty <= 0) return false;
            if (!HasItem(item, qty)) return false;

            int remainingToRemove = qty;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].itemData == item)
                {
                    if (slots[i].quantity <= remainingToRemove)
                    {
                        remainingToRemove -= slots[i].quantity;
                        slots.RemoveAt(i);
                    }
                    else
                    {
                        slots[i].quantity -= remainingToRemove;
                        remainingToRemove = 0;
                    }

                    if (remainingToRemove <= 0) break;
                }
            }

            OnInventoryUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Clears all slots in inventory.
        /// </summary>
        public void ClearInventory()
        {
            slots.Clear();
            OnInventoryUpdated?.Invoke();
        }

        /// <summary>
        /// Drops item from slot into world in front of player.
        /// </summary>
        public bool DropItemAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            if (playerHealth != null && playerHealth.IsDead) return false;

            InventorySlot slot = slots[slotIndex];
            if (slot == null || slot.itemData == null) return false;

            ItemData data = slot.itemData;
            int qtyToDrop = slot.quantity;

            if (data.dropPrefab != null)
            {
                Vector3 spawnPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;
                GameObject droppedObj = Instantiate(data.dropPrefab, spawnPos, Quaternion.identity);

                ItemPickup pickup = droppedObj.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    pickup.Setup(data, qtyToDrop);
                }
            }

            slots.RemoveAt(slotIndex);
            OnInventoryUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Compiles current inventory slots into serializable SaveData DTOs.
        /// Preserves exact slot positions, item IDs, quantities, and empty slots.
        /// </summary>
        public List<ZombieApocalypse.Save.InventorySlotSaveData> GetInventorySaveData()
        {
            List<ZombieApocalypse.Save.InventorySlotSaveData> saveDataList = new List<ZombieApocalypse.Save.InventorySlotSaveData>();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.itemData != null && slot.quantity > 0)
                {
                    saveDataList.Add(new ZombieApocalypse.Save.InventorySlotSaveData
                    {
                        slotIndex = i,
                        itemId = slot.itemData.itemId,
                        quantity = slot.quantity,
                        isEmpty = false
                    });
                }
            }
            return saveDataList;
        }

        /// <summary>
        /// Restores exact inventory state from save data.
        /// Resolves itemIds via ItemRegistry without stack mutations, notification toasts, or missing item loss.
        /// </summary>
        public void RestoreInventory(List<ZombieApocalypse.Save.InventorySlotSaveData> savedSlots)
        {
            slots.Clear();
            if (savedSlots != null)
            {
                foreach (var savedSlot in savedSlots)
                {
                    if (savedSlot == null || savedSlot.isEmpty || string.IsNullOrEmpty(savedSlot.itemId)) continue;

                    ItemData item = ZombieApocalypse.Save.ItemRegistry.GetItem(savedSlot.itemId);
                    if (item != null && savedSlot.quantity > 0)
                    {
                        slots.Add(new InventorySlot(item, savedSlot.quantity));
                    }
                    else
                    {
                        Debug.LogWarning($"[InventorySystem] Skipped invalid or unresolvable item '{savedSlot.itemId}' during restoration.");
                    }
                }
            }
            OnInventoryUpdated?.Invoke();
            Debug.Log($"[InventorySystem] Restored {slots.Count} inventory slots from save data.");
        }
    }
}
