using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace Siege.Save
{
    /// <summary>
    /// Manages saving and loading of player progress and game settings.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        
        [Header("Save Settings")]
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float autoSaveInterval = 30f;
        
        private const string SAVE_KEY = "Siege_SaveData_v1";
        private const string SETTINGS_KEY = "Siege_Settings_v1";
        private const string LEVEL_STATE_KEY = "Siege_LevelState_v1";
        
        private PlayerProgress saveData;
        private SettingsState settingsData;
        private LevelState levelState;
        
        private float autoSaveTimer;
        
        public PlayerProgress SaveData => saveData;
        public SettingsState SettingsData => settingsData;
        public LevelState LevelState => levelState;
        
        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveData();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSaveData()
        {
            saveData = new PlayerProgress();
            settingsData = new SettingsState();
            levelState = new LevelState();
            
            LoadGame();
        }
        
        private void Update()
        {
            if (autoSaveEnabled)
            {
                autoSaveTimer += Time.deltaTime;
                if (autoSaveTimer >= autoSaveInterval)
                {
                    SaveGame();
                    autoSaveTimer = 0f;
                }
            }
        }
        
        #region Save/Load Operations
        
        /// <summary>
        /// Saves all game data to persistent storage.
        /// </summary>
        public void SaveGame()
        {
            try
            {
                // Save player progress
                string progressJson = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(SAVE_KEY, progressJson);
                
                // Save settings
                string settingsJson = JsonUtility.ToJson(settingsData, true);
                PlayerPrefs.SetString(SETTINGS_KEY, settingsJson);
                
                // Save level state
                string levelJson = JsonUtility.ToJson(levelState, true);
                PlayerPrefs.SetString(LEVEL_STATE_KEY, levelJson);
                
                PlayerPrefs.Save();
                
                Debug.Log("[SaveManager] Game saved successfully!");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save game: {e.Message}");
            }
        }
        
        /// <summary>
        /// Loads all game data from persistent storage.
        /// </summary>
        public void LoadGame()
        {
            try
            {
                // Load player progress
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string progressJson = PlayerPrefs.GetString(SAVE_KEY);
                    saveData = JsonUtility.FromJson<PlayerProgress>(progressJson);
                    Debug.Log("[SaveManager] Player progress loaded.");
                }
                else
                {
                    Debug.Log("[SaveManager] No save data found, using defaults.");
                }
                
                // Load settings
                if (PlayerPrefs.HasKey(SETTINGS_KEY))
                {
                    string settingsJson = PlayerPrefs.GetString(SETTINGS_KEY);
                    settingsData = JsonUtility.FromJson<SettingsState>(settingsJson);
                    Debug.Log("[SaveManager] Settings loaded.");
                }
                else
                {
                    Debug.Log("[SaveManager] No settings found, using defaults.");
                }
                
                // Load level state
                if (PlayerPrefs.HasKey(LEVEL_STATE_KEY))
                {
                    string levelJson = PlayerPrefs.GetString(LEVEL_STATE_KEY);
                    levelState = JsonUtility.FromJson<LevelState>(levelJson);
                    Debug.Log("[SaveManager] Level state loaded.");
                }
                
                ApplySettings();
                OnLoadCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load game: {e.Message}");
            }
        }
        
        /// <summary>
        /// Deletes all save data.
        /// </summary>
        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.DeleteKey(SETTINGS_KEY);
            PlayerPrefs.DeleteKey(LEVEL_STATE_KEY);
            PlayerPrefs.Save();
            
            Debug.Log("[SaveManager] Save data deleted.");
        }
        
        /// <summary>
        /// Resets player progress to default values.
        /// </summary>
        public void ResetProgress()
        {
            saveData = new PlayerProgress();
            SaveGame();
            
            Debug.Log("[SaveManager] Progress reset.");
        }
        
        /// <summary>
        /// Checks if save data exists.
        /// </summary>
        public bool HasSaveData()
        {
            return PlayerPrefs.HasKey(SAVE_KEY);
        }
        
        #endregion
        
        #region Level Completion
        
        /// <summary>
        /// Called when a level is completed.
        /// </summary>
        public void OnLevelComplete(string levelId, int stars)
        {
            if (string.IsNullOrEmpty(levelId) || stars < 1 || stars > 3) return;
            
            LevelCompletion existingCompletion = saveData.completedLevels.Find(l => l.levelId == levelId);
            
            if (existingCompletion != null)
            {
                // Update if new stars are better
                if (stars > existingCompletion.stars)
                {
                    existingCompletion.stars = stars;
                    existingCompletion.completionTime = DateTime.Now;
                    Debug.Log($"[SaveManager] Updated {levelId} to {stars} stars.");
                }
            }
            else
            {
                // Add new completion
                LevelCompletion newCompletion = new LevelCompletion
                {
                    levelId = levelId,
                    stars = stars,
                    shotsUsed = Core.GameManager.Instance?.ShotsUsed ?? 0,
                    isCompleted = true,
                    completionTime = DateTime.Now
                };
                
                saveData.completedLevels.Add(newCompletion);
                
                // Update total stars
                saveData.totalStars += stars;
                
                Debug.Log($"[SaveManager] Completed {levelId} with {stars} stars.");
            }
            
            SaveGame();
        }
        
        /// <summary>
        /// Gets the star rating for a specific level.
        /// </summary>
        public int GetLevelStars(string levelId)
        {
            LevelCompletion completion = saveData.completedLevels.Find(l => l.levelId == levelId);
            return completion?.stars ?? 0;
        }
        
        /// <summary>
        /// Checks if a level is completed.
        /// </summary>
        public bool IsLevelCompleted(string levelId)
        {
            LevelCompletion completion = saveData.completedLevels.Find(l => l.levelId == levelId);
            return completion?.isCompleted ?? false;
        }
        
        #endregion
        
        #region Unlock System
        
        /// <summary>
        /// Unlocks an ammunition type.
        /// </summary>
        public void UnlockAmmunition(Ammunition.AmmunitionType ammoType)
        {
            if (!saveData.unlockedAmmo.Contains(ammoType))
            {
                saveData.unlockedAmmo.Add(ammoType);
                SaveGame();
                Debug.Log($"[SaveManager] Unlocked ammunition: {ammoType}");
            }
        }
        
        /// <summary>
        /// Checks if an ammunition type is unlocked.
        /// </summary>
        public bool IsAmmunitionUnlocked(Ammunition.AmmunitionType ammoType)
        {
            return saveData.unlockedAmmo.Contains(ammoType);
        }
        
        /// <summary>
        /// Unlocks a trebuchet upgrade.
        /// </summary>
        public void UnlockUpgrade(string upgradeId)
        {
            if (!saveData.unlockedUpgrades.Contains(upgradeId))
            {
                saveData.unlockedUpgrades.Add(upgradeId);
                SaveGame();
                Debug.Log($"[SaveManager] Unlocked upgrade: {upgradeId}");
            }
        }
        
        /// <summary>
        /// Checks if an upgrade is unlocked.
        /// </summary>
        public bool IsUpgradeUnlocked(string upgradeId)
        {
            return saveData.unlockedUpgrades.Contains(upgradeId);
        }
        
        /// <summary>
        /// Unlocks a cosmetic.
        /// </summary>
        public void UnlockCosmetic(string cosmeticId)
        {
            if (!saveData.ownedCosmetics.Contains(cosmeticId))
            {
                saveData.ownedCosmetics.Add(cosmeticId);
                SaveGame();
                Debug.Log($"[SaveManager] Unlocked cosmetic: {cosmeticId}");
            }
        }
        
        /// <summary>
        /// Equips a cosmetic.
        /// </summary>
        public void EquipCosmetic(string cosmeticId)
        {
            if (saveData.ownedCosmetics.Contains(cosmeticId))
            {
                saveData.equippedCosmetic = cosmeticId;
                SaveGame();
                Debug.Log($"[SaveManager] Equipped cosmetic: {cosmeticId}");
            }
        }
        
        /// <summary>
        /// Gets the currently equipped cosmetic.
        /// </summary>
        public string GetEquippedCosmetic()
        {
            return saveData.equippedCosmetic;
        }
        
        #endregion
        
        #region Currency System
        
        /// <summary>
        /// Adds currency to the player's balance.
        /// </summary>
        public void AddCurrency(int amount)
        {
            if (amount > 0)
            {
                saveData.currency += amount;
                SaveGame();
                Debug.Log($"[SaveManager] Added {amount} currency. Total: {saveData.currency}");
            }
        }
        
        /// <summary>
        /// Removes currency from the player's balance.
        /// </summary>
        public bool SpendCurrency(int amount)
        {
            if (amount > 0 && saveData.currency >= amount)
            {
                saveData.currency -= amount;
                SaveGame();
                Debug.Log($"[SaveManager] Spent {amount} currency. Remaining: {saveData.currency}");
                return true;
            }
            
            Debug.LogWarning($"[SaveManager] Cannot spend {amount} currency. Insufficient funds.");
            return false;
        }
        
        /// <summary>
        /// Gets the current currency balance.
        /// </summary>
        public int GetCurrency()
        {
            return saveData.currency;
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Applies the loaded settings to the game.
        /// </summary>
        private void ApplySettings()
        {
            AudioListener.volume = settingsData.masterVolume;
            QualitySettings.SetQualityLevel(settingsData.qualityLevel);
            
            // Apply other settings as needed
            Debug.Log("[SaveManager] Settings applied.");
        }
        
        /// <summary>
        /// Sets the master volume.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            settingsData.masterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = settingsData.masterVolume;
            SaveGame();
        }
        
        /// <summary>
        /// Sets the music volume.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            settingsData.musicVolume = Mathf.Clamp01(volume);
            SaveGame();
        }
        
        /// <summary>
        /// Sets the SFX volume.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            settingsData.sfxVolume = Mathf.Clamp01(volume);
            SaveGame();
        }
        
        /// <summary>
        /// Sets the quality level.
        /// </summary>
        public void SetQualityLevel(int level)
        {
            settingsData.qualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(settingsData.qualityLevel);
            SaveGame();
        }
        
        /// <summary>
        /// Toggles colorblind mode.
        /// </summary>
        public void ToggleColorblindMode()
        {
            settingsData.colorblindMode = !settingsData.colorblindMode;
            SaveGame();
        }
        
        /// <summary>
        /// Sets the text size.
        /// </summary>
        public void SetTextSize(TextSize size)
        {
            settingsData.textSize = size;
            SaveGame();
        }
        
        /// <summary>
        /// Toggles simplified physics mode.
        /// </summary>
        public void ToggleSimplifiedPhysics()
        {
            settingsData.simplifiedPhysics = !settingsData.simplifiedPhysics;
            SaveGame();
        }
        
        #endregion
        
        #region Level State
        
        /// <summary>
        /// Saves the current level state for pause/resume.
        /// </summary>
        public void SaveLevelState(string levelId, int shotsUsed, int shotsRemaining)
        {
            levelState.currentLevelId = levelId;
            levelState.shotsUsed = shotsUsed;
            levelState.shotsRemaining = shotsRemaining;
            levelState.lastSaveTime = DateTime.Now;
            
            SaveGame();
        }
        
        /// <summary>
        /// Clears the current level state.
        /// </summary>
        public void ClearLevelState()
        {
            levelState = new LevelState();
            SaveGame();
        }
        
        #endregion
    }
    
    #region Data Classes
    
    [Serializable]
    public class PlayerProgress
    {
        public int currentRegion = 0;
        public List<LevelCompletion> completedLevels = new List<LevelCompletion>();
        public List<Ammunition.AmmunitionType> unlockedAmmo = new List<Ammunition.AmmunitionType>
        {
            Ammunition.AmmunitionType.Stone // Default unlocked ammo
        };
        public List<string> unlockedUpgrades = new List<string>();
        public List<string> ownedCosmetics = new List<string>();
        public string equippedCosmetic = "default";
        public int totalStars = 0;
        public int currency = 0;
        public DateTime lastPlayTime = DateTime.Now;
    }
    
    [Serializable]
    public class LevelCompletion
    {
        public string levelId;
        public int stars;
        public int shotsUsed;
        public bool isCompleted;
        public string completionTime; // Serialized as string
    }
    
    [Serializable]
    public class SettingsState
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public int qualityLevel = 1;
        public bool colorblindMode = false;
        public TextSize textSize = TextSize.Medium;
        public bool simplifiedPhysics = false;
        public bool oneHandMode = true;
        public bool hapticFeedback = true;
    }
    
    [Serializable]
    public class LevelState
    {
        public string currentLevelId;
        public int shotsUsed;
        public int shotsRemaining;
        public string lastSaveTime;
    }
    
    public enum TextSize
    {
        Small,
        Medium,
        Large
    }
    
    #endregion
}
