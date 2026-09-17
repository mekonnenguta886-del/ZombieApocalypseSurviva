using UnityEngine;

namespace ZombieApocalypse.Missions
{
    /// <summary>
    /// ScriptableObject defining mission metadata, objectives, and completion criteria.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMissionData", menuName = "Zombie Apocalypse/Mission Data")]
    public class MissionData : ScriptableObject
    {
        [Header("Mission Info")]
        public string missionTitle = "Objective 1: Outbreak Origins";
        [TextArea(3, 5)]
        public string missionDescription = "Explore the abandoned city sector and secure the primary safehouse.";

        [Header("Requirements")]
        public int requiredKills = 10;
        public string targetAreaName = "City Center";
        public bool requiresKeycard = false;
    }
}
