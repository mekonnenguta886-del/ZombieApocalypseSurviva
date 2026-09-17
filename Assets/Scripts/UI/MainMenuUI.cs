using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// Handles main menu UI button events (Start Game, Test Arena, Settings, Quit Game).
    /// 
    /// ATTACH TO: MainMenu Canvas Root GameObject in MainMenu scene.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button testArenaButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (testArenaButton != null) testArenaButton.onClick.AddListener(OnTestArenaClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        public void OnStartClicked()
        {
            Debug.Log("[MainMenuUI] Loading Gameplay scene...");
            SceneManager.LoadScene("Gameplay");
        }

        public void OnTestArenaClicked()
        {
            Debug.Log("[MainMenuUI] Loading TestArena scene...");
            SceneManager.LoadScene("TestArena");
        }

        public void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] Quitting application...");
            Application.Quit();
        }
    }
}
