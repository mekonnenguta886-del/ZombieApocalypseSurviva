using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.AI;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;
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
    /// Central manager for Phase 10 dynamic world events and zombie encounters.
    /// Manages event state machine, controlled wave progression coroutines, event-specific zombie tracking,
    /// safe-zone protections, player abandon distances, reward distribution, and HUD updates.
    /// Requests zombie wave creation strictly through the authoritative ZombieSpawner.
    /// 
    /// ATTACH TO: [WorldEventManager] GameObject in scene.
    /// </summary>
    public class WorldEventManager : MonoBehaviour
    {
        public static WorldEventManager Instance { get; private set; }

        public static event Action<WorldEventData> OnWorldEventStarted;
        public static event Action<WorldEventData, int, int> OnWorldEventWaveChanged;
        public static event Action<WorldEventData> OnWorldEventCompleted;
        public static event Action<WorldEventData, string> OnWorldEventFailed;

        [Header("Runtime State")]
        [SerializeField] private WorldEventState currentState = WorldEventState.Inactive;
        [SerializeField] private WorldEventData activeEventData;
        [SerializeField] private int currentWaveIndex = 0;

        private List<ZombieHealth> trackedEventZombies = new List<ZombieHealth>();
        private ZombieHealth activeBossZombie;
        private WorldEventTrigger activeTrigger;
        private Vector3 eventCenterPosition;
        private Coroutine activeWaveCoroutine;
        private float eventTimer;
        private float waveDelayTimer;

        // Player References
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private ZombieSpawner zombieSpawner;

        public WorldEventState CurrentState => currentState;
        public WorldEventData ActiveEvent => activeEventData;
        public int CurrentWave => currentWaveIndex;
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
                zombieSpawner = FindObjectOfType<ZombieSpawner>();
            }
        }

        private void Update()
        {
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

            // Update Event Duration Timer
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
            }

            UpdateHUDOverlay();
        }

        /// <summary>
        /// Attempts to trigger a world event from a location trigger.
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

            FindReferences();

            if (playerHealth != null && playerHealth.IsDead) return false;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return false;

            activeEventData = eventData;
            activeTrigger = trigger;
            eventCenterPosition = position;
            currentWaveIndex = 0;
            eventTimer = 0f;
            activeBossZombie = null;
            trackedEventZombies.Clear();

            SetState(WorldEventState.Preparing);
            Debug.Log($"[WorldEventManager] Event '{activeEventData.displayName}' started at {position}.");

            OnWorldEventStarted?.Invoke(activeEventData);
            activeWaveCoroutine = StartCoroutine(ExecuteWaveSequence());
            return true;
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
                    // Gunshot accelerates wave delay timer
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
                Debug.Log($"[WorldEventManager] Tracked event zombie eliminated. Event zombies remaining: {trackedEventZombies.Count}");
            }
        }

        private void CompleteEvent()
        {
            if (currentState == WorldEventState.Completed) return;

            StopWaveCoroutine();
            SetState(WorldEventState.Completed);

            Debug.Log($"[WorldEventManager] Event '{activeEventData?.displayName}' SUCCESSFULLY COMPLETED!");
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

            StopWaveCoroutine();
            SetState(WorldEventState.Cancelled);

            Debug.Log($"[WorldEventManager] Event '{activeEventData?.displayName}' CANCELLED! Reason: {reason}");

            if (activeTrigger != null)
            {
                activeTrigger.StartCooldown(activeEventData.cooldownTime);
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
            if (activeEventData.rewardAmmoAmount > 0 && weapons != null)
            {
                weapons.AddReserveAmmo(activeEventData.rewardAmmoType, activeEventData.rewardAmmoAmount);
            }
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
