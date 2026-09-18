using System;
using UnityEngine;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.World;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Performs camera-centered raycasts to detect nearby IInteractable objects (Pickups, Doors, Loot Containers).
    /// Manages interaction prompts ([E] key) and handles interaction logic via IInteractable contract.
    /// Blocked when inventory UI is open or player is dead.
    /// 
    /// ATTACH TO: Player prefab GameObject.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        public event Action<string> OnInteractionPromptChanged;

        [Header("Interaction Settings")]
        [SerializeField] private float interactRange = 2.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        // References
        private Camera mainCamera;
        private PlayerInputHandler inputHandler;
        private InventorySystem inventorySystem;
        private PlayerHealth playerHealth;

        private IInteractable currentInteractable;
        private string activePrompt = "";

        public string ActivePrompt => activePrompt;

        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            inventorySystem = GetComponent<InventorySystem>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if ((playerHealth != null && playerHealth.IsDead) || (inputHandler != null && inputHandler.IsInventoryOpen))
            {
                ClearInteractionPrompt();
                return;
            }

            CheckInteractables();
            HandleInteractionInput();
        }

        private void CheckInteractables()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            Ray ray = mainCamera != null 
                ? mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position + Vector3.up * 1.2f, transform.forward);

            currentInteractable = null;
            string newPrompt = "";

            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayers, QueryTriggerInteraction.Collide))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable == null) interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    currentInteractable = interactable;
                    newPrompt = $"[E] {interactable.InteractionPrompt}";
                }
            }

            if (newPrompt != activePrompt)
            {
                activePrompt = newPrompt;
                OnInteractionPromptChanged?.Invoke(activePrompt);
            }
        }

        private void HandleInteractionInput()
        {
            if (inputHandler != null && inputHandler.InteractTriggered)
            {
                if (currentInteractable != null && currentInteractable.CanInteract(gameObject))
                {
                    currentInteractable.Interact(gameObject);
                }

                inputHandler.ResetInteractTrigger();
            }
        }

        private void ClearInteractionPrompt()
        {
            currentInteractable = null;
            if (!string.IsNullOrEmpty(activePrompt))
            {
                activePrompt = "";
                OnInteractionPromptChanged?.Invoke("");
            }
        }
    }
}
