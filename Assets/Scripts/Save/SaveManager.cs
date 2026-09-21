using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Missions;
using ZombieApocalypse.Player;
using ZombieApocalypse.UI;
using ZombieApocalypse.Weapons;
using ZombieApocalypse.World;

namespace ZombieApocalypse.Save
{
    /// <summary>
    /// Central manager for saving, loading, new game initialization, and state restoration.
    /// Manages atomic JSON save persistence, player transform load sequence, HUD notifications,
    /// and death state safety guards without overriding Phase 1-6 systems.
    /// 
    /// ATTACH TO: [SaveManager] GameObject in scene.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public event Action OnSaveStarted;
        public event Action OnSaveCompleted;
        public event Action<string> OnSaveFailed;

        public event Action OnLoadStarted;
        public event Action OnLoadCompleted;
        public event Action<string> OnLoadFailed;

        private PlayerInputHandler inputHandler;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ItemRegistry.Initialize();
        }

        private void Update()
        {
            if (inputHandler == null)
            {
                inputHandler = PlayerInputHandler.Instance;
            }

            if (inputHandler != null)
            {
                if (inputHandler.SaveTriggered)
                {
                    SaveGame();
                    inputHandler.ResetSaveTrigger();
                }
                else if (inputHandler.LoadTriggered)
                {
                    LoadGame();
                    inputHandler.ResetLoadTrigger();
                }
            }
        }

        /// <summary>
        /// Compiles current gameplay state and writes atomically to savegame.json.
        /// </summary>
        public bool SaveGame()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                NotifyFailure("Save failed: Player not found.", false);
                return false;
            }

            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead)
            {
                NotifyFailure("Cannot save while player is dead.", false);
                return false;
            }

            OnSaveStarted?.Invoke();
            Debug.Log("[SaveManager] Starting game save sequence...");

            SaveData data = new SaveData();
            data.sceneName = SceneManager.GetActiveScene().name;

            // 1. Save Player Transform & Health & Survival
            data.player.position = new Vector3Data(playerObj.transform.position);
            data.player.rotation = new Vector3Data(playerObj.transform.eulerAngles);
            if (health != null) data.player.health = health.CurrentHealth;

            PlayerSurvivalStats survival = playerObj.GetComponent<PlayerSurvivalStats>();
            if (survival != null)
            {
                data.player.hunger = survival.CurrentHunger;
                data.player.thirst = survival.CurrentThirst;
            }

            // 2. Save Inventory State
            InventorySystem inv = playerObj.GetComponent<InventorySystem>();
            if (inv != null)
            {
                data.inventory = inv.GetInventorySaveData();
            }

            // 3. Save Weapon State
            WeaponController weapons = playerObj.GetComponent<WeaponController>();
            if (weapons != null)
            {
                data.weapons = weapons.GetWeaponSaveData();
            }

            // 4. Save Mission State
            MissionManager missionMgr = MissionManager.Instance;
            if (missionMgr != null)
            {
                data.mission = missionMgr.GetMissionSaveData();
            }

            // 5. Save Doors State
            DoorController[] doors = FindObjectsOfType<DoorController>();
            foreach (var door in doors)
            {
                if (door == null) continue;
                data.doors.Add(new DoorSaveData
                {
                    doorId = door.DoorId,
                    isOpen = door.IsOpen,
                    isLocked = door.IsLocked
                });
            }

            // 6. Save Loot Containers State
            LootContainer[] containers = FindObjectsOfType<LootContainer>();
            foreach (var container in containers)
            {
                if (container == null) continue;
                data.lootContainers.Add(new LootContainerSaveData
                {
                    containerId = container.ContainerId,
                    hasBeenOpened = container.HasBeenOpened,
                    remainingContents = container.GetRemainingContentsSaveData()
                });
            }

            // 7. Save Progression State
            if (ZombieApocalypse.Progression.PlayerProgressionSystem.Instance != null)
            {
                data.progression = ZombieApocalypse.Progression.PlayerProgressionSystem.Instance.GetProgressionSaveData();
            }

            // 8. Write Save Data Atomically
            bool success = SaveFileUtility.Save(data);
            if (success)
            {
                OnSaveCompleted?.Invoke();
                ShowHUDToast("Game Saved");
                Debug.Log("[SaveManager] Save sequence completed successfully.");
            }
            else
            {
                NotifyFailure("Save operation failed.", false);
            }

            return success;
        }

        /// <summary>
        /// Reads savegame.json and restores player transform, health, survival, inventory, weapons, mission, doors, and loot.
        /// </summary>
        public bool LoadGame()
        {
            if (!SaveFileUtility.HasSave())
            {
                NotifyFailure("No save game found.", true);
                return false;
            }

            OnLoadStarted?.Invoke();
            Debug.Log("[SaveManager] Starting game load sequence...");

            SaveData data = SaveFileUtility.Load(out string errorMessage);
            if (data == null)
            {
                NotifyFailure(string.IsNullOrEmpty(errorMessage) ? "Unable to load save data." : errorMessage, true);
                return false;
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                NotifyFailure("Load failed: Player not found.", true);
                return false;
            }

            // Ensure ItemRegistry is ready
            ItemRegistry.Initialize();

            // 1. Position & Transform Load Order Safety: Temporarily disable CharacterController
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (data.player != null && data.player.position != null)
            {
                playerObj.transform.position = data.player.position.ToVector3();
                playerObj.transform.eulerAngles = data.player.rotation.ToVector3();
            }

            if (cc != null) cc.enabled = true;

            // 2. Restore Health & Survival Stats
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            if (health != null && data.player != null)
            {
                health.RestoreHealth(data.player.health);
            }

            PlayerSurvivalStats survival = playerObj.GetComponent<PlayerSurvivalStats>();
            if (survival != null && data.player != null)
            {
                survival.RestoreStats(data.player.hunger, data.player.thirst);
            }

            // 3. Restore Inventory State
            InventorySystem inv = playerObj.GetComponent<InventorySystem>();
            if (inv != null && data.inventory != null)
            {
                inv.RestoreInventory(data.inventory);
            }

            // 4. Restore Weapon State
            WeaponController weapons = playerObj.GetComponent<WeaponController>();
            if (weapons != null && data.weapons != null)
            {
                weapons.RestoreWeaponState(data.weapons.currentSlotIndex, data.weapons.slots);
            }

            // 5. Restore Mission State
            MissionManager missionMgr = MissionManager.Instance;
            if (missionMgr != null && data.mission != null)
            {
                missionMgr.RestoreMissionState(data.mission.activeMissionId, data.mission.isMissionComplete, data.mission.rewardsGranted, data.mission.objectives);
            }

            // 6. Restore Door States
            if (data.doors != null)
            {
                DoorController[] doors = FindObjectsOfType<DoorController>();
                foreach (var doorSave in data.doors)
                {
                    if (doorSave == null || string.IsNullOrEmpty(doorSave.doorId)) continue;
                    DoorController targetDoor = Array.Find(doors, d => d != null && d.DoorId.Equals(doorSave.doorId, StringComparison.OrdinalIgnoreCase));
                    if (targetDoor != null)
                    {
                        targetDoor.RestoreDoorState(doorSave.isOpen, doorSave.isLocked);
                    }
                }
            }

            // 7. Restore Loot Container States
            if (data.lootContainers != null)
            {
                LootContainer[] containers = FindObjectsOfType<LootContainer>();
                foreach (var containerSave in data.lootContainers)
                {
                    if (containerSave == null || string.IsNullOrEmpty(containerSave.containerId)) continue;
                    LootContainer targetContainer = Array.Find(containers, c => c != null && c.ContainerId.Equals(containerSave.containerId, StringComparison.OrdinalIgnoreCase));
                    if (targetContainer != null)
                    {
                        targetContainer.RestoreContainerState(containerSave.hasBeenOpened, containerSave.remainingContents);
                    }
                }
            }

            // 8. Restore Progression State
            if (ZombieApocalypse.Progression.PlayerProgressionSystem.Instance != null && data.progression != null)
            {
                ZombieApocalypse.Progression.PlayerProgressionSystem.Instance.RestoreProgressionState(data.progression);
            }

            // 9. Refresh UI
            MissionUI missionUI = FindObjectOfType<MissionUI>();
            if (missionUI != null) missionUI.RefreshUI();

            OnLoadCompleted?.Invoke();
            ShowHUDToast("Game Loaded");
            Debug.Log("[SaveManager] Load sequence completed successfully.");
            return true;
        }

        /// <summary>
        /// Checks if a valid save game exists on disk.
        /// </summary>
        public bool HasSave() => SaveFileUtility.HasSave();

        /// <summary>
        /// Deletes local save files.
        /// </summary>
        public bool DeleteSave() => SaveFileUtility.DeleteSave();

        /// <summary>
        /// Starts a fresh game by reloading the active scene without restoring save data.
        /// </summary>
        public void NewGame()
        {
            Debug.Log("[SaveManager] Starting New Game (reloading fresh scene)...");
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void NotifyFailure(string message, bool isLoad)
        {
            Debug.LogWarning($"[SaveManager] {(isLoad ? "Load" : "Save")} Error: {message}");
            if (isLoad) OnLoadFailed?.Invoke(message);
            else OnSaveFailed?.Invoke(message);

            ShowHUDToast(message);
        }

        private void ShowHUDToast(string message)
        {
            HUDController hud = FindObjectOfType<HUDController>();
            if (hud != null)
            {
                hud.ShowNotificationToast(message);
            }
        }
    }
}
