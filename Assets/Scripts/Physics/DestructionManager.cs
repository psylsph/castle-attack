using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Siege.Physics
{
    /// <summary>
    /// Manages destruction of castle structures and handles chain reactions.
    /// Monitors collision events and triggers destruction effects.
    /// </summary>
    public class DestructionManager : MonoBehaviour
    {
        public static DestructionManager Instance { get; private set; }
        
        [Header("Effects")]
        [SerializeField] private VisualEffects.DestructionEffects destructionEffects;
        
        [Header("Settings")]
        [SerializeField] private float chainReactionDelay = 0.2f;
        [SerializeField] private int maxChainReactionDepth = 5;
        [SerializeField] private bool enableChainReactions = true;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        private HashSet<Castle.StructureComponent> destroyedThisFrame;
        private int chainReactionDepth;
        
        public event System.Action<Castle.StructureComponent> OnStructureDestroyed;
        public event System.Action<List<Castle.StructureComponent>> OnChainReaction;
        
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
            destroyedThisFrame = new HashSet<Castle.StructureComponent>();
        }
        
        private void Update()
        {
            // Clear the frame's destroyed set
            destroyedThisFrame.Clear();
        }
        
        /// <summary>
        /// Called when a projectile impacts a structure.
        /// </summary>
        public void OnProjectileImpact(Ammunition.Projectile projectile, Collider2D collision)
        {
            Castle.StructureComponent structure = collision.GetComponent<Castle.StructureComponent>();
            
            if (structure != null && !structure.IsDestroyed)
            {
                float damage = CalculateDamage(projectile, structure);
                structure.TakeDamage(damage, projectile.DamageType);
                
                // Play destruction effects
                if (destructionEffects != null)
                {
                    destructionEffects.PlayDestructionEffect(collision.transform.position, structure.MaterialType);
                }
                
                // Check for chain reactions
                if (enableChainReactions && structure.IsDestroyed)
                {
                    StartCoroutine(EvaluateChainReaction(structure));
                }
                
                // Trigger event
                Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.PROJECTILE_IMPACT, new ImpactData
                {
                    projectile = projectile,
                    target = structure,
                    damage = damage
                });
            }
        }
        
        /// <summary>
        /// Calculates damage based on projectile properties and target material.
        /// </summary>
        private float CalculateDamage(Ammunition.Projectile projectile, Castle.StructureComponent structure)
        {
            Ammunition.AmmunitionData ammo = projectile.AmmoData;
            Castle.MaterialData material = GetMaterialData(structure.MaterialType);
            
            if (ammo == null || material == null) return 0f;
            
            // Base damage
            float damage = ammo.baseDamage;
            
            // Velocity modifier
            float velocityMagnitude = projectile.Rigidbody.velocity.magnitude;
            float velocityModifier = velocityMagnitude / 10f;
            damage *= velocityModifier;
            
            // Material resistance
            float materialResistance = material.density;
            damage /= materialResistance;
            
            // Weak point bonus
            if (structure.IsWeakPoint)
            {
                damage *= 2f;
            }
            
            // Special damage multipliers
            damage = ApplySpecialDamageModifiers(damage, ammo.type, material.type);
            
            if (debugMode)
            {
                Debug.Log($"[DestructionManager] Damage: {damage:F2} (Base: {ammo.baseDamage}, Velocity: {velocityModifier:F2}, Material: {materialResistance:F2})");
            }
            
            return damage;
        }
        
        /// <summary>
        /// Applies special damage modifiers based on ammo and material type.
        /// </summary>
        private float ApplySpecialDamageModifiers(float damage, Ammunition.AmmunitionType ammoType, Castle.MaterialType materialType)
        {
            float modifiedDamage = damage;
            
            // Chain Shot is effective against wood
            if (ammoType == Ammunition.AmmunitionType.ChainShot && materialType == Castle.MaterialType.Wood)
            {
                modifiedDamage *= 1.5f;
            }
            
            // Royal Boulder deals massive damage to everything
            if (ammoType == Ammunition.AmmunitionType.RoyalBoulder)
            {
                modifiedDamage *= 2f;
            }
            
            // Stone is less effective against iron
            if (ammoType == Ammunition.AmmunitionType.Stone && materialType == Castle.MaterialType.Iron)
            {
                modifiedDamage *= 0.5f;
            }
            
            return modifiedDamage;
        }
        
        /// <summary>
        /// Evaluates chain reactions after a structure is destroyed.
        /// </summary>
        private IEnumerator EvaluateChainReaction(Castle.StructureComponent destroyedStructure)
        {
            if (chainReactionDepth >= maxChainReactionDepth)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[DestructionManager] Max chain reaction depth reached.");
                }
                yield break;
            }
            
            yield return new WaitForSeconds(chainReactionDelay);
            
            List<Castle.StructureComponent> toDestroy = new List<Castle.StructureComponent>();
            
            // Check dependent structures
            foreach (Castle.StructureComponent dependent in destroyedStructure.DependentStructures)
            {
                if (dependent != null && !dependent.IsDestroyed && ShouldCollapse(dependent, destroyedStructure))
                {
                    toDestroy.Add(dependent);
                }
            }
            
            // Destroy collapsed structures
            if (toDestroy.Count > 0)
            {
                chainReactionDepth++;
                
                foreach (Castle.StructureComponent structure in toDestroy)
                {
                    // Apply structural damage
                    structure.TakeDamage(structure.MaxHealth, DamageType.Structural);
                    
                    // Play effects
                    if (destructionEffects != null)
                    {
                        destructionEffects.PlayDestructionEffect(structure.transform.position, structure.MaterialType);
                    }
                    
                    if (debugMode)
                    {
                        Debug.Log($"[DestructionManager] Chain reaction destroyed: {structure.name}");
                    }
                }
                
                // Trigger chain reaction event
                OnChainReaction?.Invoke(toDestroy);
                Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.CHAIN_REACTION, toDestroy);
                
                // Recursively check for more chain reactions
                foreach (Castle.StructureComponent structure in toDestroy)
                {
                    yield return StartCoroutine(EvaluateChainReaction(structure));
                }
                
                chainReactionDepth--;
            }
        }
        
        /// <summary>
        /// Determines if a structure should collapse based on support loss.
        /// </summary>
        private bool ShouldCollapse(Castle.StructureComponent structure, Castle.StructureComponent lostSupport)
        {
            // Check if structure is directly connected to lost support
            bool isDirectlyConnected = false;
            foreach (Joint2D joint in structure.ConnectedJoints)
            {
                if (joint != null && joint.connectedBody != null)
                {
                    Castle.StructureComponent connectedStructure = joint.connectedBody.GetComponent<Castle.StructureComponent>();
                    if (connectedStructure == lostSupport)
                    {
                        isDirectlyConnected = true;
                        break;
                    }
                }
            }
            
            // Calculate support ratio
            int totalConnections = structure.ConnectedJoints.Count;
            int activeConnections = 0;
            
            foreach (Joint2D joint in structure.ConnectedJoints)
            {
                if (joint != null && joint.enabled)
                {
                    Castle.StructureComponent connectedStructure = joint.connectedBody?.GetComponent<Castle.StructureComponent>();
                    if (connectedStructure == null || !connectedStructure.IsDestroyed)
                    {
                        activeConnections++;
                    }
                }
            }
            
            float supportRatio = totalConnections > 0 ? (float)activeConnections / totalConnections : 0f;
            
            // Collapse if insufficient support
            if (supportRatio < 0.5f)
            {
                if (debugMode)
                {
                    Debug.Log($"[DestructionManager] {structure.name} collapsing: support ratio {supportRatio:F2}");
                }
                return true;
            }
            
            // Check health threshold
            if (structure.CurrentHealth < structure.MaxHealth * 0.3f)
            {
                if (debugMode)
                {
                    Debug.Log($"[DestructionManager] {structure.name} collapsing: low health {structure.CurrentHealth:F2}/{structure.MaxHealth:F2}");
                }
                return true;
            }
            
            // Check weight distribution (simplified)
            if (isDirectlyConnected && structure.GetComponent<Rigidbody2D>() != null)
            {
                Rigidbody2D rb = structure.GetComponent<Rigidbody2D>();
                if (rb.mass > 50f && supportRatio < 0.7f)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DestructionManager] {structure.name} collapsing: heavy structure with low support");
                    }
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets material data for a given material type.
        /// </summary>
        private Castle.MaterialData GetMaterialData(Castle.MaterialType type)
        {
            // Load from Resources or Addressables
            string path = $"Materials/{type}";
            Castle.MaterialData material = Resources.Load<Castle.MaterialData>(path);
            
            if (material == null)
            {
                Debug.LogWarning($"[DestructionManager] Material data not found for type: {type}");
            }
            
            return material;
        }
        
        /// <summary>
        /// Forces the destruction of a structure (for debugging or special cases).
        /// </summary>
        public void ForceDestroy(Castle.StructureComponent structure)
        {
            if (structure != null && !structure.IsDestroyed)
            {
                structure.TakeDamage(structure.MaxHealth * 2f, DamageType.Structural);
                
                if (destructionEffects != null)
                {
                    destructionEffects.PlayDestructionEffect(structure.transform.position, structure.MaterialType);
                }
            }
        }
        
        /// <summary>
        /// Applies environmental damage to structures (fire, plague, etc.).
        /// </summary>
        public void ApplyEnvironmentalDamage(Castle.StructureComponent structure, float damage, EnvironmentalEffect effect)
        {
            if (structure == null || structure.IsDestroyed) return;
            
            Castle.MaterialData material = GetMaterialData(structure.MaterialType);
            
            switch (effect)
            {
                case EnvironmentalEffect.Fire:
                    if (material != null && material.isFlammable)
                    {
                        structure.TakeDamage(damage, DamageType.Fire);
                    }
                    break;
                    
                case EnvironmentalEffect.Plague:
                    // Plague weakens structures over time
                    structure.Weaken(damage);
                    break;
                    
                case EnvironmentalEffect.Chain:
                    // Chain shot damages connected structures
                    DamageConnectedStructures(structure, damage * 0.5f);
                    break;
            }
        }
        
        /// <summary>
        /// Damages structures connected to the given structure.
        /// </summary>
        private void DamageConnectedStructures(Castle.StructureComponent structure, float damage)
        {
            foreach (Joint2D joint in structure.ConnectedJoints)
            {
                if (joint != null && joint.connectedBody != null)
                {
                    Castle.StructureComponent connected = joint.connectedBody.GetComponent<Castle.StructureComponent>();
                    if (connected != null && !connected.IsDestroyed)
                    {
                        connected.TakeDamage(damage, DamageType.Impact);
                    }
                }
            }
        }
    }
    
    #region Data Classes
    
    public class ImpactData
    {
        public Ammunition.Projectile projectile;
        public Castle.StructureComponent target;
        public float damage;
    }
    
    public enum DamageType
    {
        Impact,
        Fire,
        Plague,
        Structural
    }
    
    public enum EnvironmentalEffect
    {
        None,
        Fire,
        Plague,
        Chain
    }
    
    #endregion
}
