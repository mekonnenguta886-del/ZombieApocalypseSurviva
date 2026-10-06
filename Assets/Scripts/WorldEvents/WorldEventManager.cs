using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.AI;
using ZombieApocalypse.Audio;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Missions;
using ZombieApocalypse.Player;
using ZombieApocalypse.Progression;
using ZombieApocalypse.Systems;
using ZombieApocalypse.UI;
using ZombieApocalypse.Weapons;
using ZombieApocalypse.World;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.WorldEvents
{
    public enum WorldEventState
    {
        Inactive,
        Preparing,
        Active,
        WaveDelay,
        Completed,
        Failed,
        Cancelled,
        Cooldown
    }

    /// <summary>
    /// Central manager for Phase 18 dynamic world events and zombie encounters.
    /// Manages event state machine, dynamic horde progression, exploration events,
    /// objective integration with MissionManager, safe-zone protections, distance guards,
    /// audio feedback, reward distribution, HUD updates, and atomic save persistence.
    /// 
    /// ATTACH TO: [WorldEventManager] GameObject in scene.
    /// </summary>
    public class WorldEventManager : MonoBehaviour
    {
        public static WorldEventManager Instance { get; private set; }

        public static event Action<WorldEventData> OnWorldEventStarted;
        public static event Action<WorldEventData, int, int> OnWorldEventWaveChanged;
        public static event Action<WorldEventData, int, int> OnWorldEventProgressUpdated;
        public static event Action<WorldEventData> OnWorldEventCompleted;
        public static event Action<WorldEventData, string> OnWorldEventFailed;

        [Header("Runtime State")]
        [SerializeField] private WorldEventState currentState = WorldEventState.Inactive;
        [SerializeField] private WorldEventData activeEventData;
        [SerializeField] private int currentWaveIndex = 0;
        [SerializeField] private int currentProgressAmount = 0;

        private List<ZombieHealth> trackedEventZombies = new List<ZombieHealth>();
        private ZombieHealth activeBossZombie;
        private WorldEventTrigger activeTrigger;
        private Vector3 eventCenterPosition;
        private Coroutine activeWaveCoroutine;
        private float eventTimer;
        private float waveDelayTimer;

        private GameObject spawnedEventObject; // Supply container or distress beacon

        // Cooldown Tracking Dictionary (Event ID -> Expiry Time)
        private Dictionary<string, float> eventCooldownExpiryMap = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> completedEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Player References
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private ZombieSpawner zombieSpawner;

        public WorldEventState CurrentState => currentState;
        public WorldEventData ActiveEvent => activeEventData;
        public int CurrentWave => currentWaveIndex;
        public int CurrentProgress => currentProgressAmount;
        public int TrackedZombieCount
        {
            get
            {
                PruneTrackedZombiesList();
                return trackedEventZombies.Count;
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
            ZombieHealth.OnZombieKilled += HandleZombieKilled;
            NoiseManager.OnNoiseEmitted += HandleNoiseEmitted;
        }

        private void OnDisable()
        {
            ZombieHealth.OnZombieKilled -= HandleZombieKilled;
            NoiseManager.OnNoiseEmitted -= HandleNoiseEmitted;
        }

        private void Start()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerHealth = playerObj.GetComponent<PlayerHealth>();
            }

            if (zombieSpawner == null)
            {
                zombieSpawner = ZombieSpawner.Instance != null ? ZombieSpawner.Instance : FindObjectOfType<ZombieSpawner>();
            }
        }

        private void Update()
        {
            UpdateCooldownTimers();

            if (currentState == WorldEventState.Inactive || activeEventData == null) return;

            if (playerTransform == null || playerHealth == null)
            {
                FindReferences();
                if (playerTransform == null) return;
            }

            // Failure Guard 1: Player Death
            if (playerHealth.IsDead && currentState != WorldEventState.Failed)
            {
                FailEvent("Player Eliminated");
                return;
            }

            // SafeZone Protection Rule
            if (SafeZoneTrigger.IsPlayerInSafeZone)
            {
                if (activeEventData.safeZoneRule == WorldEventSafeZoneRule.Cancel)
                {
                    CancelEvent("Player Entered Safe Zone");
                    return;
                }
                else if (activeEventData.safeZoneRule == WorldEventSafeZoneRule.Fail)
                {
                    FailEvent("Player Retreat to Safe Zone");
                    return;
                }
            }

            // Failure Guard 2: Abandon Distance
            float distToPlayer = Vector3.Distance(playerTransform.position, eventCenterPosition);
            if (distToPlayer > activeEventData.abandonRadius && currentState != WorldEventState.Failed)
            {
                FailEvent("Player Abandoned Event Sector");
                return;
            }

            // Update Event Duration Timer & Objectives
            if (currentState == WorldEventState.Active || currentState == WorldEventState.WaveDelay)
            {
                eventTimer += Time.deltaTime;

                if (activeEventData.completionMode == WorldEventCompletionMode.SurviveDuration)
                {
                    if (eventTimer >= activeEventData.eventDuration)
                    {
                        CompleteEvent();
                        return;
                    }
                }
                else if (activeEventData.completionMode == WorldEventCompletionMode.CollectItems || activeEventData.eventType == WorldEventType.TimedScavenge)
                {
                    if (activeEventData.targetItem != null && playerTransform != null)
                    {
                        var inv = playerTransform.GetComponent<InventorySystem>();
                        int owned = inv != null ? inv.GetItemQuantity(activeEventData.targetItem) : 0;
                        if (owned >= activeEventData.requiredItemQuantity)
                        {
                            CompleteEvent();
                            return;
                        }
                    }

                    if (activeEventData.eventDuration > 0f && eventTimer >= activeEventData.eventDuration)
                    {
                        FailEvent("Time Expired");
                        return;
                    }
                }
            }

            UpdateHUDOverlay();
        }

        private void UpdateCooldownTimers()
        {
            if (eventCooldownExpiryMap.Count == 0) return;

            List<string> expiredKeys = null;
            float currentTime = Time.time;

            foreach (var kvp in eventCooldownExpiryMap)
            {
                if (currentTime >= kvp.Value)
                {
                    if (expiredKeys == null) expiredKeys = new List<string>();
                    expiredKeys.Add(kvp.Key);
                }
            }

            if (expiredKeys != null)
            {
                foreach (var key in expiredKeys)
                {
                    eventCooldownExpiryMap.Remove(key);
                    Debug.Log($"[WorldEventManager] Cooldown expired for event/trigger '{key}'.");
                }
            }
        }

        public bool IsEventOnCooldown(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return false;
            return eventCooldownExpiryMap.ContainsKey(eventId) && Time.time < eventCooldownExpiryMap[eventId];
        }

        public float GetRemainingCooldown(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !eventCooldownExpiryMap.ContainsKey(eventId)) return 0f;
            return Mathf.Max(0f, eventCooldownExpiryMap[eventId] - Time.time);
        }

        public void RegisterTriggerCooldown(string eventId, float duration)
        {
            if (string.IsNullOrEmpty(eventId) || duration <= 0f) return;
            eventCooldownExpiryMap[eventId] = Time.time + duration;
        }

        /// <summary>
        /// Attempts to trigger a world event from a location trigger or dynamic spawner.
        /// Returns true if successfully started.
        /// </summary>
        public bool StartEvent(WorldEventData eventData, Vector3 position, WorldEventTrigger trigger)
        {
            if (eventData == null) return false;
            if (currentState != WorldEventState.Inactive && currentState != WorldEventState.Cooldown)
            {
                Debug.LogWarning($"[WorldEventManager] Cannot start event '{eventData.displayName}'. Manager is currently in state {currentState}.");
                return false;
            }

            if (IsEventOnCooldown(eventData.eventId))
            {
                Debug.LogWarning($"[WorldEventManager] Event '{eventData.displayName}' is currently on cooldown ({GetRemainingCooldown(eventData.eventId):F1}s remaining).");
                return false;
            }

            FindReferences();

            if (playerHealth != null && playerHealth.IsDead) return false;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return false;

            activeEventData = eventData;
            activeTrigger = trigger;
            eventCenterPosition = position;
            currentWaveIndex = 0;
            currentProgressAmount = 0;
            eventTimer = 0f;
            activeBossZombie = null;
            trackedEventZombies.Clear();

            SetState(WorldEventState.Preparing);
            Debug.Log($"[WorldEventManager] Event '{activeEventData.displayName}' (Type: {activeEventData.eventType}) started at {position}.");

            // Audio & HUD Toast Feedback
            if (AudioManager.Instance != null && activeEventData.warningAudioClip != null)
            {
                AudioManager.Instance.PlaySFX(activeEventData.warningAudioClip, position);
            }

            HUDController hud = HUDController.Instance != null ? HUDController.Instance : FindObjectOfType<HUDController>();
            if (hud != null)
            {
                string msg = !string.IsNullOrEmpty(activeEventData.warningMessage) ? activeEventData.warningMessage : $"EVENT WARNING: {activeEventData.displayName}";
                hud.ShowNotificationToast(msg);
            }

            // Spawn Exploration Event Interactable Object if applicable
            SpawnExplorationEventObject(position);

            OnWorldEventStarted?.Invoke(activeEventData);

            // Notify MissionManager if an associated objective ID exists
            if (!string.IsNullOrEmpty(activeEventData.associatedObjectiveId) && MissionManager.Instance != null)
            {
                Debug.Log($"[WorldEventManager] Linked with active objective '{activeEventData.associatedObjectiveId}' in MissionManager.");
            }

            activeWaveCoroutine = StartCoroutine(ExecuteWaveSequence());
            return true;
        }

        private void SpawnExplorationEventObject(Vector3 position)
        {
            if (activeEventData == null) return;

            if (activeEventData.eventType == WorldEventType.SupplyDrop || activeEventData.eventType == WorldEventType.LootDiscovery)
            {
                GameObject dropObj = new GameObject($"SupplyDropContainer_{activeEventData.eventId}");
                dropObj.transform.position = position + Vector3.up * 0.5f;

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(dropObj.transform, false);
                visual.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(1.0f, 0.6f, 0.1f);
                }

                SupplyDropContainer container = dropObj.AddComponent<SupplyDropContainer>();
                string targetId = !string.IsNullOrEmpty(activeEventData.targetInteractableId) ? activeEventData.targetInteractableId : "supply_drop_box";
                container.Setup(activeEventData.eventId, targetId);

                spawnedEventObject = dropObj;
            }
            else if (activeEventData.eventType == WorldEventType.SurvivorEncounter || activeEventData.eventType == WorldEventType.SurvivorRescue)
            {
                GameObject survivorObj = new GameObject($"SurvivorBeacon_{activeEventData.eventId}");
                survivorObj.transform.position = position + Vector3.up * 0.5f;

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.transform.SetParent(survivorObj.transform, false);
                visual.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.2f, 0.8f, 0.3f);
                }

                SurvivorDistressInteractable beacon = survivorObj.AddComponent<SurvivorDistressInteractable>();
                string targetId = !string.IsNullOrEmpty(activeEventData.targetInteractableId) ? activeEventData.targetInteractableId : "survivor_beacon";
                beacon.Setup(activeEventData.eventId, targetId);

                spawnedEventObject = survivorObj;
            }
        }

        private IEnumerator ExecuteWaveSequence()
        {
            yield return new WaitForSeconds(1.5f);

            while (currentWaveIndex < activeEventData.waveCount)
            {
                currentWaveIndex++;
                SetState(WorldEventState.Active);

                Debug.Log($"[WorldEventManager] Spawning Wave {currentWaveIndex}/{activeEventData.waveCount} for '{activeEventData.displayName}'.");
                OnWorldEventWaveChanged?.Invoke(activeEventData, currentWaveIndex, activeEventData.waveCount);

                // Boss Encounter Special Handling: Spawn Boss Zombie on Wave 1
                if (activeEventData.eventType == WorldEventType.BossEncounter && currentWaveIndex == 1 && zombieSpawner != null)
                {
                    ZombieData bossDataToSpawn = activeEventData.bossVariantOverride != null
                        ? activeEventData.bossVariantOverride
                        : zombieSpawner.BossData;

                    Vector3 spawnPos = eventCenterPosition;
                    if (UnityEngine.AI.NavMesh.SamplePosition(eventCenterPosition + Vector3.forward * 3f, out UnityEngine.AI.NavMeshHit hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        spawnPos = hit.position;
                    }

                    activeBossZombie = zombieSpawner.SpawnVariant(bossDataToSpawn, spawnPos);
                    if (activeBossZombie != null && !trackedEventZombies.Contains(activeBossZombie))
                    {
                        trackedEventZombies.Add(activeBossZombie);
                    }
                }

                // Request minion/horde wave spawn through single authoritative ZombieSpawner if enabled
                if (zombieSpawner != null && (activeEventData.eventType != WorldEventType.BossEncounter || activeEventData.spawnBossMinions))
                {
                    List<ZombieHealth> newZombies = zombieSpawner.TriggerEncounterWaveWithCallback(activeEventData.zombiesPerWave);
                    if (newZombies != null)
                    {
                        foreach (var z in newZombies)
                        {
                            if (z != null && !trackedEventZombies.Contains(z))
                            {
                                trackedEventZombies.Add(z);
                            }
                        }
                    }
                }

                // Wait for all wave zombies to be eliminated if ClearAllWaves mode
                if (activeEventData.completionMode == WorldEventCompletionMode.ClearAllWaves)
                {
                    while (TrackedZombieCount > 0)
                    {
                        if (currentState != WorldEventState.Active && currentState != WorldEventState.WaveDelay)
                        {
                            yield break; // Event cancelled or failed
                        }
                        yield return new WaitForSeconds(0.5f);
                    }
                }

                // Inter-wave delay
                if (currentWaveIndex < activeEventData.waveCount)
                {
                    SetState(WorldEventState.WaveDelay);
                    waveDelayTimer = activeEventData.waveDelay;
                    while (waveDelayTimer > 0f)
                    {
                        if (currentState != WorldEventState.Active && currentState != WorldEventState.WaveDelay)
                        {
                            yield break;
                        }
                        waveDelayTimer -= Time.deltaTime;
                        yield return null;
                    }
                }
            }

            // Boss Encounter Special Check: Explicitly wait until Boss Zombie is eliminated
            if (activeEventData.eventType == WorldEventType.BossEncounter)
            {
                while (activeBossZombie != null && !activeBossZombie.IsDead)
                {
                    if (currentState != WorldEventState.Active && currentState != WorldEventState.WaveDelay)
                    {
                        yield break;
                    }
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // All waves spawned and cleared
            if (activeEventData.completionMode == WorldEventCompletionMode.ClearAllWaves || activeEventData.eventType == WorldEventType.BossEncounter)
            {
                CompleteEvent();
            }
        }

        private void HandleNoiseEmitted(NoiseEvent noise)
        {
            if (currentState != WorldEventState.WaveDelay) return;
            if (noise.type == NoiseType.Gunshot)
            {
                float dist = Vector3.Distance(noise.position, eventCenterPosition);
                if (dist <= activeEventData.activationRadius * 1.5f)
                {
                    waveDelayTimer = Mathf.Max(0f, waveDelayTimer - 3.0f);
                    Debug.Log($"[WorldEventManager] Gunshot noise accelerated wave delay timer! Remaining delay: {waveDelayTimer:F1}s");
                }
            }
        }

        private void HandleZombieKilled(ZombieHealth zombie)
        {
            if (zombie != null && trackedEventZombies.Contains(zombie))
            {
                trackedEventZombies.Remove(zombie);
                currentProgressAmount++;
                OnWorldEventProgressUpdated?.Invoke(activeEventData, currentProgressAmount, activeEventData.waveCount * activeEventData.zombiesPerWave);
                Debug.Log($"[WorldEventManager] Tracked event zombie eliminated. Event zombies remaining: {trackedEventZombies.Count}");
            }
        }

        public void NotifyInteractableTriggered(string interactableId)
        {
            if (currentState != WorldEventState.Active && currentState != WorldEventState.Preparing) return;
            if (activeEventData == null) return;

            string targetId = !string.IsNullOrEmpty(activeEventData.targetInteractableId) ? activeEventData.targetInteractableId : "supply_drop_box";
            if (string.Equals(interactableId, targetId, StringComparison.OrdinalIgnoreCase) || activeEventData.completionMode == WorldEventCompletionMode.InteractObject)
            {
                Debug.Log($"[WorldEventManager] Target interactable '{interactableId}' triggered for event '{activeEventData.displayName}'. Completing event!");
                CompleteEvent();
            }
        }

        private void CompleteEvent()
        {
            if (currentState == WorldEventState.Completed) return;

            if (activeEventData != null && !string.IsNullOrEmpty(activeEventData.eventId))
            {
                completedEventIds.Add(activeEventData.eventId);
                RegisterTriggerCooldown(activeEventData.eventId, activeEventData.cooldownTime);
            }

            StopWaveCoroutine();
            SetState(WorldEventState.Completed);

            Debug.Log($"[WorldEventManager] Event '{activeEventData?.displayName}' SUCCESSFULLY COMPLETED!");

            // Play completion audio & toast
            if (AudioManager.Instance != null && activeEventData != null && activeEventData.completionAudioClip != null)
            {
                AudioManager.Instance.PlaySFX(activeEventData.completionAudioClip, eventCenterPosition);
            }

            HUDController hud = HUDController.Instance != null ? HUDController.Instance : FindObjectOfType<HUDController>();
            if (hud != null)
            {
                string msg = activeEventData != null && !string.IsNullOrEmpty(activeEventData.completionMessage)
                    ? activeEventData.completionMessage
                    : $"EVENT COMPLETED: {activeEventData?.displayName}";
                hud.ShowNotificationToast(msg);
            }

            GrantEventRewards();

            OnWorldEventCompleted?.Invoke(activeEventData);

            if (activeTrigger != null)
            {
                activeTrigger.StartCooldown(activeEventData.cooldownTime);
            }

            StartCoroutine(ResetToInactiveDelayed(3.0f));
        }

        public void FailEvent(string reason)
        {
            if (currentState == WorldEventState.Failed || currentState == WorldEventState.Inactive) return;

            if (activeEventData != null && !string.IsNullOrEmpty(activeEventData.eventId))
            {
                RegisterTriggerCooldown(activeEventData.eventId, activeEventData.cooldownTime);
            }

            StopWaveCoroutine();
            SetState(WorldEventState.Failed);

            Debug.LogWarning($"[WorldEventManager] Event '{activeEventData?.displayName}' FAILED! Reason: {reason}");
            OnWorldEventFailed?.Invoke(activeEventData, reason);

            if (activeTrigger != null)
            {
                activeTrigger.StartCooldown(activeEventData.cooldownTime);
            }

            StartCoroutine(ResetToInactiveDelayed(3.0f));
        }

        public void CancelEvent(string reason)
        {
            if (currentState == WorldEventState.Cancelled || currentState == WorldEventState.Inactive) return;

            if (activeEventData != null && !string.IsNullOrEmpty(activeEventData.eventId))
            {
                RegisterTriggerCooldown(activeEventData.eventId, activeEventData.cooldownTime / 2.0f);
            }

            StopWaveCoroutine();
            SetState(WorldEventState.Cancelled);

            Debug.Log($"[WorldEventManager] Event '{activeEventData?.displayName}' CANCELLED! Reason: {reason}");

            if (activeTrigger != null)
            {
                activeTrigger.StartCooldown(activeEventData.cooldownTime / 2.0f);
            }

            StartCoroutine(ResetToInactiveDelayed(2.0f));
        }

        private void StopWaveCoroutine()
        {
            if (activeWaveCoroutine != null)
            {
                StopCoroutine(activeWaveCoroutine);
                activeWaveCoroutine = null;
            }
        }

        private IEnumerator ResetToInactiveDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideHUDOverlay();

            if (spawnedEventObject != null)
            {
                Destroy(spawnedEventObject);
                spawnedEventObject = null;
            }

            trackedEventZombies.Clear();
            activeBossZombie = null;
            activeEventData = null;
            activeTrigger = null;
            SetState(WorldEventState.Inactive);
        }

        private void GrantEventRewards()
        {
            if (activeEventData == null || playerTransform == null) return;

            InventorySystem inventory = playerTransform.GetComponent<InventorySystem>();
            WeaponController weapons = playerTransform.GetComponent<WeaponController>();

            // Difficulty & Weather Reward Scaling
            float diffMult = DifficultyManager.Instance != null ? DifficultyManager.Instance.GetCurrentDifficulty().difficultyMultiplier : 1.0f;
            float weatherBonus = WeatherManager.Instance != null && WeatherManager.Instance.ActiveWeather != null && WeatherManager.Instance.ActiveWeather.environmentalCondition != null ? 1.25f : 1.0f;

            // Grant XP Reward via PlayerProgressionSystem
            int xpReward = Mathf.RoundToInt(activeEventData.rewardXP * diffMult * weatherBonus);
            if (xpReward > 0 && PlayerProgressionSystem.Instance != null)
            {
                PlayerProgressionSystem.Instance.AddXP(xpReward);
                Debug.Log($"[WorldEventManager] Granted {xpReward} XP for event '{activeEventData.displayName}'.");
            }

            // Grant Item Rewards
            if (activeEventData.rewardItems != null && inventory != null)
            {
                foreach (var slot in activeEventData.rewardItems)
                {
                    if (slot == null || slot.itemData == null || slot.quantity <= 0) continue;
                    int added = inventory.AddItem(slot.itemData, slot.quantity);
                    int overflow = slot.quantity - added;

                    if (overflow > 0 && slot.itemData.dropPrefab != null)
                    {
                        Vector3 dropPos = playerTransform.position + playerTransform.forward * 1.2f + Vector3.up * 0.5f;
                        GameObject droppedObj = Instantiate(slot.itemData.dropPrefab, dropPos, Quaternion.identity);
                        ItemPickup pickup = droppedObj.GetComponent<ItemPickup>();
                        if (pickup != null) pickup.Setup(slot.itemData, overflow);
                    }
                }
            }

            // Grant Ammo Rewards
            int ammoReward = Mathf.RoundToInt(activeEventData.rewardAmmoAmount * diffMult);
            if (ammoReward > 0 && weapons != null)
            {
                weapons.AddReserveAmmo(activeEventData.rewardAmmoType, ammoReward);
            }
        }

        public Save.WorldEventSaveData GetWorldEventSaveData()
        {
            var saveData = new Save.WorldEventSaveData();
            saveData.activeEventId = activeEventData != null ? activeEventData.eventId : "";
            saveData.currentState = currentState.ToString();
            saveData.currentWaveIndex = currentWaveIndex;
            saveData.eventTimer = eventTimer;
            saveData.currentProgressAmount = currentProgressAmount;
            saveData.completedEventIds = new List<string>(completedEventIds);

            // Compile cooldown states
            float currentTime = Time.time;
            foreach (var kvp in eventCooldownExpiryMap)
            {
                float remaining = kvp.Value - currentTime;
                if (remaining > 0f)
                {
                    saveData.cooldownEventIds.Add(kvp.Key);
                    saveData.cooldownRemainingTimes.Add(remaining);
                }
            }

            return saveData;
        }

        public void RestoreWorldEventState(Save.WorldEventSaveData saveData)
        {
            if (saveData == null) return;

            if (saveData.completedEventIds != null)
            {
                completedEventIds = new HashSet<string>(saveData.completedEventIds, StringComparer.OrdinalIgnoreCase);
            }

            // Restore Cooldowns
            if (saveData.cooldownEventIds != null && saveData.cooldownRemainingTimes != null)
            {
                eventCooldownExpiryMap.Clear();
                int count = Mathf.Min(saveData.cooldownEventIds.Count, saveData.cooldownRemainingTimes.Count);
                float currentTime = Time.time;
                for (int i = 0; i < count; i++)
                {
                    string key = saveData.cooldownEventIds[i];
                    float remaining = saveData.cooldownRemainingTimes[i];
                    if (!string.IsNullOrEmpty(key) && remaining > 0f)
                    {
                        eventCooldownExpiryMap[key] = currentTime + remaining;
                    }
                }
            }

            if (!string.IsNullOrEmpty(saveData.activeEventId) && Enum.TryParse(saveData.currentState, out WorldEventState parsedState))
            {
                if (parsedState == WorldEventState.Active || parsedState == WorldEventState.WaveDelay || parsedState == WorldEventState.Preparing)
                {
                    WorldEventData eventAsset = FindEventDataById(saveData.activeEventId);
                    if (eventAsset != null)
                    {
                        activeEventData = eventAsset;
                        currentWaveIndex = saveData.currentWaveIndex;
                        eventTimer = saveData.eventTimer;
                        currentProgressAmount = saveData.currentProgressAmount;
                        SetState(parsedState);
                        Debug.Log($"[WorldEventManager] Restored active event '{saveData.activeEventId}' in state {parsedState}.");
                    }
                }
            }
        }

        private WorldEventData FindEventDataById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            WorldEventData[] allEvents = Resources.FindObjectsOfTypeAll<WorldEventData>();
            return Array.Find(allEvents, e => e != null && string.Equals(e.eventId, id, StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateHUDOverlay()
        {
            HUDController hud = HUDController.Instance != null ? HUDController.Instance : FindObjectOfType<HUDController>();
            if (hud == null || activeEventData == null) return;

            float timeRemaining = Mathf.Max(0f, activeEventData.eventDuration - eventTimer);
            hud.SetWorldEventHUD(activeEventData.displayName, currentWaveIndex, activeEventData.waveCount, TrackedZombieCount, timeRemaining);

            if (activeBossZombie != null && !activeBossZombie.IsDead)
            {
                hud.SetBossHUD(activeBossZombie.Data?.zombieName ?? "BOSS ZOMBIE", activeBossZombie.CurrentHealth, activeBossZombie.MaxHealth);
            }
            else
            {
                hud.HideBossHUD();
            }
        }

        private void HideHUDOverlay()
        {
            HUDController hud = HUDController.Instance != null ? HUDController.Instance : FindObjectOfType<HUDController>();
            if (hud != null)
            {
                hud.HideWorldEventHUD();
                hud.HideBossHUD();
            }
        }

        private void PruneTrackedZombiesList()
        {
            for (int i = trackedEventZombies.Count - 1; i >= 0; i--)
            {
                if (trackedEventZombies[i] == null || trackedEventZombies[i].IsDead)
                {
                    trackedEventZombies.RemoveAt(i);
                }
            }
        }

        private void SetState(WorldEventState newState)
        {
            currentState = newState;
        }
    }
}
