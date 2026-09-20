using System;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Crafting
{
    public enum CraftingStatus
    {
        Success,
        InvalidRecipe,
        MissingIngredient,
        InsufficientSpace,
        TransactionFailed
    }

    /// <summary>
    /// Lightweight result model describing the outcome of a crafting transaction or check.
    /// </summary>
    [Serializable]
    public class CraftingResult
    {
        public CraftingStatus status;
        public string recipeId;
        public string message;
        public ItemData missingItem;
        public int missingQuantity;

        public bool Success => status == CraftingStatus.Success;

        public CraftingResult(CraftingStatus status, string recipeId, string message, ItemData missingItem = null, int missingQuantity = 0)
        {
            this.status = status;
            this.recipeId = recipeId ?? string.Empty;
            this.message = message ?? string.Empty;
            this.missingItem = missingItem;
            this.missingQuantity = missingQuantity;
        }

        public static CraftingResult CreateSuccess(string recipeId, string recipeName)
        {
            return new CraftingResult(CraftingStatus.Success, recipeId, $"Successfully crafted {recipeName}.");
        }

        public static CraftingResult CreateInvalid(string recipeId, string reason)
        {
            return new CraftingResult(CraftingStatus.InvalidRecipe, recipeId, reason);
        }

        public static CraftingResult CreateMissingIngredient(string recipeId, ItemData item, int missingQty)
        {
            string itemName = item != null ? item.itemName : "Required item";
            return new CraftingResult(CraftingStatus.MissingIngredient, recipeId, $"Missing ingredient: {itemName} x{missingQty}.", item, missingQty);
        }

        public static CraftingResult CreateInsufficientSpace(string recipeId)
        {
            return new CraftingResult(CraftingStatus.InsufficientSpace, recipeId, "Inventory full. Not enough space for crafted output.");
        }

        public static CraftingResult CreateFailed(string recipeId, string reason)
        {
            return new CraftingResult(CraftingStatus.TransactionFailed, recipeId, reason);
        }
    }
}
