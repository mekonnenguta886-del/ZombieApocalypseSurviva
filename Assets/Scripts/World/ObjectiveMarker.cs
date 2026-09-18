using TMPro;
using UnityEngine;
using ZombieApocalypse.Missions;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Simple floating world-space / screen-space indicator pointing towards active mission objective location.
    /// Shown/hidden dynamically as objectives update or complete.
    /// 
    /// ATTACH TO: Objective Marker GameObject (e.g. Canvas icon / world marker).
    /// </summary>
    public class ObjectiveMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        [SerializeField] private GameObject markerVisual;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 1.5f, 0f);

        private Camera mainCamera;
        private MissionManager missionManager;
        private Transform targetTransform;
        private Vector3 targetWorldPos;
        private bool hasPositionTarget = false;

        private void Start()
        {
            mainCamera = Camera.main;
            missionManager = MissionManager.Instance;

            if (missionManager != null)
            {
                missionManager.OnMissionStarted += HandleMissionUpdated;
                missionManager.OnObjectiveUpdated += HandleObjectiveUpdated;
                missionManager.OnObjectiveCompleted += HandleObjectiveUpdated;
                missionManager.OnMissionCompleted += HandleMissionUpdated;
            }

            UpdateTarget();
        }

        private void OnDestroy()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= HandleMissionUpdated;
                missionManager.OnObjectiveUpdated -= HandleObjectiveUpdated;
                missionManager.OnObjectiveCompleted -= HandleObjectiveUpdated;
                missionManager.OnMissionCompleted -= HandleMissionUpdated;
            }
        }

        private void HandleMissionUpdated(MissionData mission) => UpdateTarget();
        private void HandleObjectiveUpdated(MissionData mission, Objective obj) => UpdateTarget();

        private void UpdateTarget()
        {
            if (missionManager == null) missionManager = MissionManager.Instance;

            Objective activeObj = missionManager != null ? missionManager.CurrentActiveObjective : null;

            if (activeObj != null && !activeObj.IsCompleted)
            {
                if (activeObj.targetTransform != null)
                {
                    targetTransform = activeObj.targetTransform;
                    hasPositionTarget = true;
                }
                else if (activeObj.targetWorldPosition != Vector3.zero)
                {
                    targetWorldPos = activeObj.targetWorldPosition;
                    targetTransform = null;
                    hasPositionTarget = true;
                }
                else
                {
                    // Fallback search for LocationTrigger matching targetLocationId
                    if (!string.IsNullOrEmpty(activeObj.targetLocationId))
                    {
                        LocationTrigger[] triggers = FindObjectsOfType<LocationTrigger>();
                        foreach (var trg in triggers)
                        {
                            if (trg.LocationId.Equals(activeObj.targetLocationId, System.StringComparison.OrdinalIgnoreCase))
                            {
                                targetTransform = trg.transform;
                                hasPositionTarget = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        hasPositionTarget = false;
                    }
                }
            }
            else
            {
                hasPositionTarget = false;
            }

            if (markerVisual != null)
            {
                markerVisual.SetActive(hasPositionTarget);
            }
        }

        private void Update()
        {
            if (!hasPositionTarget)
            {
                if (markerVisual != null && markerVisual.activeSelf) markerVisual.SetActive(false);
                return;
            }

            Vector3 currentPos = targetTransform != null ? targetTransform.position : targetWorldPos;
            Vector3 worldPoint = currentPos + defaultOffset;

            // Position marker visual in world space
            transform.position = worldPoint;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Face camera
                transform.rotation = mainCamera.transform.rotation;

                // Update distance display
                if (distanceText != null)
                {
                    float dist = Vector3.Distance(mainCamera.transform.position, worldPoint);
                    distanceText.text = $"{Mathf.RoundToInt(dist)}m";
                }
            }
        }
    }
}
