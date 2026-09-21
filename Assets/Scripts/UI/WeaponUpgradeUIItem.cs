using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.Weapons;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// UI item representation of an available weapon in the Workbench Weapon List scroll panel.
    /// Displays weapon title, category, upgrade state summary, and selection highlight.
    /// </summary>
    public class WeaponUpgradeUIItem : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI summaryText;

        private int weaponIndex;
        private WeaponData weaponData;
        private Action<int> onClickCallback;

        public WeaponData WeaponData => weaponData;

        public void BindElements(Image bg, TextMeshProUGUI title, TextMeshProUGUI summary)
        {
            background = bg;
            titleText = title;
            summaryText = summary;
        }

        public void Init(int index, WeaponData weapon, Action<int> clickCallback)
        {
            weaponIndex = index;
            weaponData = weapon;
            onClickCallback = clickCallback;

            Button button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickCallback?.Invoke(weaponIndex));
        }

        public void SetData(WeaponData weapon, string summary, bool isSelected)
        {
            weaponData = weapon;

            if (weaponData == null)
            {
                SetEmpty(isSelected);
                return;
            }

            if (titleText != null)
            {
                titleText.text = weaponData.weaponName.ToUpper();
            }

            if (summaryText != null)
            {
                summaryText.text = summary;
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
            if (titleText != null) titleText.text = "EMPTY SLOT";
            if (summaryText != null) summaryText.text = "";

            if (background != null)
            {
                background.color = isSelected
                    ? new Color(0.5f, 0.4f, 0.1f, 0.75f)
                    : new Color(0.12f, 0.12f, 0.12f, 0.75f);
            }
        }
    }
}
