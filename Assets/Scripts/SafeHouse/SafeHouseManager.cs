using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Save;
using ZombieApocalypse.World;

namespace ZombieApocalypse.SafeHouse
{
    /// <summary>
    /// Central manager for Phase 17 Safe House gameplay state, sanctuary features, base upgrades, and persistence.
    /// Observes SafeZoneTrigger enter/exit events to maintain authoritative Safe House state,
    /// coordinates Stash, Rest, and Upgrade interactables, and provides save/load compilation.
    ///
    /// ATTACH TO: [SafeHouseManager] GameObject in scene.
    /// </summary>
    public class SafeHouseManager : MonoBehaviour
    {
        public static SafeHouseManager Instance { get; private set; }

        public event Action OnSafeHouseEntered;
        public event Action OnSafeHouseExited;
        public event Action OnSafeHouseStateUpdated;

        [Header("Runtime State")]
        [SerializeField] private bool isPlayerInsideSafeHouse = false;
        [SerializeField] private int lastRestedDay = 0;
        [SerializeField] private float restFatigueCooldown = 0f;

        [Header("Upgrade State (Stage 5)")]
        [SerializeField] private int stashUpgradeLevel = 0;
        [SerializeField] private int bedUpgradeLevel = 0;
        [SerializeField] private int fortificationLevel = 0;

        private BaseStashContainer stashContainer;

        public bool IsPlayerInsideSafeHouse => isPlayerInsideSafeHouse;
        public int LastRestedDay => lastRestedDay;
        public float RestFatigueCooldown => restFatigueCooldown;
        public int StashUpgradeLevel => stashUpgradeLevel;
        public int BedUpgradeLevel => bedUpgradeLevel;
        public int FortificationLevel => fortificationLevel;

        public BaseStashContainer StashContainer
        {
            get
            {
                if (stashContainer == null)
                {
                    stashContainer = FindObjectOfType<BaseStashContainer>();
                }
                return stashContainer;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            SafeZoneTrigger.OnPlayerEnteredSafeZone += HandleSafeZoneEntered;
            SafeZoneTrigger.OnPlayerExitedSafeZone += HandleSafeZoneExited;
        }

        private void OnDisable()
        {
            SafeZoneTrigger.OnPlayerEnteredSafeZone -= HandleSafeZoneEntered;
            SafeZoneTrigger.OnPlayerExitedSafeZone -= HandleSafeZoneExited;
        }

        private void Start()
        {
            // Initial state synchronization with SafeZoneTrigger
            SyncWithSafeZoneState(SafeZoneTrigger.IsPlayerInSafeZone);
        }

        private void HandleSafeZoneEntered()
        {
            SyncWithSafeZoneState(true);
        }

        private void HandleSafeZoneExited()
        {
            SyncWithSafeZoneState(false);
        }

        private void SyncWithSafeZoneState(bool isInside)
        {
            if (isPlayerInsideSafeHouse == isInside) return;

            isPlayerInsideSafeHouse = isInside;
            Debug.Log($"[SafeHouseManager] Player Safe House state updated: IsPlayerInsideSafeHouse = {isPlayerInsideSafeHouse}");

            if (isPlayerInsideSafeHouse)
            {
                OnSafeHouseEntered?.Invoke();
            }
            else
            {
                OnSafeHouseExited?.Invoke();
            }
            OnSafeHouseStateUpdated?.Invoke();
        }

        /// <summary>
        /// Validates whether the player can currently interact with Safe House amenities (Stash, Bed, Upgrades).
        /// </summary>
        public bool CanUseSafeHouseAmenities()
        {
            return isPlayerInsideSafeHouse;
        }

        /// <summary>
        /// Records rest execution metric (target day).
        /// </summary>
        public void RecordRest(int targetDay)
        {
            lastRestedDay = targetDay;
            Debug.Log($"[SafeHouseManager] Recorded rest execution: LastRestedDay = {lastRestedDay}");
            OnSafeHouseStateUpdated?.Invoke();
        }

        // ==========================================
        // STAGE 5 BASE UPGRADE METHODS
        // ==========================================

        public bool TryUpgradeStorage(InventorySystem playerInventory)
        {
            if (playerInventory == null || !isPlayerInsideSafeHouse) return false;
            if (stashUpgradeLevel >= 3) return false;

            int nextLevel = stashUpgradeLevel + 1;
            List<InventorySlot> requiredCosts = GetStorageUpgradeCosts(nextLevel);

            if (!HasRequiredMaterials(playerInventory, requiredCosts)) return false;

            DeductMaterials(playerInventory, requiredCosts);

            stashUpgradeLevel = nextLevel;
            int newCapacity = 20 + (stashUpgradeLevel * 10);
            if (StashContainer != null)
            {
                StashContainer.SetCapacity(newCapacity);
            }

            OnSafeHouseStateUpdated?.Invoke();
            Debug.Log($"[SafeHouseManager] Storage upgraded to Level {stashUpgradeLevel} ({newCapacity} slots).");
            return true;
        }

        public bool TryUpgradeBed(InventorySystem playerInventory)
        {
            if (playerInventory == null || !isPlayerInsideSafeHouse) return false;
            if (bedUpgradeLevel >= 3) return false;

            int nextLevel = bedUpgradeLevel + 1;
            List<InventorySlot> requiredCosts = GetBedUpgradeCosts(nextLevel);

            if (!HasRequiredMaterials(playerInventory, requiredCosts)) return false;

            DeductMaterials(playerInventory, requiredCosts);

            bedUpgradeLevel = nextLevel;
            OnSafeHouseStateUpdated?.Invoke();
            Debug.Log($"[SafeHouseManager] Bed upgraded to Level {bedUpgradeLevel}.");
            return true;
        }

        public bool TryUpgradeFortification(InventorySystem playerInventory)
        {
            if (playerInventory == null || !isPlayerInsideSafeHouse) return false;
            if (fortificationLevel >= 3) return false;

            int nextLevel = fortificationLevel + 1;
            List<InventorySlot> requiredCosts = GetFortificationUpgradeCosts(nextLevel);

            if (!HasRequiredMaterials(playerInventory, requiredCosts)) return false;

            DeductMaterials(playerInventory, requiredCosts);

            fortificationLevel = nextLevel;
            OnSafeHouseStateUpdated?.Invoke();
            Debug.Log($"[SafeHouseManager] Fortification upgraded to Level {fortificationLevel}.");
            return true;
        }

        public List<InventorySlot> GetStorageUpgradeCosts(int targetLevel)
        {
            List<InventorySlot> costs = new List<InventorySlot>();
            ItemData mat = GetDefaultMaterialItem();
            if (mat != null)
            {
                costs.Add(new InventorySlot(mat, targetLevel * 2));
            }
            return costs;
        }

        public List<InventorySlot> GetBedUpgradeCosts(int targetLevel)
        {
            List<InventorySlot> costs = new List<InventorySlot>();
            ItemData mat = GetDefaultMaterialItem();
            if (mat != null)
            {
                costs.Add(new InventorySlot(mat, targetLevel * 2));
            }
            return costs;
        }

        public List<InventorySlot> GetFortificationUpgradeCosts(int targetLevel)
        {
            List<InventorySlot> costs = new List<InventorySlot>();
            ItemData mat = GetDefaultMaterialItem();
            if (mat != null)
            {
                costs.Add(new InventorySlot(mat, targetLevel * 3));
            }
            return costs;
        }

        private ItemData GetDefaultMaterialItem()
        {
            ItemData item = ItemRegistry.GetItem("item_bandage");
            if (item == null) item = ItemRegistry.GetItem("item_medkit");
            if (item == null) item = ItemRegistry.GetItem("item_pistol_ammo");
            return item;
        }

        private bool HasRequiredMaterials(InventorySystem inv, List<InventorySlot> costs)
        {
            if (inv == null || costs == null) return false;
            foreach (var cost in costs)
            {
                if (cost == null || cost.itemData == null) continue;
                if (!inv.HasItem(cost.itemData, cost.quantity)) return false;
            }
            return true;
        }

        private void DeductMaterials(InventorySystem inv, List<InventorySlot> costs)
        {
            if (inv == null || costs == null) return;
            foreach (var cost in costs)
            {
                if (cost == null || cost.itemData == null) continue;
                inv.RemoveItem(cost.itemData, cost.quantity);
            }
        }

        // ==========================================
        // PERSISTENCE & SAVE/LOAD
        // ==========================================

        public SafeHouseSaveData GetSafeHouseSaveData()
        {
            var saveData = new SafeHouseSaveData();
            saveData.stashCapacity = StashContainer != null ? StashContainer.Capacity : (20 + stashUpgradeLevel * 10);
            saveData.stashItems = StashContainer != null ? StashContainer.GetStashSaveData() : new List<InventorySlotSaveData>();
            saveData.stashUpgradeLevel = stashUpgradeLevel;
            saveData.bedUpgradeLevel = bedUpgradeLevel;
            saveData.fortificationLevel = fortificationLevel;
            saveData.lastRestedDay = lastRestedDay;
            saveData.restFatigueCooldown = restFatigueCooldown;

            return saveData;
        }

        public void RestoreSafeHouseState(SafeHouseSaveData saveData)
        {
            if (saveData == null)
            {
                // Default legacy initial state
                lastRestedDay = 0;
                restFatigueCooldown = 0f;
                stashUpgradeLevel = 0;
                bedUpgradeLevel = 0;
                fortificationLevel = 0;
                if (StashContainer != null)
                {
                    StashContainer.RestoreStashState(20, new List<InventorySlotSaveData>());
                }
                Debug.Log("[SafeHouseManager] Restored default Safe House state (legacy save or null DTO).");
                return;
            }

            stashUpgradeLevel = saveData.stashUpgradeLevel;
            bedUpgradeLevel = saveData.bedUpgradeLevel;
            fortificationLevel = saveData.fortificationLevel;
            lastRestedDay = saveData.lastRestedDay;
            restFatigueCooldown = saveData.restFatigueCooldown;

            int capacityToRestore = saveData.stashCapacity > 0 ? saveData.stashCapacity : (20 + stashUpgradeLevel * 10);
            if (StashContainer != null)
            {
                StashContainer.RestoreStashState(capacityToRestore, saveData.stashItems);
            }

            OnSafeHouseStateUpdated?.Invoke();
            Debug.Log($"[SafeHouseManager] Restored Safe House state from save (LastRestedDay: {lastRestedDay}, StashCapacity: {capacityToRestore}, StashLvl: {stashUpgradeLevel}).");
        }
    }
}
