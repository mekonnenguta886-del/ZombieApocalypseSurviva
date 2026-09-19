using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ZombieApocalypse.AI;
using ZombieApocalypse.Player;
using ZombieApocalypse.Systems;
using ZombieApocalypse.World;

namespace ZombieApocalypse.Zombies
{
    /// <summary>
    /// Single authoritative Zombie Spawner system for Phase 8.
    /// Handles initial spawn points, dynamic continuous proximity spawning, active zombie count tracking
    /// via event-driven cleanup and defensive pruning, dynamic difficulty caps, variant selection,
    /// and multi-step validation (NavMesh, Safe Zone, player distance, camera FOV).
    /// 
    /// ATTACH TO: [ZombieSpawner] GameObject in environment scenes.
    /// </summary>
    public class ZombieSpawner : MonoBehaviour
    {
        [Header("Spawn Config")]
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private int numberOfZombiesToSpawn = 3;
        [SerializeField] private float spawnRadius = 4.0f;
        [SerializeField] private bool spawnOnStart = true;

        [Header("Dynamic Spawning Config")]
        [SerializeField] private bool enableDynamicSpawning = true;
        [SerializeField] private float minSpawnDistance = 20.0f;
        [SerializeField] private float maxSpawnDistance = 50.0f;
        [SerializeField] private LayerMask obstacleLayerMask = ~0;

        [Header("Zombie Variant Data References")]
        [SerializeField] private ZombieData walkerData;
        [SerializeField] private ZombieData runnerData;
        [SerializeField] private ZombieData tankData;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        private List<ZombieHealth> activeLivingZombies = new List<ZombieHealth>();
        private List<GameObject> spawnedZombies = new List<GameObject>();
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private Camera mainCamera;
        private float lastDynamicSpawnTime;

        public IReadOnlyList<GameObject> SpawnedZombies => spawnedZombies;
        public int ActiveLivingZombieCount
        {
            get
            {
                PruneActiveZombiesList();
                return activeLivingZombies.Count;
            }
        }

        private void OnEnable()
        {
            ZombieHealth.OnZombieKilled += HandleZombieKilled;
        }

        private void OnDisable()
        {
            ZombieHealth.OnZombieKilled -= HandleZombieKilled;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            EnsureDefaultVariantData();
            FindPlayerReference();

            if (spawnOnStart)
            {
                SpawnZombies();
            }

            lastDynamicSpawnTime = Time.time;
        }

        private void EnsureDefaultVariantData()
        {
            if (walkerData == null)
            {
                walkerData = ScriptableObject.CreateInstance<ZombieData>();
                walkerData.zombieName = "Walker Zombie";
                walkerData.zombieType = ZombieType.Walker;
                walkerData.maxHealth = 100f;
                walkerData.moveSpeed = 2.5f;
                walkerData.attackDamage = 15f;
                walkerData.attackRange = 1.8f;
                walkerData.attackCooldown = 1.2f;
                walkerData.sightDistance = 12.0f;
                walkerData.fieldOfViewAngle = 110.0f;
                walkerData.hearingMultiplier = 1.0f;
            }

            if (runnerData == null)
            {
                runnerData = ScriptableObject.CreateInstance<ZombieData>();
                runnerData.zombieName = "Fast Runner Zombie";
                runnerData.zombieType = ZombieType.Runner;
                runnerData.maxHealth = 60f;
                runnerData.moveSpeed = 4.6f;
                runnerData.attackDamage = 10f;
                runnerData.attackRange = 1.6f;
                runnerData.attackCooldown = 0.8f;
                runnerData.sightDistance = 16.0f;
                runnerData.fieldOfViewAngle = 120.0f;
                runnerData.hearingMultiplier = 1.2f;
            }

            if (tankData == null)
            {
                tankData = ScriptableObject.CreateInstance<ZombieData>();
                tankData.zombieName = "Tank Zombie";
                tankData.zombieType = ZombieType.Tank;
                tankData.maxHealth = 300f;
                tankData.moveSpeed = 1.7f;
                tankData.attackDamage = 35f;
                tankData.attackRange = 2.2f;
                tankData.attackCooldown = 2.0f;
                tankData.sightDistance = 10.0f;
                tankData.fieldOfViewAngle = 90.0f;
                tankData.hearingMultiplier = 0.8f;
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
            if (!enableDynamicSpawning) return;

            if (playerTransform == null || playerHealth == null)
            {
                FindPlayerReference();
                if (playerTransform == null) return;
            }

            // Player Death Guard: Stop dynamic spawning when player is dead
            if (playerHealth.IsDead) return;

            DifficultyStats difficulty = DifficultyManager.Instance != null 
                ? DifficultyManager.Instance.GetCurrentDifficulty() 
                : new DifficultyStats { maxActiveZombies = 10, spawnInterval = 20.0f, walkerWeight = 1.0f };

            if (Time.time >= lastDynamicSpawnTime + difficulty.spawnInterval)
            {
                lastDynamicSpawnTime = Time.time;

                if (ActiveLivingZombieCount < difficulty.maxActiveZombies)
                {
                    TryDynamicSpawn(difficulty);
                }
            }
        }

        public void SpawnZombies()
        {
            if (zombiePrefab == null)
            {
                Debug.LogWarning("[ZombieSpawner] Zombie Prefab is not assigned.");
                return;
            }

            for (int i = 0; i < numberOfZombiesToSpawn; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition(i);
                GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity, transform);
                zombie.name = $"Zombie_{i + 1}";
                spawnedZombies.Add(zombie);

                ZombieHealth health = zombie.GetComponent<ZombieHealth>();
                ZombieAI ai = zombie.GetComponent<ZombieAI>();

                // Select initial variant (Default to Walker if assigned)
                ZombieData data = walkerData != null ? walkerData : (health != null ? health.Data : null);
                if (health != null)
                {
                    health.Initialize(data);
                    activeLivingZombies.Add(health);
                }
                if (ai != null)
                {
                    ai.Initialize(data);
                }
            }

            Debug.Log($"[ZombieSpawner] Successfully spawned {numberOfZombiesToSpawn} initial zombies.");
        }

        private void TryDynamicSpawn(DifficultyStats difficulty)
        {
            if (zombiePrefab == null || playerTransform == null) return;

            // 5 Bounded Placement Attempts
            for (int attempt = 0; attempt < 5; attempt++)
            {
                float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector2 randomCircle = Random.insideUnitCircle.normalized * randomDist;
                Vector3 candidatePos = playerTransform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Validation 1: NavMesh
                if (!NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                {
                    continue;
                }

                Vector3 validPos = hit.position;

                // Validation 2: Safe Zone Bounds
                if (SafeZoneTrigger.IsPositionInSafeZone(validPos))
                {
                    continue;
                }

                // Validation 3: Camera FOV / Line of Sight
                if (IsPositionInPlayerFOV(validPos))
                {
                    continue;
                }

                // Valid point found! Select variant based on difficulty weights
                ZombieData variantData = SelectVariantData(difficulty);

                GameObject zombie = Instantiate(zombiePrefab, validPos, Quaternion.identity, transform);
                zombie.name = $"DynamicZombie_{variantData?.zombieName ?? "Walker"}_{activeLivingZombies.Count + 1}";
                spawnedZombies.Add(zombie);

                ZombieHealth health = zombie.GetComponent<ZombieHealth>();
                ZombieAI ai = zombie.GetComponent<ZombieAI>();

                if (health != null)
                {
                    health.Initialize(variantData);
                    activeLivingZombies.Add(health);
                }
                if (ai != null)
                {
                    ai.Initialize(variantData);
                }

                Debug.Log($"[ZombieSpawner] Dynamically spawned {variantData?.zombieName} at {validPos}. Active Living: {activeLivingZombies.Count}");
                return;
            }
        }

        /// <summary>
        /// Single-owner encounter wave spawning method for Phase 9.
        /// Spawns up to requestedCount zombies, strictly capped at available slots before reaching maxActiveZombies.
        /// </summary>
        public void TriggerEncounterWave(int requestedCount)
        {
            TriggerEncounterWaveWithCallback(requestedCount);
        }

        /// <summary>
        /// Single-owner encounter wave spawning method for Phase 10 world events.
        /// Spawns up to requestedCount zombies, capped under maxActiveZombies, and returns the exact list of spawned ZombieHealth components.
        /// </summary>
        public List<ZombieHealth> TriggerEncounterWaveWithCallback(int requestedCount)
        {
            List<ZombieHealth> spawnedList = new List<ZombieHealth>();
            if (zombiePrefab == null || playerTransform == null) return spawnedList;
            if (playerHealth != null && playerHealth.IsDead) return spawnedList;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return spawnedList;

            DifficultyStats difficulty = DifficultyManager.Instance != null 
                ? DifficultyManager.Instance.GetCurrentDifficulty() 
                : new DifficultyStats { maxActiveZombies = 10, spawnInterval = 20.0f, walkerWeight = 0.6f, runnerWeight = 0.3f, tankWeight = 0.1f };

            int availableSlots = difficulty.maxActiveZombies - ActiveLivingZombieCount;
            if (availableSlots <= 0)
            {
                Debug.LogWarning("[ZombieSpawner] Encounter wave requested, but maxActiveZombies cap is reached. 0 zombies spawned.");
                return spawnedList;
            }

            int spawnCount = Mathf.Min(requestedCount, availableSlots);
            int actualSpawned = 0;

            for (int i = 0; i < spawnCount; i++)
            {
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
                    Vector2 randomCircle = Random.insideUnitCircle.normalized * randomDist;
                    Vector3 candidatePos = playerTransform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                    if (!NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas)) continue;
                    if (SafeZoneTrigger.IsPositionInSafeZone(hit.position)) continue;
                    if (IsPositionInPlayerFOV(hit.position)) continue;

                    ZombieData variantData = SelectVariantData(difficulty);
                    GameObject zombie = Instantiate(zombiePrefab, hit.position, Quaternion.identity, transform);
                    zombie.name = $"HordeZombie_{variantData?.zombieName ?? "Walker"}_{activeLivingZombies.Count + 1}";
                    spawnedZombies.Add(zombie);

                    ZombieHealth health = zombie.GetComponent<ZombieHealth>();
                    ZombieAI ai = zombie.GetComponent<ZombieAI>();

                    if (health != null)
                    {
                        health.Initialize(variantData);
                        activeLivingZombies.Add(health);
                        spawnedList.Add(health);
                    }
                    if (ai != null)
                    {
                        ai.Initialize(variantData);
                    }

                    actualSpawned++;
                    break;
                }
            }

            Debug.Log($"[ZombieSpawner] Triggered Horde Encounter Wave. Requested: {requestedCount}, Available: {availableSlots}, Spawned: {actualSpawned}");
            return spawnedList;
        }

        private bool IsPositionInPlayerFOV(Vector3 position)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return false;

            Vector3 camPos = mainCamera.transform.position;
            Vector3 dirToTarget = (position - camPos).normalized;
            float angle = Vector3.Angle(mainCamera.transform.forward, dirToTarget);

            // If within camera FOV cone, check line of sight occlusion
            if (angle <= mainCamera.fieldOfView * 0.6f)
            {
                // If line of sight is unobstructed, target is visible in camera FOV
                if (!Physics.Linecast(camPos, position + Vector3.up * 1.0f, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    return true; // Position is visible inside player FOV
                }
            }

            return false; // Position is outside camera FOV or occluded by geometry
        }

        private ZombieData SelectVariantData(DifficultyStats difficulty)
        {
            float roll = Random.value;

            if (roll < difficulty.tankWeight && tankData != null)
            {
                return tankData;
            }
            else if (roll < (difficulty.tankWeight + difficulty.runnerWeight) && runnerData != null)
            {
                return runnerData;
            }

            return walkerData != null ? walkerData : (zombiePrefab != null ? zombiePrefab.GetComponent<ZombieHealth>()?.Data : null);
        }

        private Vector3 GetRandomSpawnPosition(int index)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform sp = spawnPoints[index % spawnPoints.Length];
                if (sp != null)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                    Vector3 candidate = sp.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                    if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                    {
                        return hit.position;
                    }
                    return sp.position;
                }
            }

            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + new Vector3(circle.x, 0f, circle.y);
            if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
            {
                return navHit.position;
            }
            return transform.position;
        }

        private void HandleZombieKilled(ZombieHealth zombieHealth)
        {
            if (zombieHealth != null && activeLivingZombies.Contains(zombieHealth))
            {
                activeLivingZombies.Remove(zombieHealth);
            }
        }

        private void PruneActiveZombiesList()
        {
            for (int i = activeLivingZombies.Count - 1; i >= 0; i--)
            {
                if (activeLivingZombies[i] == null || activeLivingZombies[i].IsDead)
                {
                    activeLivingZombies.RemoveAt(i);
                }
            }
        }
    }
}
