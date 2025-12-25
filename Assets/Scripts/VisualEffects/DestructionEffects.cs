using UnityEngine;
using System.Collections;

namespace Siege.VisualEffects
{
    /// <summary>
    /// Manages visual effects for structure destruction.
    /// Handles particle systems, debris, and impact effects.
    /// </summary>
    public class DestructionEffects : MonoBehaviour
    {
        public static DestructionEffects Instance { get; private set; }
        
        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem _woodDebris;
        [SerializeField] private ParticleSystem _stoneDebris;
        [SerializeField] private ParticleSystem _dustCloud;
        [SerializeField] private ParticleSystem _fireEffect;
        [SerializeField] private ParticleSystem _plagueEffect;
        
        [Header("Settings")]
        [SerializeField] private float _debrisLifetime = 3f;
        [SerializeField] private bool _useObjectPooling = true;
        [SerializeField] private int _poolSize = 20;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        
        private ObjectPool _particlePool;
        
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
            InitializePool();
        }
        
        private void InitializePool()
        {
            if (_useObjectPooling)
            {
                _particlePool = gameObject.AddComponent<ObjectPool>();
                _particlePool.Initialize(_poolSize);
            }
        }
        
        /// <summary>
        /// Plays destruction effect at the specified position.
        /// </summary>
        public void PlayDestructionEffect(Vector3 position, Castle.MaterialType materialType)
        {
            ParticleSystem effect = GetEffectForMaterial(materialType);
            
            if (effect != null)
            {
                PlayEffect(effect, position);
            }
            
            // Always add dust cloud
            if (_dustCloud != null)
            {
                PlayEffect(_dustCloud, position);
            }
            
            if (_debugMode)
            {
                Debug.Log($"[DestructionEffects] Playing effect for {materialType} at {position}");
            }
        }
        
        /// <summary>
        /// Plays chain reaction effects.
        /// </summary>
        public void PlayChainReactionEffect(System.Collections.Generic.List<Castle.StructureComponent> structures)
        {
            if (structures == null || structures.Count == 0) return;
            
            foreach (Castle.StructureComponent structure in structures)
            {
                if (structure != null)
                {
                    PlayDestructionEffect(structure.transform.position, structure.MaterialType);
                }
            }
            
            if (_debugMode)
            {
                Debug.Log($"[DestructionEffects] Chain reaction with {structures.Count} structures");
            }
        }
        
        /// <summary>
        /// Plays fire effect at the specified position.
        /// </summary>
        public void PlayFireEffect(Vector3 position, float duration)
        {
            if (_fireEffect == null) return;
            
            ParticleSystem fire = InstantiateEffect(_fireEffect, position);
            fire.Play();
            
            // Stop after duration
            StartCoroutine(StopEffectAfterDelay(fire, duration));
        }
        
        /// <summary>
        /// Plays plague effect at the specified position.
        /// </summary>
        public void PlayPlagueEffect(Vector3 position, float duration)
        {
            if (_plagueEffect == null) return;
            
            ParticleSystem plague = InstantiateEffect(_plagueEffect, position);
            plague.Play();
            
            // Stop after duration
            StartCoroutine(StopEffectAfterDelay(plague, duration));
        }
        
        /// <summary>
        /// Gets the appropriate particle effect for a material type.
        /// </summary>
        private ParticleSystem GetEffectForMaterial(Castle.MaterialType materialType)
        {
            switch (materialType)
            {
                case Castle.MaterialType.Wood:
                    return _woodDebris;
                case Castle.MaterialType.Stone:
                case Castle.MaterialType.ReinforcedStone:
                    return _stoneDebris;
                case Castle.MaterialType.Iron:
                    return _stoneDebris; // Use stone debris for iron as fallback
                default:
                    return _stoneDebris;
            }
        }
        
        /// <summary>
        /// Plays a particle effect at the specified position.
        /// </summary>
        private void PlayEffect(ParticleSystem prefab, Vector3 position)
        {
            if (prefab == null) return;
            
            ParticleSystem effect = InstantiateEffect(prefab, position);
            effect.Play();
            
            // Destroy after lifetime
            StartCoroutine(DestroyEffectAfterLifetime(effect));
        }
        
        /// <summary>
        /// Instantiates a particle effect.
        /// </summary>
        private ParticleSystem InstantiateEffect(ParticleSystem prefab, Vector3 position)
        {
            if (_useObjectPooling && _particlePool != null)
            {
                // Try to get from pool
                ParticleSystem pooled = _particlePool.Get<ParticleSystem>(prefab.name);
                if (pooled != null)
                {
                    pooled.transform.position = position;
                    return pooled;
                }
            }
            
            // Instantiate new
            return Instantiate(prefab, position, Quaternion.identity);
        }
        
        /// <summary>
        /// Destroys a particle effect after its lifetime.
        /// </summary>
        private System.Collections.IEnumerator DestroyEffectAfterLifetime(ParticleSystem effect)
        {
            yield return new WaitForSeconds(_debrisLifetime);
            
            if (effect != null)
            {
                if (_useObjectPooling && _particlePool != null)
                {
                    effect.Stop();
                    _particlePool.Return(effect.gameObject);
                }
                else
                {
                    Destroy(effect.gameObject);
                }
            }
        }
        
        /// <summary>
        /// Stops a particle effect after a delay.
        /// </summary>
        private System.Collections.IEnumerator StopEffectAfterDelay(ParticleSystem effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (effect != null)
            {
                effect.Stop();
                
                if (_useObjectPooling && _particlePool != null)
                {
                    _particlePool.Return(effect.gameObject);
                }
                else
                {
                    Destroy(effect.gameObject);
                }
            }
        }
        
        /// <summary>
        /// Creates a debris piece at the specified position.
        /// </summary>
        public void CreateDebrisPiece(Vector3 position, Castle.MaterialType materialType, Vector3 velocity)
        {
            // Create simple debris object
            GameObject debris = new GameObject("Debris");
            debris.transform.position = position;
            
            // Add sprite
            SpriteRenderer spriteRenderer = debris.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetDebrisSprite(materialType);
            spriteRenderer.color = GetMaterialColor(materialType);
            spriteRenderer.sortingOrder = 10;
            
            // Add collider
            CircleCollider2D collider = debris.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            
            // Add rigidbody
            Rigidbody2D rb = debris.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.velocity = velocity;
            rb.angularVelocity = Random.Range(-5f, 5f);
            
            // Destroy after delay
            Destroy(debris, _debrisLifetime);
        }
        
        /// <summary>
        /// Gets a sprite for debris based on material type.
        /// </summary>
        private Sprite GetDebrisSprite(Castle.MaterialType materialType)
        {
            // In a real implementation, you'd load sprites from resources
            return null; // Will use default square
        }
        
        /// <summary>
        /// Gets a color based on material type.
        /// </summary>
        private Color GetMaterialColor(Castle.MaterialType materialType)
        {
            switch (materialType)
            {
                case Castle.MaterialType.Wood: return new Color(0.6f, 0.4f, 0.2f);
                case Castle.MaterialType.Stone: return new Color(0.5f, 0.5f, 0.5f);
                case Castle.MaterialType.ReinforcedStone: return new Color(0.4f, 0.4f, 0.6f);
                case Castle.MaterialType.Iron: return new Color(0.3f, 0.3f, 0.3f);
                default: return Color.gray;
            }
        }
    }
    
    /// <summary>
    /// Simple object pool for particle effects.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [System.Serializable]
        private class Pool
        {
            public string name;
            public Queue<GameObject> objects = new Queue<GameObject>();
            public GameObject prefab;
        }
        
        private List<Pool> _pools = new List<Pool>();
        private int _maxSize = 50;
        
        public void Initialize(int poolSize)
        {
            _maxSize = poolSize;
        }
        
        public T Get<T>(string poolName) where T : Component
        {
            Pool pool = _pools.Find(p => p.name == poolName);
            
            if (pool != null && pool.objects.Count > 0)
            {
                GameObject obj = pool.objects.Dequeue();
                obj.SetActive(true);
                return obj.GetComponent<T>();
            }
            
            return null;
        }
        
        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            
            string poolName = obj.name.Replace("(Clone)", "").Trim();
            Pool pool = _pools.Find(p => p.name == poolName);
            
            if (pool != null)
            {
                if (pool.objects.Count < _maxSize)
                {
                    pool.objects.Enqueue(obj);
                }
                else
                {
                    Destroy(obj);
                }
            }
            }
        }
        
        public void CreatePool(string name, GameObject prefab, int initialSize)
        {
            Pool pool = new Pool { name = name, prefab = prefab };
            
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                pool.objects.Enqueue(obj);
            }
            
            _pools.Add(pool);
        }
    }
}
