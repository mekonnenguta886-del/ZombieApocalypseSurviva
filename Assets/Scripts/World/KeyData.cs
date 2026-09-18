using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Key item definition extending standard ItemData. Integrates directly into InventorySystem.
    /// Used by DoorController to check key possession without creating duplicate inventory systems.
    /// </summary>
    [CreateAssetMenu(fileName = "NewKeyData", menuName = "Zombie Apocalypse/Key Data")]
    public class KeyData : ItemData
    {
        [Header("Key Properties")]
        public string keyId = "key_house";
    }
}
