using System;
using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Manages player stamina pool for sprinting, jumping, and melee actions.
    /// Automatically regenerates over time when depleted.
    /// 
    /// ATTACH TO: Player prefab GameObject (e.g. "Player").
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        public event Action<float, float> OnStaminaChanged; // Current, Max

        [Header("Stamina Pool")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainRate = 15f; // Per second while sprinting
        [SerializeField] private float staminaRegenRate = 10f;  // Per second while idle/walking
        [SerializeField] private float regenDelaySeconds = 1.5f;

        private float currentStamina;
        private float lastDrainTime;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public bool HasStamina => currentStamina > 0f;

        private void Start()
        {
            currentStamina = maxStamina;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        private void Update()
        {
            RegenerateStamina();
        }

        /// <summary>
        /// Drains stamina during sprinting or exertion.
        /// </summary>
        public bool ConsumeStamina(float amount)
        {
            if (currentStamina < amount) return false;

            currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
            lastDrainTime = Time.time;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return true;
        }

        /// <summary>
        /// Restores player stamina pool by specified amount up to maxStamina.
        /// </summary>
        public void RestoreStamina(float amount)
        {
            currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        private void RegenerateStamina()
        {
            if (Time.time < lastDrainTime + regenDelaySeconds) return;
            if (currentStamina >= maxStamina) return;

            currentStamina = Mathf.Clamp(currentStamina + staminaRegenRate * Time.deltaTime, 0f, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }
}
