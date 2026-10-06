using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.WorldEvents
{
    public enum WorldEventType
    {
        WaveHorde,
        ZombieHorde = WaveHorde,
        Outbreak,
        SupplyAmbush,
        BossEncounter,
        SupplyDrop,
        SurvivorRescue,
        SurvivorEncounter = SurvivorRescue,
        TimedScavenge,
        LootDiscovery,
        HighThreatZone
    }

    public enum WorldEventCompletionMode
    {
        ClearAllWaves,
        SurviveDuration,
        InteractObject,
        CollectItems
    }

    public enum WorldEventSafeZoneRule
    {
        Cancel,
        Pause,
        Fail
    }

    public enum EventTimeRequirement
    {
        AnyTime,
        DayOnly,
        NightOnly
    }

    /// <summary>
    /// ScriptableObject data container defining reusable configuration for Phase 18 dynamic world events.
    /// Configurable in Unity Inspector for multiple encounter locations.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWorldEventData", menuName = "Zombie Apocalypse/World Event Data")]
    public class WorldEventData : ScriptableObject
    {
        [Header("Event Identity")]
        public string eventId = "event_outbreak_01";
        public string displayName = "Zombie Outbreak";
        [TextArea(2, 4)]
        public string description = "Survive incoming zombie waves in this sector.";
        public WorldEventType eventType = WorldEventType.WaveHorde;
        public string associatedObjectiveId = "";

        [Header("Completion & Failure Rules")]
        public WorldEventCompletionMode completionMode = WorldEventCompletionMode.ClearAllWaves;
        public WorldEventSafeZoneRule safeZoneRule = WorldEventSafeZoneRule.Cancel;
        public float eventDuration = 60.0f; // Applicable if completionMode == SurviveDuration or timer cap
        public float cooldownTime = 60.0f;

        [Header("Environment & Time Requirements")]
        public EventTimeRequirement timeRequirement = EventTimeRequirement.AnyTime;
        public string requiredWeatherId = ""; // Empty for any weather

        [Header("Wave & Zombie Configuration")]
        public int waveCount = 3;
        public int zombiesPerWave = 4;
        public float waveDelay = 8.0f;
        public float difficultyMultiplier = 1.0f;
        public float walkerProbability = 0.6f;
        public float runnerProbability = 0.3f;
        public float tankProbability = 0.1f;
        public float bossProbability = 0.0f;

        [Header("Boss Encounter Configuration")]
        public ZombieApocalypse.Zombies.ZombieData bossVariantOverride;
        public bool spawnBossMinions = true;

        [Header("Supply & Scavenge Target Config")]
        public ItemData targetItem;
        public int requiredItemQuantity = 1;
        public string targetInteractableId = "";

        [Header("Distance & Proximity Limits")]
        public float minPlayerDistance = 15.0f;
        public float maxPlayerDistance = 40.0f;
        public float activationRadius = 25.0f;
        public float abandonRadius = 60.0f;

        [Header("Difficulty Bounds")]
        public float minDifficulty = 0.5f;
        public float maxDifficulty = 3.0f;

        [Header("Event Rewards")]
        public int rewardXP = 150;
        public List<InventorySlot> rewardItems = new List<InventorySlot>();
        public AmmoType rewardAmmoType = AmmoType.Pistol;
        public int rewardAmmoAmount = 30;

        [Header("Audio & Messages")]
        public AudioClip warningAudioClip;
        public AudioClip completionAudioClip;
        public string warningMessage = "";
        public string completionMessage = "";
    }
}
