using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieApocalypse.Save
{
    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data() { }

        public Vector3Data(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public class PlayerSaveData
    {
        public Vector3Data position = new Vector3Data();
        public Vector3Data rotation = new Vector3Data();
        public float health = 100f;
        public float hunger = 100f;
        public float thirst = 100f;
    }

    [Serializable]
    public class InventorySlotSaveData
    {
        public int slotIndex;
        public string itemId;
        public int quantity;
        public bool isEmpty;
    }

    [Serializable]
    public class WeaponSlotSaveData
    {
        public string weaponName;
        public int currentMagazineAmmo;
        public int reserveAmmo;
        public int damageLevel = 0;
        public int magazineLevel = 0;
        public int fireRateLevel = 0;
        public int recoilLevel = 0;
    }

    [Serializable]
    public class WeaponSaveData
    {
        public int currentSlotIndex;
        public List<WeaponSlotSaveData> slots = new List<WeaponSlotSaveData>();
    }

    [Serializable]
    public class ObjectiveSaveData
    {
        public string objectiveId;
        public int currentAmount;
        public string state; // "Incomplete" or "Complete"
    }

    [Serializable]
    public class MissionSaveData
    {
        public string activeMissionId;
        public bool isMissionComplete;
        public bool rewardsGranted;
        public List<ObjectiveSaveData> objectives = new List<ObjectiveSaveData>();
    }

    [Serializable]
    public class DoorSaveData
    {
        public string doorId;
        public bool isOpen;
        public bool isLocked;
    }

    [Serializable]
    public class LootContainerSaveData
    {
        public string containerId;
        public bool hasBeenOpened;
        public List<InventorySlotSaveData> remainingContents = new List<InventorySlotSaveData>();
    }

    /// <summary>
    /// Root serializable DTO for JSON save file persistence.
    /// Does not store any UnityEngine objects or runtime references directly.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;
        public string saveTimestamp;
        public string sceneName = "TestArena";
        public PlayerSaveData player = new PlayerSaveData();
        public List<InventorySlotSaveData> inventory = new List<InventorySlotSaveData>();
        public WeaponSaveData weapons = new WeaponSaveData();
        public MissionSaveData mission = new MissionSaveData();
        public List<DoorSaveData> doors = new List<DoorSaveData>();
        public List<LootContainerSaveData> lootContainers = new List<LootContainerSaveData>();
    }
}
