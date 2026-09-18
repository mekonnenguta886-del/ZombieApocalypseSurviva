using System;
using UnityEngine;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Handles interactable doors with smooth hinge rotation, locked/unlocked state, and optional key requirements.
    /// Integrates with InventorySystem via IInteractable contract.
    /// 
    /// ATTACH TO: Door GameObject with BoxCollider or hinge structure.
    /// </summary>
    public class DoorController : MonoBehaviour, IInteractable
    {
        public event Action<DoorController, bool> OnDoorStateChanged; // Door, isOpen

        [Header("Door Configuration")]
        [SerializeField] private string doorName = "Door";
        [SerializeField] private Transform doorHinge;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 4f;

        [Header("Lock Settings")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private ItemData requiredKey;
        [SerializeField] private bool consumeKeyOnUse = false;

        private bool isOpen = false;
        private Quaternion closedRotation;
        private Quaternion openRotation;

        public string DoorName => doorName;
        public bool IsLocked => isLocked;
        public bool IsOpen => isOpen;

        public string InteractionPrompt
        {
            get
            {
                if (isLocked)
                {
                    string keyName = requiredKey != null ? requiredKey.itemName : "Key";
                    return $"Locked (Requires {keyName})";
                }
                return isOpen ? $"Close {doorName}" : $"Open {doorName}";
            }
        }

        private void Awake()
        {
            if (doorHinge == null) doorHinge = transform;
            closedRotation = doorHinge.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        private void Update()
        {
            Quaternion targetRot = isOpen ? openRotation : closedRotation;
            doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRot, Time.deltaTime * openSpeed);
        }

        public bool CanInteract(GameObject player)
        {
            if (player == null) return false;

            // Player death safety check
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) return false;

            return true;
        }

        public void Interact(GameObject player)
        {
            if (!CanInteract(player)) return;

            InventorySystem inventory = player.GetComponent<InventorySystem>();

            if (isLocked)
            {
                if (requiredKey != null && inventory != null && inventory.HasItem(requiredKey, 1))
                {
                    isLocked = false;
                    if (consumeKeyOnUse)
                    {
                        inventory.RemoveItem(requiredKey, 1);
                    }
                    Debug.Log($"[DoorController] Unlocked {doorName} using {requiredKey.itemName}.");
                    ToggleDoor();
                }
                else
                {
                    string reqName = requiredKey != null ? requiredKey.itemName : "Key";
                    Debug.Log($"[DoorController] {doorName} is locked! Requires {reqName}.");
                }
            }
            else
            {
                ToggleDoor();
            }
        }

        private void ToggleDoor()
        {
            isOpen = !isOpen;
            OnDoorStateChanged?.Invoke(this, isOpen);
            Debug.Log($"[DoorController] {doorName} state changed to: {(isOpen ? "Open" : "Closed")}");
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }

        [SerializeField] private string doorId = "";
        public string DoorId => string.IsNullOrEmpty(doorId) ? doorName : doorId;

        /// <summary>
        /// Restores saved open and lock states without playing interaction sounds or consuming keys.
        /// </summary>
        public void RestoreDoorState(bool open, bool locked)
        {
            isOpen = open;
            isLocked = locked;

            if (doorHinge != null)
            {
                Quaternion targetRot = isOpen ? openRotation : closedRotation;
                doorHinge.localRotation = targetRot;
            }

            Debug.Log($"[DoorController] Restored state for '{DoorId}'. Open: {isOpen}, Locked: {isLocked}");
        }
    }
}
