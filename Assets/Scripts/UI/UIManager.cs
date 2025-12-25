using UnityEngine;
using UnityEngine.UI;

namespace Siege.UI
{
    /// <summary>
    /// Manages all UI elements including HUD, menus, and touch controls.
    /// Coordinates with GameManager for UI updates.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        
        [Header("HUD Elements")]
        [SerializeField] private Text _shotsText;
        [SerializeField] private Text _goalText;
        [SerializeField] private Slider _powerSlider;
        [SerializeField] private Slider _angleSlider;
        [SerializeField] private Text _powerValueText;
        [SerializeField] private Text _angleValueText;
        [SerializeField] private Image _star1;
        [SerializeField] private Image _star2;
        [SerializeField] private Image _star3;
        
        [Header("Ammunition Selector")]
        [SerializeField] private AmmunitionSelector _ammunitionSelector;
        
        [Header("Menus")]
        [SerializeField] private GameObject _pauseMenu;
        [SerializeField] private GameObject _victoryScreen;
        [SerializeField] private GameObject _defeatScreen;
        [SerializeField] private GameObject _settingsMenu;
        
        [Header("Buttons")]
        [SerializeField] private Button _fireButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _menuButton;
        
        [Header("Touch Controls")]
        [SerializeField] private TouchControls _touchControls;
        [SerializeField] private CameraController _cameraController;
        
        [Header("Settings")]
        [SerializeField] private Toggle _aimAssistToggle;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        
        private bool _isPaused = false;
        
        // Events
        public event System.Action OnFireButtonPressed;
        public event System.Action OnPauseButtonPressed;
        public event System.Action OnRestartButtonPressed;
        public event System.Action OnNextLevelButtonPressed;
        public event System.Action OnRetryButtonPressed;
        public event System.Action OnMenuButtonPressed;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeUI();
            SetupButtonListeners();
            HideAllMenus();
        }
        
        private void InitializeUI()
        {
            // Initialize touch controls
            if (_touchControls != null)
            {
                _touchControls.Initialize();
            }
            
            // Initialize camera controller
            if (_cameraController != null)
            {
                _cameraController.Initialize();
            }
            
            // Initialize ammunition selector
            if (_ammunitionSelector != null)
            {
                _ammunitionSelector.Initialize();
            }
            
            // Load settings
            LoadSettings();
        }
        
        private void SetupButtonListeners()
        {
            if (_fireButton != null)
            {
                _fireButton.onClick.AddListener(OnFireClicked);
            }
            
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(OnPauseClicked);
            }
            
            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
            
            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            }
            
            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(OnRetryClicked);
            }
            
            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(OnMenuClicked);
            }
            
            if (_aimAssistToggle != null)
            {
                _aimAssistToggle.onValueChanged.AddListener(OnAimAssistToggled);
            }
            
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }
        
        #region HUD Updates
        
        /// <summary>
        /// Updates the HUD with current game state.
        /// </summary>
        public void UpdateHUD()
        {
            if (Core.GameManager.Instance == null) return;
            
            UpdateShotsDisplay();
            UpdateGoalDisplay();
            UpdateParameterDisplays();
            UpdateStarDisplay();
        }
        
        /// <summary>
        /// Updates the shots display.
        /// </summary>
        public void UpdateShotsDisplay()
        {
            if (_shotsText != null && Core.GameManager.Instance != null)
            {
                _shotsText.text = $"Shots: {Core.GameManager.Instance.ShotsUsed} / {Core.GameManager.Instance.CurrentLevel?.maxShots ?? 0}";
            }
        }
        
        /// <summary>
        /// Updates the goal display.
        /// </summary>
        public void UpdateGoalDisplay()
        {
            if (_goalText != null && Core.GameManager.Instance?.CurrentLevel != null)
            {
                _goalText.text = Core.GameManager.Instance.CurrentLevel.goalDescription;
            }
        }
        
        /// <summary>
        /// Updates parameter displays (power, angle).
        /// </summary>
        public void UpdateParameterDisplays()
        {
            if (_powerSlider != null && _powerValueText != null && Trebuchet.TrebuchetController.Instance != null)
            {
                float power = Trebuchet.TrebuchetController.Instance.parameters.armPullbackStrength;
                _powerSlider.value = power;
                _powerValueText.text = $"{power:F0}%";
            }
            
            if (_angleSlider != null && _angleValueText != null && Trebuchet.TrebuchetController.Instance != null)
            {
                float angle = Trebuchet.TrebuchetController.Instance.parameters.releaseAngle;
                _angleSlider.value = angle;
                _angleValueText.text = $"{angle:F0}°";
            }
        }
        
        /// <summary>
        /// Updates star display based on progress.
        /// </summary>
        public void UpdateStarDisplay()
        {
            if (Core.GameManager.Instance == null) return;
            
            int currentStars = CalculateCurrentStars();
            SetStarState(_star1, currentStars >= 1);
            SetStarState(_star2, currentStars >= 2);
            SetStarState(_star3, currentStars >= 3);
        }
        
        private void SetStarState(Image starImage, bool active)
        {
            if (starImage != null)
            {
                starImage.color = active ? Color.yellow : Color.gray;
            }
        }
        
        private int CalculateCurrentStars()
        {
            if (Core.GameManager.Instance?.CurrentLevel == null) return 0;
            
            return Core.GameManager.Instance.CurrentLevel.CalculateStars(Core.GameManager.Instance.ShotsUsed);
        }
        
        #endregion
        
        #region Menu Management
        
        /// <summary>
        /// Shows the pause menu.
        /// </summary>
        public void ShowPauseMenu()
        {
            if (_pauseMenu != null)
            {
                _pauseMenu.SetActive(true);
                _isPaused = true;
                Core.GameManager.Instance?.PauseGame();
            }
        }
        
        /// <summary>
        /// Hides the pause menu.
        /// </summary>
        public void HidePauseMenu()
        {
            if (_pauseMenu != null)
            {
                _pauseMenu.SetActive(false);
                _isPaused = false;
                Core.GameManager.Instance?.ResumeGame();
            }
        }
        
        /// <summary>
        /// Shows the victory screen.
        /// </summary>
        public void ShowVictoryScreen(int stars)
        {
            if (_victoryScreen != null)
            {
                _victoryScreen.SetActive(true);
                
                // Update star display
                SetStarState(_star1, stars >= 1);
                SetStarState(_star2, stars >= 2);
                SetStarState(_star3, stars >= 3);
            }
        }
        
        /// <summary>
        /// Shows the defeat screen.
        /// </summary>
        public void ShowDefeatScreen()
        {
            if (_defeatScreen != null)
            {
                _defeatScreen.SetActive(true);
            }
        }
        
        /// <summary>
        /// Shows the settings menu.
        /// </summary>
        public void ShowSettingsMenu()
        {
            if (_settingsMenu != null)
            {
                _settingsMenu.SetActive(true);
            }
        }
        
        /// <summary>
        /// Hides the settings menu.
        /// </summary>
        public void HideSettingsMenu()
        {
            if (_settingsMenu != null)
            {
                _settingsMenu.SetActive(false);
            }
        }
        
        /// <summary>
        /// Hides all menus.
        /// </summary>
        public void HideAllMenus()
        {
            if (_pauseMenu != null) _pauseMenu.SetActive(false);
            if (_victoryScreen != null) _victoryScreen.SetActive(false);
            if (_defeatScreen != null) _defeatScreen.SetActive(false);
            if (_settingsMenu != null) _settingsMenu.SetActive(false);
            _isPaused = false;
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnFireClicked()
        {
            OnFireButtonPressed?.Invoke();
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.BUTTON_CLICKED, "Fire");
        }
        
        private void OnPauseClicked()
        {
            if (_isPaused)
            {
                HidePauseMenu();
            }
            else
            {
                ShowPauseMenu();
            }
            OnPauseButtonPressed?.Invoke();
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.BUTTON_CLICKED, "Pause");
        }
        
        private void OnRestartClicked()
        {
            HideAllMenus();
            OnRestartButtonPressed?.Invoke();
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.BUTTON_CLICKED, "Restart");
        }
        
        private void OnNextLevelClicked()
        {
            HideAllMenus();
            OnNextLevelButtonPressed?.Invoke();
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.BUTTON_CLICKED, "NextLevel");
        }
        
        private void OnRetryClicked()
        {
            HideAllMenus();
            OnRetryButtonPressed?.Invoke();
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.BUTTON_CLICKED, "Retry");
        }
        
        private void OnMenuClicked()
        {
            HideAllMenus();
            OnMenuButtonPressed?.Invoke();
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.BUTTON_CLICKED, "Menu");
        }
        
        #endregion
        
        #region Settings
        
        private void OnAimAssistToggled(bool enabled)
        {
            if (Trebuchet.TrebuchetController.Instance != null)
            {
                Trebuchet.TrebuchetController.Instance.ghostArcEnabled = enabled;
            }
            
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.SETTINGS_CHANGED, "AimAssist");
        }
        
        private void OnMasterVolumeChanged(float value)
        {
            Save.SaveManager.Instance?.SetMasterVolume(value);
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.VOLUME_CHANGED, "Master");
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            Save.SaveManager.Instance?.SetMusicVolume(value);
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.VOLUME_CHANGED, "Music");
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            Save.SaveManager.Instance?.SetSFXVolume(value);
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.VOLUME_CHANGED, "SFX");
        }
        
        private void LoadSettings()
        {
            Save.SettingsState settings = Save.SaveManager.Instance?.SettingsData;
            
            if (settings != null)
            {
                if (_aimAssistToggle != null)
                {
                    _aimAssistToggle.isOn = !settings.simplifiedPhysics;
                }
                
                if (_masterVolumeSlider != null)
                {
                    _masterVolumeSlider.value = settings.masterVolume;
                }
                
                if (_musicVolumeSlider != null)
                {
                    _musicVolumeSlider.value = settings.musicVolume;
                }
                
                if (_sfxVolumeSlider != null)
                {
                    _sfxVolumeSlider.value = settings.sfxVolume;
                }
            }
        }
        
        #endregion
        
        #region Ammunition
        
        /// <summary>
        /// Updates the ammunition selector with available ammo.
        /// </summary>
        public void UpdateAmmunitionSelector(Level.LevelData level)
        {
            if (_ammunitionSelector != null && level != null)
            {
                _ammunitionSelector.LoadAmmunition(level.ammunition);
            }
        }
        
        /// <summary>
        /// Gets the currently selected ammunition type.
        /// </summary>
        public Ammunition.AmmunitionType GetSelectedAmmunition()
        {
            return _ammunitionSelector?.GetSelectedType() ?? Ammunition.AmmunitionType.Stone;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Enables or disables touch controls.
        /// </summary>
        public void SetTouchControlsEnabled(bool enabled)
        {
            if (_touchControls != null)
            {
                _touchControls.SetEnabled(enabled);
            }
        }
        
        /// <summary>
        /// Enables or disables the fire button.
        /// </summary>
        public void SetFireButtonEnabled(bool enabled)
        {
            if (_fireButton != null)
            {
                _fireButton.interactable = enabled;
            }
        }
        
        #endregion
    }
}
