using UnityEngine;

namespace ZombieApocalypse.Environment
{
    /// <summary>
    /// ScriptableObject defining an environmental condition (e.g. Extreme Heat, Extreme Cold, Toxic Area, Heavy Rain).
    /// Holds multipliers for survival decay, periodic health damage, movement effects, and UI feedback.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnvironmentalConditionData", menuName = "Zombie Apocalypse/Environmental Condition Data")]
    public class EnvironmentalConditionData : ScriptableObject
    {
        [Header("Identity")]
        public string conditionId = "cond_toxic_area";
        public string displayName = "Toxic Area";
        [TextArea(2, 4)]
        public string description = "Hazardous airborne toxins cause gradual health damage and accelerate thirst.";
        
        [Header("Survival Decay Multipliers")]
        [Tooltip("Multiplier applied to player hunger decay rate (1.0 = normal, 1.5 = +50% faster decay)")]
        public float hungerDecayMultiplier = 1.0f;

        [Tooltip("Multiplier applied to player thirst decay rate (1.0 = normal, 1.5 = +50% faster decay)")]
        public float thirstDecayMultiplier = 1.0f;

        [Header("Health & Hazard Effects")]
        [Tooltip("Direct health damage applied to player per second while condition is active.")]
        public float healthDamagePerSecond = 0f;

        [Tooltip("Movement speed multiplier applied to player (1.0 = normal, 0.85 = -15% speed).")]
        public float movementSpeedMultiplier = 1.0f;

        [Header("UI & Notifications")]
        public string warningMessage = "WARNING: Entering Toxic Zone!";
        public Color badgeColor = new Color(0.9f, 0.3f, 0.2f);
        public Sprite conditionIcon;
    }
}
