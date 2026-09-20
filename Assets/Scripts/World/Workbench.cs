using System;
using UnityEngine;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;
using ZombieApocalypse.Weapons;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Physical interactable Workbench object in the Safe House.
    /// Implements IInteractable contract to allow players to inspect weapon upgrade states,
    /// perform upgrades via WeaponUpgradeSystem, and trigger workbench UI events.
    /// </summary>
    public class Workbench : MonoBehaviour, IInteractable
    {
        public static event Action<GameObject> OnWorkbenchInteracted;

        [Header("Workbench Settings")]
        [SerializeField] private string workbenchName = "Safe House Workbench";

        public string InteractionPrompt => $"Use {workbenchName} (Weapon Upgrades)";

        public bool CanInteract(GameObject player)
        {
            if (player == null) return false;

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) return false;

            PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
            if (input != null && input.IsInventoryOpen) return false;

            return true;
        }

        public void Interact(GameObject player)
        {
            if (!CanInteract(player)) return;

            Debug.Log($"[Workbench] Player interacted with {workbenchName}. Opening Weapon Upgrade System.");

            // Notify listeners (such as Workbench UI / HUD controllers)
            OnWorkbenchInteracted?.Invoke(player);
        }
    }
}
