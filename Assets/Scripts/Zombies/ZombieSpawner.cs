using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieApocalypse.Zombies
{
    /// <summary>
    /// Spawns zombie enemies at designated spawn point transforms within a configurable radius.
    /// Easily expandable for wave systems in future phases.
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

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        private List<GameObject> spawnedZombies = new List<GameObject>();

        public IReadOnlyList<GameObject> SpawnedZombies => spawnedZombies;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnZombies();
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
            }

            Debug.Log($"[ZombieSpawner] Successfully spawned {numberOfZombiesToSpawn} zombies.");
        }

        private Vector3 GetRandomSpawnPosition(int index)
        {
            Vector3 basePoint = transform.position;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform targetPoint = spawnPoints[index % spawnPoints.Length];
                if (targetPoint != null)
                {
                    basePoint = targetPoint.position;
                }
            }

            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            return basePoint + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, spawnRadius);
                    }
                }
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }
        }
    }
}
