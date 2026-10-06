using UnityEngine;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;
using ZombieApocalypse.World;

namespace ZombieApocalypse.WorldEvents
{
    /// <summary>
    /// Interactable container spawned during Supply Drop or Loot Discovery dynamic world events.
    /// When interacted with by the player, awards event loot items and notifies WorldEventManager.
    /// </summary>
    public class SupplyDropContainer : MonoBehaviour, IInteractable
    {
        [Header("Supply Drop Configuration")]
        [SerializeField] private string eventId = "";
        [SerializeField] private string interactableId = "supply_drop_box";
        [SerializeField] private string promptMessage = "Press [E] Search Supply Drop";
        [SerializeField] private bool destroyOnOpen = false;

        private bool hasBeenOpened = false;

        public string PromptMessage => hasBeenOpened ? "" : promptMessage;

        public void Setup(string associatedEventId, string id = "supply_drop_box")
        {
            eventId = associatedEventId;
            interactableId = id;
            hasBeenOpened = false;
        }

        public bool CanInteract(PlayerController player)
        {
            return !hasBeenOpened;
        }

        public void Interact(PlayerController player)
        {
            if (hasBeenOpened) return;
            hasBeenOpened = true;

            Debug.Log($"[SupplyDropContainer] Player opened supply container '{interactableId}' for event '{eventId}'.");

            // Notify WorldEventManager and MissionManager of interaction
            if (WorldEventManager.Instance != null)
            {
                WorldEventManager.Instance.NotifyInteractableTriggered(interactableId);
            }

            if (ZombieApocalypse.Missions.MissionManager.Instance != null)
            {
                ZombieApocalypse.Missions.MissionManager.Instance.NotifyInteractableTriggered(interactableId);
            }

            // Visual / Audio feedback
            ZombieApocalypse.UI.HUDController hud = ZombieApocalypse.UI.HUDController.Instance;
            if (hud != null)
            {
                hud.ShowNotificationToast("SUPPLY CONTAINER OPENED!");
            }

            if (destroyOnOpen)
            {
                Destroy(gameObject, 1.5f);
            }
        }
    }
}
