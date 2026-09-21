using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.World;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// HUD UI Controller for Phase 16 Dynamic World Simulation, Day/Night Cycle, and Weather.
    /// Displays 24-hour clock (e.g. 14:30), Day Count badge (e.g. DAY 3), and Weather Profile status on GameplayCanvas.
    ///
    /// ATTACH TO: Gameplay Canvas HUD Root GameObject.
    /// </summary>
    public class WorldTimeUIController : MonoBehaviour
    {
        [Header("World Time UI Panel")]
        [SerializeField] private GameObject timeHUDPanel;
        [SerializeField] private TextMeshProUGUI timeClockText;
        [SerializeField] private TextMeshProUGUI dayCountText;
        [SerializeField] private TextMeshProUGUI weatherText;

        private void Start()
        {
            EnsureUIHierarchy();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (WorldTimeManager.Instance != null)
            {
                WorldTimeManager.Instance.OnTimeChanged += HandleTimeChanged;
                HandleTimeChanged(WorldTimeManager.Instance.TimeOfDayMinutes, WorldTimeManager.Instance.DayCount);
            }

            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.OnWeatherChanged += HandleWeatherChanged;
                if (WeatherManager.Instance.ActiveWeather != null)
                {
                    HandleWeatherChanged(WeatherManager.Instance.ActiveWeather);
                }
            }
        }

        private void UnsubscribeEvents()
        {
            if (WorldTimeManager.Instance != null)
            {
                WorldTimeManager.Instance.OnTimeChanged -= HandleTimeChanged;
            }

            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.OnWeatherChanged -= HandleWeatherChanged;
            }
        }

        private void EnsureUIHierarchy()
        {
            if (timeHUDPanel == null)
            {
                timeHUDPanel = new GameObject("WorldTimeHUDPanel");
                timeHUDPanel.transform.SetParent(transform, false);

                RectTransform rect = timeHUDPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.72f, 0.76f);
                rect.anchorMax = new Vector2(0.98f, 0.86f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image bg = timeHUDPanel.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.08f, 0.12f, 0.75f);

                // Clock Text
                GameObject clockObj = new GameObject("ClockText");
                clockObj.transform.SetParent(timeHUDPanel.transform, false);
                timeClockText = clockObj.AddComponent<TextMeshProUGUI>();
                timeClockText.fontSize = 18;
                timeClockText.fontStyle = FontStyles.Bold;
                timeClockText.color = new Color(1.0f, 0.85f, 0.3f);
                RectTransform clockRect = clockObj.GetComponent<RectTransform>();
                clockRect.anchorMin = new Vector2(0.05f, 0.5f);
                clockRect.anchorMax = new Vector2(0.55f, 0.95f);
                clockRect.offsetMin = Vector2.zero;
                clockRect.offsetMax = Vector2.zero;

                // Day Count Text
                GameObject dayObj = new GameObject("DayCountText");
                dayObj.transform.SetParent(timeHUDPanel.transform, false);
                dayCountText = dayObj.AddComponent<TextMeshProUGUI>();
                dayCountText.fontSize = 14;
                dayCountText.fontStyle = FontStyles.Bold;
                dayCountText.alignment = TextAlignmentOptions.Right;
                dayCountText.color = Color.white;
                RectTransform dayRect = dayObj.GetComponent<RectTransform>();
                dayRect.anchorMin = new Vector2(0.55f, 0.5f);
                dayRect.anchorMax = new Vector2(0.95f, 0.95f);
                dayRect.offsetMin = Vector2.zero;
                dayRect.offsetMax = Vector2.zero;

                // Weather Text
                GameObject weatherObj = new GameObject("WeatherText");
                weatherObj.transform.SetParent(timeHUDPanel.transform, false);
                weatherText = weatherObj.AddComponent<TextMeshProUGUI>();
                weatherText.fontSize = 11;
                weatherText.color = new Color(0.7f, 0.85f, 1.0f);
                RectTransform weatherRect = weatherObj.GetComponent<RectTransform>();
                weatherRect.anchorMin = new Vector2(0.05f, 0.05f);
                weatherRect.anchorMax = new Vector2(0.95f, 0.5f);
                weatherRect.offsetMin = Vector2.zero;
                weatherRect.offsetMax = Vector2.zero;
            }
        }

        private void HandleTimeChanged(float minutes, int day)
        {
            if (WorldTimeManager.Instance == null) return;

            if (timeClockText != null)
            {
                timeClockText.text = WorldTimeManager.Instance.FormattedTimeString;
                timeClockText.color = WorldTimeManager.Instance.IsNight ? new Color(0.4f, 0.7f, 1.0f) : new Color(1.0f, 0.85f, 0.3f);
            }

            if (dayCountText != null)
            {
                dayCountText.text = $"DAY {day}";
            }
        }

        private void HandleWeatherChanged(WeatherData weather)
        {
            if (weatherText != null && weather != null)
            {
                weatherText.text = weather.displayName.ToUpper();
                weatherText.color = weather.weatherBadgeColor;
            }
        }
    }
}
