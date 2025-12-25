using UnityEngine;

namespace Siege.Ammunition
{
    /// <summary>
    /// ScriptableObject defining properties for ammunition types.
    /// Used by Projectile to determine behavior and effects.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAmmo", menuName = "Siege/Ammunition")]
    public class AmmunitionData : ScriptableObject
    {
        [Header("Basic Properties")]
        [SerializeField] private AmmunitionType _type;
        [SerializeField] private string _displayName;
        [TextArea(2, 3)]
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        
        [Header("Physics")]
        [SerializeField] private float _mass = 10f;
        [SerializeField] private float _baseDamage = 50f;
        [SerializeField] private float _radius = 1f;
        
        [Header("Unlock")]
        [SerializeField] private int _unlockLevel = 1;
        [SerializeField] private bool _isRare = false;
        [SerializeField] private int _cost = 0;
        
        [Header("Prefabs & Audio")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private AudioClip _fireSound;
        [SerializeField] private AudioClip _impactSound;
        
        [Header("Special Effects")]
        [SerializeField] private bool _hasSecondaryEffect = false;
        [SerializeField] private SecondaryEffectType _effectType = SecondaryEffectType.None;
        [SerializeField] private float _effectDuration = 5f;
        [SerializeField] private float _effectRadius = 2f;
        [SerializeField] private float _effectDamagePerSecond = 10f;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _trailParticles;
        [SerializeField] private ParticleSystem _impactParticles;
        [SerializeField] private Color _projectileColor = Color.white;
        
        // Public properties
        public AmmunitionType type => _type;
        public string displayName => _displayName;
        public string description => _description;
        public Sprite icon => _icon;
        public float mass => _mass;
        public float baseDamage => _baseDamage;
        public float radius => _radius;
        public int unlockLevel => _unlockLevel;
        public bool isRare => _isRare;
        public int cost => _cost;
        public GameObject projectilePrefab => _projectilePrefab;
        public AudioClip fireSound => _fireSound;
        public AudioClip impactSound => _impactSound;
        public bool hasSecondaryEffect => _hasSecondaryEffect;
        public SecondaryEffectType effectType => _effectType;
        public float effectDuration => _effectDuration;
        public float effectRadius => _effectRadius;
        public float effectDamagePerSecond => _effectDamagePerSecond;
        public ParticleSystem trailParticles => _trailParticles;
        public ParticleSystem impactParticles => _impactParticles;
        public Color projectileColor => _projectileColor;
        
        /// <summary>
        /// Gets the projectile prefab, creating a default one if not set.
        /// </summary>
        public GameObject GetProjectilePrefab()
        {
            if (_projectilePrefab != null)
            {
                return _projectilePrefab;
            }
            
            Debug.LogWarning($"[AmmunitionData] No prefab set for {_displayName}");
            return null;
        }
        
        /// <summary>
        /// Validates the ammunition data.
        /// </summary>
        public bool IsValid()
        {
            if (_mass <= 0f)
            {
                Debug.LogWarning($"[AmmunitionData] {_displayName} has invalid mass: {_mass}");
                return false;
            }
            
            if (_baseDamage <= 0f)
            {
                Debug.LogWarning($"[AmmunitionData] {_displayName} has invalid base damage: {_baseDamage}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets a description string for UI display.
        /// </summary>
        public string GetDisplayDescription()
        {
            string desc = $"{_displayName}\n";
            desc += $"Damage: {_baseDamage}\n";
            desc += $"Mass: {_mass}kg\n";
            
            if (_hasSecondaryEffect)
            {
                desc += $"\nSpecial: {_effectType}\n";
                desc += $"Effect Duration: {_effectDuration}s\n";
                desc += $"Effect Radius: {_effectRadius}m";
            }
            
            return desc;
        }
        
        private void OnValidate()
        {
            // Ensure values are within valid ranges
            _mass = Mathf.Max(0.1f, _mass);
            _baseDamage = Mathf.Max(1f, _baseDamage);
            _radius = Mathf.Max(0.1f, _radius);
            _effectDuration = Mathf.Max(0f, _effectDuration);
            _effectRadius = Mathf.Max(0f, _effectRadius);
            _effectDamagePerSecond = Mathf.Max(0f, _effectDamagePerSecond);
            _unlockLevel = Mathf.Max(1, _unlockLevel);
            _cost = Mathf.Max(0, _cost);
        }
    }
    
    /// <summary>
    /// Types of ammunition available in the game.
    /// </summary>
    public enum AmmunitionType
    {
        Stone,           // Standard damage, reliable physics
        FirePot,         // Causes secondary fire damage over time
        PlagueBarrel,    // Spreads weakening effect to nearby structures
        ChainShot,       // Effective against wooden beams and towers
        RoyalBoulder     // Rare, massive single-shot damage
    }
    
    /// <summary>
    /// Types of secondary effects that ammunition can apply.
    /// </summary>
    public enum SecondaryEffectType
    {
        None,
        Fire,
        Plague,
        Chain
    }
}
