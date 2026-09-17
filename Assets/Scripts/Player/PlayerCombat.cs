using System.Collections;
using UnityEngine;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Manages player melee combat, attack cooldowns, forward directional hit filtering,
    /// dedicated enemy LayerMask query, and damage application to Zombie targets.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Combat Configuration")]
        [SerializeField] private float attackDamage = 35f;
        [SerializeField] private float attackRange = 2.0f;
        [SerializeField] private float attackRadius = 1.2f;
        [SerializeField] private float attackCooldown = 0.6f;

        [Header("Layer Filtering")]
        [SerializeField] private LayerMask enemyLayerMask = (1 << 6) | (1 << 7); // Default to Zombie (Layer 6) and Enemy (Layer 7)

        [Header("Attack Offset")]
        [SerializeField] private Vector3 attackPointOffset = new Vector3(0f, 1.0f, 1.0f);

        private PlayerInputHandler inputHandler;
        private PlayerAnimation playerAnimation;
        private PlayerHealth playerHealth;

        private float lastAttackTime;
        private bool isAttacking;

        public bool IsAttacking => isAttacking;

        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            playerAnimation = GetComponent<PlayerAnimation>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            // Player Death Safety: Disable combat if player is dead
            if (playerHealth != null && playerHealth.IsDead) return;

            if (inputHandler != null && inputHandler.AttackTriggered)
            {
                TryPerformAttack();
                inputHandler.ResetAttackTrigger();
            }
        }

        public bool CanAttack()
        {
            if (isAttacking) return false;
            if (playerHealth != null && playerHealth.IsDead) return false;
            return Time.time >= lastAttackTime + attackCooldown;
        }

        public void TryPerformAttack()
        {
            if (!CanAttack()) return;

            lastAttackTime = Time.time;
            StartCoroutine(PerformAttackSequence());
        }

        private IEnumerator PerformAttackSequence()
        {
            isAttacking = true;

            // Trigger Attack animation
            if (playerAnimation != null)
            {
                playerAnimation.TriggerAttack();
            }

            yield return new WaitForSeconds(0.15f);

            // Forward Attack Point calculation
            Vector3 attackPosition = transform.TransformPoint(attackPointOffset);

            // LayerMask OverlapSphere query: Only detects objects on the dedicated enemy/zombie layer
            Collider[] hitColliders = Physics.OverlapSphere(attackPosition, attackRadius, enemyLayerMask, QueryTriggerInteraction.Ignore);

            foreach (var col in hitColliders)
            {
                // Ignore self / child colliders
                if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;

                // Directional Forward Check: Ensure zombie is in front of the player (not behind)
                Vector3 dirToTarget = (col.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToTarget);

                if (dot < -0.2f)
                {
                    // Target is behind player, ignore
                    continue;
                }

                // Retrieve ZombieHealth component
                ZombieHealth zombieHealth = col.GetComponent<ZombieHealth>();
                if (zombieHealth == null)
                {
                    zombieHealth = col.GetComponentInParent<ZombieHealth>();
                }

                if (zombieHealth != null && !zombieHealth.IsDead)
                {
                    zombieHealth.TakeDamage(attackDamage);
                    Debug.Log($"[PlayerCombat] Player struck {col.name} dealing {attackDamage} damage.");
                }
            }

            yield return new WaitForSeconds(Mathf.Max(0.05f, attackCooldown - 0.15f));
            isAttacking = false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 attackPosition = transform.TransformPoint(attackPointOffset);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPosition, attackRadius);
        }
    }
}
