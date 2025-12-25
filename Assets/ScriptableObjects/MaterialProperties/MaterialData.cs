using UnityEngine;

namespace Siege.Castle
{
    /// <summary>
    /// ScriptableObject defining properties for castle materials.
    /// Used by StructureComponent to determine physics behavior and destruction characteristics.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaterial", menuName = "Siege/Material")]
    public class MaterialData : ScriptableObject
    {
        [Header("Material Type")]
        [SerializeField] private MaterialType _type;
        [SerializeField] private string _materialName;
        [TextArea(2, 3)]
        [SerializeField] private string _description;
        
        [Header("Physics Properties")]
        [SerializeField] private float _health = 100f;
        [SerializeField] private float _density = 1f;
        [SerializeField] private float _friction = 0.5f;
        [SerializeField] private float _bounciness = 0.1f;
        
        [Header("Fire Properties")]
        [SerializeField] private bool _isFlammable = false;
        [SerializeField] private float _burnDamagePerSecond = 5f;
        [SerializeField] [Range(0f, 1f)] private float _spreadChance = 0.1f;
        
        [Header("Visual Properties")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _damagedColor = Color.gray;
        [SerializeField] private Sprite[] _damageSprites;
        [SerializeField] private Texture2D _patternTexture; // For colorblind mode
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem _destructionParticles;
        [SerializeField] private AudioClip[] _destructionSounds;
        
        [Header("Accessibility")]
        [SerializeField] private bool _usePatternOverlay = false;
        
        // Public properties
        public MaterialType type => _type;
        public string materialName => _materialName;
        public string description => _description;
        public float health => _health;
        public float density => _density;
        public float friction => _friction;
        public float bounciness => _bounciness;
        public bool isFlammable => _isFlammable;
        public float burnDamagePerSecond => _burnDamagePerSecond;
        public float spreadChance => _spreadChance;
        public Color normalColor => _normalColor;
        public Color damagedColor => _damagedColor;
        public Sprite[] damageSprites => _damageSprites;
        public Texture2D patternTexture => _patternTexture;
        public ParticleSystem destructionParticles => _destructionParticles;
        public AudioClip[] destructionSounds => _destructionSounds;
        public bool usePatternOverlay => _usePatternOverlay;
        
        /// <summary>
        /// Gets the appropriate sprite based on damage percentage.
        /// </summary>
        public Sprite GetDamageSprite(float damagePercentage)
        {
            if (_damageSprites == null || _damageSprites.Length == 0)
            {
                return null;
            }
            
            int spriteIndex = Mathf.FloorToInt(damagePercentage * _damageSprites.Length);
            spriteIndex = Mathf.Clamp(spriteIndex, 0, _damageSprites.Length - 1);
            
            return _damageSprites[spriteIndex];
        }
        
        /// <summary>
        /// Gets a random destruction sound.
        /// </summary>
        public AudioClip GetRandomDestructionSound()
        {
            if (_destructionSounds == null || _destructionSounds.Length == 0)
            {
                return null;
            }
            
            return _destructionSounds[Random.Range(0, _destructionSounds.Length)];
        }
        
        /// <summary>
        /// Calculates physics material 2D based on properties.
        /// </summary>
        public PhysicsMaterial2D CreatePhysicsMaterial()
        {
            PhysicsMaterial2D material = new PhysicsMaterial2D(_materialName);
            material.friction = _friction;
            material.bounciness = _bounciness;
            return material;
        }
        
        /// <summary>
        /// Validates the material data.
        /// </summary>
        public bool IsValid()
        {
            if (_health <= 0f)
            {
                Debug.LogWarning($"[MaterialData] {_materialName} has invalid health value: {_health}");
                return false;
            }
            
            if (_density <= 0f)
            {
                Debug.LogWarning($"[MaterialData] {_materialName} has invalid density value: {_density}");
                return false;
            }
            
            return true;
        }
        
        private void OnValidate()
        {
            // Ensure values are within valid ranges
            _health = Mathf.Max(1f, _health);
            _density = Mathf.Max(0.1f, _density);
            _friction = Mathf.Clamp01(_friction);
            _bounciness = Mathf.Clamp01(_bounciness);
            _burnDamagePerSecond = Mathf.Max(0f, _burnDamagePerSecond);
            _spreadChance = Mathf.Clamp01(_spreadChance);
        }
    }
    
    /// <summary>
    /// Types of materials used in castle construction.
    /// </summary>
    public enum MaterialType
    {
        Wood,
        Stone,
        ReinforcedStone,
        Iron
    }
}
