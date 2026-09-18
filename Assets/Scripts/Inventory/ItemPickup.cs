using UnityEngine;

using ZombieApocalypse.World;

namespace ZombieApocalypse.Inventory
{
    /// <summary>
    /// Attached to 3D item pickup GameObjects in the scene. Holds an ItemData reference and quantity.
    /// Collected by player interaction (E key). Handles partial stack collection cleanly without losing items.
    /// </summary>
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("Pickup Settings")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity = 1;

        [Header("Interaction Prompt")]
        [SerializeField] private string customPrompt = "";

        public ItemData ItemData => itemData;
        public int Quantity => quantity;
        public string InteractionPrompt => string.IsNullOrEmpty(customPrompt) 
            ? $"Pick Up {itemData?.itemName ?? "Item"} x{quantity}" 
            : customPrompt;

        public bool CanInteract(GameObject player)
        {
            return player != null && itemData != null && quantity > 0;
        }

        public void Interact(GameObject player)
        {
            if (player != null)
            {
                InventorySystem inv = player.GetComponent<InventorySystem>();
                if (inv != null)
                {
                    Collect(inv);
                }
            }
        }

        public void Setup(ItemData data, int qty)
        {
            itemData = data;
            quantity = Mathf.Max(1, qty);
        }

        /// <summary>
        /// Attempts to add item to player inventory. Returns true if at least partial quantity was collected.
        /// Removes ONLY accepted quantity from pickup, leaving remaining quantity in world.
        /// </summary>
        public bool Collect(InventorySystem inventory)
        {
            if (inventory == null || itemData == null || quantity <= 0) return false;

            int added = inventory.AddItem(itemData, quantity);
            if (added > 0)
            {
                quantity -= added;
                if (quantity <= 0)
                {
                    Debug.Log($"[ItemPickup] Player fully collected {itemData.itemName}. Destroying pickup.");
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log($"[ItemPickup] Player partially collected {itemData.itemName}. Added: {added}, Remaining in world: {quantity}");
                }
                return true;
            }

            return false;
        }
    }
}
