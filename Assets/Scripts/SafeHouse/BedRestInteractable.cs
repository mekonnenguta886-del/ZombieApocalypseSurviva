using System;
using UnityEngine;
using ZombieApocalypse.Player;
using ZombieApocalypse.UI;
using ZombieApocalypse.World;

namespace ZombieApocalypse.SafeHouse
{
    /// <summary>
    /// Interactive Safe House Bed component (Stage 2).
    /// Advances world time to 06:00 AM (Dawn) instantly, increments day count when crossing midnight,
    /// recovers player health and stamina via existing authorities, and updates rest fatigue state.
    ///
    /// ATTACH TO: Bed GameObject in Safe House.
    /// </summary>
    public class BedRestInteractable : MonoBehaviour, IInteractable
    {
        public static event Action OnBedInteracted;
        public static event Action<int> OnPlayerRested; // targetDay

        [Header("Rest Configuration")]
        [SerializeField] private float healthRecoveryAmount = 100f; // Full health recovery default
        [SerializeField] private bool fullStaminaRecovery = true;

        public string InteractionPrompt => "Rest (Advance Time to 06:00 Dawn)";

        public bool CanInteract(GameObject player)
        {
            if (player == null) return false;
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) return false;

            return SafeHouseManager.Instance != null && SafeHouseManager.Instance.IsPlayerInsideSafeHouse;
        }

        public void Interact(GameObject player)
        {
            if (!CanInteract(player)) return;

            OnBedInteracted?.Invoke();
            TryRest(player);
        }

        /// <summary>
        /// Executes rest routine: fast-forwards time to 06:00 AM Dawn, recovers health/stamina,
        /// updates SafeHouseManager rest tracking, and triggers HUD notifications.
        /// </summary>
        public bool TryRest(GameObject player)
        {
            if (player == null) return false;
            if (SafeHouseManager.Instance == null || !SafeHouseManager.Instance.IsPlayerInsideSafeHouse)
            {
                ShowHUDToast("Must be inside Safe House to rest.");
                return false;
            }

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead)
            {
                ShowHUDToast("Cannot rest while dead.");
                return false;
            }

            WorldTimeManager timeMgr = WorldTimeManager.Instance;
            if (timeMgr == null)
            {
                Debug.LogWarning("[BedRestInteractable] WorldTimeManager instance not found. Cannot advance time.");
                return false;
            }

            // Time & Day Calculation: Morning Target = 06:00 AM (360 minutes)
            float targetTimeMinutes = 360.0f;
            float currentTimeMinutes = timeMgr.TimeOfDayMinutes;
            int currentDay = timeMgr.DayCount;

            int targetDay = currentDay;
            if (currentTimeMinutes >= targetTimeMinutes)
            {
                // Current time is 06:00 or later -> Advance to 06:00 AM of the NEXT day
                targetDay = currentDay + 1;
            }

            // 1. Controlled time jump via existing WorldTimeManager API
            timeMgr.SetTimeOfDay(targetTimeMinutes, targetDay);

            // 2. Health Recovery via existing PlayerHealth authority
            if (health != null)
            {
                health.Heal(healthRecoveryAmount);
            }

            // 3. Stamina Recovery via existing PlayerStamina authority
            PlayerStamina stamina = player.GetComponent<PlayerStamina>();
            if (stamina != null && fullStaminaRecovery)
            {
                stamina.RestoreStamina(stamina.MaxStamina);
            }

            // 4. Update SafeHouseManager rest state tracking
            if (SafeHouseManager.Instance != null)
            {
                SafeHouseManager.Instance.RecordRest(targetDay);
            }

            OnPlayerRested?.Invoke(targetDay);
            ShowHUDToast($"Rested until Dawn (Day {targetDay}, 06:00 AM). Health & Stamina Restored.");
            Debug.Log($"[BedRestInteractable] Player rested until Day {targetDay} 06:00 AM. Health and stamina recovered.");
            return true;
        }

        private void ShowHUDToast(string message)
        {
            HUDController hud = HUDController.Instance != null ? HUDController.Instance : FindObjectOfType<HUDController>();
            if (hud != null)
            {
                hud.ShowNotificationToast(message);
            }
        }
    }
}
