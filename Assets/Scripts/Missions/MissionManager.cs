using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieApocalypse.Missions
{
    /// <summary>
    /// Tracks mission list, active mission progress, objective updates, and rewards.
    /// 
    /// ATTACH TO: Persistent Manager GameObject (e.g. "[MissionManager]").
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public event Action<MissionData> OnMissionStarted;
        public event Action<MissionData> OnMissionCompleted;

        [Header("Missions Pipeline")]
        [SerializeField] private List<MissionData> availableMissions = new List<MissionData>();
        private int currentMissionIndex = 0;

        public MissionData ActiveMission => (currentMissionIndex >= 0 && currentMissionIndex < availableMissions.Count) 
            ? availableMissions[currentMissionIndex] 
            : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartMission(int index)
        {
            if (index < 0 || index >= availableMissions.Count) return;

            currentMissionIndex = index;
            OnMissionStarted?.Invoke(ActiveMission);
            Debug.Log($"[MissionManager] Mission Started: {ActiveMission?.missionTitle}");
        }

        public void CompleteCurrentMission()
        {
            if (ActiveMission == null) return;

            Debug.Log($"[MissionManager] Mission Completed: {ActiveMission.missionTitle}");
            OnMissionCompleted?.Invoke(ActiveMission);
            currentMissionIndex++;
        }
    }
}
