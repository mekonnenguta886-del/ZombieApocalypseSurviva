using System;
using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Manages player health pool, damage processing, healing, and death callbacks.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public event Action<float, float> OnHealthChanged; // Current, Max
        public event Action OnPlayerDied;

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;
        private bool isDead;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        private void Start()
        {
            currentHealth = maxHealth;
            isDead = false;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Applies damage to player health pool.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f && !isDead)
            {
                isDead = true;
                OnPlayerDied?.Invoke();
                Debug.Log("[PlayerHealth] Player eliminated!");
            }
        }

        /// <summary>
        /// Restores player health by specified amount.
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead) return;

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Restores saved player health cleanly without damage FX, healing popups, or death callbacks.
        /// </summary>
        public void RestoreHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            isDead = currentHealth <= 0f;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Debug.Log($"[PlayerHealth] Restored health from save: {currentHealth}/{maxHealth}");
        }
    }
}
