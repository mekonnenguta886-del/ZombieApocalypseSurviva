using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;
using ZombieApocalypse.Weapons;
using ZombieApocalypse.World;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.Missions
{
    /// <summary>
    /// Central manager for tracking missions, objective completion criteria, event subscriptions,
    /// and reward distribution. Operates entirely through events without direct UI manipulation.
    /// 
    /// ATTACH TO: [MissionManager] GameObject in scene.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public event Action<MissionData> OnMissionStarted;
        public event Action<MissionData, Objective> OnObjectiveUpdated;
        public event Action<MissionData, Objective> OnObjectiveCompleted;
        public event Action<MissionData> OnMissionCompleted;

        [Header("Missions Pipeline")]
        [SerializeField] private List<MissionData> availableMissions = new List<MissionData>();
        [SerializeField] private int activeMissionIndex = 0;
        [SerializeField] private bool autoStartFirstMission = true;

        // Runtime Mission State
        private MissionData activeMissionData;
        private List<Objective> runtimeObjectives = new List<Objective>();
        private bool rewardsGranted = false;
        private bool isMissionComplete = false;

        // Player References
        private InventorySystem playerInventory;
        private WeaponController playerWeaponController;
        private PlayerHealth playerHealth;

        public MissionData ActiveMission => activeMissionData;
        public IReadOnlyList<Objective> ActiveObjectives => runtimeObjectives;
        public Objective CurrentActiveObjective
        {
            get
            {
                foreach (var obj in runtimeObjectives)
                {
                    if (obj != null && !obj.IsCompleted) return obj;
                }
                return null;
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

        private float hordeTimer;

        private void Start()
        {
            BindPlayerReferences();

            if (autoStartFirstMission && availableMissions.Count > 0)
            {
                StartMission(activeMissionIndex);
            }
        }

        private void Update()
        {
            if (isMissionComplete || activeMissionData == null) return;
            if (playerHealth != null && playerHealth.IsDead) return;
            if (SafeZoneTrigger.IsPlayerInSafeZone) return;

            // Handle SurviveHorde duration objective progress
            hordeTimer += Time.deltaTime;
            if (hordeTimer >= 1.0f)
            {
                hordeTimer = 0f;
                foreach (var obj in runtimeObjectives)
                {
                    if (obj == null || obj.IsCompleted) continue;
                    if (obj.type == ObjectiveType.SurviveHorde)
                    {
                        obj.currentAmount = Mathf.Min(obj.currentAmount + 1, obj.requiredAmount);
                        OnObjectiveUpdated?.Invoke(activeMissionData, obj);

                        if (obj.currentAmount >= obj.requiredAmount)
                        {
                            CompleteObjective(obj);
                        }
                        break;
                    }
                }
            }
        }

        private void OnEnable()
        {
            LocationTrigger.OnLocationEntered += HandleLocationEntered;
            ZombieHealth.OnZombieKilled += HandleZombieKilled;
        }

        private void OnDisable()
        {
            LocationTrigger.OnLocationEntered -= HandleLocationEntered;
            ZombieHealth.OnZombieKilled -= HandleZombieKilled;

            if (playerInventory != null)
            {
                playerInventory.OnItemAdded -= HandleItemAdded;
            }
        }

        public void BindPlayerReferences()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                playerWeaponController = playerObj.GetComponent<WeaponController>();

                var inv = playerObj.GetComponent<InventorySystem>();
                if (inv != playerInventory)
                {
                    if (playerInventory != null) playerInventory.OnItemAdded -= HandleItemAdded;
                    playerInventory = inv;
                    if (playerInventory != null) playerInventory.OnItemAdded += HandleItemAdded;
                }
            }
        }

        public void StartMission(int index)
        {
            if (index < 0 || index >= availableMissions.Count) return;

            activeMissionIndex = index;
            activeMissionData = availableMissions[index];
            rewardsGranted = false;
            isMissionComplete = false;

            // Clone objectives for runtime tracking
            runtimeObjectives.Clear();
            if (activeMissionData != null && activeMissionData.objectives != null)
            {
                foreach (var obj in activeMissionData.objectives)
                {
                    if (obj != null)
                    {
                        runtimeObjectives.Add(obj.Clone());
                    }
                }
            }

            Debug.Log($"[MissionManager] Mission Started: {activeMissionData?.missionTitle}");
            OnMissionStarted?.Invoke(activeMissionData);
        }

        private void HandleLocationEntered(string locationId, string locationName)
        {
            if (isMissionComplete || activeMissionData == null) return;
            if (playerHealth != null && playerHealth.IsDead) return;

            foreach (var obj in runtimeObjectives)
            {
                if (obj == null || obj.IsCompleted) continue;

                if ((obj.type == ObjectiveType.ReachLocation || obj.type == ObjectiveType.Extraction) 
                    && obj.targetLocationId.Equals(locationId, StringComparison.OrdinalIgnoreCase))
                {
                    CompleteObjective(obj);
                    break;
                }
            }
        }

        private void HandleZombieKilled(ZombieHealth zombie)
        {
            if (isMissionComplete || activeMissionData == null) return;
            if (playerHealth != null && playerHealth.IsDead) return;

            foreach (var obj in runtimeObjectives)
            {
                if (obj == null || obj.IsCompleted) continue;

                if (obj.type == ObjectiveType.KillZombies)
                {
                    obj.currentAmount = Mathf.Min(obj.currentAmount + 1, obj.requiredAmount);
                    OnObjectiveUpdated?.Invoke(activeMissionData, obj);

                    if (obj.currentAmount >= obj.requiredAmount)
                    {
                        CompleteObjective(obj);
                    }
                    break;
                }
            }
        }

        private void HandleItemAdded(ItemData item, int quantity)
        {
            if (isMissionComplete || activeMissionData == null || item == null) return;
            if (playerHealth != null && playerHealth.IsDead) return;

            foreach (var obj in runtimeObjectives)
            {
                if (obj == null || obj.IsCompleted) continue;

                if (obj.type == ObjectiveType.CollectItem && obj.targetItem == item)
                {
                    obj.currentAmount = Mathf.Min(obj.currentAmount + quantity, obj.requiredAmount);
                    OnObjectiveUpdated?.Invoke(activeMissionData, obj);

                    if (obj.currentAmount >= obj.requiredAmount)
                    {
                        CompleteObjective(obj);
                    }
                    break;
                }
            }
        }

        public void NotifyInteractableTriggered(string interactableId)
        {
            if (isMissionComplete || activeMissionData == null || string.IsNullOrEmpty(interactableId)) return;
            if (playerHealth != null && playerHealth.IsDead) return;

            foreach (var obj in runtimeObjectives)
            {
                if (obj == null || obj.IsCompleted) continue;

                if (obj.type == ObjectiveType.Interact && obj.targetInteractableId.Equals(interactableId, StringComparison.OrdinalIgnoreCase))
                {
                    CompleteObjective(obj);
                    break;
                }
            }
        }

        private void CompleteObjective(Objective obj)
        {
            if (obj == null || obj.state == ObjectiveState.Complete) return;

            obj.state = ObjectiveState.Complete;
            Debug.Log($"[MissionManager] Objective Completed: {obj.description}");
            OnObjectiveCompleted?.Invoke(activeMissionData, obj);

            CheckMissionCompletion();
        }

        private void CheckMissionCompletion()
        {
            if (isMissionComplete) return;

            foreach (var obj in runtimeObjectives)
            {
                if (obj != null && !obj.IsCompleted) return; // Still has incomplete objectives
            }

            CompleteCurrentMission();
        }

        public void CompleteCurrentMission()
        {
            if (isMissionComplete || activeMissionData == null) return;
            isMissionComplete = true;

            Debug.Log($"[MissionManager] Mission Fully Completed: {activeMissionData.missionTitle}");
            GrantRewards();
            OnMissionCompleted?.Invoke(activeMissionData);
        }

        private void GrantRewards()
        {
            // Duplicate reward protection
            if (rewardsGranted || activeMissionData == null) return;
            rewardsGranted = true;

            if (playerInventory == null) BindPlayerReferences();

            // Grant Item Rewards
            if (activeMissionData.rewardItems != null)
            {
                foreach (var reward in activeMissionData.rewardItems)
                {
                    if (reward == null || reward.itemData == null || reward.quantity <= 0) continue;

                    int added = playerInventory != null ? playerInventory.AddItem(reward.itemData, reward.quantity) : 0;
                    int uncollected = reward.quantity - added;

                    if (uncollected > 0)
                    {
                        // Spawn world pickup near player if inventory is full
                        SpawnRewardPickupInWorld(reward.itemData, uncollected);
                    }
                }
            }

            // Grant Ammo Rewards
            if (activeMissionData.rewardAmmoAmount > 0 && playerWeaponController != null)
            {
                playerWeaponController.AddReserveAmmo(activeMissionData.rewardAmmoType, activeMissionData.rewardAmmoAmount);
            }

            Debug.Log($"[MissionManager] Mission rewards granted successfully for: {activeMissionData.missionTitle}");
        }

        private void SpawnRewardPickupInWorld(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return;

            Vector3 spawnPos = Vector3.zero;
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                spawnPos = playerObj.transform.position + playerObj.transform.forward * 1.5f + Vector3.up * 0.5f;
            }

            if (item.dropPrefab != null)
            {
                GameObject droppedObj = Instantiate(item.dropPrefab, spawnPos, Quaternion.identity);
                ItemPickup pickup = droppedObj.GetComponent<ItemPickup>();
                if (pickup != null) pickup.Setup(item, quantity);
            }
            else
            {
                // Fallback primitive pickup if dropPrefab is null
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = $"Reward_{item.itemName}";
                sphere.transform.position = spawnPos;
                sphere.transform.localScale = Vector3.one * 0.4f;

                ItemPickup pickup = sphere.AddComponent<ItemPickup>();
                pickup.Setup(item, quantity);
            }

            Debug.LogWarning($"[MissionManager] Inventory full! Spawned overflow reward pickup: {item.itemName} x{quantity} in world.");
        }

        /// <summary>
        /// Compiles active mission and runtime objective states into serializable MissionSaveData DTO.
        /// </summary>
        public ZombieApocalypse.Save.MissionSaveData GetMissionSaveData()
        {
            var saveData = new ZombieApocalypse.Save.MissionSaveData();
            saveData.activeMissionId = activeMissionData != null ? activeMissionData.missionId : "";
            saveData.isMissionComplete = isMissionComplete;
            saveData.rewardsGranted = rewardsGranted;

            foreach (var obj in runtimeObjectives)
            {
                if (obj != null)
                {
                    saveData.objectives.Add(new ZombieApocalypse.Save.ObjectiveSaveData
                    {
                        objectiveId = obj.objectiveId,
                        currentAmount = obj.currentAmount,
                        state = obj.state.ToString()
                    });
                }
            }

            return saveData;
        }

        /// <summary>
        /// Restores mission progress, objective counts, completion state, and reward-granted status from save data.
        /// Does NOT re-grant rewards or re-trigger objective completion events.
        /// </summary>
        public void RestoreMissionState(string missionId, bool isComplete, bool grantedRewards, List<ZombieApocalypse.Save.ObjectiveSaveData> savedObjectives)
        {
            if (activeMissionData == null || !activeMissionData.missionId.Equals(missionId, StringComparison.OrdinalIgnoreCase))
            {
                int index = availableMissions.FindIndex(m => m != null && m.missionId.Equals(missionId, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    StartMission(index);
                }
            }

            isMissionComplete = isComplete;
            rewardsGranted = grantedRewards;

            if (savedObjectives != null)
            {
                foreach (var savedObj in savedObjectives)
                {
                    if (savedObj == null || string.IsNullOrEmpty(savedObj.objectiveId)) continue;

                    var runtimeObj = runtimeObjectives.Find(o => o != null && o.objectiveId.Equals(savedObj.objectiveId, StringComparison.OrdinalIgnoreCase));
                    if (runtimeObj != null)
                    {
                        runtimeObj.currentAmount = savedObj.currentAmount;
                        if (Enum.TryParse(savedObj.state, out ObjectiveState parsedState))
                        {
                            runtimeObj.state = parsedState;
                        }
                    }
                }
            }

            OnMissionStarted?.Invoke(activeMissionData);
            if (CurrentActiveObjective != null)
            {
                OnObjectiveUpdated?.Invoke(activeMissionData, CurrentActiveObjective);
            }

            Debug.Log($"[MissionManager] Restored mission '{missionId}'. Complete: {isMissionComplete}, RewardsGranted: {rewardsGranted}");
        }
    }
}
