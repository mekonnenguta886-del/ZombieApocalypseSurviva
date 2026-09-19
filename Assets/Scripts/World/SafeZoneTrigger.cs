using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Player;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Trigger component placed on safe houses (e.g. SafeHouse building).
    /// Tracks player entry/exit to grant full protection from zombie aggression and block zombie spawning.
    ///
    /// ATTACH TO: SafeHouse GameObject with Collider (isTrigger = true).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SafeZoneTrigger : MonoBehaviour
    {
        private static bool isPlayerInSafeZone = false;
        private static List<Collider> safeZoneColliders = new List<Collider>();

        public static bool IsPlayerInSafeZone => isPlayerInSafeZone;

        private void OnEnable()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && !safeZoneColliders.Contains(col))
            {
                safeZoneColliders.Add(col);
            }
        }

        private void OnDisable()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && safeZoneColliders.Contains(col))
            {
                safeZoneColliders.Remove(col);
            }
        }

        /// <summary>
        /// Checks if a world position is inside any active safe zone trigger bounds.
        /// </summary>
        public static bool IsPositionInSafeZone(Vector3 position)
        {
            for (int i = safeZoneColliders.Count - 1; i >= 0; i--)
            {
                Collider col = safeZoneColliders[i];
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                {
                    safeZoneColliders.RemoveAt(i);
                    continue;
                }

                if (col.bounds.Contains(position))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                isPlayerInSafeZone = true;
                Debug.Log("[SafeZoneTrigger] Player entered Safe Zone! Zombie aggression paused.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                isPlayerInSafeZone = false;
                Debug.Log("[SafeZoneTrigger] Player exited Safe Zone.");
            }
        }
    }
}
