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
    /// ScriptableObject defining stats for zombie variants.
    /// </summary>
    [CreateAssetMenu(fileName = "NewZombieData", menuName = "Zombie Apocalypse/Zombie Data")]
    public class ZombieData : ScriptableObject
    {
        [Header("Identity")]
        public string zombieName = "Walker";
        public ZombieType zombieType = ZombieType.Walker;

        [Header("Attributes")]
        public float maxHealth = 100f;
        public float moveSpeed = 2.5f;
        public float attackDamage = 15f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1.2f;

        [Header("Perception")]
        public float detectionRadius = 12f;
    }
}
