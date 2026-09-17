using System;
using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Manages player health pool, damage processing, healing, and death callbacks.
    /// 
    /// ATTACH TO: Player prefab GameObject (e.g. "Player").
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public event Action<float, float> OnHealthChanged; // Current, Max
        public event Action OnPlayerDied;

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        private void Start()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Applies damage to player health pool.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (IsDead)
            {
                OnPlayerDied?.Invoke();
                Debug.Log("[PlayerHealth] Player has died.");
            }
        }

        /// <summary>
        /// Restores player health by specified amount.
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
