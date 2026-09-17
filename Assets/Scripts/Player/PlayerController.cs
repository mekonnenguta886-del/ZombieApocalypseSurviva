using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Handles responsive 3rd-person player locomotion, camera-relative movement,
    /// smooth rotation, crouch height adjustment, jumping, and slope gravity.
    /// Connected to PlayerStamina for sprinting drain.
    /// 
    /// ATTACH TO: Player prefab GameObject containing CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float runSpeed = 5.0f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float crouchSpeed = 1.8f;
        [SerializeField] private float acceleration = 12.0f;
        [SerializeField] private float deceleration = 10.0f;
        [SerializeField] private float rotationSpeed = 12.0f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -19.62f;
        [SerializeField] private float groundedGravity = -2.0f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckTransform;
        [SerializeField] private float groundCheckRadius = 0.25f;
        [SerializeField] private LayerMask groundLayerMask = ~0; // Default all layers

        [Header("Crouching Settings")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.2f;
        [SerializeField] private Vector3 standingCenter = new Vector3(0, 0.9f, 0);
        [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.6f, 0);

        [Header("Stamina Costs")]
        [SerializeField] private float sprintStaminaCostPerSecond = 15.0f;

        // References
        private CharacterController characterController;
        private PlayerInputHandler inputHandler;
        private PlayerStamina playerStamina;
        private Transform cameraTransform;

        // Locomotion States
        private Vector3 currentVelocity;
        private float verticalVelocity;
        private float activeMoveSpeed;
        private bool isGrounded;
        private bool isCrouching;
        private bool isSprinting;

        // Public Properties exposed for PlayerAnimation & UI
        public float CurrentSpeed => activeMoveSpeed;
        public bool IsGrounded => isGrounded;
        public bool IsCrouching => isCrouching;
        public bool IsSprinting => isSprinting;
        public float VerticalVelocity => verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputHandler = GetComponent<PlayerInputHandler>();
            playerStamina = GetComponent<PlayerStamina>();
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("[PlayerController] Main Camera not found. Camera-relative movement will default to world axes.");
            }

            // Fallback for missing input handler
            if (inputHandler == null)
            {
                inputHandler = gameObject.AddComponent<PlayerInputHandler>();
            }
        }

        private void Update()
        {
            CheckGroundedState();
            HandleCrouch();
            HandleMovement();
            HandleJumpAndGravity();

            // Apply calculated movement to CharacterController
            Vector3 finalMove = currentVelocity + Vector3.up * verticalVelocity;
            characterController.Move(finalMove * Time.deltaTime);
        }

        private void CheckGroundedState()
        {
            if (groundCheckTransform != null)
            {
                isGrounded = Physics.CheckSphere(groundCheckTransform.position, groundCheckRadius, groundLayerMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                isGrounded = characterController.isGrounded;
            }

            if (isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = groundedGravity;
            }
        }

        private void HandleCrouch()
        {
            if (inputHandler != null && inputHandler.CrouchTriggered)
            {
                isCrouching = !isCrouching;
                inputHandler.ResetCrouchTrigger();

                // Adjust CharacterController height and center
                characterController.height = isCrouching ? crouchingHeight : standingHeight;
                characterController.center = isCrouching ? crouchingCenter : standingCenter;

                // Cancel sprint if crouching
                if (isCrouching)
                {
                    isSprinting = false;
                }
            }
        }

        private void HandleMovement()
        {
            Vector2 moveInput = inputHandler != null ? inputHandler.MoveInput : Vector2.zero;
            bool wantsToSprint = inputHandler != null && inputHandler.SprintHeld;

            // Determine Target Speed & Sprint Stamina Drain
            float targetSpeed = 0f;

            if (moveInput.magnitude > 0.1f)
            {
                if (isCrouching)
                {
                    targetSpeed = crouchSpeed;
                    isSprinting = false;
                }
                else if (wantsToSprint)
                {
                    // Check if player has stamina to sprint
                    if (playerStamina != null && playerStamina.HasStamina)
                    {
                        bool consumed = playerStamina.ConsumeStamina(sprintStaminaCostPerSecond * Time.deltaTime);
                        if (consumed)
                        {
                            targetSpeed = sprintSpeed;
                            isSprinting = true;
                        }
                        else
                        {
                            targetSpeed = runSpeed;
                            isSprinting = false;
                        }
                    }
                    else
                    {
                        // No stamina or no stamina system attached
                        targetSpeed = playerStamina == null ? sprintSpeed : runSpeed;
                        isSprinting = playerStamina == null;
                    }
                }
                else
                {
                    targetSpeed = runSpeed;
                    isSprinting = false;
                }
            }
            else
            {
                targetSpeed = 0f;
                isSprinting = false;
            }

            // Smooth Acceleration / Deceleration
            float accelRate = (targetSpeed > activeMoveSpeed) ? acceleration : deceleration;
            activeMoveSpeed = Mathf.MoveTowards(activeMoveSpeed, targetSpeed, accelRate * Time.deltaTime);

            // Calculate Camera-Relative Direction
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 targetMoveDir = (forward * moveInput.y + right * moveInput.x).normalized;

            // Calculate movement velocity vector
            currentVelocity = targetMoveDir * activeMoveSpeed;

            // Smooth Character Rotation towards movement direction
            if (targetMoveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void HandleJumpAndGravity()
        {
            if (inputHandler != null && inputHandler.JumpTriggered)
            {
                if (isGrounded && !isCrouching)
                {
                    // v = sqrt(2 * g * h)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    isGrounded = false;
                }
                inputHandler.ResetJumpTrigger();
            }

            // Apply gravity over time
            verticalVelocity += gravity * Time.deltaTime;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckTransform != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
            }
        }
    }
}
