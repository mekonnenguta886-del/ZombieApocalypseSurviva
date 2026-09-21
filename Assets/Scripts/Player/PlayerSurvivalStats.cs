using System;
using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Manages player hunger and thirst survival metrics, gradual decay over time,
    /// warning threshold triggers, and health integration for starvation/dehydration.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class PlayerSurvivalStats : MonoBehaviour
    {
        public event Action<float, float, float, float> OnSurvivalStatsChanged; // hunger, maxHunger, thirst, maxThirst
        public event Action<string> OnLowSurvivalWarning;

        [Header("Hunger Settings")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float hungerDecayRatePerSec = 0.3f; // ~5.5 mins to zero

        [Header("Thirst Settings")]
        [SerializeField] private float maxThirst = 100f;
        [SerializeField] private float thirstDecayRatePerSec = 0.5f; // ~3.3 mins to zero

        [Header("Warning Thresholds")]
        [SerializeField] private float lowThresholdPercentage = 0.20f;

        // Current runtime values
        private float currentHunger;
        private float currentThirst;
        private PlayerHealth playerHealth;

        private float lastWarningTime;
        private const float WARNING_COOLDOWN = 10f;

        public float MaxHunger => maxHunger;
        public float CurrentHunger => currentHunger;
        public float MaxThirst => maxThirst;
        public float CurrentThirst => currentThirst;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void Start()
        {
            currentHunger = maxHunger;
            currentThirst = maxThirst;
            NotifyHUD();
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead) return;

            // Decay stats over time (adjusted by Survival Skill Perk multiplier)
            float decayMult = ZombieApocalypse.Progression.PlayerProgressionSystem.Instance != null 
                ? ZombieApocalypse.Progression.PlayerProgressionSystem.Instance.GetSurvivalDecayMultiplier() 
                : 1.0f;

            currentHunger = Mathf.Clamp(currentHunger - (hungerDecayRatePerSec * decayMult * Time.deltaTime), 0f, maxHunger);
            currentThirst = Mathf.Clamp(currentThirst - (thirstDecayRatePerSec * decayMult * Time.deltaTime), 0f, maxThirst);

            CheckWarningThresholds();
            NotifyHUD();
        }

        private void CheckWarningThresholds()
        {
            if (Time.time < lastWarningTime + WARNING_COOLDOWN) return;

            bool lowHunger = currentHunger <= maxHunger * lowThresholdPercentage;
            bool lowThirst = currentThirst <= maxThirst * lowThresholdPercentage;

            if (lowHunger && lowThirst)
            {
                OnLowSurvivalWarning?.Invoke("CRITICAL: LOW HUNGER & THIRST!");
                lastWarningTime = Time.time;
            }
            else if (lowHunger)
            {
                OnLowSurvivalWarning?.Invoke("WARNING: LOW HUNGER!");
                lastWarningTime = Time.time;
            }
            else if (lowThirst)
            {
                OnLowSurvivalWarning?.Invoke("WARNING: LOW THIRST!");
                lastWarningTime = Time.time;
            }
        }

        public void RestoreHunger(float amount)
        {
            if (playerHealth != null && playerHealth.IsDead) return;
            currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
            NotifyHUD();
            Debug.Log($"[PlayerSurvivalStats] Restored Hunger by {amount}. Current: {currentHunger}/{maxHunger}");
        }

        public void RestoreThirst(float amount)
        {
            if (playerHealth != null && playerHealth.IsDead) return;
            currentThirst = Mathf.Clamp(currentThirst + amount, 0f, maxThirst);
            NotifyHUD();
            Debug.Log($"[PlayerSurvivalStats] Restored Thirst by {amount}. Current: {currentThirst}/{maxThirst}");
        }

        /// <summary>
        /// Restores saved hunger and thirst metrics cleanly without warning spam or decay events.
        /// </summary>
        public void RestoreStats(float hunger, float thirst)
        {
            currentHunger = Mathf.Clamp(hunger, 0f, maxHunger);
            currentThirst = Mathf.Clamp(thirst, 0f, maxThirst);
            NotifyHUD();
            Debug.Log($"[PlayerSurvivalStats] Restored survival stats from save. Hunger: {currentHunger}/{maxHunger}, Thirst: {currentThirst}/{maxThirst}");
        }

        private void NotifyHUD()
        {
            OnSurvivalStatsChanged?.Invoke(currentHunger, maxHunger, currentThirst, maxThirst);
        }
    }
}
