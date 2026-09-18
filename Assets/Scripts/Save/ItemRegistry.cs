using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Save
{
    /// <summary>
    /// Item resolver mapping string itemIds to runtime ItemData ScriptableObject assets.
    /// Safely resolves items without fragile hardcoded paths or asset duplication.
    /// </summary>
    public static class ItemRegistry
    {
        private static readonly Dictionary<string, ItemData> registry = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized && registry.Count > 0) return;

            registry.Clear();
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();

            foreach (var item in allItems)
            {
                if (item == null || string.IsNullOrEmpty(item.itemId)) continue;

                if (registry.ContainsKey(item.itemId))
                {
                    if (registry[item.itemId] != item)
                    {
                        Debug.LogWarning($"[ItemRegistry] Duplicate itemId detected: '{item.itemId}' on '{item.name}'. Keeping initial asset.");
                    }
                }
                else
                {
                    registry[item.itemId] = item;
                }
            }

            isInitialized = true;
            Debug.Log($"[ItemRegistry] Initialized item registry with {registry.Count} unique item IDs.");
        }

        public static ItemData GetItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            if (!isInitialized)
            {
                Initialize();
            }

            if (registry.TryGetValue(itemId, out ItemData item) && item != null)
            {
                return item;
            }

            // Secondary fallback search if dynamic assets loaded later
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var candidate in allItems)
            {
                if (candidate != null && !string.IsNullOrEmpty(candidate.itemId) &&
                    candidate.itemId.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                {
                    registry[itemId] = candidate;
                    return candidate;
                }
            }

            Debug.LogWarning($"[ItemRegistry] Could not resolve ItemData for itemId: '{itemId}'. Skipping item.");
            return null;
        }
    }
}
