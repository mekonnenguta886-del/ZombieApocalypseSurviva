using System.Collections;
using UnityEngine;
using ZombieApocalypse.Weapons;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Preserved Phase 3 Melee Combat system. Handles player melee attacks, attack cooldowns,
    /// directional hit filtering, and enemy LayerMask queries.
    /// Integrated safely alongside Phase 4 ranged weapons.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Melee Combat Config")]
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
        private WeaponController weaponController;

        private float lastAttackTime;
        private bool isAttacking;

        public bool IsAttacking => isAttacking;

        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            playerAnimation = GetComponent<PlayerAnimation>();
            playerHealth = GetComponent<PlayerHealth>();
            weaponController = GetComponent<WeaponController>();
        }

        private void Update()
        {
            // Player Death Safety: Disable combat if player is dead
            if (playerHealth != null && playerHealth.IsDead) return;

            // If ranged weapon is active and aiming, skip melee trigger to avoid input conflicts
            if (weaponController != null && weaponController.CurrentWeapon != null && inputHandler != null && inputHandler.AimHeld)
            {
                return;
            }

            if (inputHandler != null && inputHandler.AttackTriggered)
            {
                // Only trigger melee if no ranged weapon is firing/reloading or if explicitly triggered
                if (weaponController == null || weaponController.CurrentWeapon == null || (!weaponController.IsReloading && !inputHandler.AimHeld))
                {
                    TryPerformAttack();
                }
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

            if (playerAnimation != null)
            {
                playerAnimation.TriggerAttack();
            }

            yield return new WaitForSeconds(0.15f);

            Vector3 attackPosition = transform.TransformPoint(attackPointOffset);
            Collider[] hitColliders = Physics.OverlapSphere(attackPosition, attackRadius, enemyLayerMask, QueryTriggerInteraction.Ignore);

            foreach (var col in hitColliders)
            {
                if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;

                Vector3 dirToTarget = (col.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToTarget);

                if (dot < -0.2f) continue; // Behind player guard

                ZombieHealth zombieHealth = col.GetComponent<ZombieHealth>();
                if (zombieHealth == null)
                {
                    zombieHealth = col.GetComponentInParent<ZombieHealth>();
                }

                if (zombieHealth != null && !zombieHealth.IsDead)
                {
                    zombieHealth.TakeDamage(attackDamage);
                    Debug.Log($"[PlayerCombat] Melee struck {col.name} dealing {attackDamage} damage.");
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
