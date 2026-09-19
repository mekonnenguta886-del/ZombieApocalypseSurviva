using UnityEngine;

namespace ZombieApocalypse.Zombies
{
    public enum ZombieType
    {
        Walker,
        Runner,
        Tank,
        Boss
    }

    /// <summary>
    /// ScriptableObject data container holding all configuration stats for zombie types.
    /// Used by ZombieAI and ZombieHealth for centralized configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewZombieData", menuName = "Zombie Apocalypse/Zombie Data")]
    public class ZombieData : ScriptableObject
    {
        [Header("Identity")]
        public string zombieName = "Walker";
        public ZombieType zombieType = ZombieType.Walker;

        [Header("Attributes & Combat Timing")]
        public float maxHealth = 100f;
        public float moveSpeed = 2.5f;
        public float attackDamage = 15f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1.2f;
        public float attackWindupTime = 0.4f;
        public float staggerThreshold = 30.0f;
        public bool hasStaggerArmor = false;

        [Header("Resistances & Weights")]
        [Range(0f, 1f)] public float damageResistance = 0.0f;
        [Range(0f, 2f)] public float staggerResistance = 0.0f;
        [Range(0f, 1f)] public float knockbackResistance = 0.0f;
        public float spawnWeight = 1.0f;
        public float difficultyMultiplier = 1.0f;

        [Header("Boss Special Abilities")]
        public bool isBoss = false;
        public bool enableGroundSlam = false;
        public float groundSlamDamage = 30.0f;
        public float groundSlamRadius = 4.0f;
        public float groundSlamCooldown = 8.0f;

        public bool enableRoar = false;
        public float roarNoiseRadius = 35.0f;
        public float roarCooldown = 12.0f;

        public bool enableRageState = false;
        [Range(0.1f, 0.9f)] public float rageHealthThreshold = 0.35f;
        public float rageSpeedMultiplier = 1.35f;
        public float rageDamageMultiplier = 1.25f;
        public float rageCooldownMultiplier = 0.75f;

        [Header("Perception & Hearing")]
        public float detectionRadius = 12.0f;
        public float loseTargetRadius = 16.0f;
        public float sightDistance = 12.0f;
        public float fieldOfViewAngle = 110.0f;
        public float hearingMultiplier = 1.0f;

        [Header("Investigation & Search")]
        public float searchRadius = 6.0f;
        public float investigateDuration = 5.0f;

        [Header("Cleanup")]
        public float deathDelay = 5.0f;
    }
}
