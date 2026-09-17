using UnityEngine;

namespace ZombieApocalypse.Inventory
{
    public enum ItemType
    {
        Weapon,
        Ammunition,
        Medical,
        Resource
    }

    /// <summary>
    /// ScriptableObject defining item data (Weapons, Ammo, Medkits, Crafting Materials).
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Zombie Apocalypse/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "item_medkit";
        public string itemName = "First Aid Kit";
        public ItemType itemType = ItemType.Medical;

        [Header("Stacking")]
        public bool isStackable = true;
        public int maxStackSize = 5;

        [Header("Visuals")]
        public Sprite itemIcon;
        public GameObject dropPrefab;
    }
}
