using UnityEngine;
using ZombieApocalypse.Player;
using ZombieApocalypse.World;

namespace ZombieApocalypse.WorldEvents
{
    /// <summary>
    /// Interactable distress beacon / survivor package for Survivor Encounter dynamic events.
    /// Provides interaction feedback and completes associated event objectives.
    /// </summary>
    public class SurvivorDistressInteractable : MonoBehaviour, IInteractable
    {
        [Header("Survivor Encounters Configuration")]
        [SerializeField] private string eventId = "";
        [SerializeField] private string interactableId = "survivor_beacon";
        [SerializeField] private string promptMessage = "Press [E] Assist Survivor / Signal Rescue";

        private bool isTriggered = false;

        public string PromptMessage => isTriggered ? "" : promptMessage;

        public void Setup(string associatedEventId, string id = "survivor_beacon")
        {
            eventId = associatedEventId;
            interactableId = id;
            isTriggered = false;
        }

        public bool CanInteract(PlayerController player)
        {
            return !isTriggered;
        }

        public void Interact(PlayerController player)
        {
            if (isTriggered) return;
            isTriggered = true;

            Debug.Log($"[SurvivorDistressInteractable] Player activated survivor beacon/rescue '{interactableId}' for event '{eventId}'.");

            if (WorldEventManager.Instance != null)
            {
                WorldEventManager.Instance.NotifyInteractableTriggered(interactableId);
            }

            if (ZombieApocalypse.Missions.MissionManager.Instance != null)
            {
                ZombieApocalypse.Missions.MissionManager.Instance.NotifyInteractableTriggered(interactableId);
            }

            ZombieApocalypse.UI.HUDController hud = ZombieApocalypse.UI.HUDController.Instance;
            if (hud != null)
            {
                hud.ShowNotificationToast("SURVIVOR RESCUED / BEACON ACTIVATED!");
            }
        }
    }
}
