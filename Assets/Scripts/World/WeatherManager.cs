using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieApocalypse.Environment;
using ZombieApocalypse.UI;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Dynamic Weather Manager for Phase 16.
    /// Controls weather state transitions (Clear, Heavy Rain, Dense Fog, Toxic Storm),
    /// automatically updating EnvironmentalConditionManager and firing HUD notification callbacks.
    ///
    /// ATTACH TO: [WeatherManager] GameObject in scene.
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance { get; private set; }

        public event Action<WeatherData> OnWeatherChanged;

        [Header("Weather Profile References")]
        [SerializeField] private WeatherData clearWeather;
        [SerializeField] private WeatherData rainWeather;
        [SerializeField] private WeatherData fogWeather;
        [SerializeField] private WeatherData stormWeather;

        [Header("Runtime Weather State")]
        [SerializeField] private WeatherData activeWeather;
        [SerializeField] private float weatherTimer = 0f;
        [SerializeField] private float nextTransitionTime = 180f; // in-game minutes

        public WeatherData ActiveWeather => activeWeather;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureDefaultProfiles();
        }

        private void Start()
        {
            if (activeWeather == null)
            {
                SetWeather(clearWeather);
            }

            if (WorldTimeManager.Instance != null)
            {
                WorldTimeManager.Instance.OnTimeChanged += HandleTimeProgressed;
            }
        }

        private void OnDestroy()
        {
            if (WorldTimeManager.Instance != null)
            {
                WorldTimeManager.Instance.OnTimeChanged -= HandleTimeProgressed;
            }
        }

        private void EnsureDefaultProfiles()
        {
            if (clearWeather == null)
            {
                clearWeather = ScriptableObject.CreateInstance<WeatherData>();
                clearWeather.weatherId = "weather_clear";
                clearWeather.displayName = "Clear Weather";
                clearWeather.description = "Clear skies with standard environment conditions.";
                clearWeather.minDurationMinutes = 180f;
                clearWeather.maxDurationMinutes = 360f;
                clearWeather.transitionWeight = 1.0f;
                clearWeather.weatherBadgeColor = new Color(0.3f, 0.7f, 1.0f);
            }

            if (rainWeather == null)
            {
                rainWeather = ScriptableObject.CreateInstance<WeatherData>();
                rainWeather.weatherId = "weather_rain";
                rainWeather.displayName = "Heavy Rain";
                rainWeather.description = "Heavy rainfall accelerates thirst drain.";
                rainWeather.minDurationMinutes = 120f;
                rainWeather.maxDurationMinutes = 240f;
                rainWeather.transitionWeight = 0.6f;
                rainWeather.weatherBadgeColor = new Color(0.2f, 0.4f, 0.9f);

                var rainCond = ScriptableObject.CreateInstance<EnvironmentalConditionData>();
                rainCond.conditionId = "cond_rain";
                rainCond.displayName = "Heavy Rain";
                rainCond.description = "Heavy rain increases thirst drain rate.";
                rainCond.thirstDecayMultiplier = 1.35f;
                rainCond.warningMessage = "WEATHER: Heavy Rain started!";
                rainCond.badgeColor = new Color(0.2f, 0.5f, 0.9f);
                rainWeather.environmentalCondition = rainCond;
            }

            if (fogWeather == null)
            {
                fogWeather = ScriptableObject.CreateInstance<WeatherData>();
                fogWeather.weatherId = "weather_fog";
                fogWeather.displayName = "Dense Fog";
                fogWeather.description = "Thick fog reduces visibility and increases hunger drain.";
                fogWeather.minDurationMinutes = 90f;
                fogWeather.maxDurationMinutes = 180f;
                fogWeather.transitionWeight = 0.4f;
                fogWeather.weatherBadgeColor = new Color(0.6f, 0.6f, 0.6f);

                var fogCond = ScriptableObject.CreateInstance<EnvironmentalConditionData>();
                fogCond.conditionId = "cond_fog";
                fogCond.displayName = "Dense Fog";
                fogCond.description = "Dense fog accelerates hunger drain.";
                fogCond.hungerDecayMultiplier = 1.30f;
                fogCond.warningMessage = "WEATHER: Dense Fog rolling in!";
                fogCond.badgeColor = new Color(0.6f, 0.6f, 0.6f);
                fogWeather.environmentalCondition = fogCond;
            }

            if (stormWeather == null)
            {
                stormWeather = ScriptableObject.CreateInstance<WeatherData>();
                stormWeather.weatherId = "weather_toxic_storm";
                stormWeather.displayName = "Toxic Storm";
                stormWeather.description = "Hazardous storm causing environmental damage and severe dehydration.";
                stormWeather.minDurationMinutes = 60f;
                stormWeather.maxDurationMinutes = 120f;
                stormWeather.transitionWeight = 0.2f;
                stormWeather.weatherBadgeColor = new Color(0.9f, 0.2f, 0.2f);

                var stormCond = ScriptableObject.CreateInstance<EnvironmentalConditionData>();
                stormCond.conditionId = "cond_toxic_storm";
                stormCond.displayName = "Toxic Storm";
                stormCond.description = "Severe chemical storm causing periodic damage and high thirst drain.";
                stormCond.healthDamagePerSecond = 2.0f;
                stormCond.thirstDecayMultiplier = 1.60f;
                stormCond.hungerDecayMultiplier = 1.25f;
                stormCond.warningMessage = "WARNING: Toxic Storm Active!";
                stormCond.badgeColor = new Color(0.9f, 0.2f, 0.2f);
                stormWeather.environmentalCondition = stormCond;
            }
        }

        private void HandleTimeProgressed(float timeMinutes, int day)
        {
            weatherTimer += Time.deltaTime * 60.0f / 60.0f; // Tracks game minutes

            if (weatherTimer >= nextTransitionTime)
            {
                weatherTimer = 0f;
                TransitionToRandomWeather();
            }
        }

        private void TransitionToRandomWeather()
        {
            EnsureDefaultProfiles();
            List<WeatherData> candidates = new List<WeatherData> { clearWeather, rainWeather, fogWeather, stormWeather };

            // Exclude current weather to guarantee a transition
            candidates.Remove(activeWeather);

            int index = UnityEngine.Random.Range(0, candidates.Count);
            WeatherData nextWeather = candidates[index] != null ? candidates[index] : clearWeather;
            SetWeather(nextWeather);
        }

        public void SetWeather(WeatherData weather)
        {
            EnsureDefaultProfiles();
            WeatherData target = weather != null ? weather : clearWeather;

            if (activeWeather == target) return;

            activeWeather = target;
            weatherTimer = 0f;
            nextTransitionTime = UnityEngine.Random.Range(activeWeather.minDurationMinutes, activeWeather.maxDurationMinutes);

            Debug.Log($"[WeatherManager] Weather transitioned to: {activeWeather.displayName} (Duration: {nextTransitionTime:F0}m)");
            OnWeatherChanged?.Invoke(activeWeather);

            // Update EnvironmentalConditionManager
            if (EnvironmentalConditionManager.Instance != null)
            {
                if (activeWeather.environmentalCondition != null)
                {
                    EnvironmentalConditionManager.Instance.SetCondition(activeWeather.environmentalCondition);
                }
                else
                {
                    EnvironmentalConditionManager.Instance.ClearCondition();
                }
            }

            ShowHUDToast($"Weather: {activeWeather.displayName}");
        }

        public void RestoreWeatherState(string weatherId)
        {
            EnsureDefaultProfiles();
            if (string.IsNullOrEmpty(weatherId) || weatherId.Equals("weather_clear", StringComparison.OrdinalIgnoreCase))
            {
                SetWeather(clearWeather);
                return;
            }

            WeatherData matched = FindWeatherById(weatherId);
            SetWeather(matched != null ? matched : clearWeather);
        }

        private WeatherData FindWeatherById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (clearWeather != null && clearWeather.weatherId.Equals(id, StringComparison.OrdinalIgnoreCase)) return clearWeather;
            if (rainWeather != null && rainWeather.weatherId.Equals(id, StringComparison.OrdinalIgnoreCase)) return rainWeather;
            if (fogWeather != null && fogWeather.weatherId.Equals(id, StringComparison.OrdinalIgnoreCase)) return fogWeather;
            if (stormWeather != null && stormWeather.weatherId.Equals(id, StringComparison.OrdinalIgnoreCase)) return stormWeather;

            WeatherData[] all = Resources.FindObjectsOfTypeAll<WeatherData>();
            return Array.Find(all, w => w != null && string.Equals(w.weatherId, id, StringComparison.OrdinalIgnoreCase));
        }

        private void ShowHUDToast(string message)
        {
            HUDController hud = HUDController.Instance != null ? HUDController.Instance : FindObjectOfType<HUDController>();
            if (hud != null)
            {
                hud.ShowNotificationToast(message);
            }
        }
    }
}
