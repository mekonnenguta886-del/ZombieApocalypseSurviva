using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Updates game HUD UI elements (Health bar, Stamina bar, Ammo counter, Objective text).
    /// 
    /// ATTACH TO: Gameplay Canvas HUD Root GameObject.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health & Stamina UI")]
        [SerializeField] private Slider healthBarSlider;
        [SerializeField] private Slider staminaBarSlider;

        [Header("Weapon & Ammo UI")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private Image crosshairImage;

        [Header("Mission UI")]
        [SerializeField] private TextMeshProUGUI objectiveText;

        public void UpdateHealth(float current, float max)
        {
            if (healthBarSlider != null)
            {
                healthBarSlider.value = Mathf.Clamp01(current / max);
            }
        }

        public void UpdateStamina(float current, float max)
        {
            if (staminaBarSlider != null)
            {
                staminaBarSlider.value = Mathf.Clamp01(current / max);
            }
        }

        public void UpdateAmmo(int currentMag, int reserve)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{currentMag} / {reserve}";
            }
        }

        public void SetObjective(string objective)
        {
            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
        }
    }
}
