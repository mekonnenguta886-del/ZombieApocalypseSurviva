using UnityEngine;
using ZombieApocalypse.Player;

namespace ZombieApocalypse.Environment
{
    /// <summary>
    /// Attached to 3D Trigger Colliders to identify environmental hazard zones (Toxic Areas, Radiation, Extreme Heat/Cold).
    /// Detects player entry/exit and registers active hazard conditions with EnvironmentalConditionManager.
    /// 
    /// ATTACH TO: Hazard Zone GameObject with BoxCollider (IsTrigger = true).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnvironmentalHazardZone : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] private string zoneId = "zone_toxic_sector";
        [SerializeField] private string zoneName = "Toxic Sector";
        [SerializeField] private EnvironmentalConditionData conditionData;

        public string ZoneId => zoneId;
        public string ZoneName => zoneName;
        public EnvironmentalConditionData ConditionData => conditionData;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null && health.IsDead) return;

                Debug.Log($"[EnvironmentalHazardZone] Player entered hazard zone: {zoneName} ({zoneId})");
                if (EnvironmentalConditionManager.Instance != null)
                {
                    EnvironmentalConditionManager.Instance.RegisterZone(this);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                Debug.Log($"[EnvironmentalHazardZone] Player exited hazard zone: {zoneName} ({zoneId})");
                if (EnvironmentalConditionManager.Instance != null)
                {
                    EnvironmentalConditionManager.Instance.UnregisterZone(this);
                }
            }
        }

        private void OnDisable()
        {
            if (EnvironmentalConditionManager.Instance != null)
            {
                EnvironmentalConditionManager.Instance.UnregisterZone(this);
            }
        }
    }
}
