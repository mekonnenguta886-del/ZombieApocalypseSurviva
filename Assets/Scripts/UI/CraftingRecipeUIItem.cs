using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.Crafting;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// UI item representation of a crafting recipe in the Recipe List scroll panel.
    /// Handles visual selection state, text formatting, and click callbacks.
    /// </summary>
    public class CraftingRecipeUIItem : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI outputText;

        private int itemIndex;
        private CraftingRecipeData recipeData;
        private Action<int> onClickCallback;

        public CraftingRecipeData RecipeData => recipeData;

        public void BindElements(Image bg, TextMeshProUGUI title, TextMeshProUGUI output)
        {
            background = bg;
            titleText = title;
            outputText = output;
        }

        public void Init(int index, CraftingRecipeData recipe, Action<int> clickCallback)
        {
            itemIndex = index;
            recipeData = recipe;
            onClickCallback = clickCallback;

            Button button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickCallback?.Invoke(itemIndex));
        }

        public void SetData(CraftingRecipeData recipe, bool isSelected)
        {
            recipeData = recipe;

            if (recipeData == null)
            {
                SetEmpty(isSelected);
                return;
            }

            if (titleText != null)
            {
                titleText.text = string.IsNullOrEmpty(recipeData.displayName) ? recipeData.name : recipeData.displayName;
            }

            if (outputText != null)
            {
                if (recipeData.outputs != null && recipeData.outputs.Count > 0 && recipeData.outputs[0] != null && recipeData.outputs[0].item != null)
                {
                    var outItem = recipeData.outputs[0];
                    outputText.text = $"{outItem.item.itemName} x{outItem.quantity}";
                }
                else
                {
                    outputText.text = "Output N/A";
                }
            }

            if (background != null)
            {
                background.color = isSelected
                    ? new Color(0.9f, 0.65f, 0.15f, 0.95f) // Warm amber selected highlight
                    : new Color(0.2f, 0.2f, 0.2f, 0.85f);
            }
        }

        public void SetEmpty(bool isSelected)
        {
            if (titleText != null) titleText.text = "Empty Recipe";
            if (outputText != null) outputText.text = "";

            if (background != null)
            {
                background.color = isSelected
                    ? new Color(0.5f, 0.4f, 0.1f, 0.75f)
                    : new Color(0.12f, 0.12f, 0.12f, 0.75f);
            }
        }
    }
}
