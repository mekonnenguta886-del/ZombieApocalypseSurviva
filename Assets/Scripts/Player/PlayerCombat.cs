using System.Collections;
using UnityEngine;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Manages player melee combat, attack cooldowns, attack area detection, and damage application to Zombie targets.
    /// Modular design ready for expansion to equipped weapons in future phases.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Combat Configuration")]
        [SerializeField] private float attackDamage = 35f;
        [SerializeField] private float attackRange = 2.0f;
        [SerializeField] private float attackRadius = 1.0f;
        [SerializeField] private float attackCooldown = 0.6f;
        [SerializeField] private LayerMask enemyLayerMask = ~0; // Default all layers

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

            // Trigger animation
            if (playerAnimation != null)
            {
                playerAnimation.TriggerAttack();
            }

            // Small delay to align hit registration with attack motion
            yield return new WaitForSeconds(0.15f);

            // Detect enemy targets in front of player
            Vector3 attackPosition = transform.TransformPoint(attackPointOffset);
            Collider[] hitColliders = Physics.OverlapSphere(attackPosition, attackRadius, enemyLayerMask, QueryTriggerInteraction.Ignore);

            foreach (var col in hitColliders)
            {
                // Ignore self
                if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;

                // Check for Zombie Health component
                ZombieHealth zombieHealth = col.GetComponent<ZombieHealth>();
                if (zombieHealth == null)
                {
                    zombieHealth = col.GetComponentInParent<ZombieHealth>();
                }

                if (zombieHealth != null && !zombieHealth.IsDead)
                {
                    zombieHealth.TakeDamage(attackDamage);
                    Debug.Log($"[PlayerCombat] Player struck {col.name} for {attackDamage} damage!");
                }
            }

            yield return new WaitForSeconds(attackCooldown - 0.15f);
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
