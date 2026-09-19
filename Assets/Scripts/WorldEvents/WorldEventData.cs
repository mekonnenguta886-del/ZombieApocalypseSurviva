using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.WorldEvents
{
    public enum WorldEventType
    {
        WaveHorde,
        Outbreak,
        SupplyAmbush,
        BossEncounter
    }

    public enum WorldEventCompletionMode
    {
        ClearAllWaves,
        SurviveDuration
    }

    public enum WorldEventSafeZoneRule
    {
        Cancel,
        Pause,
        Fail
    }

    /// <summary>
    /// ScriptableObject data container defining reusable configuration for Phase 10 world events.
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

        [Header("Completion & Failure Rules")]
        public WorldEventCompletionMode completionMode = WorldEventCompletionMode.ClearAllWaves;
        public WorldEventSafeZoneRule safeZoneRule = WorldEventSafeZoneRule.Cancel;
        public float eventDuration = 60.0f; // Applicable if completionMode == SurviveDuration or timer cap
        public float cooldownTime = 60.0f;

        [Header("Wave & Zombie Configuration")]
        public int waveCount = 3;
        public int zombiesPerWave = 4;
        public float waveDelay = 8.0f;
        public float difficultyMultiplier = 1.0f;

        [Header("Boss Encounter Configuration")]
        public ZombieApocalypse.Zombies.ZombieData bossVariantOverride;
        public bool spawnBossMinions = true;

        [Header("Distance & Proximity Limits")]
        public float minPlayerDistance = 15.0f;
        public float maxPlayerDistance = 40.0f;
        public float activationRadius = 25.0f;
        public float abandonRadius = 60.0f;

        [Header("Event Rewards")]
        public List<InventorySlot> rewardItems = new List<InventorySlot>();
        public AmmoType rewardAmmoType = AmmoType.Pistol;
        public int rewardAmmoAmount = 30;
    }
}
