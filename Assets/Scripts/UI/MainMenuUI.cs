using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieApocalypse.Save;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Handles main menu UI button events (New Game, Continue, Test Arena, Settings, Quit Game).
    /// Supports detecting existing saves and safe loading transitions.
    /// 
    /// ATTACH TO: MainMenu Canvas Root GameObject in MainMenu scene.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton; // Dual-purposed as New Game
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button testArenaButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Panel")]
        [SerializeField] private SettingsUIController settingsPanel;

        private void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnNewGameClicked);
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);

            if (continueButton != null)
            {
                bool hasSave = SaveFileUtility.HasSave();
                continueButton.interactable = hasSave;
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (testArenaButton != null) testArenaButton.onClick.AddListener(OnTestArenaClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        public void OnStartClicked()
        {
            OnNewGameClicked();
        }

        public void OnNewGameClicked()
        {
            Debug.Log("[MainMenuUI] Starting New Game...");
            SaveManager.LoadSaveOnSceneLoad = false;
            SceneManager.LoadScene("Gameplay");
        }

        public void OnContinueClicked()
        {
            if (!SaveFileUtility.HasSave())
            {
                Debug.LogWarning("[MainMenuUI] Continue clicked but no save file exists.");
                return;
            }

            Debug.Log("[MainMenuUI] Continuing Game (loading active save)...");
            SaveManager.LoadSaveOnSceneLoad = true;
            SceneManager.LoadScene("Gameplay");
        }

        public void OnTestArenaClicked()
        {
            Debug.Log("[MainMenuUI] Loading TestArena scene...");
            SaveManager.LoadSaveOnSceneLoad = false;
            SceneManager.LoadScene("TestArena");
        }

        public void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] Opening Settings Panel...");
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

        public void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] Quitting application...");
            Application.Quit();
        }
    }
}
