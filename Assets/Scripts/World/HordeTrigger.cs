using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieApocalypse.Player;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Lifecycle-safe world trigger component for Phase 9 horde encounters.
    /// Triggers capped zombie encounter waves via the single authoritative ZombieSpawner.
    /// Enforces a global active encounter guard, cooldown timer, and SafeHouse protection.
    /// 
    /// ATTACH TO: Horde Encounter Trigger GameObject with Collider (isTrigger = true).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HordeTrigger : MonoBehaviour
    {
        private static bool isHordeEncounterActive = false;

        [Header("Horde Encounter Config")]
        [SerializeField] private int waveSize = 5;
        [SerializeField] private float cooldownTime = 60.0f;

        private float lastTriggerTime = -999f;
        private bool isPlayerInsideThisTrigger = false;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleSceneChanged;
        }

        private static void HandleSceneChanged(Scene current, Scene next)
        {
            isHordeEncounterActive = false;
        }

        public static void ResetGlobalEncounterGuard()
        {
            isHordeEncounterActive = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isHordeEncounterActive) return;
            if (Time.time < lastTriggerTime + cooldownTime) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null && !isPlayerInsideThisTrigger)
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null && health.IsDead) return;
                if (SafeZoneTrigger.IsPlayerInSafeZone) return;

                isPlayerInsideThisTrigger = true;
                isHordeEncounterActive = true;
                lastTriggerTime = Time.time;

                ZombieSpawner spawner = FindObjectOfType<ZombieSpawner>();
                if (spawner != null)
                {
                    spawner.TriggerEncounterWave(waveSize);
                }

                Debug.Log($"[HordeTrigger] Activated Horde Encounter on '{gameObject.name}' with wave size {waveSize}.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                isPlayerInsideThisTrigger = false;
                // Allow new encounter trigger after cooldown
                Invoke(nameof(ResetEncounterGuardDelayed), 15.0f);
            }
        }

        private void ResetEncounterGuardDelayed()
        {
            isHordeEncounterActive = false;
        }
    }
}
