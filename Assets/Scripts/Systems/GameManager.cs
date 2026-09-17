using UnityEngine;

namespace ZombieApocalypse.Core
{
    /// <summary>
    /// Global Game State enumerator.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        MissionComplete
    }

    /// <summary>
    /// Core Game Manager singleton.
    /// Controls overall game flow, state transitions, and persistent managers.
    /// 
    /// ATTACH TO: Persistent GameObject (e.g. "[GameManager]" in scenes).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        public GameState CurrentState => currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetGameState(currentState);
        }

        /// <summary>
        /// Changes current game state and broadcasts notification to listeners.
        /// </summary>
        public void SetGameState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"[GameManager] Game State changed to: {newState}");
        }
    }
}
