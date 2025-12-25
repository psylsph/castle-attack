using UnityEngine;
using System.Collections.Generic;

namespace Siege.Castle
{
    /// <summary>
    /// Builds castle structures from blueprints.
    /// Creates structural joints and applies material properties.
    /// </summary>
    public class CastleBuilder : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform _castleParent;
        [SerializeField] private bool _debugMode = false;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _defaultStructurePrefab;
        [SerializeField] private GameObject _keepPrefab;
        [SerializeField] private GameObject _bannerPrefab;
        
        private Dictionary<string, StructureComponent> _structureMap = new Dictionary<string, StructureComponent>();
        
        // Events
        public event System.Action<StructureComponent> OnStructureBuilt;
        public event System.Action OnCastleComplete;
        
        public Dictionary<string, StructureComponent> structureMap => _structureMap;
        
        private void Awake()
        {
            InitializeParent();
        }
        
        private void InitializeParent()
        {
            if (_castleParent == null)
            {
                GameObject parentObj = new GameObject("Castle");
                parentObj.transform.SetParent(transform);
                _castleParent = parentObj.transform;
            }
        }
        
        /// <summary>
        /// Builds a castle from blueprints.
        /// </summary>
        public void BuildCastle(List<Level.StructureBlueprint> blueprints, Vector2 offset)
        {
            if (blueprints == null || blueprints.Count == 0)
            {
                Debug.LogWarning("[CastleBuilder] No blueprints provided!");
                return;
            }
            
            Debug.Log($"[CastleBuilder] Building castle with {blueprints.Count} structures...");
            
            // Clear existing
            ClearCastle();
            
            // First pass: spawn all structures
            foreach (Level.StructureBlueprint blueprint in blueprints)
            {
                StructureComponent structure = SpawnStructure(blueprint, offset);
                if (structure != null)
                {
                    _structureMap[blueprint.componentId] = structure;
                    OnStructureBuilt?.Invoke(structure);
                }
            }
            
            // Second pass: create connections
            foreach (Level.StructureBlueprint blueprint in blueprints)
            {
                if (_structureMap.TryGetValue(blueprint.componentId, out StructureComponent structure))
                {
                    CreateConnections(structure, blueprint.connectedTo);
                }
            }
            
            // Trigger completion event
            OnCastleComplete?.Invoke();
            
            if (_debugMode)
            {
                Debug.Log($"[CastleBuilder] Castle built with {_structureMap.Count} structures");
            }
        }
        
        /// <summary>
        /// Clears the current castle.
        /// </summary>
        public void ClearCastle()
        {
            foreach (StructureComponent structure in _structureMap.Values)
            {
                if (structure != null)
                {
                    Destroy(structure.gameObject);
                }
            }
            
            _structureMap.Clear();
            
            // Clear parent
            foreach (Transform child in _castleParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        /// <summary>
        /// Spawns a single structure from blueprint.
        /// </summary>
        private StructureComponent SpawnStructure(Level.StructureBlueprint blueprint, Vector2 offset)
        {
            GameObject structureObj;
            
            // Choose prefab based on type
            if (blueprint.isKeep && _keepPrefab != null)
            {
                structureObj = Instantiate(_keepPrefab);
            }
            else if (blueprint.isTargetBanner && _bannerPrefab != null)
            {
                structureObj = Instantiate(_bannerPrefab);
            }
            else if (!string.IsNullOrEmpty(blueprint.prefabPath))
            {
                GameObject prefab = Resources.Load<GameObject>(blueprint.prefabPath);
                if (prefab != null)
                {
                    structureObj = Instantiate(prefab);
                }
                else
                {
                    structureObj = CreateDefaultStructure(blueprint);
                }
            }
            else if (_defaultStructurePrefab != null)
            {
                structureObj = Instantiate(_defaultStructurePrefab);
            }
            else
            {
                structureObj = CreateDefaultStructure(blueprint);
            }
            
            // Set transform
            structureObj.transform.position = (Vector3)blueprint.position + (Vector3)offset;
            structureObj.transform.rotation = Quaternion.Euler(0f, 0f, blueprint.rotation);
            structureObj.transform.localScale = (Vector3)blueprint.scale;
            structureObj.transform.SetParent(_castleParent);
            
            // Get or add StructureComponent
            StructureComponent structure = structureObj.GetComponent<StructureComponent>();
            if (structure == null)
            {
                structure = structureObj.AddComponent<StructureComponent>();
            }
            
            // Set material type (would need public setter in StructureComponent)
            // For now, we'll rely on the material being set in the prefab or inspector
            
            // Apply material visual properties
            ApplyMaterialVisuals(structure, blueprint.materialType);
            
            return structure;
        }
        
        /// <summary>
        /// Creates a default structure GameObject.
        /// </summary>
        private GameObject CreateDefaultStructure(Level.StructureBlueprint blueprint)
        {
            GameObject structureObj = new GameObject(blueprint.componentId);
            
            // Add sprite renderer
            SpriteRenderer spriteRenderer = structureObj.AddComponent<SpriteRenderer>();
            spriteRenderer.color = GetMaterialColor(blueprint.materialType);
            
            // Add collider
            BoxCollider2D collider = structureObj.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            
            // Add rigidbody
            Rigidbody2D rb = structureObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            
            return structureObj;
        }
        
        /// <summary>
        /// Creates joint connections between structures.
        /// </summary>
        private void CreateConnections(StructureComponent structure, List<string> connectedIds)
        {
            foreach (string connectedId in connectedIds)
            {
                if (_structureMap.TryGetValue(connectedId, out StructureComponent connectedStructure))
                {
                    CreateJoint(structure, connectedStructure);
                }
            }
        }
        
        /// <summary>
        /// Creates a joint between two structures.
        /// </summary>
        private void CreateJoint(StructureComponent structureA, StructureComponent structureB)
        {
            Rigidbody2D rbA = structureA.GetComponent<Rigidbody2D>();
            Rigidbody2D rbB = structureB.GetComponent<Rigidbody2D>();
            
            if (rbA == null || rbB == null)
            {
                Debug.LogWarning("[CastleBuilder] Cannot create joint: missing Rigidbody2D");
                return;
            }
            
            // Create fixed joint
            FixedJoint2D joint = structureA.gameObject.AddComponent<FixedJoint2D>();
            joint.connectedBody = rbB;
            joint.enableCollision = false;
            joint.breakForce = 5000f;
            joint.breakTorque = 5000f;
            
            // Add to structure's connections
            structureA.AddConnection(structureB, joint);
            
            if (_debugMode)
            {
                Debug.Log($"[CastleBuilder] Created joint between {structureA.name} and {structureB.name}");
            }
        }
        
        /// <summary>
        /// Applies visual properties based on material type.
        /// </summary>
        private void ApplyMaterialVisuals(StructureComponent structure, MaterialType materialType)
        {
            MaterialData materialData = LoadMaterialData(materialType);
            
            if (materialData != null)
            {
                // Apply color
                SpriteRenderer spriteRenderer = structure.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = materialData.normalColor;
                }
                
                // Apply physics material
                Collider2D collider = structure.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.sharedMaterial = materialData.CreatePhysicsMaterial();
                }
            }
        }
        
        /// <summary>
        /// Loads material data from Resources.
        /// </summary>
        private MaterialData LoadMaterialData(MaterialType type)
        {
            string path = $"Materials/{type}";
            return Resources.Load<MaterialData>(path);
        }
        
        /// <summary>
        /// Gets a color based on material type.
        /// </summary>
        private Color GetMaterialColor(MaterialType materialType)
        {
            switch (materialType)
            {
                case MaterialType.Wood: return new Color(0.6f, 0.4f, 0.2f);
                case MaterialType.Stone: return new Color(0.5f, 0.5f, 0.5f);
                case MaterialType.ReinforcedStone: return new Color(0.4f, 0.4f, 0.6f);
                case MaterialType.Iron: return new Color(0.3f, 0.3f, 0.3f);
                default: return Color.white;
            }
        }
        
        /// <summary>
        /// Gets a structure by its ID.
        /// </summary>
        public StructureComponent GetStructure(string id)
        {
            return _structureMap.TryGetValue(id, out StructureComponent structure) ? structure : null;
        }
        
        /// <summary>
        /// Gets all structures of a specific type.
        /// </summary>
        public List<StructureComponent> GetStructuresByType(MaterialType materialType)
        {
            List<StructureComponent> result = new List<StructureComponent>();
            
            foreach (StructureComponent structure in _structureMap.Values)
            {
                if (structure.MaterialType == materialType)
                {
                    result.Add(structure);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets all keep structures.
        /// </summary>
        public List<StructureComponent> GetKeepStructures()
        {
            List<StructureComponent> result = new List<StructureComponent>();
            
            foreach (StructureComponent structure in _structureMap.Values)
            {
                if (structure.IsKeep)
                {
                    result.Add(structure);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets all target banners.
        /// </summary>
        public List<StructureComponent> GetTargetBanners()
        {
            List<StructureComponent> result = new List<StructureComponent>();
            
            foreach (StructureComponent structure in _structureMap.Values)
            {
                if (structure.IsTargetBanner)
                {
                    result.Add(structure);
                }
            }
            
            return result;
        }
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!_debugMode) return;
            
            // Draw all joints
            Gizmos.color = Color.cyan;
            foreach (StructureComponent structure in _structureMap.Values)
            {
                if (structure == null) continue;
                
                foreach (Joint2D joint in structure.ConnectedJoints)
                {
                    if (joint != null && joint.connectedBody != null)
                    {
                        Gizmos.DrawLine(structure.transform.position, joint.connectedBody.transform.position);
                    }
                }
            }
            
            // Draw weak points
            Gizmos.color = Color.red;
            foreach (StructureComponent structure in _structureMap.Values)
            {
                if (structure != null && structure.IsWeakPoint)
                {
                    Gizmos.DrawWireSphere(structure.transform.position, 0.5f);
                }
            }
        }
        
        #endregion
    }
}
