using UnityEngine;
using System.Collections;

namespace Siege.Ammunition
{
    /// <summary>
    /// Represents a projectile fired from the trebuchet.
    /// Handles physics, collision, and special effects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;
        
        [Header("Settings")]
        [SerializeField] private float _lifetime = 10f;
        [SerializeField] private bool _destroyOnImpact = true;
        [SerializeField] private float _impactForce = 100f;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem _trailParticles;
        [SerializeField] private ParticleSystem _impactParticles;
        
        // Runtime data
        private AmmunitionData _ammoData;
        private float _spawnTime;
        private bool _hasImpacted = false;
        private Coroutine _secondaryEffectCoroutine;
        
        // Public properties
        public Rigidbody2D Rigidbody => _rb;
        public AmmunitionData AmmoData => _ammoData;
        public Physics.DamageType DamageType => GetDamageType();
        public bool HasImpacted => _hasImpacted;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
            }
            
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }
            
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            if (_trailRenderer == null)
            {
                _trailRenderer = GetComponent<TrailRenderer>();
            }
            
            // Setup collision
            if (_collider != null)
            {
                _collider.isTrigger = false;
            }
        }
        
        private void Start()
        {
            _spawnTime = Time.time;
        }
        
        private void Update()
        {
            // Check lifetime
            if (Time.time - _spawnTime > _lifetime)
            {
                DestroyProjectile();
            }
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_hasImpacted) return;
            
            HandleImpact(collision);
        }
        
        /// <summary>
        /// Initializes the projectile with launch parameters.
        /// </summary>
        public void Initialize(float velocity, Vector3 direction, AmmunitionData ammoData)
        {
            _ammoData = ammoData;
            
            if (_rb != null)
            {
                _rb.velocity = direction * velocity;
                _rb.mass = ammoData?.mass ?? 10f;
            }
            
            // Apply visual settings
            if (_spriteRenderer != null && ammoData != null)
            {
                _spriteRenderer.color = ammoData.projectileColor;
            }
            
            // Setup trail
            if (_trailRenderer != null && ammoData?.trailParticles != null)
            {
                _trailRenderer.enabled = true;
            }
            
            // Spawn trail particles
            if (ammoData?.trailParticles != null)
            {
                _trailParticles = Instantiate(ammoData.trailParticles, transform);
            }
            
            // Trigger spawn event
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.PROJECTILE_SPAWNED, this);
        }
        
        /// <summary>
        /// Handles collision with objects.
        /// </summary>
        private void HandleImpact(Collision2D collision)
        {
            if (_hasImpacted) return;
            
            _hasImpacted = true;
            
            // Apply impact force
            if (collision.rigidbody != null)
            {
                Vector2 impactDirection = _rb.velocity.normalized;
                collision.rigidbody.AddForce(impactDirection * _impactForce, ForceMode2D.Impulse);
            }
            
            // Notify destruction manager
            Physics.DestructionManager.Instance?.OnProjectileImpact(this, collision.collider);
            
            // Play impact effects
            PlayImpactEffects(collision.contacts[0].point);
            
            // Apply secondary effects
            if (_ammoData?.hasSecondaryEffect == true)
            {
                ApplySecondaryEffects(collision.contacts[0].point);
            }
            
            // Trigger impact event
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.PROJECTILE_IMPACT, new ImpactData
            {
                projectile = this,
                collision = collision,
                point = collision.contacts[0].point
            });
            
            // Destroy projectile if configured
            if (_destroyOnImpact)
            {
                StartCoroutine(DestroyAfterDelay(0.1f));
            }
        }
        
        /// <summary>
        /// Plays visual and audio effects on impact.
        /// </summary>
        private void PlayImpactEffects(Vector3 impactPoint)
        {
            // Play particles
            if (_ammoData?.impactParticles != null)
            {
                ParticleSystem particles = Instantiate(_ammoData.impactParticles, impactPoint, Quaternion.identity);
                Destroy(particles.gameObject, 2f);
            }
            
            if (_impactParticles != null)
            {
                _impactParticles.transform.position = impactPoint;
                _impactParticles.Play();
            }
            
            // Play sound
            if (_ammoData?.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(_ammoData.impactSound, impactPoint);
            }
            
            // Screen shake (would be handled by camera controller)
            // CameraController.Instance?.ShakeCamera(0.3f, 0.5f);
        }
        
        /// <summary>
        /// Applies secondary effects based on ammunition type.
        /// </summary>
        private void ApplySecondaryEffects(Vector3 impactPoint)
        {
            if (_ammoData == null) return;
            
            switch (_ammoData.effectType)
            {
                case SecondaryEffectType.Fire:
                    _secondaryEffectCoroutine = StartCoroutine(ApplyFireEffect(impactPoint));
                    break;
                    
                case SecondaryEffectType.Plague:
                    _secondaryEffectCoroutine = StartCoroutine(ApplyPlagueEffect(impactPoint));
                    break;
                    
                case SecondaryEffectType.Chain:
                    ApplyChainEffect(impactPoint);
                    break;
            }
        }
        
        /// <summary>
        /// Applies fire damage over time to nearby structures.
        /// </summary>
        private IEnumerator ApplyFireEffect(Vector3 impactPoint)
        {
            float elapsed = 0f;
            
            while (elapsed < _ammoData.effectDuration)
            {
                // Find nearby structures
                Collider2D[] colliders = Physics2D.OverlapCircleAll(impactPoint, _ammoData.effectRadius);
                
                foreach (Collider2D collider in colliders)
                {
                    Castle.StructureComponent structure = collider.GetComponent<Castle.StructureComponent>();
                    if (structure != null && !structure.IsDestroyed)
                    {
                        Physics.DestructionManager.Instance?.ApplyEnvironmentalDamage(
                            structure,
                            _ammoData.effectDamagePerSecond * Time.deltaTime,
                            Physics.EnvironmentalEffect.Fire
                        );
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        /// <summary>
        /// Applies plague weakening effect to nearby structures.
        /// </summary>
        private IEnumerator ApplyPlagueEffect(Vector3 impactPoint)
        {
            float elapsed = 0f;
            
            while (elapsed < _ammoData.effectDuration)
            {
                // Find nearby structures
                Collider2D[] colliders = Physics2D.OverlapCircleAll(impactPoint, _ammoData.effectRadius);
                
                foreach (Collider2D collider in colliders)
                {
                    Castle.StructureComponent structure = collider.GetComponent<Castle.StructureComponent>();
                    if (structure != null && !structure.IsDestroyed)
                    {
                        Physics.DestructionManager.Instance?.ApplyEnvironmentalDamage(
                            structure,
                            _ammoData.effectDamagePerSecond * Time.deltaTime,
                            Physics.EnvironmentalEffect.Plague
                        );
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        /// <summary>
        /// Applies chain damage to connected structures.
        /// </summary>
        private void ApplyChainEffect(Vector3 impactPoint)
        {
            // Find structures in radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(impactPoint, _ammoData.effectRadius);
            
            foreach (Collider2D collider in colliders)
            {
                Castle.StructureComponent structure = collider.GetComponent<Castle.StructureComponent>();
                if (structure != null && !structure.IsDestroyed)
                {
                    // Chain shot is especially effective against wood
                    if (structure.MaterialType == Castle.MaterialType.Wood)
                    {
                        Physics.DestructionManager.Instance?.ApplyEnvironmentalDamage(
                            structure,
                            _ammoData.baseDamage * 0.5f,
                            Physics.EnvironmentalEffect.Chain
                        );
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the damage type based on ammunition type.
        /// </summary>
        private Physics.DamageType GetDamageType()
        {
            if (_ammoData == null) return Physics.DamageType.Impact;
            
            switch (_ammoData.effectType)
            {
                case SecondaryEffectType.Fire:
                    return Physics.DamageType.Fire;
                case SecondaryEffectType.Plague:
                    return Physics.DamageType.Plague;
                default:
                    return Physics.DamageType.Impact;
            }
        }
        
        /// <summary>
        /// Destroys the projectile.
        /// </summary>
        public void DestroyProjectile()
        {
            // Stop any ongoing effects
            if (_secondaryEffectCoroutine != null)
            {
                StopCoroutine(_secondaryEffectCoroutine);
            }
            
            // Trigger destroyed event
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.PROJECTILE_DESTROYED, this);
            
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Destroys the projectile after a delay.
        /// </summary>
        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            DestroyProjectile();
        }
        
        /// <summary>
        /// Gets the current velocity of the projectile.
        /// </summary>
        public Vector2 GetVelocity()
        {
            return _rb != null ? _rb.velocity : Vector2.zero;
        }
        
        /// <summary>
        /// Gets the current speed of the projectile.
        /// </summary>
        public float GetSpeed()
        {
            return _rb != null ? _rb.velocity.magnitude : 0f;
        }
        
        /// <summary>
        /// Gets the mass of the projectile.
        /// </summary>
        public float GetMass()
        {
            return _rb != null ? _rb.mass : 10f;
        }
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            // Draw velocity direction
            if (_rb != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)_rb.velocity * 0.1f);
            }
            
            // Draw effect radius
            if (_ammoData?.hasSecondaryEffect == true)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _ammoData.effectRadius);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Data class for impact events.
    /// </summary>
    public class ImpactData
    {
        public Projectile projectile;
        public Collision2D collision;
        public Vector3 point;
    }
}
