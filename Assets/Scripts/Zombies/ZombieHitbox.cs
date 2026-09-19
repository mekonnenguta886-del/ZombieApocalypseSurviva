using UnityEngine;

namespace ZombieApocalypse.Zombies
{
    public enum HitboxType
    {
        Head,
        Body,
        Limb
    }

    /// <summary>
    /// Attached to specific child colliders (e.g. head collider) on zombie prefabs.
    /// Passes damage multiplier info back to parent ZombieHealth component on hit.
    /// </summary>
    public class ZombieHitbox : MonoBehaviour
    {
        [Header("Hitbox Properties")]
        [SerializeField] private HitboxType hitboxType = HitboxType.Head;
        [SerializeField] private float damageMultiplier = 2.0f;
        [SerializeField] private ZombieHealth ownerHealth;

        public HitboxType Type => hitboxType;
        public float DamageMultiplier => damageMultiplier;
        public ZombieHealth OwnerHealth => ownerHealth;

        private void Awake()
        {
            if (ownerHealth == null)
            {
                ownerHealth = GetComponentInParent<ZombieHealth>();
            }
        }
    }
}
