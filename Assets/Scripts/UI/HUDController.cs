using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.Inventory;
using ZombieApocalypse.Player;
using ZombieApocalypse.Weapons;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Updates all HUD UI elements (Health bar, Stamina bar, Hunger bar, Thirst bar, Weapon name, Ammo count,
    /// Crosshair, Interaction prompt, Notification toasts, Low survival warnings, Death overlay).
    /// 
    /// ATTACH TO: Gameplay Canvas HUD Root GameObject.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health & Stamina UI")]
        [SerializeField] private Slider healthBarSlider;
        [SerializeField] private Slider staminaBarSlider;

        [Header("Survival UI (Hunger & Thirst)")]
        [SerializeField] private Slider hungerBarSlider;
        [SerializeField] private Slider thirstBarSlider;
        [SerializeField] private TextMeshProUGUI lowSurvivalWarningText;

        [Header("Weapon & Ammo UI")]
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private GameObject crosshairOverlay;

        [Header("Interaction & Pickup Feedback")]
        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [SerializeField] private TextMeshProUGUI notificationToastText;

        [Header("Overlay Panels")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI objectiveText;

        // Player References
        private PlayerHealth playerHealth;
        private PlayerStamina playerStamina;
        private PlayerSurvivalStats survivalStats;
        private PlayerController playerController;
        private WeaponController weaponController;
        private PlayerInteraction playerInteraction;
        private InventorySystem inventorySystem;

        public static HUDController Instance { get; private set; }

        private float toastDisplayTimer;

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
            FindAndBindPlayer();
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (crosshairOverlay != null) crosshairOverlay.SetActive(false);
            if (lowSurvivalWarningText != null) lowSurvivalWarningText.gameObject.SetActive(false);
            if (notificationToastText != null) notificationToastText.gameObject.SetActive(false);
        }

        public void FindAndBindPlayer()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                playerStamina = playerObj.GetComponent<PlayerStamina>();
                survivalStats = playerObj.GetComponent<PlayerSurvivalStats>();
                playerController = playerObj.GetComponent<PlayerController>();
                weaponController = playerObj.GetComponent<WeaponController>();
                playerInteraction = playerObj.GetComponent<PlayerInteraction>();
                inventorySystem = playerObj.GetComponent<InventorySystem>();

                if (playerHealth != null)
                {
                    playerHealth.OnHealthChanged += UpdateHealth;
                    playerHealth.OnPlayerDied += ShowGameOverScreen;
                    UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
                }

                if (playerStamina != null)
                {
                    playerStamina.OnStaminaChanged += UpdateStamina;
                    UpdateStamina(playerStamina.CurrentStamina, playerStamina.MaxStamina);
                }

                if (survivalStats != null)
                {
                    survivalStats.OnSurvivalStatsChanged += UpdateSurvivalStats;
                    survivalStats.OnLowSurvivalWarning += ShowLowSurvivalWarning;
                    UpdateSurvivalStats(survivalStats.CurrentHunger, survivalStats.MaxHunger, survivalStats.CurrentThirst, survivalStats.MaxThirst);
                }

                if (weaponController != null)
                {
                    weaponController.OnWeaponStateChanged += UpdateWeaponHUD;
                    if (weaponController.CurrentWeapon != null)
                    {
                        UpdateWeaponHUD(weaponController.CurrentSlot.currentMagazineAmmo, weaponController.CurrentSlot.reserveAmmo, weaponController.CurrentWeapon.weaponName, weaponController.IsReloading, false);
                    }
                }

                if (playerInteraction != null)
                {
                    playerInteraction.OnInteractionPromptChanged += UpdateInteractionPrompt;
                    UpdateInteractionPrompt(playerInteraction.ActivePrompt);
                }

                if (inventorySystem != null)
                {
                    inventorySystem.OnInventoryNotification += ShowNotificationToast;
                }
            }
        }

        private void Update()
        {
            // Toggle Crosshair display when player is aiming
            if (playerController != null && crosshairOverlay != null)
            {
                crosshairOverlay.SetActive(playerController.IsAiming);
            }

            // Hide toast after timer expires
            if (toastDisplayTimer > 0f)
            {
                toastDisplayTimer -= Time.deltaTime;
                if (toastDisplayTimer <= 0f && notificationToastText != null)
                {
                    notificationToastText.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealth;
                playerHealth.OnPlayerDied -= ShowGameOverScreen;
            }

            if (playerStamina != null)
            {
                playerStamina.OnStaminaChanged -= UpdateStamina;
            }

            if (survivalStats != null)
            {
                survivalStats.OnSurvivalStatsChanged -= UpdateSurvivalStats;
                survivalStats.OnLowSurvivalWarning -= ShowLowSurvivalWarning;
            }

            if (weaponController != null)
            {
                weaponController.OnWeaponStateChanged -= UpdateWeaponHUD;
            }

            if (playerInteraction != null)
            {
                playerInteraction.OnInteractionPromptChanged -= UpdateInteractionPrompt;
            }

            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryNotification -= ShowNotificationToast;
            }
        }

        public void UpdateHealth(float current, float max)
        {
            if (healthBarSlider != null)
            {
                healthBarSlider.value = Mathf.Clamp01(current / max);
            }
        }

        public void UpdateStamina(float current, float max)
        {
            if (staminaBarSlider != null)
            {
                staminaBarSlider.value = Mathf.Clamp01(current / max);
            }
        }

        public void UpdateSurvivalStats(float hunger, float maxHunger, float thirst, float maxThirst)
        {
            if (hungerBarSlider != null)
            {
                hungerBarSlider.value = Mathf.Clamp01(hunger / maxHunger);
            }

            if (thirstBarSlider != null)
            {
                thirstBarSlider.value = Mathf.Clamp01(thirst / maxThirst);
            }
        }

        public void UpdateWeaponHUD(int currentMag, int reserveAmmo, string weaponName, bool isReloading, bool isEmpty)
        {
            if (weaponNameText != null)
            {
                weaponNameText.text = weaponName.ToUpper();
            }

            if (ammoText != null)
            {
                if (isReloading)
                {
                    ammoText.text = "RELOADING...";
                }
                else if (isEmpty)
                {
                    ammoText.text = "EMPTY";
                }
                else
                {
                    ammoText.text = $"{currentMag} / {reserveAmmo}";
                }
            }
        }

        public void UpdateInteractionPrompt(string promptText)
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.text = promptText;
                interactionPromptText.gameObject.SetActive(!string.IsNullOrEmpty(promptText));
            }
        }

        public void ShowNotificationToast(string message)
        {
            if (notificationToastText != null)
            {
                notificationToastText.text = message;
                notificationToastText.gameObject.SetActive(true);
                toastDisplayTimer = 2.5f;
            }
        }

        public void ShowLowSurvivalWarning(string warningMessage)
        {
            if (lowSurvivalWarningText != null)
            {
                lowSurvivalWarningText.text = warningMessage;
                lowSurvivalWarningText.gameObject.SetActive(true);
                Invoke(nameof(HideLowSurvivalWarning), 3.5f);
            }
        }

        private void HideLowSurvivalWarning()
        {
            if (lowSurvivalWarningText != null)
            {
                lowSurvivalWarningText.gameObject.SetActive(false);
            }
        }

        public void ShowGameOverScreen()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            if (crosshairOverlay != null)
            {
                crosshairOverlay.SetActive(false);
            }
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }
        }

        public void SetObjective(string objective)
        {
            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
        }

        public void ShowHitmarker(bool isCritical)
        {
            if (notificationToastText != null)
            {
                string text = isCritical ? "<color=red>HEADSHOT!</color>" : "<color=white>HIT</color>";
                Debug.Log($"[HUDController] Hitmarker triggered ({text})");
            }
        }

        public void ShowDirectionalDamageIndicator(Vector3 originPos)
        {
            Vector3 playerPos = playerController != null ? playerController.transform.position : transform.position;
            Vector3 dir = (originPos - playerPos).normalized;
            Debug.Log($"[HUDController] Directional Damage Indicator triggered from origin {originPos} (Direction: {dir})");
        }

        public void SetWorldEventHUD(string title, int waveCurrent, int waveMax, int zombiesRemaining, float timeRemaining)
        {
            if (notificationToastText != null)
            {
                int min = Mathf.FloorToInt(timeRemaining / 60f);
                int sec = Mathf.FloorToInt(timeRemaining % 60f);
                string text = $"<color=orange><b>{title.ToUpper()}</b></color>\nWave: {waveCurrent}/{waveMax} | Zombies: {zombiesRemaining} | Time: {min:D2}:{sec:D2}";
                notificationToastText.text = text;
                notificationToastText.gameObject.SetActive(true);
                toastDisplayTimer = 1.0f;
            }
        }

        public void HideWorldEventHUD()
        {
            if (notificationToastText != null)
            {
                notificationToastText.gameObject.SetActive(false);
            }
        }
    }
}
