using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.Player;
using ZombieApocalypse.Weapons;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Updates all HUD UI elements (Health bar, Stamina bar, Weapon name, Ammo count, Crosshair, Death overlay).
    /// Binds automatically to PlayerHealth, PlayerStamina, and WeaponController events.
    /// 
    /// ATTACH TO: Gameplay Canvas HUD Root GameObject.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health & Stamina UI")]
        [SerializeField] private Slider healthBarSlider;
        [SerializeField] private Slider staminaBarSlider;

        [Header("Weapon & Ammo UI")]
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private GameObject crosshairOverlay;

        [Header("Overlay Panels")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI objectiveText;

        private PlayerHealth playerHealth;
        private PlayerStamina playerStamina;
        private PlayerController playerController;
        private WeaponController weaponController;

        private void Start()
        {
            FindAndBindPlayer();
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (crosshairOverlay != null) crosshairOverlay.SetActive(false);
        }

        public void FindAndBindPlayer()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                playerStamina = playerObj.GetComponent<PlayerStamina>();
                playerController = playerObj.GetComponent<PlayerController>();
                weaponController = playerObj.GetComponent<WeaponController>();

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

                if (weaponController != null)
                {
                    weaponController.OnWeaponStateChanged += UpdateWeaponHUD;
                    if (weaponController.CurrentWeapon != null)
                    {
                        UpdateWeaponHUD(weaponController.CurrentSlot.currentMagazineAmmo, weaponController.CurrentSlot.reserveAmmo, weaponController.CurrentWeapon.weaponName, weaponController.IsReloading, false);
                    }
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

            if (weaponController != null)
            {
                weaponController.OnWeaponStateChanged -= UpdateWeaponHUD;
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
        }

        public void SetObjective(string objective)
        {
            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
        }
    }
}
