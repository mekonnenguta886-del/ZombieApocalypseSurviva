using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Uses the auto-generated PlayerInputActions wrapper class from PlayerInputActions.inputactions.
    /// Manages action map enable/disable lifecycle, gameplay input gating when inventory is open,
    /// and exposes input getters for PlayerController, PlayerCombat, and WeaponController.
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
        public bool FireHeld { get; private set; }
        public bool FireTriggered { get; private set; }
        public bool AimHeld { get; private set; }
        public bool ReloadTriggered { get; private set; }
        public bool Weapon1Triggered { get; private set; }
        public bool Weapon2Triggered { get; private set; }
        public bool Weapon3Triggered { get; private set; }
        public bool InteractTriggered { get; private set; }
        public bool InventoryTriggered { get; private set; }
        public bool SaveTriggered { get; private set; }
        public bool LoadTriggered { get; private set; }

        public bool IsInventoryOpen { get; set; }

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
            if (lockCursor && !IsInventoryOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (inputActions == null) return;

            // Check Inventory toggle key always so player can open/close inventory UI
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                InventoryTriggered = true;
            }

            // If inventory UI is open, block all gameplay actions to prevent unintended shooting, aiming, reloading, melee, switching, or raycasting
            if (IsInventoryOpen)
            {
                MoveInput = Vector2.zero;
                LookInput = Vector2.zero;
                JumpTriggered = false;
                SprintHeld = false;
                CrouchTriggered = false;
                AttackTriggered = false;
                FireHeld = false;
                FireTriggered = false;
                AimHeld = false;
                ReloadTriggered = false;
                Weapon1Triggered = false;
                Weapon2Triggered = false;
                Weapon3Triggered = false;
                InteractTriggered = false;
                return;
            }

            // Normal Gameplay Input Processing
            MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
            LookInput = inputActions.Player.Look.ReadValue<Vector2>();

            if (inputActions.Player.Jump.wasPressedThisFrame) JumpTriggered = true;
            SprintHeld = inputActions.Player.Sprint.IsPressed();
            if (inputActions.Player.Crouch.wasPressedThisFrame) CrouchTriggered = true;
            if (inputActions.Player.Attack.wasPressedThisFrame) AttackTriggered = true;

            // Ranged weapon controls
            FireHeld = inputActions.Player.Fire.IsPressed();
            if (inputActions.Player.Fire.wasPressedThisFrame) FireTriggered = true;
            AimHeld = inputActions.Player.Aim.IsPressed();
            if (inputActions.Player.Reload.wasPressedThisFrame) ReloadTriggered = true;

            // Weapon slot triggers
            if (inputActions.Player.Weapon1.wasPressedThisFrame) Weapon1Triggered = true;
            if (inputActions.Player.Weapon2.wasPressedThisFrame) Weapon2Triggered = true;
            if (inputActions.Player.Weapon3.wasPressedThisFrame) Weapon3Triggered = true;

            // Interaction trigger
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                InteractTriggered = true;
            }

            // Quick Save / Quick Load Hotkeys
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveTriggered = true;
            }
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                LoadTriggered = true;
            }
        }

        public void ResetJumpTrigger() => JumpTriggered = false;
        public void ResetCrouchTrigger() => CrouchTriggered = false;
        public void ResetAttackTrigger() => AttackTriggered = false;
        public void ResetFireTrigger() => FireTriggered = false;
        public void ResetReloadTrigger() => ReloadTriggered = false;
        public void ResetInteractTrigger() => InteractTriggered = false;
        public void ResetInventoryTrigger() => InventoryTriggered = false;
        public void ResetSaveTrigger() => SaveTriggered = false;
        public void ResetLoadTrigger() => LoadTriggered = false;
        public void ResetWeaponTriggers()
        {
            Weapon1Triggered = false;
            Weapon2Triggered = false;
            Weapon3Triggered = false;
        }
    }
}
