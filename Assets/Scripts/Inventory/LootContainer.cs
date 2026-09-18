using System;
using System.Collections.Generic;
using UnityEngine;

using ZombieApocalypse.World;

namespace ZombieApocalypse.Inventory
{
    [Serializable]
    public class LootEntry
    {
        public ItemData itemData;
        public int minQuantity = 1;
        public int maxQuantity = 3;
        [Range(0f, 1f)] public float dropChance = 0.7f;
    }

    /// <summary>
    /// Attached to Supply Box / Crate GameObjects. Generates configurable random loot ONCE on initial opening.
    /// State persists for the session. Items are collected directly into player inventory on interaction,
    /// storing any uncollected items for future collection.
    /// </summary>
    public class LootContainer : MonoBehaviour, IInteractable
    {
        [Header("Container Configuration")]
        [SerializeField] private string containerName = "Supply Box";
        [SerializeField] private List<LootEntry> lootTable = new List<LootEntry>();

        private bool hasBeenOpened = false;
        private List<InventorySlot> containerContents = new List<InventorySlot>();

        public string ContainerName => containerName;
        public bool HasBeenOpened => hasBeenOpened;
        public string InteractionPrompt => hasBeenOpened 
            ? (containerContents.Count > 0 ? $"Search {containerName} (Opened)" : $"{containerName} (Empty)") 
            : $"Open {containerName}";

        public bool CanInteract(GameObject player)
        {
            return player != null && (!hasBeenOpened || containerContents.Count > 0);
        }

        public void Interact(GameObject player)
        {
            if (player != null)
            {
                InventorySystem inv = player.GetComponent<InventorySystem>();
                if (inv != null)
                {
                    Interact(inv);
                }
            }
        }

        /// <summary>
        /// Interacts with container. Generates loot on first open and attempts to add contents to player inventory.
        /// Handles partial fit by keeping uncollected items in container for future retrieval.
        /// </summary>
        public bool Interact(InventorySystem playerInventory)
        {
            if (playerInventory == null) return false;

            if (!hasBeenOpened)
            {
                GenerateLoot();
                hasBeenOpened = true;
            }

            if (containerContents.Count == 0)
            {
                Debug.Log($"[LootContainer] {containerName} is empty.");
                return false;
            }

            List<InventorySlot> remainingContents = new List<InventorySlot>();
            int totalCollected = 0;

            foreach (var slot in containerContents)
            {
                if (slot == null || slot.itemData == null || slot.quantity <= 0) continue;

                int added = playerInventory.AddItem(slot.itemData, slot.quantity);
                if (added > 0)
                {
                    totalCollected += added;
                    int remainingQty = slot.quantity - added;
                    if (remainingQty > 0)
                    {
                        remainingContents.Add(new InventorySlot(slot.itemData, remainingQty));
                    }
                }
                else
                {
                    remainingContents.Add(slot);
                }
            }

            containerContents = remainingContents;
            return totalCollected > 0;
        }

        private void GenerateLoot()
        {
            containerContents.Clear();

            foreach (var entry in lootTable)
            {
                if (entry == null || entry.itemData == null) continue;

                float roll = UnityEngine.Random.value;
                if (roll <= entry.dropChance)
                {
                    int qty = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                    containerContents.Add(new InventorySlot(entry.itemData, qty));
                    Debug.Log($"[LootContainer] Generated {entry.itemData.itemName} x{qty} in {containerName}");
                }
            }

            if (containerContents.Count == 0 && lootTable.Count > 0)
            {
                // Fallback guarantee: at least 1 item drops
                var fallback = lootTable[UnityEngine.Random.Range(0, lootTable.Count)];
                if (fallback != null && fallback.itemData != null)
                {
                    containerContents.Add(new InventorySlot(fallback.itemData, fallback.minQuantity));
                }
            }
        }

        [SerializeField] private string containerId = "";
        public string ContainerId => string.IsNullOrEmpty(containerId) ? containerName : containerId;

        /// <summary>
        /// Compiles current remaining contents into serializable DTO list.
        /// </summary>
        public List<ZombieApocalypse.Save.InventorySlotSaveData> GetRemainingContentsSaveData()
        {
            var list = new List<ZombieApocalypse.Save.InventorySlotSaveData>();
            for (int i = 0; i < containerContents.Count; i++)
            {
                var slot = containerContents[i];
                if (slot != null && slot.itemData != null && slot.quantity > 0)
                {
                    list.Add(new ZombieApocalypse.Save.InventorySlotSaveData
                    {
                        slotIndex = i,
                        itemId = slot.itemData.itemId,
                        quantity = slot.quantity,
                        isEmpty = false
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// Restores container open state and uncollected items from save data without re-generating random loot.
        /// </summary>
        public void RestoreContainerState(bool opened, List<ZombieApocalypse.Save.InventorySlotSaveData> savedContents)
        {
            hasBeenOpened = opened;
            containerContents.Clear();

            if (savedContents != null)
            {
                foreach (var itemSave in savedContents)
                {
                    if (itemSave == null || string.IsNullOrEmpty(itemSave.itemId) || itemSave.quantity <= 0) continue;

                    ItemData data = ZombieApocalypse.Save.ItemRegistry.GetItem(itemSave.itemId);
                    if (data != null)
                    {
                        containerContents.Add(new InventorySlot(data, itemSave.quantity));
                    }
                }
            }

            Debug.Log($"[LootContainer] Restored '{ContainerId}'. Opened: {hasBeenOpened}, Remaining Items: {containerContents.Count}");
        }
    }
}
