using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieApocalypse.Save;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Controls Pause Menu overlay toggle, Resume, Save Game, Settings, and Quit to Main Menu.
    /// 
    /// ATTACH TO: Gameplay Canvas PauseMenu Root GameObject.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Menu Overlay")]
        [SerializeField] private GameObject pauseMenuContainer;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Settings Panel")]
        [SerializeField] private SettingsUIController settingsPanel;

        private bool isPaused;

        public bool IsPaused => isPaused;

        private void Start()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (saveGameButton != null) saveGameButton.onClick.AddListener(SaveGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);

            SetPauseState(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            SetPauseState(!isPaused);
        }

        public void SetPauseState(bool pause)
        {
            isPaused = pause;
            Time.timeScale = isPaused ? 0f : 1f;

            if (pauseMenuContainer != null)
            {
                pauseMenuContainer.SetActive(isPaused);
            }

            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused;
        }

        public void ResumeGame()
        {
            SetPauseState(false);
        }

        public void SaveGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
        }

        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.OpenPanel();
            }
            else
            {
                SettingsUIController panelInScene = FindObjectOfType<SettingsUIController>(true);
                if (panelInScene != null) panelInScene.OpenPanel();
            }
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
