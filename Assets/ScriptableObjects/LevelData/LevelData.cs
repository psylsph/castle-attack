using UnityEngine;
using System.Collections.Generic;

namespace Siege.Level
{
    /// <summary>
    /// ScriptableObject defining level data including castle configuration, environment, and goals.
    /// Used by LevelManager to load and manage levels.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "Siege/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Identification")]
        [SerializeField] private string _levelId = "level_001";
        [SerializeField] private string _levelName = "Level 1";
        [TextArea(2, 3)]
        [SerializeField] private string _levelDescription = "Destroy the castle!";
        [SerializeField] private int _regionIndex = 0;
        [SerializeField] private int _levelIndex = 0;
        
        [Header("Level Goal")]
        [SerializeField] private LevelGoalType _goalType = LevelGoalType.DestroyKeep;
        [TextArea(2, 3)]
        [SerializeField] private string _goalDescription = "Destroy the keep to complete the level.";
        [SerializeField] private float _goalCompletionThreshold = 0.9f;
        
        [Header("Castle Configuration")]
        [SerializeField] private List<StructureBlueprint> _castleBlueprints = new List<StructureBlueprint>();
        [SerializeField] private Vector2 _castlePosition = Vector2.zero;
        
        [Header("Environment")]
        [SerializeField] private EnvironmentVariables _environment = new EnvironmentVariables();
        
        [Header("Ammunition")]
        [SerializeField] private List<AmmunitionAllocation> _ammunition = new List<AmmunitionAllocation>();
        [SerializeField] private int _maxShots = 10;
        
        [Header("Difficulty Settings")]
        [SerializeField] private bool _trajectoryPreviewEnabled = true;
        [SerializeField] private int _starThreshold1 = 5;
        [SerializeField] private int _starThreshold2 = 3;
        [SerializeField] private int _starThreshold3 = 1;
        
        [Header("Special Settings")]
        [SerializeField] private bool _isChallengeLevel = false;
        [SerializeField] private bool _isBonusLevel = false;
        [SerializeField] private string _requiredUnlockId = "";
        
        // Public properties
        public string levelId => _levelId;
        public string levelName => _levelName;
        public string levelDescription => _levelDescription;
        public int regionIndex => _regionIndex;
        public int levelIndex => _levelIndex;
        public LevelGoalType goalType => _goalType;
        public string goalDescription => _goalDescription;
        public float goalCompletionThreshold => _goalCompletionThreshold;
        public List<StructureBlueprint> castleBlueprints => _castleBlueprints;
        public Vector2 castlePosition => _castlePosition;
        public EnvironmentVariables environment => _environment;
        public List<AmmunitionAllocation> ammunition => _ammunition;
        public int maxShots => _maxShots;
        public bool trajectoryPreviewEnabled => _trajectoryPreviewEnabled;
        public int starThreshold1 => _starThreshold1;
        public int starThreshold2 => _starThreshold2;
        public int starThreshold3 => _starThreshold3;
        public bool isChallengeLevel => _isChallengeLevel;
        public bool isBonusLevel => _isBonusLevel;
        public string requiredUnlockId => _requiredUnlockId;
        
        /// <summary>
        /// Gets the star threshold for a given star count.
        /// </summary>
        public int GetStarThreshold(int stars)
        {
            switch (stars)
            {
                case 1: return _starThreshold1;
                case 2: return _starThreshold2;
                case 3: return _starThreshold3;
                default: return _maxShots;
            }
        }
        
        /// <summary>
        /// Calculates stars based on shots used.
        /// </summary>
        public int CalculateStars(int shotsUsed)
        {
            if (shotsUsed <= _starThreshold3) return 3;
            if (shotsUsed <= _starThreshold2) return 2;
            if (shotsUsed <= _starThreshold1) return 1;
            return 0;
        }
        
        /// <summary>
        /// Gets the ammunition count for a specific type.
        /// </summary>
        public int GetAmmunitionCount(Ammunition.AmmunitionType type)
        {
            foreach (AmmunitionAllocation allocation in _ammunition)
            {
                if (allocation.type == type)
                {
                    return allocation.quantity;
                }
            }
            return 0;
        }
        
        /// <summary>
        /// Checks if the level is unlocked based on player progress.
        /// </summary>
        public bool IsUnlocked()
        {
            if (string.IsNullOrEmpty(_requiredUnlockId))
            {
                return true;
            }
            
            return Save.SaveManager.Instance?.IsLevelCompleted(_requiredUnlockId) ?? false;
        }
        
        /// <summary>
        /// Validates the level data.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(_levelId))
            {
                Debug.LogWarning($"[LevelData] {_levelName} has invalid level ID");
                return false;
            }
            
            if (_castleBlueprints.Count == 0)
            {
                Debug.LogWarning($"[LevelData] {_levelName} has no castle blueprints");
                return false;
            }
            
            if (_maxShots <= 0)
            {
                Debug.LogWarning($"[LevelData] {_levelName} has invalid max shots: {_maxShots}");
                return false;
            }
            
            if (_starThreshold3 > _starThreshold2 || _starThreshold2 > _starThreshold1)
            {
                Debug.LogWarning($"[LevelData] {_levelName} has invalid star thresholds");
                return false;
            }
            
            return true;
        }
        
        private void OnValidate()
        {
            // Ensure valid ranges
            _maxShots = Mathf.Max(1, _maxShots);
            _starThreshold1 = Mathf.Clamp(_starThreshold1, 1, _maxShots);
            _starThreshold2 = Mathf.Clamp(_starThreshold2, 1, _starThreshold1);
            _starThreshold3 = Mathf.Clamp(_starThreshold3, 1, _starThreshold2);
            _goalCompletionThreshold = Mathf.Clamp01(_goalCompletionThreshold);
            _regionIndex = Mathf.Max(0, _regionIndex);
            _levelIndex = Mathf.Max(0, _levelIndex);
        }
    }
    
    /// <summary>
    /// Blueprint for a single structure component.
    /// </summary>
    [System.Serializable]
    public class StructureBlueprint
    {
        public string componentId = "structure_001";
        public Vector2 position = Vector2.zero;
        public float rotation = 0f;
        public Vector2 scale = Vector2.one;
        public Castle.MaterialType materialType = Castle.MaterialType.Stone;
        public bool isKeep = false;
        public bool isTargetBanner = false;
        public bool isWeakPoint = false;
        public List<string> connectedTo = new List<string>();
        public string prefabPath = "";
    }
    
    /// <summary>
    /// Ammunition allocation for a level.
    /// </summary>
    [System.Serializable]
    public class AmmunitionAllocation
    {
        public Ammunition.AmmunitionType type = Ammunition.AmmunitionType.Stone;
        public int quantity = 10;
    }
    
    /// <summary>
    /// Environmental variables affecting gameplay.
    /// </summary>
    [System.Serializable]
    public class EnvironmentVariables
    {
        [Header("Wind")]
        public Vector2 windDirection = Vector2.zero;
        [Range(0f, 10f)]
        public float windStrength = 0f;
        
        [Header("Elevation")]
        [Range(-10f, 10f)]
        public float elevationModifier = 0f;
        
        [Header("Obstacles")]
        public List<ObstacleData> obstacles = new List<ObstacleData>();
        
        [Header("Visuals")]
        public BackgroundTheme backgroundTheme = BackgroundTheme.Plains;
        public Color skyColor = Color.white;
        public Color groundColor = Color.green;
    }
    
    /// <summary>
    /// Data for an obstacle in the level.
    /// </summary>
    [System.Serializable]
    public class ObstacleData
    {
        public string obstacleId = "obstacle_001";
        public Vector2 position = Vector2.zero;
        public ObstacleType type = ObstacleType.Hill;
        public float scale = 1f;
        public float rotation = 0f;
    }
    
    /// <summary>
    /// Types of level goals.
    /// </summary>
    public enum LevelGoalType
    {
        DestroyKeep,        // Eliminate the main castle keep
        EliminateBanner,    // Destroy specific target banner
        CollapseStructure,   // Bring down designated structure
        DefeatDefenders     // Indirect elimination via destruction
    }
    
    /// <summary>
    /// Types of obstacles.
    /// </summary>
    public enum ObstacleType
    {
        Hill,
        Wall,
        Moat,
        Tree,
        Rock
    }
    
    /// <summary>
    /// Background themes for levels.
    /// </summary>
    public enum BackgroundTheme
    {
        Plains,
        Mountains,
        Coastal,
        Desert,
        Forest,
        Snow
    }
}
