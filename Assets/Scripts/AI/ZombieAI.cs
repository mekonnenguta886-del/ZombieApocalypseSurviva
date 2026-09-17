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
    /// Simple finite state machine zombie AI.
    /// Handles player detection, pathfinding chase, attack execution, and cooldowns.
    /// 
    /// ATTACH TO: Zombie prefab GameObject.
    /// </summary>
    public class ZombieAI : MonoBehaviour
    {
        [Header("Zombie Data Config")]
        [SerializeField] private ZombieData zombieData;

        [Header("Perception & Attack Settings")]
        [SerializeField] private float detectionRadius = 12.0f;
        [SerializeField] private float loseTargetRadius = 16.0f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 15.0f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float rotationSpeed = 8.0f;

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

            if (zombieData != null)
            {
                detectionRadius = zombieData.detectionRadius;
                attackRange = zombieData.attackRange;
                attackDamage = zombieData.attackDamage;
                attackCooldown = zombieData.attackCooldown;
                moveSpeed = zombieData.moveSpeed;
            }
        }

        private void Start()
        {
            FindPlayerReference();

            if (navMeshAgent != null)
            {
                navMeshAgent.speed = moveSpeed;
                navMeshAgent.stoppingDistance = attackRange * 0.8f;
            }

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
            if (zombieHealth != null && zombieHealth.IsDead)
            {
                currentState = AIState.Dead;
                return;
            }

            if (playerTransform == null || playerHealth == null)
            {
                FindPlayerReference();
                if (playerTransform == null) return;
            }

            // Stop AI if player is dead
            if (playerHealth.IsDead)
            {
                SetState(AIState.Idle);
                UpdateAnimator(0f, false);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            switch (currentState)
            {
                case AIState.Idle:
                    UpdateIdleState(distanceToPlayer);
                    break;
                case AIState.Chase:
                    UpdateChaseState(distanceToPlayer);
                    break;
                case AIState.Attack:
                    UpdateAttackState(distanceToPlayer);
                    break;
            }
        }

        private void UpdateIdleState(float distanceToPlayer)
        {
            UpdateAnimator(0f, false);

            if (distanceToPlayer <= detectionRadius)
            {
                SetState(AIState.Chase);
            }
        }

        private void UpdateChaseState(float distanceToPlayer)
        {
            if (distanceToPlayer > loseTargetRadius)
            {
                SetState(AIState.Idle);
                if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.ResetPath();
                }
                return;
            }

            if (distanceToPlayer <= attackRange)
            {
                SetState(AIState.Attack);
                return;
            }

            // Move towards player
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(playerTransform.position);
            }
            else
            {
                // Fallback direct movement
                Vector3 moveDir = (playerTransform.position - transform.position).normalized;
                moveDir.y = 0f;
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }

            // Smooth rotation towards player
            RotateTowardsPlayer();
            UpdateAnimator(moveSpeed, false);
        }

        private void UpdateAttackState(float distanceToPlayer)
        {
            if (distanceToPlayer > attackRange * 1.3f)
            {
                SetState(AIState.Chase);
                return;
            }

            // Stop movement
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
            }

            RotateTowardsPlayer();
            UpdateAnimator(0f, true);

            // Execute Attack on Cooldown
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                ExecuteAttack();
            }
        }

        private void ExecuteAttack()
        {
            if (playerHealth != null && !playerHealth.IsDead)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"[ZombieAI] Zombie {gameObject.name} attacked player dealing {attackDamage} damage!");
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
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
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
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
