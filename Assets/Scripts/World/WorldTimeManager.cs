using System;
using UnityEngine;
using ZombieApocalypse.UI;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// Centralized manager for Phase 16 Day/Night cycle, time progression, night aggression scaling,
    /// time-of-day clock formatting, and save persistence.
    ///
    /// ATTACH TO: [WorldTimeManager] GameObject in scene.
    /// </summary>
    public class WorldTimeManager : MonoBehaviour
    {
        public static WorldTimeManager Instance { get; private set; }

        public event Action<float, int> OnTimeChanged; // currentTimeMinutes, dayCount
        public event Action<bool> OnDayNightStateChanged; // isNight

        [Header("Time Configuration")]
        [Tooltip("Starting time of day in minutes (480 = 08:00 AM).")]
        [SerializeField] private float timeOfDayMinutes = 480f; // 08:00 AM default
        [SerializeField] private int dayCount = 1;

        [Tooltip("Time speed multiplier. 60.0 means 1 real second = 1 game minute (24 real minutes = 1 game day).")]
        [SerializeField] private float timeMultiplier = 60.0f;

        [Header("Night Aggression Config")]
        [SerializeField] private float nightStartMinutes = 1200f; // 20:00 (8:00 PM)
        [SerializeField] private float nightEndMinutes = 360f;    // 06:00 (6:00 AM)
        [SerializeField] private float nightSpawnCapMultiplier = 1.50f; // +50% max zombies at night
        [SerializeField] private float nightAggressionMultiplier = 1.35f; // +35% zombie speed/damage at night

        private bool isNight = false;

        public float TimeOfDayMinutes => timeOfDayMinutes;
        public int DayCount => dayCount;
        public bool IsNight => isNight;

        public string FormattedTimeString
        {
            get
            {
                int hours = Mathf.FloorToInt(timeOfDayMinutes / 60f) % 24;
                int minutes = Mathf.FloorToInt(timeOfDayMinutes % 60f);
                return $"{hours:D2}:{minutes:D2}";
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EvaluateDayNightState(true);
            NotifyTimeChanged();
        }

        private void Update()
        {
            // Advance time of day
            timeOfDayMinutes += (Time.deltaTime * timeMultiplier) / 60f;

            // Handle day rollover (24 hours = 1440 minutes)
            if (timeOfDayMinutes >= 1440f)
            {
                timeOfDayMinutes -= 1440f;
                dayCount++;
                Debug.Log($"[WorldTimeManager] Day Rollover! Advanced to Day {dayCount}.");
            }

            EvaluateDayNightState(false);
            NotifyTimeChanged();
        }

        private void EvaluateDayNightState(bool forceNotify)
        {
            bool newIsNight = timeOfDayMinutes >= nightStartMinutes || timeOfDayMinutes < nightEndMinutes;

            if (newIsNight != isNight || forceNotify)
            {
                isNight = newIsNight;
                Debug.Log($"[WorldTimeManager] Day/Night state changed. IsNight: {isNight} (Time: {FormattedTimeString})");
                OnDayNightStateChanged?.Invoke(isNight);

                if (isNight)
                {
                    ShowHUDToast("NIGHTFALL: Zombie Aggression Increased!");
                }
                else if (!forceNotify)
                {
                    ShowHUDToast("DAWN: Safe daylight has returned.");
                }
            }
        }

        public float GetNightSpawnCapMultiplier()
        {
            return isNight ? nightSpawnCapMultiplier : 1.0f;
        }

        public float GetNightAggressionMultiplier()
        {
            return isNight ? nightAggressionMultiplier : 1.0f;
        }

        public void SetTimeOfDay(float minutes, int day)
        {
            timeOfDayMinutes = Mathf.Clamp(minutes, 0f, 1440f);
            dayCount = Mathf.Max(1, day);
            EvaluateDayNightState(true);
            NotifyTimeChanged();
        }

        private void NotifyTimeChanged()
        {
            OnTimeChanged?.Invoke(timeOfDayMinutes, dayCount);
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
