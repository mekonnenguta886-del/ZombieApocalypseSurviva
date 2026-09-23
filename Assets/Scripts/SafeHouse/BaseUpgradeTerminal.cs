using System;
using UnityEngine;
using ZombieApocalypse.Player;
using ZombieApocalypse.World;

namespace ZombieApocalypse.SafeHouse
{
    /// <summary>
    /// Interactive Safe House Base Upgrade Terminal (Stage 5).
    /// Provides interaction point for upgrading Safe House amenities (Storage Capacity, Bed Recovery, Fortifications).
    /// Enforces Safe House location restrictions and communicates via IInteractable.
    ///
    /// ATTACH TO: [BaseUpgradeTerminal] GameObject in Safe House.
    /// </summary>
    public class BaseUpgradeTerminal : MonoBehaviour, IInteractable
    {
        public static event Action OnTerminalInteracted;

        public string InteractionPrompt => "Open Base Upgrade Terminal";

        public bool CanInteract(GameObject player)
        {
            if (player == null) return false;
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) return false;

            return SafeHouseManager.Instance != null && SafeHouseManager.Instance.IsPlayerInsideSafeHouse;
        }

        public void Interact(GameObject player)
        {
            if (!CanInteract(player)) return;

            Debug.Log("[BaseUpgradeTerminal] Player interacted with Base Upgrade Terminal.");
            OnTerminalInteracted?.Invoke();
        }
    }
}
