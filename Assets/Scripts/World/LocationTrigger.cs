using System;
using UnityEngine;
using ZombieApocalypse.Player;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Attached to 3D Trigger Colliders to identify world zones (SafeHouse, MedicalBuilding, ZombieZone, ExtractionZone).
    /// Notifies MissionManager and UI when player enters without per-frame distance polling.
    /// 
    /// ATTACH TO: Location Trigger GameObject with BoxCollider (IsTrigger = true).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LocationTrigger : MonoBehaviour
    {
        public static event Action<string, string> OnLocationEntered; // locationId, locationName

        [Header("Location Configuration")]
        [SerializeField] private string locationId = "loc_med_building";
        [SerializeField] private string locationName = "Medical Building";
        [SerializeField] private bool triggerOnceOnly = false;

        private bool hasTriggered = false;

        public string LocationId => locationId;
        public string LocationName => locationName;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnceOnly && hasTriggered) return;

            // Verify player entry
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null && health.IsDead) return;

                hasTriggered = true;
                Debug.Log($"[LocationTrigger] Player entered location: {locationName} ({locationId})");
                OnLocationEntered?.Invoke(locationId, locationName);
            }
        }
    }
}
