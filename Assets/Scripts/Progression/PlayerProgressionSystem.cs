using System;
using UnityEngine;
using ZombieApocalypse.Missions;
using ZombieApocalypse.Save;
using ZombieApocalypse.UI;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.Progression
{
    public enum SkillCategory
    {
        Combat,
        Survival,
        Scavenging,
        Crafting
    }

    /// <summary>
    /// Centralized manager for player XP, level progression, skill points, and perk upgrades.
    /// Subscribes to existing gameplay events (ZombieHealth.OnZombieKilled, MissionManager.OnMissionCompleted)
    /// without mutating underlying combat or mission systems.
    /// 
    /// ATTACH TO: [PlayerProgressionSystem] manager GameObject in scene.
    /// </summary>
    public class PlayerProgressionSystem : MonoBehaviour
    {
        public static PlayerProgressionSystem Instance { get; private set; }

        public event Action<int, int, int> OnXPChanged; // currentXPInLevel, requiredXPForNextLevel, totalXP
        public event Action<int, int, int> OnLevelUp;   // oldLevel, newLevel, skillPointsGranted
        public event Action OnProgressionUpdated;

        [Header("Progression Data Config")]
        [SerializeField] private PlayerProgressionData configData;

        // Runtime Progression State
        private int currentLevel = 1;
        private int currentXP = 0; // Progress toward next level
        private int totalXP = 0;   // Cumulative lifetime XP
        private int skillPoints = 0;

        // Skill Levels
        private int combatSkillLevel = 0;
        private int survivalSkillLevel = 0;
        private int scavengingSkillLevel = 0;
        private int craftingSkillLevel = 0;

        // Notification Aggregation Timer for rapid kills
        private float recentXPAggregateTimer = 0f;
        private int recentXPAmount = 0;

        public int CurrentLevel => currentLevel;
        public int CurrentXP => currentXP;
        public int TotalXP => totalXP;
        public int SkillPoints => skillPoints;
        public int RequiredXPForNextLevel => GetRequiredXP(currentLevel);

        public int CombatSkillLevel => combatSkillLevel;
        public int SurvivalSkillLevel => survivalSkillLevel;
        public int ScavengingSkillLevel => scavengingSkillLevel;
        public int CraftingSkillLevel => craftingSkillLevel;

        public PlayerProgressionData Config => configData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadDefaultConfigIfMissing();
        }

        private void OnEnable()
        {
            ZombieHealth.OnZombieKilled += HandleZombieKilled;
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
            }
        }

        private void OnDisable()
        {
            ZombieHealth.OnZombieKilled -= HandleZombieKilled;
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
            }
        }

        private void Start()
        {
            // Subscribe to MissionManager if it initialized after Awake
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
                MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
            }

            NotifyStateChanged();
        }

        private void Update()
        {
            // Flush aggregated XP toast notification if kills occurred rapidly
            if (recentXPAggregateTimer > 0f)
            {
                recentXPAggregateTimer -= Time.deltaTime;
                if (recentXPAggregateTimer <= 0f && recentXPAmount > 0)
                {
                    ShowHUDToast($"+{recentXPAmount} XP");
                    recentXPAmount = 0;
                }
            }
        }

        private void LoadDefaultConfigIfMissing()
        {
            if (configData == null)
            {
                configData = ScriptableObject.CreateInstance<PlayerProgressionData>();
            }
        }

        private int GetRequiredXP(int level)
        {
            LoadDefaultConfigIfMissing();
            return configData != null ? configData.GetXPRequiredForNextLevel(level) : 100 + (level - 1) * 150;
        }

        // ==========================================
        // GAMEPLAY EVENT HANDLERS
        // ==========================================

        private void HandleZombieKilled(ZombieHealth zombie)
        {
            if (zombie == null) return;
            int xpReward = configData != null ? configData.xpPerZombieKill : 25;

            // Aggregate notification toast for multi-kills
            recentXPAmount += xpReward;
            recentXPAggregateTimer = 0.4f;

            AddXP(xpReward);
        }

        private void HandleMissionCompleted(MissionData mission)
        {
            int xpReward = configData != null ? configData.xpPerMissionCompleted : 200;
            ShowHUDToast($"+{xpReward} XP (Mission Completed!)");
            AddXP(xpReward);
        }

        // ==========================================
        // XP & MULTI-LEVEL GAIN LOGIC
        // ==========================================

        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            LoadDefaultConfigIfMissing();
            int maxLevel = configData != null ? configData.maxLevel : 50;
            int ptsPerLevel = configData != null ? configData.skillPointsPerLevel : 1;

            if (currentLevel >= maxLevel)
            {
                totalXP += amount;
                NotifyStateChanged();
                return;
            }

            int oldLevel = currentLevel;
            totalXP += amount;
            currentXP += amount;

            int levelsGained = 0;
            int required = GetRequiredXP(currentLevel);

            // Execute atomic multi-level gain loop
            while (currentXP >= required && currentLevel < maxLevel)
            {
                currentXP -= required;
                currentLevel++;
                skillPoints += ptsPerLevel;
                levelsGained++;

                required = GetRequiredXP(currentLevel);
            }

            if (currentLevel >= maxLevel)
            {
                currentXP = 0;
            }

            if (levelsGained > 0)
            {
                int pointsGranted = levelsGained * ptsPerLevel;
                Debug.Log($"[PlayerProgressionSystem] LEVEL UP! Level {oldLevel} → {currentLevel}. Granted {pointsGranted} Skill Point(s).");

                ShowHUDToast($"LEVEL UP! Reached Level {currentLevel} (+{pointsGranted} Skill Point)");
                OnLevelUp?.Invoke(oldLevel, currentLevel, pointsGranted);
            }

            NotifyStateChanged();
        }

        // ==========================================
        // SKILL UPGRADE ACTIONS
        // ==========================================

        public bool TryUpgradeSkill(SkillCategory category)
        {
            if (skillPoints <= 0) return false;

            LoadDefaultConfigIfMissing();
            int maxSkillLevel = configData != null ? configData.maxSkillLevel : 5;
            int currentSkillLvl = GetSkillLevel(category);

            if (currentSkillLvl >= maxSkillLevel) return false;

            skillPoints--;
            SetSkillLevel(category, currentSkillLvl + 1);

            Debug.Log($"[PlayerProgressionSystem] Upgraded {category} Skill to Level {currentSkillLvl + 1}. Skill Points remaining: {skillPoints}");
            NotifyStateChanged();
            return true;
        }

        public int GetSkillLevel(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.Combat: return combatSkillLevel;
                case SkillCategory.Survival: return survivalSkillLevel;
                case SkillCategory.Scavenging: return scavengingSkillLevel;
                case SkillCategory.Crafting: return craftingSkillLevel;
                default: return 0;
            }
        }

        private void SetSkillLevel(SkillCategory category, int level)
        {
            switch (category)
            {
                case SkillCategory.Combat: combatSkillLevel = level; break;
                case SkillCategory.Survival: survivalSkillLevel = level; break;
                case SkillCategory.Scavenging: scavengingSkillLevel = level; break;
                case SkillCategory.Crafting: craftingSkillLevel = level; break;
            }
        }

        // ==========================================
        // PASSIVE EFFECT EXPORTERS
        // ==========================================

        /// <summary>
        /// Combat skill bonus: +5% damage per level (Up to +25% max).
        /// </summary>
        public float GetCombatDamageMultiplier()
        {
            return 1.0f + (combatSkillLevel * 0.05f);
        }

        /// <summary>
        /// Survival skill bonus: -5% hunger/thirst decay per level (Up to -25% max).
        /// </summary>
        public float GetSurvivalDecayMultiplier()
        {
            return Mathf.Max(0.5f, 1.0f - (survivalSkillLevel * 0.05f));
        }

        /// <summary>
        /// Scavenging skill bonus: +10% extra resource yield per level.
        /// </summary>
        public float GetScavengingLootBonus()
        {
            return 1.0f + (scavengingSkillLevel * 0.10f);
        }

        /// <summary>
        /// Crafting skill bonus: +5% crafting efficiency yield per level.
        /// </summary>
        public float GetCraftingYieldBonus()
        {
            return 1.0f + (craftingSkillLevel * 0.05f);
        }

        // ==========================================
        // PERSISTENCE DTO HOOKS
        // ==========================================

        public ProgressionSaveData GetProgressionSaveData()
        {
            return new ProgressionSaveData
            {
                currentLevel = currentLevel,
                currentXP = currentXP,
                totalXP = totalXP,
                skillPoints = skillPoints,
                combatSkillLevel = combatSkillLevel,
                survivalSkillLevel = survivalSkillLevel,
                scavengingSkillLevel = scavengingSkillLevel,
                craftingSkillLevel = craftingSkillLevel
            };
        }

        public void RestoreProgressionState(ProgressionSaveData saveData)
        {
            if (saveData == null) return;

            currentLevel = Mathf.Max(1, saveData.currentLevel);
            currentXP = Mathf.Max(0, saveData.currentXP);
            totalXP = Mathf.Max(0, saveData.totalXP);
            skillPoints = Mathf.Max(0, saveData.skillPoints);

            combatSkillLevel = Mathf.Clamp(saveData.combatSkillLevel, 0, 5);
            survivalSkillLevel = Mathf.Clamp(saveData.survivalSkillLevel, 0, 5);
            scavengingSkillLevel = Mathf.Clamp(saveData.scavengingSkillLevel, 0, 5);
            craftingSkillLevel = Mathf.Clamp(saveData.craftingSkillLevel, 0, 5);

            NotifyStateChanged();
            Debug.Log($"[PlayerProgressionSystem] Restored progression from save. Level {currentLevel}, XP {currentXP}, Skill Points {skillPoints}.");
        }

        private void NotifyStateChanged()
        {
            int required = GetRequiredXP(currentLevel);
            OnXPChanged?.Invoke(currentXP, required, totalXP);
            OnProgressionUpdated?.Invoke();
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
