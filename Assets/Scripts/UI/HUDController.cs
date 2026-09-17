using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.Player;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Updates HUD UI elements (Health Bar, Stamina Bar, Objective Text, Death Screen).
    /// Listens to PlayerHealth and PlayerStamina events automatically.
    /// 
    /// ATTACH TO: Gameplay Canvas HUD Root GameObject.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health & Stamina UI")]
        [SerializeField] private Slider healthBarSlider;
        [SerializeField] private Slider staminaBarSlider;

        [Header("Overlay UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI objectiveText;

        private PlayerHealth playerHealth;
        private PlayerStamina playerStamina;

        private void Start()
        {
            FindAndBindPlayer();
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        public void FindAndBindPlayer()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                playerStamina = playerObj.GetComponent<PlayerStamina>();

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

        public void ShowGameOverScreen()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
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
