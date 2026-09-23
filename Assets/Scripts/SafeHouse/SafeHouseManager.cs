using System;
using UnityEngine;
using ZombieApocalypse.World;

namespace ZombieApocalypse.SafeHouse
{
    /// <summary>
    /// Central manager for Phase 17 Safe House gameplay state and future sanctuary features.
    /// Observes SafeZoneTrigger enter/exit events to maintain authoritative Safe House state,
    /// firing OnSafeHouseEntered / OnSafeHouseExited events without replacing physical SafeZoneTrigger detection.
    ///
    /// ATTACH TO: [SafeHouseManager] GameObject in scene.
    /// </summary>
    public class SafeHouseManager : MonoBehaviour
    {
        public static SafeHouseManager Instance { get; private set; }

        public event Action OnSafeHouseEntered;
        public event Action OnSafeHouseExited;

        private bool isPlayerInsideSafeHouse = false;

        public bool IsPlayerInsideSafeHouse => isPlayerInsideSafeHouse;

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
        }

        // ==========================================
        // FUTURE PHASE 17 EXTENSION STUBS
        // ==========================================

        /// <summary>
        /// Validates whether the player can currently interact with Safe House amenities (Stash, Bed, Upgrades).
        /// </summary>
        public bool CanUseSafeHouseAmenities()
        {
            return isPlayerInsideSafeHouse;
        }
    }
}
