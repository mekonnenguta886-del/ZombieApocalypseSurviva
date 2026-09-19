using System;
using UnityEngine;
using UnityEngine.AI;

namespace ZombieApocalypse.Zombies
{
    /// <summary>
    /// Handles zombie health pool, damage reception, death state safety, collider disarming, and despawn cleanup.
    /// Configuration is read from ZombieData ScriptableObject.
    /// 
    /// ATTACH TO: Zombie prefab GameObject.
    /// </summary>
    public class ZombieHealth : MonoBehaviour
    {
        public event Action<float, float> OnHealthChanged; // Current, Max
        public event Action OnZombieDied;
        public static event Action<ZombieHealth> OnZombieKilled;

        [Header("Zombie Data Config")]
        [SerializeField] private ZombieData zombieData;

        private float currentHealth;
        private bool isDead;
        private bool isInitialized = false;
        private Animator animator;
        private Collider zombieCollider;
        private NavMeshAgent navMeshAgent;

        public ZombieData Data => zombieData;
        public float MaxHealth => zombieData != null ? zombieData.maxHealth : 100f;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public float DeathDelay => zombieData != null ? zombieData.deathDelay : 5.0f;

        private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            zombieCollider = GetComponent<Collider>();
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        public void Initialize(ZombieData data)
        {
            if (data != null)
            {
                zombieData = data;
            }
            currentHealth = MaxHealth;
            isDead = false;
            isInitialized = true;
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        private void Start()
        {
            if (!isInitialized)
            {
                currentHealth = MaxHealth;
                isDead = false;
                isInitialized = true;
            }
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void TakeDamage(float damage)
        {
            // Death Safety: Prevent damage processing if already dead
            if (isDead) return;

            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, MaxHealth);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);

            if (animator != null && currentHealth > 0)
            {
                animator.SetTrigger(HurtTriggerHash);
            }

            // Trigger Stagger in ZombieAI if damage exceeds threshold and zombie lacks stagger armor
            if (currentHealth > 0f && zombieData != null && !zombieData.hasStaggerArmor && damage >= zombieData.staggerThreshold)
            {
                ZombieAI ai = GetComponent<ZombieAI>();
                if (ai != null)
                {
                    ai.TriggerStagger(0.5f);
                }
            }

            if (currentHealth <= 0f && !isDead)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            OnZombieDied?.Invoke();
            OnZombieKilled?.Invoke(this);

            // Disable physics collider to prevent blocking movement or accepting further hit detection
            if (zombieCollider != null)
            {
                zombieCollider.enabled = false;
            }

            // Stop NavMeshAgent pathfinding and movement
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
            }

            // Trigger death state in Animator
            if (animator != null)
            {
                animator.SetBool(IsDeadHash, true);
            }

            Debug.Log($"[ZombieHealth] Zombie {gameObject.name} eliminated.");

            // Destroy object after configured deathDelay
            Destroy(gameObject, DeathDelay);
        }
    }
}
