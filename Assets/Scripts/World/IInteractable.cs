using UnityEngine;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Universal interaction contract for world objects (Doors, Loot Containers, Pickups, Terminals, Quest Objects).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Prompt string shown in HUD when player looks at this object (e.g. "Open Door", "Pick Up Medkit").
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Validates whether the player can currently interact with this object.
        /// </summary>
        bool CanInteract(GameObject player);

        /// <summary>
        /// Executes interaction logic when player presses interaction key ([E]).
        /// </summary>
        void Interact(GameObject player);
    }
}
