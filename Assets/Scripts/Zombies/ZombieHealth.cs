using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ZombieApocalypse.Zombies
{
    /// <summary>
    /// Handles zombie health, damage reception, death state, collider disabling, and cleanup despawning.
    /// 
    /// ATTACH TO: Zombie prefab GameObject.
    /// </summary>
    public class ZombieHealth : MonoBehaviour
    {
        public event Action<float, float> OnHealthChanged; // Current, Max
        public event Action OnZombieDied;

        [Header("Zombie Data Config")]
        [SerializeField] private ZombieData zombieData;
        [SerializeField] private float despawnDelay = 5.0f;

        private float currentHealth;
        private bool isDead;
        private Animator animator;
        private Collider zombieCollider;

        public float MaxHealth => zombieData != null ? zombieData.maxHealth : 100f;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            zombieCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            currentHealth = MaxHealth;
            isDead = false;
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, MaxHealth);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);

            if (animator != null && currentHealth > 0)
            {
                animator.SetTrigger(HurtTriggerHash);
            }

            if (currentHealth <= 0f && !isDead)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            OnZombieDied?.Invoke();

            // Disable collider & physics
            if (zombieCollider != null)
            {
                zombieCollider.enabled = false;
            }

            // Disable NavMeshAgent if attached
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            // Trigger death state on animator
            if (animator != null)
            {
                animator.SetBool(IsDeadHash, true);
            }

            Debug.Log($"[ZombieHealth] Zombie {name} died.");

            // Despawn object after delay
            Destroy(gameObject, despawnDelay);
        }
    }
}
