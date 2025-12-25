using UnityEngine;

namespace Siege.Core
{
    /// <summary>
    /// Central game manager that controls the overall game state and flow.
    /// Implements singleton pattern for global access.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private LevelData currentLevel;
        [SerializeField] private int shotsUsed = 0;
        [SerializeField] private int shotsRemaining = 0;
        
        [Header("References")]
        [SerializeField] private Trebuchet.TrebuchetController trebuchet;
        [SerializeField] private Level.LevelManager levelManager;
        [SerializeField] private UI.UIManager uiManager;
        
        // Events for game state changes
        public System.Action<GameState> OnGameStateChanged;
        public System.Action<int> OnShotsUpdated;
        public System.Action OnLevelCompleted;
        public System.Action OnLevelFailed;
        
        public GameState CurrentState => currentState;
        public LevelData CurrentLevel => currentLevel;
        public int ShotsUsed => shotsUsed;
        public int ShotsRemaining => shotsRemaining;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeGame()
        {
            Debug.Log("[GameManager] Initializing game...");
            
            // Load saved progress
            Save.SaveManager.Instance?.LoadGame();
            
            // Initialize subsystems
            InitializeSubsystems();
        }
        
        private void InitializeSubsystems()
        {
            // Initialize event system if present
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Initialize();
            }
        }
        
        /// <summary>
        /// Starts a new level with the given level data.
        /// </summary>
        public void StartLevel(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError("[GameManager] Cannot start null level!");
                return;
            }
            
            Debug.Log($"[GameManager] Starting level: {level.levelName}");
            
            currentLevel = level;
            shotsUsed = 0;
            shotsRemaining = level.maxShots;
            
            ChangeState(GameState.Playing);
            
            // Load the level
            if (levelManager != null)
            {
                levelManager.LoadLevel(level);
            }
            
            // Update UI
            if (uiManager != null)
            {
                uiManager.UpdateHUD();
            }
            
            // Reset trebuchet
            if (trebuchet != null)
            {
                trebuchet.ResetToDefaultPosition();
            }
        }
        
        /// <summary>
        /// Called when a projectile is fired.
        /// </summary>
        public void OnProjectileFired()
        {
            shotsUsed++;
            shotsRemaining--;
            
            Debug.Log($"[GameManager] Shot fired. Shots used: {shotsUsed}, Remaining: {shotsRemaining}");
            
            OnShotsUpdated?.Invoke(shotsRemaining);
            
            if (uiManager != null)
            {
                uiManager.UpdateHUD();
            }
            
            // Check if we should evaluate completion
            if (shotsRemaining <= 0)
            {
                Invoke(nameof(CheckLevelCompletion), 2f); // Delay to allow physics to resolve
            }
        }
        
        /// <summary>
        /// Called when a structure is destroyed.
        /// </summary>
        public void OnStructureDestroyed(Castle.StructureComponent structure)
        {
            Debug.Log($"[GameManager] Structure destroyed: {structure.name}");
            
            // Check if goal is complete
            if (levelManager != null)
            {
                levelManager.CheckGoalCompletion();
            }
        }
        
        /// <summary>
        /// Checks if the level is complete or failed.
        /// </summary>
        public void CheckLevelCompletion()
        {
            if (levelManager == null) return;
            
            bool goalComplete = levelManager.IsGoalComplete();
            
            if (goalComplete)
            {
                CompleteLevel();
            }
            else if (shotsRemaining <= 0)
            {
                FailLevel();
            }
        }
        
        /// <summary>
        /// Completes the current level and calculates stars.
        /// </summary>
        private void CompleteLevel()
        {
            Debug.Log("[GameManager] Level completed!");
            
            ChangeState(GameState.Victory);
            
            int stars = CalculateStars();
            
            // Save progress
            if (Save.SaveManager.Instance != null)
            {
                Save.SaveManager.Instance.OnLevelComplete(currentLevel.levelId, stars);
            }
            
            OnLevelCompleted?.Invoke();
            
            // Show victory screen
            if (uiManager != null)
            {
                uiManager.ShowVictoryScreen(stars);
            }
        }
        
        /// <summary>
        /// Fails the current level.
        /// </summary>
        private void FailLevel()
        {
            Debug.Log("[GameManager] Level failed!");
            
            ChangeState(GameState.Defeat);
            
            OnLevelFailed?.Invoke();
            
            // Show defeat screen
            if (uiManager != null)
            {
                uiManager.ShowDefeatScreen();
            }
        }
        
        /// <summary>
        /// Calculates the star rating based on shots used.
        /// </summary>
        private int CalculateStars()
        {
            if (currentLevel == null) return 0;
            
            if (shotsUsed <= currentLevel.starThreshold3) return 3;
            if (shotsUsed <= currentLevel.starThreshold2) return 2;
            return 1;
        }
        
        /// <summary>
        /// Changes the current game state.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;
            
            Debug.Log($"[GameManager] State changing from {currentState} to {newState}");
            
            GameState previousState = currentState;
            currentState = newState;
            
            OnGameStateChanged?.Invoke(newState);
            
            // Handle state-specific logic
            OnStateChanged(previousState, newState);
        }
        
        private void OnStateChanged(GameState previousState, GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                    
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                    
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                    
                case GameState.Victory:
                case GameState.Defeat:
                    Time.timeScale = 0.5f; // Slow motion for dramatic effect
                    break;
            }
        }
        
        /// <summary>
        /// Pauses the game.
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }
        
        /// <summary>
        /// Resumes the game.
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }
        
        /// <summary>
        /// Restarts the current level.
        /// </summary>
        public void RestartLevel()
        {
            if (currentLevel != null)
            {
                StartLevel(currentLevel);
            }
        }
        
        /// <summary>
        /// Returns to the main menu.
        /// </summary>
        public void ReturnToMainMenu()
        {
            ChangeState(GameState.MainMenu);
            
            // Load main menu scene (would use SceneManager in Unity)
            Debug.Log("[GameManager] Returning to main menu");
        }
        
        /// <summary>
        /// Quits the application.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game...");
            
            // Save before quitting
            Save.SaveManager.Instance?.SaveGame();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        private void OnDestroy()
        {
            // Clean up
            if (Instance == this)
            {
                Save.SaveManager.Instance?.SaveGame();
            }
        }
    }
    
    /// <summary>
    /// Represents the possible states of the game.
    /// </summary>
    public enum GameState
    {
        Boot,           // Initial boot screen
        MainMenu,       // Main menu
        LevelSelect,    // Level/world map selection
        Playing,        // Active gameplay
        Paused,         // Game paused
        Victory,        // Level completed
        Defeat,         // Level failed
        Shop,           // In-game shop
        Settings        // Settings menu
    }
}
