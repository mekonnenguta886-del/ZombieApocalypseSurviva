using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Crafting
{
    /// <summary>
    /// Centralized runtime crafting system manager.
    /// Handles recipe discovery, ingredient validation, inventory space simulation,
    /// and atomic crafting transactions integrated with the player's InventorySystem.
    ///
    /// ATTACH TO: Player prefab or central Manager GameObject.
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        public event Action OnCraftingCompleted;
        public event Action<CraftingResult> OnCraftingResult;

        [Header("Recipes")]
        [SerializeField] private List<CraftingRecipeData> availableRecipes = new List<CraftingRecipeData>();

        [Header("Target Inventory (Auto-assigned if attached to Player)")]
        [SerializeField] private InventorySystem playerInventory;

        public IReadOnlyList<CraftingRecipeData> AvailableRecipes => availableRecipes;
        public InventorySystem TargetInventory => playerInventory;

        private void Awake()
        {
            if (playerInventory == null)
            {
                playerInventory = GetComponent<InventorySystem>();
            }

            LoadDefaultRecipesIfEmpty();
        }

        /// <summary>
        /// Auto-populates available recipes from loaded assets if recipe list is empty.
        /// </summary>
        public void LoadDefaultRecipesIfEmpty()
        {
            if (availableRecipes == null || availableRecipes.Count == 0)
            {
                availableRecipes = new List<CraftingRecipeData>();
                CraftingRecipeData[] recipes = Resources.FindObjectsOfTypeAll<CraftingRecipeData>();
                foreach (var r in recipes)
                {
                    if (r == null || !r.IsValid() || availableRecipes.Contains(r)) continue;

                    CraftingRecipeData existing = availableRecipes.Find(x => x != null && string.Equals(x.recipeId, r.recipeId, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        Debug.LogWarning($"[CraftingSystem] Duplicate recipeId '{r.recipeId}' detected on asset '{r.name}'. Skipping asset; keeping '{existing.name}'.");
                        continue;
                    }

                    availableRecipes.Add(r);
                }
            }
        }

        /// <summary>
        /// Sets or overrides target inventory system.
        /// </summary>
        public void SetTargetInventory(InventorySystem inventory)
        {
            playerInventory = inventory;
        }

        /// <summary>
        /// Registers a recipe to available recipes if valid and not already present.
        /// </summary>
        public void RegisterRecipe(CraftingRecipeData recipe)
        {
            if (recipe == null || !recipe.IsValid()) return;
            if (availableRecipes.Contains(recipe)) return;

            CraftingRecipeData existing = GetRecipeById(recipe.recipeId);
            if (existing != null)
            {
                Debug.LogWarning($"[CraftingSystem] Duplicate recipeId '{recipe.recipeId}' detected on '{recipe.name}'. Keeping existing asset '{existing.name}'.");
                return;
            }

            availableRecipes.Add(recipe);
        }

        /// <summary>
        /// Retrieves recipe by its unique string recipeId.
        /// </summary>
        public CraftingRecipeData GetRecipeById(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            return availableRecipes.Find(r => r != null && string.Equals(r.recipeId, recipeId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns a copy of all available recipes loaded in the crafting system.
        /// </summary>
        public List<CraftingRecipeData> GetAvailableRecipes()
        {
            return new List<CraftingRecipeData>(availableRecipes);
        }

        /// <summary>
        /// Checks if specified recipe can be crafted using target inventory.
        /// Performs recipe validation, ingredient availability check, and output space simulation.
        /// </summary>
        public CraftingResult CanCraft(CraftingRecipeData recipe)
        {
            return CanCraft(recipe, playerInventory);
        }

        /// <summary>
        /// Checks if specified recipe can be crafted using specified inventory.
        /// </summary>
        public CraftingResult CanCraft(CraftingRecipeData recipe, InventorySystem inventory)
        {
            if (recipe == null || !recipe.IsValid())
            {
                return CraftingResult.CreateInvalid(recipe != null ? recipe.recipeId : null, "Invalid or null recipe data.");
            }

            if (inventory == null)
            {
                return CraftingResult.CreateFailed(recipe.recipeId, "Target inventory is null.");
            }

            var health = inventory.GetComponent<ZombieApocalypse.Player.PlayerHealth>();
            if (health != null && health.IsDead)
            {
                return CraftingResult.CreateFailed(recipe.recipeId, "Cannot craft while player is dead.");
            }

            // Step 1: Check ingredient requirements
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null || ing.item == null || ing.quantity <= 0) continue;

                int currentQty = inventory.GetItemQuantity(ing.item);
                if (currentQty < ing.quantity)
                {
                    return CraftingResult.CreateMissingIngredient(recipe.recipeId, ing.item, ing.quantity - currentQty);
                }
            }

            // Step 2: Simulate output capacity in post-consumption inventory state
            if (!SimulateCanFitOutputs(inventory, recipe.ingredients, recipe.outputs))
            {
                return CraftingResult.CreateInsufficientSpace(recipe.recipeId);
            }

            return CraftingResult.CreateSuccess(recipe.recipeId, recipe.displayName);
        }

        /// <summary>
        /// Attempts to craft specified recipe using target inventory.
        /// Executes atomic transaction: removes ingredients and adds outputs only if all checks pass.
        /// Rollbacks any removed ingredients if output addition fails unexpectedly.
        /// </summary>
        public CraftingResult TryCraft(CraftingRecipeData recipe)
        {
            return TryCraft(recipe, playerInventory);
        }

        /// <summary>
        /// Attempts to craft specified recipe using specified inventory.
        /// </summary>
        public CraftingResult TryCraft(CraftingRecipeData recipe, InventorySystem inventory)
        {
            CraftingResult checkResult = CanCraft(recipe, inventory);
            if (!checkResult.Success)
            {
                OnCraftingResult?.Invoke(checkResult);
                return checkResult;
            }

            List<CraftingIngredient> removedIngredients = new List<CraftingIngredient>();

            // Atomic Execution Phase
            // Step 1: Remove all ingredients
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null || ing.item == null || ing.quantity <= 0) continue;
                bool removed = inventory.RemoveItem(ing.item, ing.quantity);
                if (!removed)
                {
                    // Rollback any ingredients removed so far in this transaction
                    foreach (var prevRemoved in removedIngredients)
                    {
                        inventory.AddItem(prevRemoved.item, prevRemoved.quantity);
                    }

                    Debug.LogError($"[CraftingSystem] Transaction failure: Failed to remove ingredient {ing.item.itemName} x{ing.quantity}. Rolled back ingredients.");
                    CraftingResult failResult = CraftingResult.CreateFailed(recipe.recipeId, "Transaction failed during ingredient removal.");
                    OnCraftingResult?.Invoke(failResult);
                    return failResult;
                }
                removedIngredients.Add(ing);
            }

            // Step 2: Add all outputs
            List<CraftingOutput> addedOutputs = new List<CraftingOutput>();
            bool outputFailed = false;

            foreach (var outItem in recipe.outputs)
            {
                if (outItem == null || outItem.item == null || outItem.quantity <= 0) continue;
                int added = inventory.AddItem(outItem.item, outItem.quantity);
                if (added < outItem.quantity)
                {
                    outputFailed = true;
                    if (added > 0)
                    {
                        addedOutputs.Add(new CraftingOutput { item = outItem.item, quantity = added });
                    }
                    break;
                }
                addedOutputs.Add(new CraftingOutput { item = outItem.item, quantity = added });
            }

            if (outputFailed)
            {
                // Rollback: Remove any partially added outputs and restore all removed ingredients
                foreach (var addedOut in addedOutputs)
                {
                    inventory.RemoveItem(addedOut.item, addedOut.quantity);
                }
                foreach (var ing in removedIngredients)
                {
                    inventory.AddItem(ing.item, ing.quantity);
                }

                Debug.LogError($"[CraftingSystem] Transaction failure: Failed to add outputs for recipe '{recipe.recipeId}'. Rolled back transaction.");
                CraftingResult failResult = CraftingResult.CreateFailed(recipe.recipeId, "Transaction failed during output addition. Ingredients restored.");
                OnCraftingResult?.Invoke(failResult);
                return failResult;
            }

            CraftingResult successResult = CraftingResult.CreateSuccess(recipe.recipeId, recipe.displayName);
            OnCraftingCompleted?.Invoke();
            OnCraftingResult?.Invoke(successResult);
            return successResult;
        }

        /// <summary>
        /// Returns list of ingredients missing from target inventory required to craft specified recipe.
        /// </summary>
        public List<CraftingIngredient> GetMissingIngredients(CraftingRecipeData recipe)
        {
            return GetMissingIngredients(recipe, playerInventory);
        }

        /// <summary>
        /// Returns list of ingredients missing from specified inventory required to craft specified recipe.
        /// </summary>
        public List<CraftingIngredient> GetMissingIngredients(CraftingRecipeData recipe, InventorySystem inventory)
        {
            List<CraftingIngredient> missing = new List<CraftingIngredient>();
            if (recipe == null || recipe.ingredients == null || inventory == null) return missing;

            foreach (var ing in recipe.ingredients)
            {
                if (ing == null || ing.item == null || ing.quantity <= 0) continue;

                int currentQty = inventory.GetItemQuantity(ing.item);
                if (currentQty < ing.quantity)
                {
                    missing.Add(new CraftingIngredient
                    {
                        item = ing.item,
                        quantity = ing.quantity - currentQty
                    });
                }
            }

            return missing;
        }

        /// <summary>
        /// Simulates whether all outputs can fit into inventory after ingredients are removed.
        /// Does not mutate actual inventory slots.
        /// </summary>
        private bool SimulateCanFitOutputs(InventorySystem inventory, List<CraftingIngredient> ingredients, List<CraftingOutput> outputs)
        {
            if (inventory == null) return false;
            if (outputs == null || outputs.Count == 0) return true;

            // 1. Copy current slots into virtual slot list
            List<SimulatedSlot> virtualSlots = new List<SimulatedSlot>();
            var realSlots = inventory.Slots;
            if (realSlots != null)
            {
                foreach (var slot in realSlots)
                {
                    if (slot != null && slot.itemData != null && slot.quantity > 0)
                    {
                        virtualSlots.Add(new SimulatedSlot(slot.itemData, slot.quantity));
                    }
                }
            }

            // 2. Simulate ingredient removal (matching InventorySystem.RemoveItem backward slot removal)
            if (ingredients != null)
            {
                foreach (var ing in ingredients)
                {
                    if (ing == null || ing.item == null || ing.quantity <= 0) continue;

                    int remainingToRemove = ing.quantity;
                    for (int i = virtualSlots.Count - 1; i >= 0; i--)
                    {
                        if (virtualSlots[i].itemData == ing.item)
                        {
                            if (virtualSlots[i].quantity <= remainingToRemove)
                            {
                                remainingToRemove -= virtualSlots[i].quantity;
                                virtualSlots.RemoveAt(i);
                            }
                            else
                            {
                                virtualSlots[i].quantity -= remainingToRemove;
                                remainingToRemove = 0;
                            }

                            if (remainingToRemove <= 0) break;
                        }
                    }
                }
            }

            // 3. Simulate output addition (matching InventorySystem.AddItem forward stack filling & slot creation)
            int maxSlots = inventory.MaxSlots;

            foreach (var outItem in outputs)
            {
                if (outItem == null || outItem.item == null || outItem.quantity <= 0) continue;

                ItemData item = outItem.item;
                int amountToAdd = outItem.quantity;
                int addedCount = 0;

                // Step A: Fill existing stackable virtual slots
                if (item.isStackable)
                {
                    foreach (var slot in virtualSlots)
                    {
                        if (slot.itemData == item && slot.quantity < item.maxStackSize)
                        {
                            int addable = Mathf.Min(amountToAdd - addedCount, item.maxStackSize - slot.quantity);
                            slot.quantity += addable;
                            addedCount += addable;

                            if (addedCount >= amountToAdd) break;
                        }
                    }
                }

                // Step B: Fill new virtual slots up to maxSlots
                while (addedCount < amountToAdd && virtualSlots.Count < maxSlots)
                {
                    int spaceRemaining = amountToAdd - addedCount;
                    int qtyForNewSlot = item.isStackable ? Mathf.Min(spaceRemaining, item.maxStackSize) : 1;
                    virtualSlots.Add(new SimulatedSlot(item, qtyForNewSlot));
                    addedCount += qtyForNewSlot;
                }

                if (addedCount < amountToAdd)
                {
                    // Could not fit output!
                    return false;
                }
            }

            return true;
        }

        private class SimulatedSlot
        {
            public ItemData itemData;
            public int quantity;

            public SimulatedSlot(ItemData item, int qty)
            {
                itemData = item;
                quantity = qty;
            }
        }
    }
}
