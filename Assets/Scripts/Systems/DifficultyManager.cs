using System;
using UnityEngine;

namespace ZombieApocalypse.Systems
{
    [Serializable]
    public struct DifficultyStats
    {
        public int maxActiveZombies;
        public float spawnInterval;
        public float walkerWeight;
        public float runnerWeight;
        public float tankWeight;
        public float difficultyMultiplier;

        public DifficultyStats(int maxActive = 10, float interval = 20.0f, float walker = 0.6f, float runner = 0.3f, float tank = 0.1f, float multiplier = 1.0f)
        {
            maxActiveZombies = maxActive;
            spawnInterval = interval;
            walkerWeight = walker;
            runnerWeight = runner;
            tankWeight = tank;
            difficultyMultiplier = multiplier;
        }
    }

    /// <summary>
    /// Singleton managing dynamic game difficulty levels and spawning stat multipliers.
    /// Provides GetCurrentDifficulty() to ZombieSpawner and WorldEventManager.
    ///
    /// ATTACH TO: [DifficultyManager] GameObject in scene.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Header("Difficulty Configuration")]
        [SerializeField] private int baseMaxActiveZombies = 10;
        [SerializeField] private float baseSpawnInterval = 20.0f;
        [SerializeField] private float walkerWeight = 0.6f;
        [SerializeField] private float runnerWeight = 0.3f;
        [SerializeField] private float tankWeight = 0.1f;
        [SerializeField] private float currentMultiplier = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public DifficultyStats GetCurrentDifficulty()
        {
            return new DifficultyStats(
                maxActiveZombies: Mathf.RoundToInt(baseMaxActiveZombies * currentMultiplier),
                interval: Mathf.Max(5.0f, baseSpawnInterval / currentMultiplier),
                walker: walkerWeight,
                runner: runnerWeight,
                tank: tankWeight,
                multiplier: currentMultiplier
            );
        }

        public void SetDifficultyMultiplier(float multiplier)
        {
            currentMultiplier = Mathf.Clamp(multiplier, 0.5f, 3.0f);
            Debug.Log($"[DifficultyManager] Difficulty multiplier updated to: {currentMultiplier:F2}");
        }
    }
}
