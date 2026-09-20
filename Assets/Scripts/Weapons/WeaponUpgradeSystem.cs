using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Weapons
{
    /// <summary>
    /// Centralized weapon upgrade manager. Calculates runtime effective stats (damage, magazine capacity,
    /// fire rate, recoil) per weapon without mutating base WeaponData ScriptableObjects.
    /// Handles material checking and atomic upgrade transactions integrated with InventorySystem.
    ///
    /// ATTACH TO: Player prefab or central Manager GameObject.
    /// </summary>
    public class WeaponUpgradeSystem : MonoBehaviour
    {
        public static WeaponUpgradeSystem Instance { get; private set; }

        public event Action<string, WeaponUpgradeType, int> OnWeaponUpgraded;

        [Header("Stat Scaling per Level")]
        [SerializeField] private float damageBonusPerLevel = 5.0f;
        [SerializeField] private int magazineBonusPerLevel = 4;
        [SerializeField] private float fireRateBonusPerLevel = 0.02f; // Reduces cooldown by 0.02s per level
        [SerializeField] private float recoilBonusPerLevel = 0.15f;    // Reduces recoil by 0.15 per level
        [SerializeField] private int maxUpgradeLevel = 3;

        // Per-weapon runtime upgrade state dictionary indexed by weapon ID / weaponName
        private readonly Dictionary<string, WeaponUpgradeState> upgradeStates = new Dictionary<string, WeaponUpgradeState>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Retrieves or creates runtime upgrade state for specified weapon.
        /// </summary>
        public WeaponUpgradeState GetUpgradeState(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return new WeaponUpgradeState();

            if (!upgradeStates.TryGetValue(weaponId, out WeaponUpgradeState state))
            {
                state = new WeaponUpgradeState(weaponId);
                upgradeStates[weaponId] = state;
            }

            return state;
        }

        /// <summary>
        /// Sets or restores upgrade state for specified weapon.
        /// </summary>
        public void SetUpgradeState(string weaponId, WeaponUpgradeState state)
        {
            if (string.IsNullOrEmpty(weaponId) || state == null) return;
            upgradeStates[weaponId] = state;
        }

        /// <summary>
        /// Returns current level for given weapon and upgrade type.
        /// </summary>
        public int GetUpgradeLevel(string weaponId, WeaponUpgradeType type)
        {
            return GetUpgradeState(weaponId).GetLevel(type);
        }

        /// <summary>
        /// Calculates effective damage including upgrade bonuses.
        /// </summary>
        public float GetEffectiveDamage(WeaponData weapon)
        {
            if (weapon == null) return 0f;
            int level = GetUpgradeLevel(weapon.weaponName, WeaponUpgradeType.Damage);
            return weapon.damage + (level * damageBonusPerLevel);
        }

        /// <summary>
        /// Calculates effective magazine size including upgrade bonuses.
        /// </summary>
        public int GetEffectiveMagazineSize(WeaponData weapon)
        {
            if (weapon == null) return 0;
            int level = GetUpgradeLevel(weapon.weaponName, WeaponUpgradeType.MagazineSize);
            return weapon.magazineSize + (level * magazineBonusPerLevel);
        }

        /// <summary>
        /// Calculates effective fire rate (seconds per shot) including upgrade bonuses.
        /// </summary>
        public float GetEffectiveFireRate(WeaponData weapon)
        {
            if (weapon == null) return 0.25f;
            int level = GetUpgradeLevel(weapon.weaponName, WeaponUpgradeType.FireRate);
            return Mathf.Max(0.05f, weapon.fireRate - (level * fireRateBonusPerLevel));
        }

        /// <summary>
        /// Calculates effective recoil amount including upgrade bonuses.
        /// </summary>
        public float GetEffectiveRecoil(WeaponData weapon)
        {
            if (weapon == null) return 1.0f;
            int level = GetUpgradeLevel(weapon.weaponName, WeaponUpgradeType.Recoil);
            return Mathf.Max(0.1f, weapon.recoilAmount - (level * recoilBonusPerLevel));
        }

        /// <summary>
        /// Validates if an upgrade can be applied for specified weapon using target inventory.
        /// Checks weapon validity, max level, and material availability.
        /// </summary>
        public WeaponUpgradeResult CanUpgrade(WeaponData weapon, WeaponUpgradeType type, InventorySystem inventory, List<WeaponUpgradeCost> costs = null)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.weaponName))
            {
                return WeaponUpgradeResult.CreateFailed(null, type, "Invalid weapon data.");
            }

            if (inventory == null)
            {
                return WeaponUpgradeResult.CreateFailed(weapon.weaponName, type, "Target inventory is null.");
            }

            int currentLevel = GetUpgradeLevel(weapon.weaponName, type);
            if (currentLevel >= maxUpgradeLevel)
            {
                return WeaponUpgradeResult.CreateMaxLevel(weapon.weaponName, type);
            }

            if (costs != null)
            {
                foreach (var cost in costs)
                {
                    if (cost == null || cost.item == null || cost.quantity <= 0) continue;
                    int owned = inventory.GetItemQuantity(cost.item);
                    if (owned < cost.quantity)
                    {
                        return WeaponUpgradeResult.CreateMissingMaterials(weapon.weaponName, type, cost.item, cost.quantity - owned);
                    }
                }
            }

            return WeaponUpgradeResult.CreateSuccess(weapon.weaponName, type, currentLevel + 1);
        }

        public WeaponUpgradeResult CanUpgrade(WeaponData weapon, WeaponUpgradeType type, InventorySystem inventory, ItemData singleCostItem, int singleCostQuantity = 1)
        {
            List<WeaponUpgradeCost> costs = null;
            if (singleCostItem != null && singleCostQuantity > 0)
            {
                costs = new List<WeaponUpgradeCost> { new WeaponUpgradeCost { item = singleCostItem, quantity = singleCostQuantity } };
            }
            return CanCraftOrUpgrade(weapon, type, inventory, costs);
        }

        private WeaponUpgradeResult CanCraftOrUpgrade(WeaponData weapon, WeaponUpgradeType type, InventorySystem inventory, List<WeaponUpgradeCost> costs)
        {
            return CanUpgrade(weapon, type, inventory, costs);
        }

        /// <summary>
        /// Attempts atomic upgrade transaction for specified weapon.
        /// Removes required materials and increments upgrade level only if all checks pass.
        /// Performs explicit rollback of removed materials if transaction is interrupted.
        /// </summary>
        public WeaponUpgradeResult TryUpgrade(WeaponData weapon, WeaponUpgradeType type, InventorySystem inventory, List<WeaponUpgradeCost> costs = null)
        {
            WeaponUpgradeResult check = CanUpgrade(weapon, type, inventory, costs);
            if (!check.Success) return check;

            WeaponUpgradeState state = GetUpgradeState(weapon.weaponName);
            int previousLevel = state.GetLevel(type);
            int newLevel = previousLevel + 1;

            List<WeaponUpgradeCost> removedMaterials = new List<WeaponUpgradeCost>();
            bool removalFailed = false;

            // Execute material consumption with tracking for rollback protection
            if (costs != null)
            {
                foreach (var cost in costs)
                {
                    if (cost == null || cost.item == null || cost.quantity <= 0) continue;

                    bool removed = inventory.RemoveItem(cost.item, cost.quantity);
                    if (!removed)
                    {
                        removalFailed = true;
                        break;
                    }
                    removedMaterials.Add(cost);
                }
            }

            if (removalFailed)
            {
                // Rollback any materials removed before the failure
                foreach (var removedCost in removedMaterials)
                {
                    inventory.AddItem(removedCost.item, removedCost.quantity);
                }

                Debug.LogError($"[WeaponUpgradeSystem] Upgrade transaction failed during material removal for '{weapon.weaponName}'. Rolled back materials.");
                return WeaponUpgradeResult.CreateFailed(weapon.weaponName, type, "Failed to remove required materials from inventory.");
            }

            // Apply Upgrade Level
            try
            {
                state.SetLevel(type, newLevel);
            }
            catch (Exception ex)
            {
                // Rollback state and materials on unexpected exception
                state.SetLevel(type, previousLevel);
                foreach (var removedCost in removedMaterials)
                {
                    inventory.AddItem(removedCost.item, removedCost.quantity);
                }
                Debug.LogError($"[WeaponUpgradeSystem] Exception applying upgrade state for '{weapon.weaponName}': {ex.Message}. Transaction rolled back.");
                return WeaponUpgradeResult.CreateFailed(weapon.weaponName, type, $"Transaction exception: {ex.Message}");
            }

            Debug.Log($"[WeaponUpgradeSystem] Upgraded '{weapon.weaponName}' {type} to Level {newLevel}.");
            OnWeaponUpgraded?.Invoke(weapon.weaponName, type, newLevel);

            return WeaponUpgradeResult.CreateSuccess(weapon.weaponName, type, newLevel);
        }

        public WeaponUpgradeResult TryUpgrade(WeaponData weapon, WeaponUpgradeType type, InventorySystem inventory, ItemData singleCostItem, int singleCostQuantity = 1)
        {
            List<WeaponUpgradeCost> costs = null;
            if (singleCostItem != null && singleCostQuantity > 0)
            {
                costs = new List<WeaponUpgradeCost> { new WeaponUpgradeCost { item = singleCostItem, quantity = singleCostQuantity } };
            }
            return TryUpgrade(weapon, type, inventory, costs);
        }
    }
}
