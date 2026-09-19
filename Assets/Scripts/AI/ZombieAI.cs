using System;
using UnityEngine;
using UnityEngine.AI;
using ZombieApocalypse.Player;
using ZombieApocalypse.World;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.AI
{
    public enum AIState
    {
        Idle,
        Patrol,
        Investigate,
        Chase,
        Attack,
        Search,
        ReturnToPatrol,
        Dead
    }

    /// <summary>
    /// Upgraded Phase 8 NavMeshAgent-based Zombie AI state machine.
    /// Manages 8 AI states (Idle, Patrol, Investigate, Chase, Attack, Search, ReturnToPatrol, Dead),
    /// event-driven noise hearing, interval-based visual FOV perception, non-recursive group alerts,
    /// Safe Zone target drop, and deterministic patrol pathfinding.
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

        [Header("Perception Layer Mask")]
        [SerializeField] private LayerMask obstacleLayerMask = ~0;

        [Header("Patrol Config")]
        [SerializeField] private Transform[] patrolWaypoints;
        [SerializeField] private float patrolRadius = 10.0f;
        [SerializeField] private float patrolWaitTime = 3.0f;
        [SerializeField] private int maxPatrolPointAttempts = 5;

        // References
        private NavMeshAgent navMeshAgent;
        private ZombieHealth zombieHealth;
        private Animator animator;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private PlayerController playerController;

        // Runtime AI States
        private Vector3 initialSpawnPosition;
        private Vector3 targetNoisePosition;
        private Vector3 currentPatrolDestination;
        private float lastAttackTime;
        private float lastPerceptionCheckTime;
        private float perceptionCheckInterval = 0.2f;
        private float patrolWaitTimer;
        private float searchTimer;
        private float investigateTimer;
        private float lastAlertTime;
        private float alertCooldown = 3.0f;
        private int currentWaypointIndex = 0;
        private bool isWaitingAtPatrolPoint = false;

        public AIState CurrentState => currentState;

        // Parameter Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            zombieHealth = GetComponent<ZombieHealth>();
            animator = GetComponentInChildren<Animator>();

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        private void Start()
        {
            initialSpawnPosition = transform.position;
            ApplyZombieDataConfig();
            FindPlayerReference();

            if (zombieHealth != null)
            {
                zombieHealth.OnZombieDied += HandleZombieDeath;
            }

            NoiseManager.OnNoiseEmitted += HandleNoiseEvent;
        }

        private void OnDestroy()
        {
            if (zombieHealth != null)
            {
                zombieHealth.OnZombieDied -= HandleZombieDeath;
            }

            NoiseManager.OnNoiseEmitted -= HandleNoiseEvent;
        }

        public void Initialize(ZombieData data)
        {
            if (data != null)
            {
                zombieData = data;
            }
            ApplyZombieDataConfig();
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
                playerController = playerObj.GetComponent<PlayerController>();
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

            // Player Death Safety Guard: If player is dead, halt chase/attack and return to Idle
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

            // Safe Zone Guard: If player is in Safe House, drop chase target immediately
            if (SafeZoneTrigger.IsPlayerInSafeZone && (currentState == AIState.Chase || currentState == AIState.Attack))
            {
                SetState(AIState.ReturnToPatrol);
                return;
            }

            // Throttled Visual Perception Check
            if (Time.time >= lastPerceptionCheckTime + perceptionCheckInterval)
            {
                lastPerceptionCheckTime = Time.time;
                CheckVisualPerception();
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            float loseRad = zombieData != null ? zombieData.loseTargetRadius : 16f;
            float atkRange = zombieData != null ? zombieData.attackRange : 1.8f;

            switch (currentState)
            {
                case AIState.Idle:
                    UpdateAnimator(0f, false);
                    patrolWaitTimer += Time.deltaTime;
                    if (patrolWaitTimer >= patrolWaitTime)
                    {
                        patrolWaitTimer = 0f;
                        SetState(AIState.Patrol);
                    }
                    break;

                case AIState.Patrol:
                    ExecutePatrolBehavior();
                    break;

                case AIState.Investigate:
                    ExecuteInvestigateBehavior();
                    break;

                case AIState.Search:
                    ExecuteSearchBehavior();
                    break;

                case AIState.Chase:
                    if (distanceToPlayer > loseRad || SafeZoneTrigger.IsPlayerInSafeZone)
                    {
                        SetState(AIState.Search);
                        return;
                    }

                    if (distanceToPlayer <= atkRange)
                    {
                        SetState(AIState.Attack);
                        return;
                    }

                    SetNavMeshDestination(playerTransform.position);
                    UpdateAnimator(navMeshAgent != null ? navMeshAgent.speed : 2.5f, false);
                    break;

                case AIState.Attack:
                    if (distanceToPlayer > atkRange * 1.3f)
                    {
                        SetState(AIState.Chase);
                        return;
                    }

                    StopNavMeshMovement();
                    RotateTowardsPlayer();
                    UpdateAnimator(0f, true);

                    float cooldown = zombieData != null ? zombieData.attackCooldown : 1.2f;
                    if (Time.time >= lastAttackTime + cooldown)
                    {
                        lastAttackTime = Time.time;
                        ExecuteAttack();
                    }
                    break;

                case AIState.ReturnToPatrol:
                    ExecuteReturnToPatrolBehavior();
                    break;
            }
        }

        private void CheckVisualPerception()
        {
            if (playerTransform == null || playerHealth == null || playerHealth.IsDead) return;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return;
            if (currentState == AIState.Chase || currentState == AIState.Attack || currentState == AIState.Dead) return;

            float sightDist = zombieData != null ? zombieData.sightDistance : 12f;
            if (playerController != null && playerController.IsCrouching)
            {
                sightDist *= 0.5f; // Crouch stealth footprint modifier
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer > sightDist) return;

            // FOV Angle Check
            Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            float fov = zombieData != null ? zombieData.fieldOfViewAngle : 110f;

            if (angle <= fov * 0.5f)
            {
                // Line of Sight Occlusion Raycast
                Vector3 eyePos = transform.position + Vector3.up * 1.5f;
                Vector3 playerCenter = playerTransform.position + Vector3.up * 1.0f;

                if (!Physics.Linecast(eyePos, playerCenter, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    // Player Spotted! Transition to Chase and alert nearby zombies
                    bool firstSpot = (currentState != AIState.Chase);
                    SetState(AIState.Chase);

                    if (firstSpot)
                    {
                        ZombieGroupAlert.AlertNearbyZombies(transform.position, 10.0f, playerTransform.position, this);
                    }
                }
            }
        }

        private void HandleNoiseEvent(NoiseEvent noiseEvent)
        {
            if (zombieHealth != null && zombieHealth.IsDead) return;
            if (playerHealth != null && playerHealth.IsDead) return;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return;
            if (currentState == AIState.Chase || currentState == AIState.Attack || currentState == AIState.Dead) return;

            float mult = zombieData != null ? zombieData.hearingMultiplier : 1.0f;
            float effectiveRadius = noiseEvent.radius * mult;

            float dist = Vector3.Distance(transform.position, noiseEvent.position);
            if (dist <= effectiveRadius)
            {
                targetNoisePosition = noiseEvent.position;
                SetState(AIState.Investigate);
            }
        }

        public void ReceiveGroupAlert(Vector3 targetPosition)
        {
            if (zombieHealth != null && zombieHealth.IsDead) return;
            if (playerHealth != null && playerHealth.IsDead) return;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return;
            if (currentState == AIState.Chase || currentState == AIState.Attack || currentState == AIState.Dead) return;

            if (Time.time < lastAlertTime + alertCooldown) return;
            lastAlertTime = Time.time;

            targetNoisePosition = targetPosition;
            SetState(AIState.Investigate);
        }

        private void ExecutePatrolBehavior()
        {
            if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh) return;

            if (isWaitingAtPatrolPoint)
            {
                UpdateAnimator(0f, false);
                patrolWaitTimer += Time.deltaTime;
                if (patrolWaitTimer >= patrolWaitTime)
                {
                    patrolWaitTimer = 0f;
                    isWaitingAtPatrolPoint = false;
                    SelectNextPatrolDestination();
                }
                return;
            }

            if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= 1.0f)
            {
                isWaitingAtPatrolPoint = true;
                patrolWaitTimer = 0f;
                StopNavMeshMovement();
                UpdateAnimator(0f, false);
                return;
            }

            UpdateAnimator(navMeshAgent.speed, false);
        }

        private void SelectNextPatrolDestination()
        {
            if (patrolWaypoints != null && patrolWaypoints.Length > 0)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                Transform targetWp = patrolWaypoints[currentWaypointIndex];
                if (targetWp != null)
                {
                    currentPatrolDestination = targetWp.position;
                    SetNavMeshDestination(currentPatrolDestination);
                    return;
                }
            }

            // Fallback: Random NavMesh point near initial spawn origin
            Vector3 origin = initialSpawnPosition;
            for (int i = 0; i < maxPatrolPointAttempts; i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * patrolRadius;
                Vector3 candidate = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    currentPatrolDestination = hit.position;
                    SetNavMeshDestination(currentPatrolDestination);
                    return;
                }
            }

            currentPatrolDestination = initialSpawnPosition;
            SetNavMeshDestination(currentPatrolDestination);
        }

        private void ExecuteInvestigateBehavior()
        {
            investigateTimer += Time.deltaTime;
            SetNavMeshDestination(targetNoisePosition);
            UpdateAnimator(navMeshAgent != null ? navMeshAgent.speed : 2.5f, false);

            if ((navMeshAgent != null && navMeshAgent.remainingDistance <= 1.5f) || investigateTimer >= 12.0f)
            {
                investigateTimer = 0f;
                SetState(AIState.Search);
            }
        }

        private void ExecuteSearchBehavior()
        {
            searchTimer += Time.deltaTime;
            float maxSearchTime = zombieData != null ? zombieData.investigateDuration : 5.0f;

            if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= 1.0f)
            {
                float radius = zombieData != null ? zombieData.searchRadius : 6.0f;
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 searchCandidate = targetNoisePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(searchCandidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    SetNavMeshDestination(hit.position);
                }
            }

            UpdateAnimator(navMeshAgent != null ? navMeshAgent.speed * 0.7f : 1.8f, false);

            if (searchTimer >= maxSearchTime)
            {
                searchTimer = 0f;
                SetState(AIState.ReturnToPatrol);
            }
        }

        private void ExecuteReturnToPatrolBehavior()
        {
            SetNavMeshDestination(initialSpawnPosition);
            UpdateAnimator(navMeshAgent != null ? navMeshAgent.speed : 2.5f, false);

            if (navMeshAgent != null && navMeshAgent.remainingDistance <= 1.5f)
            {
                SetState(AIState.Patrol);
            }
        }

        private void SetNavMeshDestination(Vector3 destination)
        {
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(destination);
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
            if (currentState == newState) return;

            currentState = newState;
            if (currentState == AIState.Patrol)
            {
                SelectNextPatrolDestination();
            }
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
