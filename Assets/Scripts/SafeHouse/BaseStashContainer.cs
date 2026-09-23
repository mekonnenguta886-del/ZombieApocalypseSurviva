using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;
using ZombieApocalypse.Save;
using ZombieApocalypse.World;

namespace ZombieApocalypse.SafeHouse
{
    /// <summary>
    /// Interactive persistent Safe House storage container (Stage 2).
    /// Provides atomic item deposit and withdrawal between player inventory and stash,
    /// respecting capacity limits, item stack rules, and Safe House restrictions.
    ///
    /// ATTACH TO: Base Stash GameObject in Safe House.
    /// </summary>
    public class BaseStashContainer : MonoBehaviour, IInteractable
    {
        public event Action OnStashUpdated;
        public event Action<string> OnStashNotification;

        [Header("Stash Configuration")]
        [SerializeField] private int capacity = 20;
        [SerializeField] private List<InventorySlot> stashSlots = new List<InventorySlot>();

        public IReadOnlyList<InventorySlot> StashSlots => stashSlots;
        public int Capacity => capacity;
        public int UsedSlots => stashSlots.Count;
        public bool IsFull => stashSlots.Count >= capacity;

        // IInteractable implementation
        public string InteractionPrompt => "Open Base Stash";

        public bool CanInteract(GameObject player)
        {
            if (player == null) return false;
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) return false;

            return SafeHouseManager.Instance != null && SafeHouseManager.Instance.IsPlayerInsideSafeHouse;
        }

        public void Interact(GameObject player)
        {
            if (!CanInteract(player)) return;

            Debug.Log($"[BaseStashContainer] Player interacted with Base Stash ({stashSlots.Count}/{capacity} slots used).");
            OnStashNotification?.Invoke($"Base Stash opened ({stashSlots.Count}/{capacity} slots)");
        }

        public void SetCapacity(int newCapacity)
        {
            capacity = Mathf.Max(1, newCapacity);
            OnStashUpdated?.Invoke();
        }

        /// <summary>
        /// Attempts to deposit items from player inventory slot index into stash.
        /// Atomic transfer: does not lose items or partially corrupt state.
        /// </summary>
        public int TryDeposit(InventorySystem playerInventory, int playerSlotIndex, int amount = -1)
        {
            if (playerInventory == null) return 0;
            if (SafeHouseManager.Instance == null || !SafeHouseManager.Instance.IsPlayerInsideSafeHouse)
            {
                OnStashNotification?.Invoke("Must be inside Safe House to access Stash.");
                return 0;
            }

            IReadOnlyList<InventorySlot> playerSlots = playerInventory.Slots;
            if (playerSlotIndex < 0 || playerSlotIndex >= playerSlots.Count) return 0;

            InventorySlot sourceSlot = playerSlots[playerSlotIndex];
            if (sourceSlot == null || sourceSlot.itemData == null || sourceSlot.quantity <= 0) return 0;

            ItemData item = sourceSlot.itemData;
            int transferAmount = amount > 0 ? Mathf.Min(amount, sourceSlot.quantity) : sourceSlot.quantity;
            int depositedCount = 0;

            // Step 1: Try filling existing matching stacks in stash
            if (item.isStackable)
            {
                foreach (var stashSlot in stashSlots)
                {
                    if (stashSlot.itemData == item && stashSlot.quantity < item.maxStackSize)
                    {
                        int spaceInStack = item.maxStackSize - stashSlot.quantity;
                        int addable = Mathf.Min(transferAmount - depositedCount, spaceInStack);
                        stashSlot.quantity += addable;
                        depositedCount += addable;

                        if (depositedCount >= transferAmount) break;
                    }
                }
            }

            // Step 2: Add to new stash slots if capacity allows
            while (depositedCount < transferAmount && stashSlots.Count < capacity)
            {
                int remainingToDeposit = transferAmount - depositedCount;
                int qtyForNewSlot = item.isStackable ? Mathf.Min(remainingToDeposit, item.maxStackSize) : 1;
                stashSlots.Add(new InventorySlot(item, qtyForNewSlot));
                depositedCount += qtyForNewSlot;
            }

            // Step 3: Remove deposited amount from player inventory atomically
            if (depositedCount > 0)
            {
                playerInventory.RemoveItemAt(playerSlotIndex, depositedCount);
                OnStashNotification?.Invoke($"Stashed {item.itemName} x{depositedCount}");
                OnStashUpdated?.Invoke();
            }
            else
            {
                OnStashNotification?.Invoke("Stash Full");
            }

            return depositedCount;
        }

        /// <summary>
        /// Attempts to withdraw items from stash slot index into player inventory.
        /// Atomic transfer: respects player inventory capacity.
        /// </summary>
        public int TryWithdraw(InventorySystem playerInventory, int stashSlotIndex, int amount = -1)
        {
            if (playerInventory == null) return 0;
            if (SafeHouseManager.Instance == null || !SafeHouseManager.Instance.IsPlayerInsideSafeHouse)
            {
                OnStashNotification?.Invoke("Must be inside Safe House to access Stash.");
                return 0;
            }

            if (stashSlotIndex < 0 || stashSlotIndex >= stashSlots.Count) return 0;

            InventorySlot stashSlot = stashSlots[stashSlotIndex];
            if (stashSlot == null || stashSlot.itemData == null || stashSlot.quantity <= 0) return 0;

            ItemData item = stashSlot.itemData;
            int requestedAmount = amount > 0 ? Mathf.Min(amount, stashSlot.quantity) : stashSlot.quantity;

            // Attempt add to player inventory
            int addedCount = playerInventory.AddItem(item, requestedAmount);

            // Deduct added count from stash slot
            if (addedCount > 0)
            {
                stashSlot.quantity -= addedCount;
                if (stashSlot.quantity <= 0)
                {
                    stashSlots.RemoveAt(stashSlotIndex);
                }

                OnStashNotification?.Invoke($"Withdrew {item.itemName} x{addedCount}");
                OnStashUpdated?.Invoke();
            }
            else
            {
                OnStashNotification?.Invoke("Inventory Full");
            }

            return addedCount;
        }

        // ==========================================
        // PERSISTENCE & SAVE/LOAD
        // ==========================================

        public List<InventorySlotSaveData> GetStashSaveData()
        {
            List<InventorySlotSaveData> saveDataList = new List<InventorySlotSaveData>();
            for (int i = 0; i < stashSlots.Count; i++)
            {
                var slot = stashSlots[i];
                if (slot != null && slot.itemData != null && slot.quantity > 0)
                {
                    saveDataList.Add(new InventorySlotSaveData
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

        public void RestoreStashState(int savedCapacity, List<InventorySlotSaveData> savedSlots)
        {
            if (savedCapacity > 0)
            {
                capacity = savedCapacity;
            }

            stashSlots.Clear();
            if (savedSlots != null)
            {
                foreach (var savedSlot in savedSlots)
                {
                    if (savedSlot == null || savedSlot.isEmpty || string.IsNullOrEmpty(savedSlot.itemId)) continue;

                    ItemData item = ItemRegistry.GetItem(savedSlot.itemId);
                    if (item != null && savedSlot.quantity > 0)
                    {
                        stashSlots.Add(new InventorySlot(item, savedSlot.quantity));
                    }
                    else
                    {
                        Debug.LogWarning($"[BaseStashContainer] Skipped unresolvable stash item '{savedSlot.itemId}'.");
                    }
                }
            }

            OnStashUpdated?.Invoke();
            Debug.Log($"[BaseStashContainer] Restored {stashSlots.Count} stash slots (Capacity: {capacity}).");
        }
    }
}
