using System.Collections.Generic;
using UnityEngine;

namespace ZombieApocalypse.Progression
{
    /// <summary>
    /// ScriptableObject configuration for player progression thresholds, XP source values, and skill limits.
    /// Centralizes XP curves so progression metrics remain data-driven and decoupled from gameplay logic.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerProgressionData", menuName = "Zombie Apocalypse/Player Progression Data")]
    public class PlayerProgressionData : ScriptableObject
    {
        [Header("Leveling Config")]
        public int startingLevel = 1;
        public int maxLevel = 50;
        public int skillPointsPerLevel = 1;

        [Header("XP Rewards")]
        public int xpPerZombieKill = 25;
        public int xpPerMissionCompleted = 200;

        [Header("Skill Config")]
        public int maxSkillLevel = 5;

        [Header("Level XP Thresholds (Index 0 = Level 1->2 required XP)")]
        [SerializeField] private List<int> customThresholds = new List<int>
        {
            100, // Level 1 -> 2
            250, // Level 2 -> 3
            450, // Level 3 -> 4
            700, // Level 4 -> 5
            1000, // Level 5 -> 6
            1350, // Level 6 -> 7
            1750, // Level 7 -> 8
            2200, // Level 8 -> 9
            2700  // Level 9 -> 10
        };

        /// <summary>
        /// Calculates or looks up the required XP to advance from currentLevel to currentLevel + 1.
        /// Uses custom threshold array when available or a scalable curve formula as fallback.
        /// </summary>
        public int GetXPRequiredForNextLevel(int currentLevel)
        {
            if (currentLevel < 1) currentLevel = 1;
            if (currentLevel >= maxLevel) return int.MaxValue;

            int index = currentLevel - 1;
            if (customThresholds != null && index >= 0 && index < customThresholds.Count)
            {
                return customThresholds[index];
            }

            // Fallback scalable curve formula: 100 + (level - 1) * 150
            return 100 + (currentLevel - 1) * 150;
        }
    }
}
