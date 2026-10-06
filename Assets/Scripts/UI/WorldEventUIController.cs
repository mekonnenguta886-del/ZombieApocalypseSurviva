using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.WorldEvents;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// UI overlay controller for Phase 14 World Events HUD overlay.
    /// Subscribes to WorldEventManager events to display active event title, objective checklist,
    /// wave counts, countdown timers, and completion/failure notifications.
    /// 
    /// ATTACH TO: Gameplay Canvas WorldEvent UI root GameObject.
    /// </summary>
    public class WorldEventUIController : MonoBehaviour
    {
        public static WorldEventUIController Instance { get; private set; }

        [Header("Event Panel")]
        [SerializeField] private GameObject eventPanel;
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventDescText;
        [SerializeField] private TextMeshProUGUI eventProgressText;
        [SerializeField] private TextMeshProUGUI eventTimerText;
        [SerializeField] private TextMeshProUGUI rewardPreviewText;

        private WorldEventData activeEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            WorldEventManager.OnWorldEventStarted += HandleEventStarted;
            WorldEventManager.OnWorldEventWaveChanged += HandleWaveChanged;
            WorldEventManager.OnWorldEventCompleted += HandleEventCompleted;
            WorldEventManager.OnWorldEventFailed += HandleEventFailed;
        }

        private void OnDisable()
        {
            WorldEventManager.OnWorldEventStarted -= HandleEventStarted;
            WorldEventManager.OnWorldEventWaveChanged -= HandleWaveChanged;
            WorldEventManager.OnWorldEventCompleted -= HandleEventCompleted;
            WorldEventManager.OnWorldEventFailed -= HandleEventFailed;
        }

        private void Start()
        {
            EnsureUIStructure();
            if (eventPanel != null)
            {
                eventPanel.SetActive(false);
            }
        }

        private void HandleEventStarted(WorldEventData data)
        {
            activeEvent = data;
            EnsureUIStructure();

            if (eventPanel != null) eventPanel.SetActive(true);

            if (eventTitleText != null && activeEvent != null)
            {
                eventTitleText.text = activeEvent.displayName.ToUpper();
            }

            if (eventDescText != null && activeEvent != null)
            {
                eventDescText.text = activeEvent.description;
            }

            if (rewardPreviewText != null && activeEvent != null)
            {
                rewardPreviewText.text = $"REWARD: <color=#FFC107>+{activeEvent.rewardXP} XP</color>";
            }

            UpdateProgressText(1, activeEvent != null ? activeEvent.waveCount : 1, 0, activeEvent != null ? activeEvent.eventDuration : 0f);
        }

        private void HandleWaveChanged(WorldEventData data, int currentWave, int totalWaves)
        {
            activeEvent = data;
            int zombiesRemaining = WorldEventManager.Instance != null ? WorldEventManager.Instance.TrackedZombieCount : 0;
            UpdateProgressText(currentWave, totalWaves, zombiesRemaining, activeEvent != null ? activeEvent.eventDuration : 0f);
        }

        private void HandleEventCompleted(WorldEventData data)
        {
            if (eventProgressText != null)
            {
                eventProgressText.text = "<color=#4CAF50>EVENT COMPLETED!</color>";
            }

            Invoke(nameof(HidePanel), 3.0f);
        }

        private void HandleEventFailed(WorldEventData data, string reason)
        {
            if (eventProgressText != null)
            {
                eventProgressText.text = $"<color=#F44336>EVENT FAILED ({reason})</color>";
            }

            Invoke(nameof(HidePanel), 3.0f);
        }

        private void HidePanel()
        {
            if (eventPanel != null)
            {
                eventPanel.SetActive(false);
            }
        }

        public void UpdateProgressText(int wave, int maxWaves, int zombiesRemaining, float timeRemaining)
        {
            if (activeEvent == null) return;

            if (eventProgressText != null)
            {
                switch (activeEvent.eventType)
                {
                    case WorldEventType.WaveHorde:
                    case WorldEventType.Outbreak:
                    case WorldEventType.SupplyAmbush:
                    case WorldEventType.HighThreatZone:
                        eventProgressText.text = $"Wave: {wave}/{maxWaves}  |  Zombies Left: {zombiesRemaining}";
                        break;
                    case WorldEventType.BossEncounter:
                        eventProgressText.text = "ELIMINATE THE BOSS ZOMBIE!";
                        break;
                    case WorldEventType.SupplyDrop:
                    case WorldEventType.LootDiscovery:
                        eventProgressText.text = "Search & recover emergency supply crate!";
                        break;
                    case WorldEventType.SurvivorRescue:
                        eventProgressText.text = "Interact with survivor distress beacon!";
                        break;
                    case WorldEventType.TimedScavenge:
                        eventProgressText.text = $"Collect required supplies before timer expires!";
                        break;
                }
            }

            if (eventTimerText != null && timeRemaining > 0f)
            {
                int min = Mathf.FloorToInt(timeRemaining / 60f);
                int sec = Mathf.FloorToInt(timeRemaining % 60f);
                eventTimerText.text = $"Time: {min:D2}:{sec:D2}";
            }
            else if (eventTimerText != null)
            {
                eventTimerText.text = "";
            }
        }

        private void EnsureUIStructure()
        {
            if (eventPanel != null) return;

            GameObject canvasObj = GameObject.Find("GameplayCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GameplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            eventPanel = new GameObject("WorldEventHUDPanel", typeof(RectTransform), typeof(Image));
            eventPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform rect = eventPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.82f);
            rect.anchorMax = new Vector2(0.65f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            eventPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

            GameObject titleObj = new GameObject("EventTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(eventPanel.transform, false);
            eventTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            eventTitleText.text = "WORLD EVENT";
            eventTitleText.fontSize = 16;
            eventTitleText.fontStyle = FontStyles.Bold;
            eventTitleText.color = new Color(1f, 0.85f, 0.2f);
            eventTitleText.alignment = TextAlignmentOptions.Center;
            titleObj.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.65f);
            titleObj.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);

            GameObject descObj = new GameObject("EventDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(eventPanel.transform, false);
            eventDescText = descObj.AddComponent<TextMeshProUGUI>();
            eventDescText.text = "";
            eventDescText.fontSize = 12;
            eventDescText.color = Color.white;
            eventDescText.alignment = TextAlignmentOptions.Center;
            descObj.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.35f);
            descObj.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.65f);

            GameObject progObj = new GameObject("EventProgress", typeof(RectTransform), typeof(TextMeshProUGUI));
            progObj.transform.SetParent(eventPanel.transform, false);
            eventProgressText = progObj.AddComponent<TextMeshProUGUI>();
            eventProgressText.text = "";
            eventProgressText.fontSize = 12;
            eventProgressText.fontStyle = FontStyles.Bold;
            eventProgressText.color = new Color(0.2f, 0.8f, 1f);
            eventProgressText.alignment = TextAlignmentOptions.Center;
            progObj.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.05f);
            progObj.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.35f);
        }
    }
}
