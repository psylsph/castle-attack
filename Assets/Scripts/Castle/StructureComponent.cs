using UnityEngine;
using System.Collections.Generic;

namespace Siege.Castle
{
    /// <summary>
    /// Represents a structural component of a castle.
    /// Handles health, damage, and destruction events.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class StructureComponent : MonoBehaviour, IDamageable
    {
        [Header("Structure Properties")]
        [SerializeField] private MaterialType _materialType = MaterialType.Stone;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private bool _isWeakPoint = false;
        [SerializeField] private bool _isKeep = false;
        [SerializeField] private bool _isTargetBanner = false;
        
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private List<SpriteRenderer> _damageIndicators;
        
        [Header("Physics")]
        [SerializeField] private List<Joint2D> _connectedJoints = new List<Joint2D>();
        [SerializeField] private bool _isStatic = false;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        
        // Runtime data
        private float _currentHealth;
        private bool _isDestroyed = false;
        private List<StructureComponent> _dependentStructures = new List<StructureComponent>();
        private MaterialData _materialData;
        
        // Events
        public event System.Action<float> OnDamaged;
        public event System.Action OnDestroyed;
        
        // Public properties
        public MaterialType MaterialType => _materialType;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsWeakPoint => _isWeakPoint;
        public bool IsKeep => _isKeep;
        public bool IsTargetBanner => _isTargetBanner;
        public bool IsDestroyed => _isDestroyed;
        public List<Joint2D> ConnectedJoints => _connectedJoints;
        public List<StructureComponent> DependentStructures => _dependentStructures;
        public MaterialData MaterialData => _materialData;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            LoadMaterialData();
            InitializePhysics();
            FindDependentStructures();
            
            if (_debugMode)
            {
                Debug.Log($"[StructureComponent] Initialized: {name} - Material: {_materialType}, Health: {_maxHealth}");
            }
        }
        
        private void InitializeComponents()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            // Find all joints
            _connectedJoints.Clear();
            Joint2D[] joints = GetComponents<Joint2D>();
            _connectedJoints.AddRange(joints);
        }
        
        private void LoadMaterialData()
        {
            // Load material data from Resources
            string path = $"Materials/{_materialType}";
            _materialData = Resources.Load<MaterialData>(path);
            
            if (_materialData == null)
            {
                Debug.LogWarning($"[StructureComponent] Material data not found for: {_materialType}");
            }
            else
            {
                // Override max health if material data exists
                if (_maxHealth == 100f)
                {
                    _maxHealth = _materialData.health;
                }
            }
            
            _currentHealth = _maxHealth;
        }
        
        private void InitializePhysics()
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            
            if (_isStatic)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = _materialData?.density ?? 1f;
            }
            
            // Apply material physics
            if (_materialData != null)
            {
                PhysicsMaterial2D physicsMaterial = _materialData.CreatePhysicsMaterial();
                GetComponent<Collider2D>().sharedMaterial = physicsMaterial;
            }
        }
        
        private void FindDependentStructures()
        {
            _dependentStructures.Clear();
            
            foreach (Joint2D joint in _connectedJoints)
            {
                if (joint != null && joint.connectedBody != null)
                {
                    StructureComponent connectedStructure = joint.connectedBody.GetComponent<StructureComponent>();
                    if (connectedStructure != null && connectedStructure != this)
                    {
                        _dependentStructures.Add(connectedStructure);
                    }
                }
            }
        }
        
        #region Damage System
        
        /// <summary>
        /// Applies damage to the structure.
        /// </summary>
        public void TakeDamage(float damage, Physics.DamageType damageType)
        {
            if (_isDestroyed) return;
            
            float actualDamage = CalculateActualDamage(damage, damageType);
            _currentHealth -= actualDamage;
            
            if (_debugMode)
            {
                Debug.Log($"[StructureComponent] {name} took {actualDamage:F2} damage ({damageType}). Health: {_currentHealth:F2}/{_maxHealth:F2}");
            }
            
            // Update visuals
            UpdateDamageVisuals();
            
            // Trigger event
            OnDamaged?.Invoke(actualDamage);
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.STRUCTURE_DAMAGED, new DamageEventData
            {
                structure = this,
                damage = actualDamage,
                damageType = damageType
            });
            
            // Check for destruction
            if (_currentHealth <= 0f)
            {
                DestroyStructure();
            }
        }
        
        /// <summary>
        /// Calculates actual damage based on damage type and material.
        /// </summary>
        private float CalculateActualDamage(float damage, Physics.DamageType damageType)
        {
            float actualDamage = damage;
            
            // Apply material resistances
            if (_materialData != null)
            {
                switch (damageType)
                {
                    case Physics.DamageType.Fire:
                        if (!_materialData.isFlammable)
                        {
                            actualDamage *= 0.1f; // Fire is ineffective on non-flammable materials
                        }
                        break;
                        
                    case Physics.DamageType.Plague:
                        actualDamage *= 0.5f; // Plague weakens rather than destroys
                        break;
                        
                    case Physics.DamageType.Structural:
                        actualDamage *= 1.5f; // Structural damage is more effective
                        break;
                }
            }
            
            return actualDamage;
        }
        
        /// <summary>
        /// Weakens the structure (for plague effect).
        /// </summary>
        public void Weaken(float amount)
        {
            if (_isDestroyed) return;
            
            _maxHealth -= amount;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            
            UpdateDamageVisuals();
            
            if (_currentHealth <= 0f)
            {
                DestroyStructure();
            }
        }
        
        /// <summary>
        /// Destroys the structure.
        /// </summary>
        public void DestroyStructure()
        {
            if (_isDestroyed) return;
            
            _isDestroyed = true;
            
            if (_debugMode)
            {
                Debug.Log($"[StructureComponent] {name} destroyed!");
            }
            
            // Break all joints
            foreach (Joint2D joint in _connectedJoints)
            {
                if (joint != null)
                {
                    Destroy(joint);
                }
            }
            
            // Make dynamic if it was static
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Static)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.WakeUp();
            }
            
            // Trigger events
            OnDestroyed?.Invoke();
            Physics.DestructionManager.Instance?.OnStructureDestroyed(this);
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.STRUCTURE_DESTROYED, this);
            
            // Notify game manager
            if (_isKeep || _isTargetBanner)
            {
                Core.GameManager.Instance?.OnStructureDestroyed(this);
            }
            
            // Schedule destruction after physics settles
            StartCoroutine(DestroyAfterDelay(2f));
        }
        
        private System.Collections.IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Play destruction effects
            if (_materialData?.destructionParticles != null)
            {
                ParticleSystem particles = Instantiate(_materialData.destructionParticles, transform.position, Quaternion.identity);
                Destroy(particles.gameObject, 3f);
            }
            
            // Play destruction sound
            if (_materialData?.destructionSounds != null && _materialData.destructionSounds.Length > 0)
            {
                AudioClip sound = _materialData.GetRandomDestructionSound();
                if (sound != null)
                {
                    AudioSource.PlayClipAtPoint(sound, transform.position);
                }
            }
            
            Destroy(gameObject);
        }
        
        #endregion
        
        #region Visual Updates
        
        /// <summary>
        /// Updates visual indicators based on damage.
        /// </summary>
        private void UpdateDamageVisuals()
        {
            float damagePercentage = 1f - (_currentHealth / _maxHealth);
            
            // Update sprite color
            if (_spriteRenderer != null && _materialData != null)
            {
                _spriteRenderer.color = Color.Lerp(_materialData.normalColor, _materialData.damagedColor, damagePercentage);
            }
            
            // Update damage sprite
            if (_materialData != null)
            {
                Sprite damageSprite = _materialData.GetDamageSprite(damagePercentage);
                if (damageSprite != null && _spriteRenderer != null)
                {
                    _spriteRenderer.sprite = damageSprite;
                }
            }
            
            // Show/hide damage indicators
            for (int i = 0; i < _damageIndicators.Count; i++)
            {
                if (_damageIndicators[i] != null)
                {
                    _damageIndicators[i].enabled = damagePercentage > 0.3f + (i * 0.2f);
                }
            }
        }
        
        /// <summary>
        /// Shows weak point indicator.
        /// </summary>
        public void ShowWeakPointIndicator(bool show)
        {
            if (_isWeakPoint && _damageIndicators.Count > 0)
            {
                foreach (SpriteRenderer indicator in _damageIndicators)
                {
                    if (indicator != null)
                    {
                        indicator.enabled = show;
                    }
                }
            }
        }
        
        #endregion
        
        #region Connection Management
        
        /// <summary>
        /// Adds a joint connection to another structure.
        /// </summary>
        public void AddConnection(StructureComponent other, Joint2D joint)
        {
            if (other == null || joint == null) return;
            
            if (!_dependentStructures.Contains(other))
            {
                _dependentStructures.Add(other);
            }
            
            if (!_connectedJoints.Contains(joint))
            {
                _connectedJoints.Add(joint);
            }
        }
        
        /// <summary>
        /// Removes a joint connection.
        /// </summary>
        public void RemoveConnection(Joint2D joint)
        {
            if (joint != null && _connectedJoints.Contains(joint))
            {
                _connectedJoints.Remove(joint);
                
                // Remove from dependent structures
                StructureComponent connected = joint.connectedBody?.GetComponent<StructureComponent>();
                if (connected != null)
                {
                    _dependentStructures.Remove(connected);
                }
            }
        }
        
        #endregion
        
        #region IDamageable Implementation
        
        void IDamageable.TakeDamage(float damage, Physics.DamageType damageType)
        {
            TakeDamage(damage, damageType);
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!_debugMode) return;
            
            // Draw connections
            Gizmos.color = Color.cyan;
            foreach (Joint2D joint in _connectedJoints)
            {
                if (joint != null && joint.connectedBody != null)
                {
                    Gizmos.DrawLine(transform.position, joint.connectedBody.transform.position);
                }
            }
            
            // Draw weak point indicator
            if (_isWeakPoint)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
            
            // Draw keep indicator
            if (_isKeep)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 1.5f);
            }
            
            // Draw target banner indicator
            if (_isTargetBanner)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
        
        #endregion
    }
    
    #region Interfaces
    
    /// <summary>
    /// Interface for objects that can take damage.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage, Physics.DamageType damageType);
    }
    
    #endregion
    
    #region Data Classes
    
    /// <summary>
    /// Data class for damage events.
    /// </summary>
    public class DamageEventData
    {
        public StructureComponent structure;
        public float damage;
        public Physics.DamageType damageType;
    }
    
    #endregion
}
