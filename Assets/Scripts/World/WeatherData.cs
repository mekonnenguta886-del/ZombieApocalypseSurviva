using UnityEngine;
using ZombieApocalypse.Environment;

namespace ZombieApocalypse.World
{
    /// <summary>
    /// ScriptableObject defining a weather profile (e.g. Clear, Heavy Rain, Dense Fog, Toxic Storm).
    /// Links dynamic weather profiles to Phase 15 EnvironmentalConditionData and transition rules.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeatherData", menuName = "Zombie Apocalypse/Weather Data")]
    public class WeatherData : ScriptableObject
    {
        [Header("Identity")]
        public string weatherId = "weather_clear";
        public string displayName = "Clear Weather";
        [TextArea(2, 4)]
        public string description = "Clear skies with normal survival decay conditions.";

        [Header("Linked Environmental Condition")]
        [Tooltip("Associated Phase 15 EnvironmentalConditionData applied when this weather profile is active.")]
        public EnvironmentalConditionData environmentalCondition;

        [Header("Dynamic Transition Config")]
        [Tooltip("Minimum duration in game-world minutes for this weather profile.")]
        public float minDurationMinutes = 120f; // 2 in-game hours

        [Tooltip("Maximum duration in game-world minutes for this weather profile.")]
        public float maxDurationMinutes = 360f; // 6 in-game hours

        [Tooltip("Probability weight when selecting next random weather transition.")]
        public float transitionWeight = 1.0f;

        [Header("Visuals & UI")]
        public Color weatherBadgeColor = new Color(0.3f, 0.7f, 1.0f);
        public Sprite weatherIcon;
    }
}
