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
    /// ScriptableObject defining stats for zombie variants (Walker, Runner, Tank, Boss).
    /// </summary>
    [CreateAssetMenu(fileName = "NewZombieData", menuName = "Zombie Apocalypse/Zombie Data")]
    public class ZombieData : ScriptableObject
    {
        [Header("Identity")]
        public string zombieName = "Walker";
        public ZombieType zombieType = ZombieType.Walker;

        [Header("Attributes")]
        public float maxHealth = 100f;
        public float moveSpeed = 2.0f;
        public float attackDamage = 15f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.2f;

        [Header("Perception")]
        public float sightDistance = 15f;
        public float hearingRadius = 10f;
    }
}
