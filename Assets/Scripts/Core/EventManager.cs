using UnityEngine;
using System;
using System.Collections.Generic;

namespace Siege.Core
{
    /// <summary>
    /// Central event manager for handling game-wide events.
    /// Uses a string-based event system for loose coupling between systems.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }
        
        private Dictionary<string, List<Action<object>>> eventDictionary;
        private Dictionary<string, List<Action>> eventDictionaryNoArgs;
        
        private bool isInitialized = false;
        
        public bool IsInitialized => isInitialized;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Initializes the event manager.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            
            eventDictionary = new Dictionary<string, List<Action<object>>>();
            eventDictionaryNoArgs = new Dictionary<string, List<Action>>();
            
            isInitialized = true;
            
            Debug.Log("[EventManager] Initialized.");
        }
        
        #region Event Registration
        
        /// <summary>
        /// Adds a listener for an event with arguments.
        /// </summary>
        public void AddListener(string eventName, Action<object> listener)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[EventManager] Not initialized. Call Initialize() first.");
                return;
            }
            
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[EventManager] Cannot add listener for null or empty event name.");
                return;
            }
            
            if (listener == null)
            {
                Debug.LogWarning($"[EventManager] Cannot add null listener for event: {eventName}");
                return;
            }
            
            if (!eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName] = new List<Action<object>>();
            }
            
            if (!eventDictionary[eventName].Contains(listener))
            {
                eventDictionary[eventName].Add(listener);
            }
        }
        
        /// <summary>
        /// Adds a listener for an event without arguments.
        /// </summary>
        public void AddListener(string eventName, Action listener)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[EventManager] Not initialized. Call Initialize() first.");
                return;
            }
            
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[EventManager] Cannot add listener for null or empty event name.");
                return;
            }
            
            if (listener == null)
            {
                Debug.LogWarning($"[EventManager] Cannot add null listener for event: {eventName}");
                return;
            }
            
            if (!eventDictionaryNoArgs.ContainsKey(eventName))
            {
                eventDictionaryNoArgs[eventName] = new List<Action>();
            }
            
            if (!eventDictionaryNoArgs[eventName].Contains(listener))
            {
                eventDictionaryNoArgs[eventName].Add(listener);
            }
        }
        
        /// <summary>
        /// Removes a listener from an event with arguments.
        /// </summary>
        public void RemoveListener(string eventName, Action<object> listener)
        {
            if (!isInitialized) return;
            
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName].Remove(listener);
                
                if (eventDictionary[eventName].Count == 0)
                {
                    eventDictionary.Remove(eventName);
                }
            }
        }
        
        /// <summary>
        /// Removes a listener from an event without arguments.
        /// </summary>
        public void RemoveListener(string eventName, Action listener)
        {
            if (!isInitialized) return;
            
            if (eventDictionaryNoArgs.ContainsKey(eventName))
            {
                eventDictionaryNoArgs[eventName].Remove(listener);
                
                if (eventDictionaryNoArgs[eventName].Count == 0)
                {
                    eventDictionaryNoArgs.Remove(eventName);
                }
            }
        }
        
        /// <summary>
        /// Removes all listeners for a specific event.
        /// </summary>
        public void RemoveAllListeners(string eventName)
        {
            if (!isInitialized) return;
            
            if (eventDictionary.ContainsKey(eventName))
            {
                eventDictionary.Remove(eventName);
            }
            
            if (eventDictionaryNoArgs.ContainsKey(eventName))
            {
                eventDictionaryNoArgs.Remove(eventName);
            }
        }
        
        #endregion
        
        #region Event Triggering
        
        /// <summary>
        /// Triggers an event with arguments.
        /// </summary>
        public void TriggerEvent(string eventName, object data = null)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[EventManager] Not initialized. Call Initialize() first.");
                return;
            }
            
            if (eventDictionary.ContainsKey(eventName))
            {
                // Create a copy to avoid issues if listeners modify the list during iteration
                List<Action<object>> listeners = new List<Action<object>>(eventDictionary[eventName]);
                
                foreach (Action<object> listener in listeners)
                {
                    try
                    {
                        listener?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventManager] Error in listener for event '{eventName}': {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Triggers an event without arguments.
        /// </summary>
        public void TriggerEvent(string eventName)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[EventManager] Not initialized. Call Initialize() first.");
                return;
            }
            
            if (eventDictionaryNoArgs.ContainsKey(eventName))
            {
                // Create a copy to avoid issues if listeners modify the list during iteration
                List<Action> listeners = new List<Action>(eventDictionaryNoArgs[eventName]);
                
                foreach (Action listener in listeners)
                {
                    try
                    {
                        listener?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventManager] Error in listener for event '{eventName}': {e.Message}");
                    }
                }
            }
        }
        
        #endregion
        
        #region Event Checking
        
        /// <summary>
        /// Checks if an event has any listeners.
        /// </summary>
        public bool HasListeners(string eventName)
        {
            if (!isInitialized) return false;
            
            return eventDictionary.ContainsKey(eventName) || eventDictionaryNoArgs.ContainsKey(eventName);
        }
        
        /// <summary>
        /// Gets the number of listeners for an event.
        /// </summary>
        public int GetListenerCount(string eventName)
        {
            if (!isInitialized) return 0;
            
            int count = 0;
            
            if (eventDictionary.ContainsKey(eventName))
            {
                count += eventDictionary[eventName].Count;
            }
            
            if (eventDictionaryNoArgs.ContainsKey(eventName))
            {
                count += eventDictionaryNoArgs[eventName].Count;
            }
            
            return count;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Logs all registered events for debugging.
        /// </summary>
        public void LogAllEvents()
        {
            if (!isInitialized)
            {
                Debug.Log("[EventManager] Not initialized.");
                return;
            }
            
            Debug.Log($"[EventManager] Registered events ({eventDictionary.Count + eventDictionaryNoArgs.Count}):");
            
            foreach (var kvp in eventDictionary)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value.Count} listener(s)");
            }
            
            foreach (var kvp in eventDictionaryNoArgs)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value.Count} listener(s)");
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                eventDictionary?.Clear();
                eventDictionaryNoArgs?.Clear();
            }
        }
    }
    
    #region Event Names Constants
    
    /// <summary>
    /// Centralized event name constants to avoid string typos.
    /// </summary>
    public static class GameEvents
    {
        // Game State Events
        public const string GAME_STARTED = "GameStarted";
        public const string GAME_PAUSED = "GamePaused";
        public const string GAME_RESUMED = "GameResumed";
        public const string GAME_OVER = "GameOver";
        
        // Level Events
        public const string LEVEL_STARTED = "LevelStarted";
        public const string LEVEL_COMPLETED = "LevelCompleted";
        public const string LEVEL_FAILED = "LevelFailed";
        public const string LEVEL_RESTARTED = "LevelRestarted";
        
        // Trebuchet Events
        public const string TREBUCHET_FIRED = "TrebuchetFired";
        public const string TREBUCHET_RESET = "TrebuchetReset";
        public const string PARAMETER_CHANGED = "ParameterChanged";
        
        // Projectile Events
        public const string PROJECTILE_SPAWNED = "ProjectileSpawned";
        public const string PROJECTILE_IMPACT = "ProjectileImpact";
        public const string PROJECTILE_DESTROYED = "ProjectileDestroyed";
        
        // Destruction Events
        public const string STRUCTURE_DAMAGED = "StructureDamaged";
        public const string STRUCTURE_DESTROYED = "StructureDestroyed";
        public const string CHAIN_REACTION = "ChainReaction";
        
        // UI Events
        public const string UI_OPENED = "UIOpened";
        public const string UI_CLOSED = "UIClosed";
        public const string BUTTON_CLICKED = "ButtonClicked";
        
        // Progression Events
        public const string UNLOCK_ACHIEVED = "UnlockAchieved";
        public const string STARS_EARNED = "StarsEarned";
        public const string CURRENCY_CHANGED = "CurrencyChanged";
        
        // Audio Events
        public const string PLAY_SOUND = "PlaySound";
        public const string STOP_SOUND = "StopSound";
        public const string PLAY_MUSIC = "PlayMusic";
        public const string STOP_MUSIC = "StopMusic";
        
        // Settings Events
        public const string SETTINGS_CHANGED = "SettingsChanged";
        public const string VOLUME_CHANGED = "VolumeChanged";
    }
    
    #endregion
}
