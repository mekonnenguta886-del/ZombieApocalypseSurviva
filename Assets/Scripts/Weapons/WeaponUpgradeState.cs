using System;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Weapons
{
    public enum WeaponUpgradeStatus
    {
        Success,
        InvalidWeapon,
        MaxLevelReached,
        MissingMaterials,
        Failed
    }

    /// <summary>
    /// Holds runtime upgrade levels per weapon instance, decoupled from ScriptableObject shared assets.
    /// </summary>
    [Serializable]
    public class WeaponUpgradeState
    {
        public string weaponId = "";
        public int damageLevel = 0;
        public int magazineLevel = 0;
        public int fireRateLevel = 0;
        public int recoilLevel = 0;

        public WeaponUpgradeState() { }

        public WeaponUpgradeState(string id)
        {
            weaponId = id ?? "";
        }

        public int GetLevel(WeaponUpgradeType type)
        {
            switch (type)
            {
                case WeaponUpgradeType.Damage: return damageLevel;
                case WeaponUpgradeType.MagazineSize: return magazineLevel;
                case WeaponUpgradeType.FireRate: return fireRateLevel;
                case WeaponUpgradeType.Recoil: return recoilLevel;
                default: return 0;
            }
        }

        public void SetLevel(WeaponUpgradeType type, int level)
        {
            switch (type)
            {
                case WeaponUpgradeType.Damage: damageLevel = level; break;
                case WeaponUpgradeType.MagazineSize: magazineLevel = level; break;
                case WeaponUpgradeType.FireRate: fireRateLevel = level; break;
                case WeaponUpgradeType.Recoil: recoilLevel = level; break;
            }
        }
    }

    /// <summary>
    /// Result model returned after inspecting or performing an upgrade transaction.
    /// </summary>
    [Serializable]
    public class WeaponUpgradeResult
    {
        public WeaponUpgradeStatus status;
        public string weaponId;
        public WeaponUpgradeType upgradeType;
        public string message;
        public ItemData missingItem;
        public int missingQuantity;

        public bool Success => status == WeaponUpgradeStatus.Success;

        public WeaponUpgradeResult(WeaponUpgradeStatus status, string weaponId, WeaponUpgradeType upgradeType, string message, ItemData missingItem = null, int missingQty = 0)
        {
            this.status = status;
            this.weaponId = weaponId ?? "";
            this.upgradeType = upgradeType;
            this.message = message ?? "";
            this.missingItem = missingItem;
            this.missingQuantity = missingQty;
        }

        public static WeaponUpgradeResult CreateSuccess(string weaponId, WeaponUpgradeType type, int newLevel)
        {
            return new WeaponUpgradeResult(WeaponUpgradeStatus.Success, weaponId, type, $"Upgraded {weaponId} {type} to Level {newLevel}.");
        }

        public static WeaponUpgradeResult CreateMaxLevel(string weaponId, WeaponUpgradeType type)
        {
            return new WeaponUpgradeResult(WeaponUpgradeStatus.MaxLevelReached, weaponId, type, $"{type} for {weaponId} is already at maximum level.");
        }

        public static WeaponUpgradeResult CreateMissingMaterials(string weaponId, WeaponUpgradeType type, ItemData item, int missingQty)
        {
            string itemName = item != null ? item.itemName : "Upgrade Material";
            return new WeaponUpgradeResult(WeaponUpgradeStatus.MissingMaterials, weaponId, type, $"Missing material for upgrade: {itemName} x{missingQty}.", item, missingQty);
        }

        public static WeaponUpgradeResult CreateFailed(string weaponId, WeaponUpgradeType type, string reason)
        {
            return new WeaponUpgradeResult(WeaponUpgradeStatus.Failed, weaponId, type, reason);
        }
    }
}
