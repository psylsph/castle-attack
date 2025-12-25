using UnityEngine;
using System.Collections.Generic;

namespace Siege.Level
{
    /// <summary>
    /// Manages level loading, goal tracking, and castle building.
    /// Coordinates with GameManager for level flow.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }
        
        [Header("References")]
        [SerializeField] private Transform _castleParent;
        [SerializeField] private Transform _obstacleParent;
        [SerializeField] private Castle.CastleBuilder _castleBuilder;
        
        [Header("Settings")]
        [SerializeField] private bool _debugMode = false;
        
        // Runtime data
        private LevelData _currentLevel;
        private List<Castle.StructureComponent> _allStructures = new List<Castle.StructureComponent>();
        private List<Castle.StructureComponent> _goalTargets = new List<Castle.StructureComponent>();
        private bool _isGoalComplete = false;
        
        // Events
        public event System.Action OnLevelLoaded;
        public event System.Action<bool> OnGoalCompleted;
        public event System.Action<Castle.StructureComponent> OnStructureSpawned;
        
        // Public properties
        public LevelData currentLevel => _currentLevel;
        public List<Castle.StructureComponent> allStructures => _allStructures;
        public bool isGoalComplete => _isGoalComplete;
        
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
            InitializeParents();
        }
        
        private void InitializeParents()
        {
            // Create parent objects if not assigned
            if (_castleParent == null)
            {
                GameObject castleParentObj = new GameObject("Castle");
                _castleParent = castleParentObj.transform;
            }
            
            if (_obstacleParent == null)
            {
                GameObject obstacleParentObj = new GameObject("Obstacles");
                _obstacleParent = obstacleParentObj.transform;
            }
        }
        
        #region Level Loading
        
        /// <summary>
        /// Loads a level from level data.
        /// </summary>
        public void LoadLevel(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError("[LevelManager] Cannot load null level!");
                return;
            }
            
            if (!level.IsValid())
            {
                Debug.LogError($"[LevelManager] Level data is invalid: {level.levelName}");
                return;
            }
            
            Debug.Log($"[LevelManager] Loading level: {level.levelName}");
            
            // Clear existing level
            ClearLevel();
            
            // Set current level
            _currentLevel = level;
            _isGoalComplete = false;
            
            // Build castle
            if (_castleBuilder != null)
            {
                _castleBuilder.BuildCastle(level.castleBlueprints, level.castlePosition);
            }
            else
            {
                BuildCastleFromBlueprints(level.castleBlueprints, level.castlePosition);
            }
            
            // Spawn obstacles
            SpawnObstacles(level.environment.obstacles);
            
            // Apply environment settings
            ApplyEnvironmentSettings(level.environment);
            
            // Find goal targets
            FindGoalTargets();
            
            // Trigger event
            OnLevelLoaded?.Invoke();
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.LEVEL_STARTED, level);
            
            if (_debugMode)
            {
                Debug.Log($"[LevelManager] Level loaded. Structures: {_allStructures.Count}, Goal targets: {_goalTargets.Count}");
            }
        }
        
        /// <summary>
        /// Clears the current level.
        /// </summary>
        public void ClearLevel()
        {
            Debug.Log("[LevelManager] Clearing level...");
            
            // Destroy all structures
            foreach (Castle.StructureComponent structure in _allStructures)
            {
                if (structure != null)
                {
                    Destroy(structure.gameObject);
                }
            }
            
            // Destroy all obstacles
            foreach (Transform child in _obstacleParent)
            {
                Destroy(child.gameObject);
            }
            
            // Clear lists
            _allStructures.Clear();
            _goalTargets.Clear();
            
            _currentLevel = null;
            _isGoalComplete = false;
        }
        
        /// <summary>
        /// Builds castle from blueprints (fallback if CastleBuilder not available).
        /// </summary>
        private void BuildCastleFromBlueprints(List<StructureBlueprint> blueprints, Vector2 position)
        {
            Dictionary<string, Castle.StructureComponent> structureMap = new Dictionary<string, Castle.StructureComponent>();
            
            // First pass: spawn all structures
            foreach (StructureBlueprint blueprint in blueprints)
            {
                Castle.StructureComponent structure = SpawnStructure(blueprint, position);
                if (structure != null)
                {
                    structureMap[blueprint.componentId] = structure;
                    _allStructures.Add(structure);
                    OnStructureSpawned?.Invoke(structure);
                }
            }
            
            // Second pass: create connections
            foreach (StructureBlueprint blueprint in blueprints)
            {
                if (structureMap.TryGetValue(blueprint.componentId, out Castle.StructureComponent structure))
                {
                    CreateConnections(structure, blueprint.connectedTo, structureMap);
                }
            }
        }
        
        /// <summary>
        /// Spawns a single structure from blueprint.
        /// </summary>
        private Castle.StructureComponent SpawnStructure(StructureBlueprint blueprint, Vector2 offset)
        {
            // Load prefab or create default
            GameObject structureObj;
            
            if (!string.IsNullOrEmpty(blueprint.prefabPath))
            {
                structureObj = Resources.Load<GameObject>(blueprint.prefabPath);
                if (structureObj != null)
                {
                    structureObj = Instantiate(structureObj);
                }
                else
                {
                    structureObj = CreateDefaultStructure(blueprint);
                }
            }
            else
            {
                structureObj = CreateDefaultStructure(blueprint);
            }
            
            // Set position and rotation
            structureObj.transform.position = (Vector3)blueprint.position + (Vector3)offset;
            structureObj.transform.rotation = Quaternion.Euler(0f, 0f, blueprint.rotation);
            structureObj.transform.localScale = (Vector3)blueprint.scale;
            
            // Set parent
            structureObj.transform.SetParent(_castleParent);
            
            // Get or add StructureComponent
            Castle.StructureComponent structure = structureObj.GetComponent<Castle.StructureComponent>();
            if (structure == null)
            {
                structure = structureObj.AddComponent<Castle.StructureComponent>();
            }
            
            // Set material type (using reflection since we can't directly set private fields)
            // In practice, you'd make these public or add a setter method
            
            return structure;
        }
        
        /// <summary>
        /// Creates a default structure GameObject.
        /// </summary>
        private GameObject CreateDefaultStructure(StructureBlueprint blueprint)
        {
            GameObject structureObj = new GameObject(blueprint.componentId);
            
            // Add sprite renderer
            SpriteRenderer spriteRenderer = structureObj.AddComponent<SpriteRenderer>();
            spriteRenderer.color = GetMaterialColor(blueprint.materialType);
            
            // Add collider
            BoxCollider2D collider = structureObj.AddComponent<BoxCollider2D>();
            
            // Add rigidbody
            Rigidbody2D rb = structureObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            
            // Add StructureComponent
            Castle.StructureComponent structure = structureObj.AddComponent<Castle.StructureComponent>();
            
            return structureObj;
        }
        
        /// <summary>
        /// Creates joint connections between structures.
        /// </summary>
        private void CreateConnections(Castle.StructureComponent structure, List<string> connectedIds, Dictionary<string, Castle.StructureComponent> structureMap)
        {
            foreach (string connectedId in connectedIds)
            {
                if (structureMap.TryGetValue(connectedId, out Castle.StructureComponent connectedStructure))
                {
                    // Create joint
                    FixedJoint2D joint = structure.gameObject.AddComponent<FixedJoint2D>();
                    joint.connectedBody = connectedStructure.GetComponent<Rigidbody2D>();
                    joint.enableCollision = false;
                    
                    // Add to structure's connections
                    structure.AddConnection(connectedStructure, joint);
                }
            }
        }
        
        /// <summary>
        /// Spawns obstacles from environment data.
        /// </summary>
        private void SpawnObstacles(List<ObstacleData> obstacles)
        {
            foreach (ObstacleData obstacleData in obstacles)
            {
                GameObject obstacleObj = new GameObject(obstacleData.obstacleId);
                obstacleObj.transform.position = (Vector3)obstacleData.position;
                obstacleObj.transform.rotation = Quaternion.Euler(0f, 0f, obstacleData.rotation);
                obstacleObj.transform.localScale = Vector3.one * obstacleData.scale;
                obstacleObj.transform.SetParent(_obstacleParent);
                
                // Add collider based on type
                switch (obstacleData.type)
                {
                    case ObstacleType.Hill:
                        obstacleObj.AddComponent<CircleCollider2D>();
                        break;
                    case ObstacleType.Wall:
                    case ObstacleType.Rock:
                        obstacleObj.AddComponent<BoxCollider2D>();
                        break;
                    case ObstacleType.Moat:
                        // Moat would be a trigger zone
                        BoxCollider2D moatCollider = obstacleObj.AddComponent<BoxCollider2D>();
                        moatCollider.isTrigger = true;
                        break;
                }
                
                // Add visual
                SpriteRenderer spriteRenderer = obstacleObj.AddComponent<SpriteRenderer>();
                spriteRenderer.color = GetObstacleColor(obstacleData.type);
            }
        }
        
        /// <summary>
        /// Applies environment settings to the level.
        /// </summary>
        private void ApplyEnvironmentSettings(EnvironmentVariables environment)
        {
            // Apply wind (would affect physics)
            Physics2D.gravity = new Vector2(environment.windDirection.x * environment.windStrength, -9.81f);
            
            // Apply elevation (would affect camera)
            // CameraController.Instance?.SetElevation(environment.elevationModifier);
            
            // Apply background
            Camera.main?.backgroundColor = environment.skyColor;
        }
        
        #endregion
        
        #region Goal Management
        
        /// <summary>
        /// Finds all structures that are relevant to the level goal.
        /// </summary>
        private void FindGoalTargets()
        {
            _goalTargets.Clear();
            
            switch (_currentLevel.goalType)
            {
                case LevelGoalType.DestroyKeep:
                    foreach (Castle.StructureComponent structure in _allStructures)
                    {
                        if (structure.IsKeep)
                        {
                            _goalTargets.Add(structure);
                        }
                    }
                    break;
                    
                case LevelGoalType.EliminateBanner:
                    foreach (Castle.StructureComponent structure in _allStructures)
                    {
                        if (structure.IsTargetBanner)
                        {
                            _goalTargets.Add(structure);
                        }
                    }
                    break;
                    
                case LevelGoalType.CollapseStructure:
                    // All structures are targets
                    _goalTargets.AddRange(_allStructures);
                    break;
                    
                case LevelGoalType.DefeatDefenders:
                    // Would check for defender entities
                    break;
            }
            
            if (_debugMode)
            {
                Debug.Log($"[LevelManager] Found {_goalTargets.Count} goal targets for {_currentLevel.goalType}");
            }
        }
        
        /// <summary>
        /// Checks if the level goal is complete.
        /// </summary>
        public void CheckGoalCompletion()
        {
            if (_isGoalComplete) return;
            
            bool goalMet = false;
            
            switch (_currentLevel.goalType)
            {
                case LevelGoalType.DestroyKeep:
                    goalMet = CheckKeepDestroyed();
                    break;
                    
                case LevelGoalType.EliminateBanner:
                    goalMet = CheckBannerEliminated();
                    break;
                    
                case LevelGoalType.CollapseStructure:
                    goalMet = CheckStructureCollapsed();
                    break;
                    
                case LevelGoalType.DefeatDefenders:
                    goalMet = CheckDefendersDefeated();
                    break;
            }
            
            if (goalMet)
            {
                _isGoalComplete = true;
                OnGoalCompleted?.Invoke(true);
                Debug.Log("[LevelManager] Goal completed!");
            }
        }
        
        private bool CheckKeepDestroyed()
        {
            int destroyedCount = 0;
            foreach (Castle.StructureComponent structure in _goalTargets)
            {
                if (structure.IsDestroyed)
                {
                    destroyedCount++;
                }
            }
            
            float completionRatio = (float)destroyedCount / _goalTargets.Count;
            return completionRatio >= _currentLevel.goalCompletionThreshold;
        }
        
        private bool CheckBannerEliminated()
        {
            int destroyedCount = 0;
            foreach (Castle.StructureComponent structure in _goalTargets)
            {
                if (structure.IsDestroyed)
                {
                    destroyedCount++;
                }
            }
            
            float completionRatio = (float)destroyedCount / _goalTargets.Count;
            return completionRatio >= _currentLevel.goalCompletionThreshold;
        }
        
        private bool CheckStructureCollapsed()
        {
            int destroyedCount = 0;
            foreach (Castle.StructureComponent structure in _allStructures)
            {
                if (structure.IsDestroyed)
                {
                    destroyedCount++;
                }
            }
            
            float completionRatio = (float)destroyedCount / _allStructures.Count;
            return completionRatio >= _currentLevel.goalCompletionThreshold;
        }
        
        private bool CheckDefendersDefeated()
        {
            // Would check defender entities
            return false;
        }
        
        /// <summary>
        /// Gets the current goal completion percentage.
        /// </summary>
        public float GetGoalCompletionPercentage()
        {
            if (_goalTargets.Count == 0) return 0f;
            
            int destroyedCount = 0;
            foreach (Castle.StructureComponent structure in _goalTargets)
            {
                if (structure.IsDestroyed)
                {
                    destroyedCount++;
                }
            }
            
            return (float)destroyedCount / _goalTargets.Count;
        }
        
        /// <summary>
        /// Checks if the goal is complete.
        /// </summary>
        public bool IsGoalComplete()
        {
            return _isGoalComplete;
        }
        
        #endregion
        
        #region Helper Methods
        
        private Color GetMaterialColor(Castle.MaterialType materialType)
        {
            switch (materialType)
            {
                case Castle.MaterialType.Wood: return new Color(0.6f, 0.4f, 0.2f);
                case Castle.MaterialType.Stone: return new Color(0.5f, 0.5f, 0.5f);
                case Castle.MaterialType.ReinforcedStone: return new Color(0.4f, 0.4f, 0.6f);
                case Castle.MaterialType.Iron: return new Color(0.3f, 0.3f, 0.3f);
                default: return Color.white;
            }
        }
        
        private Color GetObstacleColor(ObstacleType obstacleType)
        {
            switch (obstacleType)
            {
                case ObstacleType.Hill: return new Color(0.2f, 0.5f, 0.2f);
                case ObstacleType.Wall: return new Color(0.4f, 0.3f, 0.2f);
                case ObstacleType.Moat: return new Color(0.2f, 0.4f, 0.8f);
                case ObstacleType.Tree: return new Color(0.1f, 0.4f, 0.1f);
                case ObstacleType.Rock: return new Color(0.3f, 0.3f, 0.3f);
                default: return Color.gray;
            }
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!_debugMode) return;
            
            // Draw goal targets
            Gizmos.color = Color.yellow;
            foreach (Castle.StructureComponent structure in _goalTargets)
            {
                if (structure != null)
                {
                    Gizmos.DrawWireSphere(structure.transform.position, 0.5f);
                }
            }
        }
        
        #endregion
    }
}
