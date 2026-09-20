using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Weapons
{
    public enum WeaponUpgradeType
    {
        Damage,
        MagazineSize,
        FireRate,
        Recoil
    }

    [Serializable]
    public class WeaponUpgradeCost
    {
        public ItemData item;
        public int quantity = 1;
    }

    /// <summary>
    /// ScriptableObject defining costs, max levels, and stat bonuses for weapon upgrade categories.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponUpgradeData", menuName = "Zombie Apocalypse/Weapon Upgrade Data")]
    public class WeaponUpgradeData : ScriptableObject
    {
        [Header("Upgrade Identity")]
        public string upgradeId = "upgrade_damage";
        public string displayName = "Damage Upgrade";
        [TextArea(2, 3)]
        public string description = "Increases bullet impact damage.";
        public WeaponUpgradeType upgradeType = WeaponUpgradeType.Damage;

        [Header("Levels & Scaling")]
        public int maxLevel = 3;
        public float bonusPerLevel = 5.0f; // e.g., +5 damage, +4 mag size, -0.02s fire rate, -0.15 recoil

        [Header("Material Costs per Level (Index 0 = Level 1 cost)")]
        public List<WeaponUpgradeCost> costsPerLevel = new List<WeaponUpgradeCost>();
    }
}
