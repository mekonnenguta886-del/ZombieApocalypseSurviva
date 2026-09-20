using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Crafting
{
    [Serializable]
    public class CraftingIngredient
    {
        public ItemData item;
        public int quantity = 1;

        public bool IsValid => item != null && quantity > 0;
    }

    [Serializable]
    public class CraftingOutput
    {
        public ItemData item;
        public int quantity = 1;

        public bool IsValid => item != null && quantity > 0;
    }

    /// <summary>
    /// ScriptableObject container defining a data-driven crafting recipe.
    /// Specifies required ingredients, produced outputs, crafting duration, and validation rules.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCraftingRecipeData", menuName = "Zombie Apocalypse/Crafting Recipe Data")]
    public class CraftingRecipeData : ScriptableObject
    {
        [Header("Recipe Identity")]
        public string recipeId = "";
        public string displayName = "";
        [TextArea(2, 4)]
        public string description = "";

        [Header("Crafting Timing")]
        public float craftingTime = 1.0f;

        [Header("Ingredients & Outputs")]
        public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
        public List<CraftingOutput> outputs = new List<CraftingOutput>();

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                recipeId = $"recipe_{name.ToLower().Replace(" ", "_").Replace("recipe_", "")}";
            }

            if (ingredients != null)
            {
                foreach (var ing in ingredients)
                {
                    if (ing != null && ing.quantity <= 0)
                    {
                        Debug.LogWarning($"[CraftingRecipeData] Ingredient quantity on '{name}' must be at least 1. Resetting to 1.", this);
                        ing.quantity = 1;
                    }
                }
            }

            if (outputs != null)
            {
                foreach (var outItem in outputs)
                {
                    if (outItem != null && outItem.quantity <= 0)
                    {
                        Debug.LogWarning($"[CraftingRecipeData] Output quantity on '{name}' must be at least 1. Resetting to 1.", this);
                        outItem.quantity = 1;
                    }
                }
            }
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return false;
            if (ingredients == null || ingredients.Count == 0) return false;
            if (outputs == null || outputs.Count == 0) return false;

            foreach (var ing in ingredients)
            {
                if (ing == null || !ing.IsValid) return false;
            }

            foreach (var outItem in outputs)
            {
                if (outItem == null || !outItem.IsValid) return false;
            }

            return true;
        }
    }
}
