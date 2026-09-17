using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Binds player locomotion state and actions to Unity Animator parameters.
    /// Drives Blend Trees (MoveSpeed, AimPitch) and Triggers (Fire, Reload, Hurt).
    /// 
    /// ATTACH TO: Player prefab GameObject containing Animator component.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimation : MonoBehaviour
    {
        private Animator animator;

        // Animator parameter hashes for performance optimization
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int FireTriggerHash = Animator.StringToHash("Fire");
        private static readonly int ReloadTriggerHash = Animator.StringToHash("Reload");

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void UpdateMovement(float speed, bool isGrounded)
        {
            if (animator == null) return;
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsGroundedHash, isGrounded);
        }

        public void SetAiming(bool isAiming)
        {
            if (animator == null) return;
            animator.SetBool(IsAimingHash, isAiming);
        }

        public void TriggerFire()
        {
            if (animator == null) return;
            animator.SetTrigger(FireTriggerHash);
        }

        public void TriggerReload()
        {
            if (animator == null) return;
            animator.SetTrigger(ReloadTriggerHash);
        }
    }
}
