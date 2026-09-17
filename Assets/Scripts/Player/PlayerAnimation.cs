using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Reads movement and combat state from PlayerController / PlayerCombat and updates Animator parameters.
    /// Drives Blend Trees (Locomotion Speed) and Triggers (Attack, Hurt, Die).
    /// 
    /// ATTACH TO: Player prefab GameObject containing Animator component.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimation : MonoBehaviour
    {
        [Header("Animator Component")]
        [SerializeField] private Animator animator;
        [SerializeField] private float speedDampTime = 0.1f;

        private PlayerController playerController;

        // Parameter Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int DieTriggerHash = Animator.StringToHash("Die");

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            if (animator == null || playerController == null) return;

            float speedValue = playerController.CurrentSpeed;

            animator.SetFloat(SpeedHash, speedValue, speedDampTime, Time.deltaTime);
            animator.SetBool(IsGroundedHash, playerController.IsGrounded);
            animator.SetBool(IsSprintingHash, playerController.IsSprinting);
            animator.SetBool(IsCrouchingHash, playerController.IsCrouching);
            animator.SetFloat(VerticalVelocityHash, playerController.VerticalVelocity);
        }

        public void TriggerAttack()
        {
            if (animator == null) return;
            animator.SetTrigger(AttackTriggerHash);
        }

        public void TriggerDeath()
        {
            if (animator == null) return;
            animator.SetTrigger(DieTriggerHash);
        }

        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
        }
    }
}
