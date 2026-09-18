using UnityEngine;

namespace ZombieApocalypse.Inventory
{
    public enum ItemCategory
    {
        Medical,
        Food,
        Water,
        Ammunition,
        Miscellaneous
    }

    public enum AmmoType
    {
        None,
        Pistol,
        Shotgun,
        Rifle
    }

    /// <summary>
    /// ScriptableObject defining item properties, categories, stacking, consumable restoration amounts, and ammo types.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Zombie Apocalypse/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "item_bandage";
        public string itemName = "Bandage";
        [TextArea(2, 4)]
        public string description = "A simple medical bandage that restores a small amount of health.";
        public ItemCategory category = ItemCategory.Medical;

        [Header("Stacking")]
        public bool isStackable = true;
        public int maxStackSize = 5;

        [Header("Consumable Stats")]
        public bool isConsumable = true;
        public float healthRestore = 20f;
        public float hungerRestore = 0f;
        public float thirstRestore = 0f;

        [Header("Ammunition Settings")]
        public AmmoType ammoType = AmmoType.None;
        public int ammoAmount = 0;

        [Header("Visuals & World Representation")]
        public Sprite itemIcon;
        public GameObject dropPrefab;

        // Convenient Aliases / Properties
        public int maxStack => maxStackSize;
        public Sprite icon => itemIcon;
        public ItemCategory itemType => category;
    }
}
