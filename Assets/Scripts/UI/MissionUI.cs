using TMPro;
using UnityEngine;
using ZombieApocalypse.Missions;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Displays current mission title, description, and objective checklist on HUD.
    /// Subscribes cleanly to MissionManager events without modifying existing Phase 1-5 UI panels.
    /// 
    /// ATTACH TO: Mission Panel GameObject inside Gameplay HUD Canvas.
    /// </summary>
    public class MissionUI : MonoBehaviour
    {
        [Header("UI Text References")]
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI missionDescriptionText;
        [SerializeField] private TextMeshProUGUI objectivesListText;
        [SerializeField] private GameObject missionCompleteBanner;

        private MissionManager missionManager;

        private void Start()
        {
            missionManager = MissionManager.Instance;

            if (missionCompleteBanner != null)
            {
                missionCompleteBanner.SetActive(false);
            }

            if (missionManager != null)
            {
                missionManager.OnMissionStarted += HandleMissionStarted;
                missionManager.OnObjectiveUpdated += HandleObjectiveUpdated;
                missionManager.OnObjectiveCompleted += HandleObjectiveCompleted;
                missionManager.OnMissionCompleted += HandleMissionCompleted;

                if (missionManager.ActiveMission != null)
                {
                    HandleMissionStarted(missionManager.ActiveMission);
                }
            }
        }

        private void OnDestroy()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= HandleMissionStarted;
                missionManager.OnObjectiveUpdated -= HandleObjectiveUpdated;
                missionManager.OnObjectiveCompleted -= HandleObjectiveCompleted;
                missionManager.OnMissionCompleted -= HandleMissionCompleted;
            }
        }

        private void HandleMissionStarted(MissionData mission)
        {
            if (missionCompleteBanner != null) missionCompleteBanner.SetActive(false);
            RefreshUI();
        }

        private void HandleObjectiveUpdated(MissionData mission, Objective objective)
        {
            RefreshUI();
        }

        private void HandleObjectiveCompleted(MissionData mission, Objective objective)
        {
            RefreshUI();
        }

        private void HandleMissionCompleted(MissionData mission)
        {
            RefreshUI();
            if (missionCompleteBanner != null)
            {
                missionCompleteBanner.SetActive(true);
                Invoke(nameof(HideBanner), 5.0f);
            }
        }

        private void HideBanner()
        {
            if (missionCompleteBanner != null)
            {
                missionCompleteBanner.SetActive(false);
            }
        }

        public void RefreshUI()
        {
            if (missionManager == null) missionManager = MissionManager.Instance;
            if (missionManager == null || missionManager.ActiveMission == null) return;

            MissionData mission = missionManager.ActiveMission;

            if (missionTitleText != null)
            {
                missionTitleText.text = $"CURRENT MISSION\n{mission.missionTitle.ToUpper()}";
            }

            if (missionDescriptionText != null)
            {
                missionDescriptionText.text = mission.missionDescription;
            }

            if (objectivesListText != null)
            {
                string checklist = "";
                var objectives = missionManager.ActiveObjectives;

                if (objectives != null && objectives.Count > 0)
                {
                    foreach (var obj in objectives)
                    {
                        if (obj == null) continue;

                        string checkmark = obj.IsCompleted ? "☑" : "☐";
                        string colorTag = obj.IsCompleted ? "<color=#4CAF50>" : "<color=#FFFFFF>";
                        checklist += $"{colorTag}{checkmark} {obj.GetProgressString()}</color>\n";
                    }
                }

                objectivesListText.text = checklist;
            }
        }
    }
}
