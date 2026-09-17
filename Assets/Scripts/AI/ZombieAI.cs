using UnityEngine;
using UnityEngine.AI;
using ZombieApocalypse.Player;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.AI
{
    public enum AIState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    /// <summary>
    /// Smooth NavMeshAgent-based Zombie AI state machine.
    /// Manages player perception, pathfinding chase, attack execution, and death safety.
    /// Reads configuration values directly from ZombieData ScriptableObject.
    /// 
    /// ATTACH TO: Zombie prefab GameObject.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ZombieHealth))]
    public class ZombieAI : MonoBehaviour
    {
        [Header("Zombie Data Reference")]
        [SerializeField] private ZombieData zombieData;

        [Header("Current State")]
        [SerializeField] private AIState currentState = AIState.Idle;

        // References
        private NavMeshAgent navMeshAgent;
        private ZombieHealth zombieHealth;
        private Animator animator;
        private Transform playerTransform;
        private PlayerHealth playerHealth;

        private float lastAttackTime;

        public AIState CurrentState => currentState;

        // Parameter Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            zombieHealth = GetComponent<ZombieHealth>();
            animator = GetComponentInChildren<Animator>();

            // Configure Rigidbody safety if present
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        private void Start()
        {
            ApplyZombieDataConfig();
            FindPlayerReference();

            if (zombieHealth != null)
            {
                zombieHealth.OnZombieDied += HandleZombieDeath;
            }
        }

        private void OnDestroy()
        {
            if (zombieHealth != null)
            {
                zombieHealth.OnZombieDied -= HandleZombieDeath;
            }
        }

        private void ApplyZombieDataConfig()
        {
            if (zombieData == null && zombieHealth != null)
            {
                zombieData = zombieHealth.Data;
            }

            if (zombieData != null && navMeshAgent != null)
            {
                navMeshAgent.speed = zombieData.moveSpeed;
                navMeshAgent.stoppingDistance = zombieData.attackRange * 0.85f;
            }
        }

        private void FindPlayerReference()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerHealth = playerObj.GetComponent<PlayerHealth>();
            }
        }

        private void Update()
        {
            // Death Safety Guard: If zombie is dead, remain in DEAD state and stop AI
            if (zombieHealth != null && zombieHealth.IsDead)
            {
                if (currentState != AIState.Dead)
                {
                    HandleZombieDeath();
                }
                return;
            }

            if (playerTransform == null || playerHealth == null)
            {
                FindPlayerReference();
                if (playerTransform == null) return;
            }

            // Player Death Safety: If player is dead, cease all chase and attack behaviors
            if (playerHealth.IsDead)
            {
                if (currentState != AIState.Idle)
                {
                    SetState(AIState.Idle);
                    StopNavMeshMovement();
                    UpdateAnimator(0f, false);
                }
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            float detectionRad = zombieData != null ? zombieData.detectionRadius : 12f;
            float loseRad = zombieData != null ? zombieData.loseTargetRadius : 16f;
            float atkRange = zombieData != null ? zombieData.attackRange : 1.8f;

            switch (currentState)
            {
                case AIState.Idle:
                    UpdateAnimator(0f, false);
                    if (distanceToPlayer <= detectionRad)
                    {
                        SetState(AIState.Chase);
                    }
                    break;

                case AIState.Chase:
                    if (distanceToPlayer > loseRad)
                    {
                        SetState(AIState.Idle);
                        StopNavMeshMovement();
                        return;
                    }

                    if (distanceToPlayer <= atkRange)
                    {
                        SetState(AIState.Attack);
                        return;
                    }

                    // Move towards player via NavMeshAgent
                    if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                    {
                        navMeshAgent.isStopped = false;
                        navMeshAgent.SetDestination(playerTransform.position);
                    }

                    UpdateAnimator(navMeshAgent != null ? navMeshAgent.speed : 2.5f, false);
                    break;

                case AIState.Attack:
                    if (distanceToPlayer > atkRange * 1.3f)
                    {
                        SetState(AIState.Chase);
                        return;
                    }

                    // Stop locomotion during attack
                    StopNavMeshMovement();
                    RotateTowardsPlayer();
                    UpdateAnimator(0f, true);

                    // Execute Attack on Cooldown
                    float cooldown = zombieData != null ? zombieData.attackCooldown : 1.2f;
                    if (Time.time >= lastAttackTime + cooldown)
                    {
                        lastAttackTime = Time.time;
                        ExecuteAttack();
                    }
                    break;
            }
        }

        private void StopNavMeshMovement()
        {
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
        }

        private void ExecuteAttack()
        {
            if (playerHealth != null && !playerHealth.IsDead)
            {
                float damage = zombieData != null ? zombieData.attackDamage : 15f;
                playerHealth.TakeDamage(damage);
                Debug.Log($"[ZombieAI] {gameObject.name} attacked player dealing {damage} damage.");
            }
        }

        private void RotateTowardsPlayer()
        {
            if (playerTransform == null) return;
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 8.0f);
            }
        }

        private void UpdateAnimator(float speed, bool isAttacking)
        {
            if (animator == null) return;
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsAttackingHash, isAttacking);
        }

        private void SetState(AIState newState)
        {
            currentState = newState;
        }

        private void HandleZombieDeath()
        {
            currentState = AIState.Dead;
            StopNavMeshMovement();
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            float detRad = zombieData != null ? zombieData.detectionRadius : 12f;
            float atkRad = zombieData != null ? zombieData.attackRange : 1.8f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detRad);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, atkRad);
        }
    }
}
