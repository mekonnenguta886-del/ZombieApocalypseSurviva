using UnityEngine;
using ZombieApocalypse.Player;
using ZombieApocalypse.World;

namespace ZombieApocalypse.WorldEvents
{
    /// <summary>
    /// World trigger component placed at encounter locations (e.g. Abandoned House, Supply Warehouse, Zombie Zone).
    /// Detects player entry and requests WorldEventManager to initiate the assigned WorldEventData.
    /// Manages location-specific cooldown timers to prevent immediate reactivation.
    /// 
    /// ATTACH TO: World Encounter Trigger GameObject with Collider (isTrigger = true).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldEventTrigger : MonoBehaviour
    {
        [Header("Event Configuration")]
        [SerializeField] private WorldEventData eventData;
        [SerializeField] private bool autoCreateDefaultData = true;

        private float cooldownTimer = 0f;
        private bool isPlayerInside = false;

        public WorldEventData EventData => eventData;
        public bool IsOnCooldown => cooldownTimer > 0f;
        public float CooldownTimer => cooldownTimer;

        private void Start()
        {
            EnsureDefaultEventData();
        }

        private void EnsureDefaultEventData()
        {
            if (eventData == null && autoCreateDefaultData)
            {
                eventData = ScriptableObject.CreateInstance<WorldEventData>();
                eventData.eventId = $"event_{gameObject.name.ToLower().Replace(" ", "_")}";
                eventData.displayName = $"Outbreak at {gameObject.name}";
                eventData.description = "Survive incoming zombie waves in this sector.";
                eventData.eventType = WorldEventType.WaveHorde;
                eventData.completionMode = WorldEventCompletionMode.ClearAllWaves;
                eventData.safeZoneRule = WorldEventSafeZoneRule.Cancel;
                eventData.waveCount = 3;
                eventData.zombiesPerWave = 4;
                eventData.waveDelay = 6.0f;
                eventData.cooldownTime = 60.0f;
                eventData.activationRadius = 25.0f;
                eventData.abandonRadius = 60.0f;
                eventData.rewardAmmoAmount = 30;
            }
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    cooldownTimer = 0f;
                    Debug.Log($"[WorldEventTrigger] Cooldown expired for '{gameObject.name}'. Ready for reactivation.");
                }
            }
        }

        public void StartCooldown(float duration)
        {
            cooldownTimer = Mathf.Max(cooldownTimer, duration);
            Debug.Log($"[WorldEventTrigger] Cooldown started on '{gameObject.name}' for {cooldownTimer:F1}s.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (cooldownTimer > 0f) return;
            if (WorldEventManager.Instance != null && WorldEventManager.Instance.CurrentState != WorldEventState.Inactive) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null && !isPlayerInside)
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null && health.IsDead) return;
                if (SafeZoneTrigger.IsPlayerInSafeZone) return;

                isPlayerInside = true;

                if (WorldEventManager.Instance != null && eventData != null)
                {
                    WorldEventManager.Instance.StartEvent(eventData, transform.position, this);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                isPlayerInside = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            float rad = eventData != null ? eventData.activationRadius : 25.0f;
            Gizmos.DrawWireSphere(transform.position, rad);

            Gizmos.color = Color.magenta;
            float abRad = eventData != null ? eventData.abandonRadius : 60.0f;
            Gizmos.DrawWireSphere(transform.position, abRad);
        }
    }
}
