using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Player;
using ZombieApocalypse.Save;
using ZombieApocalypse.UI;
using ZombieApocalypse.World;

namespace ZombieApocalypse.Environment
{
    /// <summary>
    /// Centralized singleton manager for Phase 15 environmental conditions and hazard zones.
    /// Controls dynamic condition states, decay multipliers, periodic health damage ticks,
    /// SafeHouse protection overrides, save/load persistence, and HUD UI notifications.
    /// 
    /// ATTACH TO: [EnvironmentalConditionManager] GameObject in scene.
    /// </summary>
    public class EnvironmentalConditionManager : MonoBehaviour
    {
        public static EnvironmentalConditionManager Instance { get; private set; }

        public event Action<EnvironmentalConditionData> OnConditionChanged;
        public event Action<string> OnHazardWarning;

        [Header("Default Condition Config")]
        [SerializeField] private EnvironmentalConditionData normalCondition;

        [Header("Runtime State")]
        [SerializeField] private EnvironmentalConditionData activeCondition;
        [SerializeField] private float conditionTimer = 0f;

        private List<EnvironmentalHazardZone> activeZones = new List<EnvironmentalHazardZone>();
        private PlayerHealth playerHealth;
        private PlayerController playerController;
        private float damageTickTimer = 0f;
        private const float DAMAGE_TICK_INTERVAL = 1.0f;

        public EnvironmentalConditionData ActiveCondition => activeCondition;
        public bool IsHazardActive => activeCondition != null && activeCondition != normalCondition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureNormalCondition();
        }

        private void Start()
        {
            FindReferences();
            if (activeCondition == null)
            {
                SetCondition(normalCondition);
            }
        }

        private void EnsureNormalCondition()
        {
            if (normalCondition == null)
            {
                normalCondition = ScriptableObject.CreateInstance<EnvironmentalConditionData>();
                normalCondition.conditionId = "normal";
                normalCondition.displayName = "Normal Environment";
                normalCondition.description = "Standard weather and environmental conditions.";
                normalCondition.hungerDecayMultiplier = 1.0f;
                normalCondition.thirstDecayMultiplier = 1.0f;
                normalCondition.healthDamagePerSecond = 0f;
                normalCondition.movementSpeedMultiplier = 1.0f;
                normalCondition.warningMessage = "";
                normalCondition.badgeColor = new Color(0.2f, 0.8f, 0.3f);
            }
        }

        private void FindReferences()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            if (playerHealth == null)
            {
                FindReferences();
            }

            // SafeHouse Exemption Rule: Safe House suppresses environmental damage and extreme decay
            bool isSafe = SafeZoneTrigger.IsPlayerInSafeZone;

            // Handle periodic health damage tick for hazardous environments
            if (!isSafe && activeCondition != null && activeCondition.healthDamagePerSecond > 0f)
            {
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    damageTickTimer += Time.deltaTime;
                    if (damageTickTimer >= DAMAGE_TICK_INTERVAL)
                    {
                        damageTickTimer -= DAMAGE_TICK_INTERVAL;
                        float damageAmount = activeCondition.healthDamagePerSecond * DAMAGE_TICK_INTERVAL;
                        playerHealth.TakeDamage(damageAmount);
                        Debug.Log($"[EnvironmentalConditionManager] Applied {damageAmount} hazard damage tick to player ({activeCondition.displayName}).");
                    }
                }
            }
            else
            {
                damageTickTimer = 0f;
            }
        }

        /// <summary>
        /// Gets current effective hunger decay multiplier (1.0 in SafeHouse or Normal).
        /// </summary>
        public float GetHungerDecayMultiplier()
        {
            if (SafeZoneTrigger.IsPlayerInSafeZone) return 1.0f;
            return activeCondition != null ? activeCondition.hungerDecayMultiplier : 1.0f;
        }

        /// <summary>
        /// Gets current effective thirst decay multiplier (1.0 in SafeHouse or Normal).
        /// </summary>
        public float GetThirstDecayMultiplier()
        {
            if (SafeZoneTrigger.IsPlayerInSafeZone) return 1.0f;
            return activeCondition != null ? activeCondition.thirstDecayMultiplier : 1.0f;
        }

        /// <summary>
        /// Registers player entry into an environmental hazard zone.
        /// </summary>
        public void RegisterZone(EnvironmentalHazardZone zone)
        {
            if (zone == null) return;
            if (!activeZones.Contains(zone))
            {
                activeZones.Add(zone);
                EvaluateActiveCondition();
            }
        }

        /// <summary>
        /// Unregisters player exit from an environmental hazard zone.
        /// </summary>
        public void UnregisterZone(EnvironmentalHazardZone zone)
        {
            if (zone == null) return;
            if (activeZones.Contains(zone))
            {
                activeZones.Remove(zone);
                EvaluateActiveCondition();
            }
        }

        private void EvaluateActiveCondition()
        {
            EnsureNormalCondition();

            if (activeZones.Count > 0)
            {
                // Take the most recent or highest priority active zone condition
                EnvironmentalHazardZone topZone = activeZones[activeZones.Count - 1];
                if (topZone != null && topZone.ConditionData != null)
                {
                    SetCondition(topZone.ConditionData);
                    return;
                }
            }

            SetCondition(normalCondition);
        }

        /// <summary>
        /// Sets current active environmental condition and fires event callbacks.
        /// </summary>
        public void SetCondition(EnvironmentalConditionData condition)
        {
            EnsureNormalCondition();
            EnvironmentalConditionData targetCondition = condition != null ? condition : normalCondition;

            if (activeCondition == targetCondition) return;

            activeCondition = targetCondition;
            damageTickTimer = 0f;
            Debug.Log($"[EnvironmentalConditionManager] Environmental condition changed to: {activeCondition.displayName}");

            OnConditionChanged?.Invoke(activeCondition);

            if (!string.IsNullOrEmpty(activeCondition.warningMessage))
            {
                OnHazardWarning?.Invoke(activeCondition.warningMessage);
                ShowHUDToast(activeCondition.warningMessage);
            }
        }

        /// <summary>
        /// Clears hazard condition back to normal environment.
        /// </summary>
        public void ClearCondition()
        {
            SetCondition(normalCondition);
        }

        private void ShowHUDToast(string message)
        {
            HUDController hud = HUDController.Instance != null ? HUDController.Instance : FindObjectOfType<HUDController>();
            if (hud != null)
            {
                hud.ShowNotificationToast(message);
            }
        }

        // ==========================================
        // PERSISTENCE & SAVE/LOAD
        // ==========================================

        public EnvironmentalSaveData GetEnvironmentalSaveData()
        {
            var saveData = new EnvironmentalSaveData();
            saveData.activeConditionId = activeCondition != null ? activeCondition.conditionId : "normal";
            saveData.conditionTimer = conditionTimer;
            saveData.currentZoneId = activeZones.Count > 0 && activeZones[activeZones.Count - 1] != null 
                ? activeZones[activeZones.Count - 1].ZoneId 
                : "";

            return saveData;
        }

        public void RestoreEnvironmentalState(EnvironmentalSaveData saveData)
        {
            EnsureNormalCondition();
            if (saveData == null || string.IsNullOrEmpty(saveData.activeConditionId) || saveData.activeConditionId.Equals("normal", StringComparison.OrdinalIgnoreCase))
            {
                SetCondition(normalCondition);
                return;
            }

            EnvironmentalConditionData matchedData = FindConditionById(saveData.activeConditionId);
            if (matchedData != null)
            {
                SetCondition(matchedData);
                Debug.Log($"[EnvironmentalConditionManager] Restored environmental condition from save: {matchedData.displayName}");
            }
            else
            {
                SetCondition(normalCondition);
            }
        }

        private EnvironmentalConditionData FindConditionById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnvironmentalConditionData[] all = Resources.FindObjectsOfTypeAll<EnvironmentalConditionData>();
            return Array.Find(all, c => c != null && string.Equals(c.conditionId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
