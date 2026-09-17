using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Reads movement state from PlayerController and updates Animator parameters.
    /// Drives Blend Trees (Locomotion Speed) and transitions (Grounded, Sprinting, Crouching, VerticalVelocity).
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

        // Optimized Parameter Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");

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

            // Map controller speeds to normalized blend values (0 = Idle, 1 = Walk, 2 = Run, 3 = Sprint)
            float speedValue = playerController.CurrentSpeed;

            animator.SetFloat(SpeedHash, speedValue, speedDampTime, Time.deltaTime);
            animator.SetBool(IsGroundedHash, playerController.IsGrounded);
            animator.SetBool(IsSprintingHash, playerController.IsSprinting);
            animator.SetBool(IsCrouchingHash, playerController.IsCrouching);
            animator.SetFloat(VerticalVelocityHash, playerController.VerticalVelocity);
        }

        /// <summary>
        /// Explicitly assigns target Animator component if instantiated dynamically.
        /// </summary>
        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
        }
    }
}
