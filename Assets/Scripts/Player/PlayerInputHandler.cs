using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Uses the auto-generated PlayerInputActions wrapper class from PlayerInputActions.inputactions.
    /// Manages action map enable/disable lifecycle and exposes input getters for PlayerController and PlayerCombat.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        public static PlayerInputHandler Instance { get; private set; }

        private PlayerInputActions inputActions;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpTriggered { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchTriggered { get; private set; }
        public bool AttackTriggered { get; private set; }

        [Header("Cursor Settings")]
        [SerializeField] private bool lockCursor = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            if (inputActions != null)
            {
                inputActions.Player.Enable();
            }
        }

        private void OnDisable()
        {
            if (inputActions != null)
            {
                inputActions.Player.Disable();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Start()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (inputActions == null) return;

            MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
            LookInput = inputActions.Player.Look.ReadValue<Vector2>();

            if (inputActions.Player.Jump.wasPressedThisFrame)
            {
                JumpTriggered = true;
            }

            SprintHeld = inputActions.Player.Sprint.IsPressed();

            if (inputActions.Player.Crouch.wasPressedThisFrame)
            {
                CrouchTriggered = true;
            }

            if (inputActions.Player.Attack.wasPressedThisFrame)
            {
                AttackTriggered = true;
            }
        }

        public void ResetJumpTrigger()
        {
            JumpTriggered = false;
        }

        public void ResetCrouchTrigger()
        {
            CrouchTriggered = false;
        }

        public void ResetAttackTrigger()
        {
            AttackTriggered = false;
        }
    }
}
