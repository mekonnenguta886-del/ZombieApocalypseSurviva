using System;
using UnityEngine;

namespace ZombieApocalypse.Zombies
{
    /// <summary>
    /// Manages zombie health, hit reactions, ragdoll activation, and death state.
    /// 
    /// ATTACH TO: Zombie prefab GameObjects (e.g. "Zombie_Walker", "Zombie_Runner", etc.).
    /// </summary>
    public class ZombieHealth : MonoBehaviour
    {
        public event Action<float, float> OnHealthChanged;
        public event Action OnZombieDied;

        [Header("Zombie Stats")]
        [SerializeField] private ZombieData zombieData;
        private float currentHealth;

        public float MaxHealth => zombieData != null ? zombieData.maxHealth : 100f;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        private void Start()
        {
            if (zombieData != null)
            {
                currentHealth = zombieData.maxHealth;
            }
            else
            {
                currentHealth = 100f;
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, MaxHealth);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);

            if (IsDead)
            {
                OnZombieDied?.Invoke();
                Debug.Log($"[ZombieHealth] Zombie {gameObject.name} eliminated.");
            }
        }
    }
}
