using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Controls Pause Menu overlay toggle, Resume, Save Game, and Quit to Main Menu.
    /// 
    /// ATTACH TO: Gameplay Canvas PauseMenu Root GameObject.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Menu Overlay")]
        [SerializeField] private GameObject pauseMenuContainer;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        private bool isPaused;

        public bool IsPaused => isPaused;

        private void Start()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);

            SetPauseState(false);
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
        }

        public void ResumeGame()
        {
            SetPauseState(false);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
