using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Handles user input reading using Unity's New Input System.
    /// Provides decoupled input getters for movement, camera look, sprint, crouch, and jump.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        public static PlayerInputHandler Instance { get; private set; }

        [Header("Input Values")]
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpTriggered { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchTriggered { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool lockCursor = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
            ReadInput();
        }

        private void ReadInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                // Movement (WASD / Arrows)
                float moveX = 0f;
                float moveY = 0f;

                if (Keyboard.current.wKey.isPressed) moveY += 1f;
                if (Keyboard.current.sKey.isPressed) moveY -= 1f;
                if (Keyboard.current.dKey.isPressed) moveX += 1f;
                if (Keyboard.current.aKey.isPressed) moveX -= 1f;

                MoveInput = new Vector2(moveX, moveY).normalized;

                // Sprint (Left Shift)
                SprintHeld = Keyboard.current.leftShiftKey.isPressed;

                // Jump (Space)
                JumpTriggered = Keyboard.current.spaceKey.wasPressedThisFrame;

                // Crouch (Left Ctrl or C)
                CrouchTriggered = Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame;
            }

            if (Mouse.current != null)
            {
                // Mouse Delta
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                LookInput = mouseDelta;
            }
#else
            // Fallback for Legacy Input Manager
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            MoveInput = new Vector2(moveX, moveY).normalized;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            LookInput = new Vector2(mouseX, mouseY);

            SprintHeld = Input.GetKey(KeyCode.LeftShift);
            JumpTriggered = Input.GetKeyDown(KeyCode.Space);
            CrouchTriggered = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C);
#endif
        }

        public void ResetJumpTrigger()
        {
            JumpTriggered = false;
        }

        public void ResetCrouchTrigger()
        {
            CrouchTriggered = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
