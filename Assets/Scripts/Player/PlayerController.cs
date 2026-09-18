using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Handles responsive 3rd-person player locomotion, camera-relative movement,
    /// smooth rotation, crouch height adjustment, jumping, slope gravity, and aim direction locking.
    /// Connected to PlayerStamina for sprinting drain. Auto-ensures survival and inventory components.
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
        [SerializeField] private LayerMask groundLayerMask = ~0;

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
        private PlayerHealth playerHealth;
        private Transform cameraTransform;

        // Locomotion States
        private Vector3 currentVelocity;
        private float verticalVelocity;
        private float activeMoveSpeed;
        private bool isGrounded;
        private bool isCrouching;
        private bool isSprinting;
        private bool isAiming;

        // Public Properties exposed for PlayerAnimation, WeaponController & UI
        public float CurrentSpeed => activeMoveSpeed;
        public bool IsGrounded => isGrounded;
        public bool IsCrouching => isCrouching;
        public bool IsSprinting => isSprinting;
        public bool IsAiming => isAiming;
        public float VerticalVelocity => verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputHandler = GetComponent<PlayerInputHandler>();
            playerStamina = GetComponent<PlayerStamina>();
            playerHealth = GetComponent<PlayerHealth>();

            // Auto-ensure Phase 5 survival & inventory components exist on Player GameObject
            if (GetComponent<InventorySystem>() == null) gameObject.AddComponent<InventorySystem>();
            if (GetComponent<PlayerSurvivalStats>() == null) gameObject.AddComponent<PlayerSurvivalStats>();
            if (GetComponent<PlayerInteraction>() == null) gameObject.AddComponent<PlayerInteraction>();
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (inputHandler == null)
            {
                inputHandler = gameObject.AddComponent<PlayerInputHandler>();
            }
        }

        private void Update()
        {
            // Disable movement logic if player is dead
            if (playerHealth != null && playerHealth.IsDead)
            {
                activeMoveSpeed = 0f;
                isSprinting = false;
                isAiming = false;
                return;
            }

            CheckGroundedState();
            HandleCrouch();
            HandleMovement();
            HandleJumpAndGravity();

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

                characterController.height = isCrouching ? crouchingHeight : standingHeight;
                characterController.center = isCrouching ? crouchingCenter : standingCenter;

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
            isAiming = inputHandler != null && inputHandler.AimHeld;

            // Determine Target Speed
            float targetSpeed = 0f;

            if (moveInput.magnitude > 0.1f)
            {
                if (isCrouching)
                {
                    targetSpeed = crouchSpeed;
                    isSprinting = false;
                }
                else if (isAiming)
                {
                    // Slow down locomotion while aiming
                    targetSpeed = walkSpeed;
                    isSprinting = false;
                }
                else if (wantsToSprint)
                {
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

            // Calculate Camera-Relative Movement Direction
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 targetMoveDir = (forward * moveInput.y + right * moveInput.x).normalized;
            currentVelocity = targetMoveDir * activeMoveSpeed;

            // Character Rotation Handling
            if (isAiming && cameraTransform != null)
            {
                // When aiming, align player rotation directly to camera look direction
                Vector3 cameraLookDir = cameraTransform.forward;
                cameraLookDir.y = 0f;
                if (cameraLookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion aimRotation = Quaternion.LookRotation(cameraLookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, aimRotation, rotationSpeed * 1.5f * Time.deltaTime);
                }
            }
            else if (targetMoveDir.sqrMagnitude > 0.01f)
            {
                // Normal locomotion rotation towards movement vector
                Quaternion targetRotation = Quaternion.LookRotation(targetMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void HandleJumpAndGravity()
        {
            if (inputHandler != null && inputHandler.JumpTriggered)
            {
                if (isGrounded && !isCrouching && !isAiming)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    isGrounded = false;
                }
                inputHandler.ResetJumpTrigger();
            }

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
