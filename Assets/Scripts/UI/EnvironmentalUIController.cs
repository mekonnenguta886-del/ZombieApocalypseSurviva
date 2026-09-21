using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.Environment;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// HUD UI Overlay for Phase 15 Environmental Hazards and Dynamic World Conditions.
    /// Displays active condition status badges, decay rate indicators, and hazard warning banners.
    /// 
    /// ATTACH TO: Gameplay Canvas HUD Root GameObject.
    /// </summary>
    public class EnvironmentalUIController : MonoBehaviour
    {
        [Header("Environmental Condition HUD Panel")]
        [SerializeField] private GameObject conditionHUDPanel;
        [SerializeField] private TextMeshProUGUI conditionTitleText;
        [SerializeField] private TextMeshProUGUI conditionDetailsText;
        [SerializeField] private Image conditionBadgeImage;

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
            if (EnvironmentalConditionManager.Instance != null)
            {
                EnvironmentalConditionManager.Instance.OnConditionChanged += HandleConditionChanged;
                HandleConditionChanged(EnvironmentalConditionManager.Instance.ActiveCondition);
            }
        }

        private void UnsubscribeEvents()
        {
            if (EnvironmentalConditionManager.Instance != null)
            {
                EnvironmentalConditionManager.Instance.OnConditionChanged -= HandleConditionChanged;
            }
        }

        private void EnsureUIHierarchy()
        {
            if (conditionHUDPanel == null)
            {
                // Auto-create lightweight HUD panel if not wired via Inspector
                conditionHUDPanel = new GameObject("EnvironmentalHUDPanel");
                conditionHUDPanel.transform.SetParent(transform, false);

                RectTransform rect = conditionHUDPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.72f, 0.88f);
                rect.anchorMax = new Vector2(0.98f, 0.98f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image bg = conditionHUDPanel.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
                conditionBadgeImage = bg;

                GameObject titleObj = new GameObject("ConditionTitleText");
                titleObj.transform.SetParent(conditionHUDPanel.transform, false);
                conditionTitleText = titleObj.AddComponent<TextMeshProUGUI>();
                conditionTitleText.fontSize = 14;
                conditionTitleText.fontStyle = FontStyles.Bold;
                conditionTitleText.color = Color.white;
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.05f, 0.5f);
                titleRect.anchorMax = new Vector2(0.95f, 0.95f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;

                GameObject detailsObj = new GameObject("ConditionDetailsText");
                detailsObj.transform.SetParent(conditionHUDPanel.transform, false);
                conditionDetailsText = detailsObj.AddComponent<TextMeshProUGUI>();
                conditionDetailsText.fontSize = 11;
                conditionDetailsText.color = new Color(0.85f, 0.85f, 0.85f);
                RectTransform detailsRect = detailsObj.GetComponent<RectTransform>();
                detailsRect.anchorMin = new Vector2(0.05f, 0.05f);
                detailsRect.anchorMax = new Vector2(0.95f, 0.5f);
                detailsRect.offsetMin = Vector2.zero;
                detailsRect.offsetMax = Vector2.zero;
            }

            if (conditionHUDPanel != null)
            {
                conditionHUDPanel.SetActive(false);
            }
        }

        private void HandleConditionChanged(EnvironmentalConditionData condition)
        {
            if (condition == null || string.IsNullOrEmpty(condition.conditionId) || condition.conditionId.Equals("normal", System.StringComparison.OrdinalIgnoreCase))
            {
                if (conditionHUDPanel != null) conditionHUDPanel.SetActive(false);
                return;
            }

            if (conditionHUDPanel != null)
            {
                conditionHUDPanel.SetActive(true);
            }

            if (conditionTitleText != null)
            {
                conditionTitleText.text = condition.displayName.ToUpper();
                conditionTitleText.color = condition.badgeColor;
            }

            if (conditionDetailsText != null)
            {
                string details = "";
                if (condition.healthDamagePerSecond > 0f)
                {
                    details += $"Hazard Damage: {condition.healthDamagePerSecond:F1}/s\n";
                }
                if (condition.thirstDecayMultiplier > 1.0f)
                {
                    int thirstPct = Mathf.RoundToInt((condition.thirstDecayMultiplier - 1.0f) * 100f);
                    details += $"Thirst Drain +{thirstPct}%\n";
                }
                if (condition.hungerDecayMultiplier > 1.0f)
                {
                    int hungerPct = Mathf.RoundToInt((condition.hungerDecayMultiplier - 1.0f) * 100f);
                    details += $"Hunger Drain +{hungerPct}%\n";
                }
                conditionDetailsText.text = details.TrimEnd();
            }

            if (conditionBadgeImage != null)
            {
                conditionBadgeImage.color = new Color(condition.badgeColor.r * 0.3f, condition.badgeColor.g * 0.3f, condition.badgeColor.b * 0.3f, 0.85f);
            }
        }
    }
}
