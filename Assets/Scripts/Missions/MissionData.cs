using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Missions
{
    /// <summary>
    /// ScriptableObject defining mission metadata, objective pipeline, and reward distribution.
    /// Configurable directly inside the Unity Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMissionData", menuName = "Zombie Apocalypse/Mission Data")]
    public class MissionData : ScriptableObject
    {
        [Header("Mission Identity")]
        public string missionId = "mission_med_run";
        public string missionTitle = "Medical Supply Run";
        [TextArea(3, 5)]
        public string missionDescription = "Explore the sector, reach the Medical Building, collect a Medkit, and return safely to the Safe House.";

        [Header("Mission Objectives")]
        public List<Objective> objectives = new List<Objective>();

        [Header("Mission Rewards")]
        public List<InventorySlot> rewardItems = new List<InventorySlot>();
        public AmmoType rewardAmmoType = AmmoType.Pistol;
        public int rewardAmmoAmount = 30;
        public int experienceReward = 100;
    }
}
