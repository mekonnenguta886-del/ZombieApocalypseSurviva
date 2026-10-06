using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Player;
using ZombieApocalypse.Systems;
using ZombieApocalypse.World;

namespace ZombieApocalypse.WorldEvents
{
    /// <summary>
    /// Spawner controller for dynamic world events during exploration.
    /// Periodically evaluates player position, world time (day/night), weather conditions, and difficulty
    /// to trigger dynamic world events (Zombie Horde, Supply Drop, High Threat Zone, Survivor Encounter, etc.)
    /// with strict cooldowns and safe house protections.
    /// 
    /// ATTACH TO: [WorldEventSpawner] GameObject in scene.
    /// </summary>
    public class WorldEventSpawner : MonoBehaviour
    {
        public static WorldEventSpawner Instance { get; private set; }

        [Header("Event Pool")]
        [SerializeField] private List<WorldEventData> availableEventProfiles = new List<WorldEventData>();

        [Header("Dynamic Spawning Interval")]
        [Tooltip("Minimum time (seconds) between dynamic world events.")]
        [SerializeField] private float minIntervalSeconds = 90.0f;
        [Tooltip("Maximum time (seconds) between dynamic world events.")]
        [SerializeField] private float maxIntervalSeconds = 180.0f;
        [SerializeField] private bool enableDynamicSpawning = true;

        [Header("Distance Parameters")]
        [SerializeField] private float minSpawnDistance = 20.0f;
        [SerializeField] private float maxSpawnDistance = 45.0f;

        private float nextSpawnTimer = 0f;
        private Transform playerTransform;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ResetSpawnTimer();
            EnsureDefaultProfiles();
        }

        private void EnsureDefaultProfiles()
        {
            if (availableEventProfiles.Count == 0)
            {
                // Profile 1: Zombie Horde Event
                WorldEventData horde = ScriptableObject.CreateInstance<WorldEventData>();
                horde.eventId = "event_dynamic_horde_01";
                horde.displayName = "ZOMBIE HORDE APPROACHING";
                horde.description = "A massive swarm of zombies is converging on your location! Prepare for combat!";
                horde.eventType = WorldEventType.ZombieHorde;
                horde.completionMode = WorldEventCompletionMode.ClearAllWaves;
                horde.safeZoneRule = WorldEventSafeZoneRule.Cancel;
                horde.waveCount = 3;
                horde.zombiesPerWave = 5;
                horde.waveDelay = 6.0f;
                horde.cooldownTime = 90.0f;
                horde.rewardXP = 200;
                horde.rewardAmmoAmount = 40;
                availableEventProfiles.Add(horde);

                // Profile 2: Supply Drop Event
                WorldEventData supply = ScriptableObject.CreateInstance<WorldEventData>();
                supply.eventId = "event_dynamic_supply_01";
                supply.displayName = "EMERGENCY SUPPLY DROP";
                supply.description = "An emergency medical supply crate has been discovered nearby. Secure the perimeter and retrieve the supplies!";
                supply.eventType = WorldEventType.SupplyDrop;
                supply.completionMode = WorldEventCompletionMode.InteractObject;
                supply.safeZoneRule = WorldEventSafeZoneRule.Cancel;
                supply.targetInteractableId = "supply_drop_box";
                supply.eventDuration = 120.0f;
                supply.cooldownTime = 120.0f;
                supply.rewardXP = 150;
                supply.rewardAmmoAmount = 25;
                availableEventProfiles.Add(supply);

                // Profile 3: High Threat Zone Event
                WorldEventData threat = ScriptableObject.CreateInstance<WorldEventData>();
                threat.eventId = "event_dynamic_highthreat_01";
                threat.displayName = "HIGH THREAT HAZARD ZONE";
                threat.description = "You have entered a high threat zombie territory! Clear all threats in the zone to claim rewards.";
                threat.eventType = WorldEventType.HighThreatZone;
                threat.completionMode = WorldEventCompletionMode.ClearAllWaves;
                threat.safeZoneRule = WorldEventSafeZoneRule.Fail;
                threat.waveCount = 2;
                threat.zombiesPerWave = 6;
                threat.waveDelay = 5.0f;
                threat.cooldownTime = 150.0f;
                threat.rewardXP = 250;
                threat.rewardAmmoAmount = 50;
                availableEventProfiles.Add(threat);
            }
        }

        private void Update()
        {
            if (!enableDynamicSpawning) return;

            if (playerTransform == null || playerHealth == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            if (playerHealth.IsDead) return;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return;

            // Wait if WorldEventManager is currently executing an event
            if (WorldEventManager.Instance != null && WorldEventManager.Instance.CurrentState != WorldEventState.Inactive)
            {
                return;
            }

            nextSpawnTimer -= Time.deltaTime;
            if (nextSpawnTimer <= 0f)
            {
                ResetSpawnTimer();
                TryTriggerDynamicEvent();
            }
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                playerHealth = p.GetComponent<PlayerHealth>();
            }
        }

        private void ResetSpawnTimer()
        {
            nextSpawnTimer = UnityEngine.Random.Range(minIntervalSeconds, maxIntervalSeconds);
        }

        public bool TryTriggerDynamicEvent()
        {
            if (playerTransform == null || playerHealth == null || playerHealth.IsDead) return false;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return false;

            if (WorldEventManager.Instance != null && WorldEventManager.Instance.CurrentState != WorldEventState.Inactive)
            {
                return false;
            }

            EnsureDefaultProfiles();

            // Filter candidates based on Time of Day & Weather
            bool isNight = WorldTimeManager.Instance != null && WorldTimeManager.Instance.IsNight;
            string activeWeatherId = WeatherManager.Instance != null && WeatherManager.Instance.ActiveWeather != null
                ? WeatherManager.Instance.ActiveWeather.weatherId
                : "";

            List<WorldEventData> validCandidates = new List<WorldEventData>();
            foreach (var profile in availableEventProfiles)
            {
                if (profile == null) continue;

                // Check Cooldown
                if (WorldEventManager.Instance != null && WorldEventManager.Instance.IsEventOnCooldown(profile.eventId))
                {
                    continue;
                }

                // Check Time requirement
                if (profile.timeRequirement == EventTimeRequirement.DayOnly && isNight) continue;
                if (profile.timeRequirement == EventTimeRequirement.NightOnly && !isNight) continue;

                // Check Weather requirement
                if (!string.IsNullOrEmpty(profile.requiredWeatherId) && !profile.requiredWeatherId.Equals(activeWeatherId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                validCandidates.Add(profile);
            }

            if (validCandidates.Count == 0)
            {
                return false;
            }

            // At Night: Increase probability of Zombie Horde events
            WorldEventData selectedProfile = null;
            if (isNight)
            {
                var hordeEvents = validCandidates.FindAll(e => e.eventType == WorldEventType.ZombieHorde || e.eventType == WorldEventType.WaveHorde);
                if (hordeEvents.Count > 0 && UnityEngine.Random.value < 0.75f)
                {
                    selectedProfile = hordeEvents[UnityEngine.Random.Range(0, hordeEvents.Count)];
                }
            }

            if (selectedProfile == null)
            {
                selectedProfile = validCandidates[UnityEngine.Random.Range(0, validCandidates.Count)];
            }

            // Find valid spawn location near player outside safe zone
            Vector3 spawnPos = playerTransform.position;
            for (int i = 0; i < 5; i++)
            {
                Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector3 candidate = playerTransform.position + new Vector3(circle.x, 0f, circle.y);

                if (UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit hit, 4.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    if (!SafeZoneTrigger.IsPositionInSafeZone(hit.position))
                    {
                        spawnPos = hit.position;
                        break;
                    }
                }
            }

            // Start event via WorldEventManager
            if (WorldEventManager.Instance != null)
            {
                bool success = WorldEventManager.Instance.StartEvent(selectedProfile, spawnPos, null);
                if (success)
                {
                    Debug.Log($"[WorldEventSpawner] Dynamically initiated world event '{selectedProfile.displayName}' at {spawnPos}.");
                    return true;
                }
            }

            return false;
        }
    }
}
