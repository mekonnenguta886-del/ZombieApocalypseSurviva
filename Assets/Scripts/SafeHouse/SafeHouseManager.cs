using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Save;
using ZombieApocalypse.World;

namespace ZombieApocalypse.SafeHouse
{
    /// <summary>
    /// Central manager for Phase 17 Safe House gameplay state, sanctuary features, and persistence.
    /// Observes SafeZoneTrigger enter/exit events to maintain authoritative Safe House state,
    /// coordinates Stash and Rest interactables, and provides save/load compilation.
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

        [Header("Upgrade Placeholders (Stage 5 Foundation)")]
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
        // PERSISTENCE & SAVE/LOAD
        // ==========================================

        public SafeHouseSaveData GetSafeHouseSaveData()
        {
            var saveData = new SafeHouseSaveData();
            saveData.stashCapacity = StashContainer != null ? StashContainer.Capacity : 20;
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

            int capacityToRestore = saveData.stashCapacity > 0 ? saveData.stashCapacity : 20;
            if (StashContainer != null)
            {
                StashContainer.RestoreStashState(capacityToRestore, saveData.stashItems);
            }

            OnSafeHouseStateUpdated?.Invoke();
            Debug.Log($"[SafeHouseManager] Restored Safe House state from save (LastRestedDay: {lastRestedDay}, StashCapacity: {capacityToRestore}).");
        }
    }
}
