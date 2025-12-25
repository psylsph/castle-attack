using UnityEngine;

namespace Siege.Trebuchet
{
    /// <summary>
    /// Data class for trebuchet parameters.
    /// Manages adjustable trebuchet settings and calculates launch physics.
    /// </summary>
    [System.Serializable]
    public class TrebuchetParameters
    {
        [Header("Adjustable Parameters")]
        [Range(0f, 100f)]
        [SerializeField] private float _armPullbackStrength = 50f;
        
        [Range(0f, 90f)]
        [SerializeField] private float _releaseAngle = 45f;
        
        [Range(10f, 100f)]
        [SerializeField] private float _counterweightMass = 50f;
        
        [Range(1f, 5f)]
        [SerializeField] private float _slingLength = 2f;
        
        // Runtime calculated values
        private float _launchVelocity;
        private Vector3 _launchDirection;
        private bool _isDirty = true;
        
        // Constants for physics calculations
        private const float GRAVITY = 9.81f;
        private const float ARM_LENGTH = 5f;
        private const float EFFICIENCY = 0.7f;
        
        // Public properties
        public float armPullbackStrength
        {
            get => _armPullbackStrength;
            set
            {
                _armPullbackStrength = Mathf.Clamp(value, 0f, 100f);
                _isDirty = true;
            }
        }
        
        public float releaseAngle
        {
            get => _releaseAngle;
            set
            {
                _releaseAngle = Mathf.Clamp(value, 0f, 90f);
                _isDirty = true;
            }
        }
        
        public float counterweightMass
        {
            get => _counterweightMass;
            set
            {
                _counterweightMass = Mathf.Clamp(value, 10f, 100f);
                _isDirty = true;
            }
        }
        
        public float slingLength
        {
            get => _slingLength;
            set
            {
                _slingLength = Mathf.Clamp(value, 1f, 5f);
                _isDirty = true;
            }
        }
        
        public float launchVelocity
        {
            get
            {
                if (_isDirty)
                {
                    CalculateLaunchPhysics();
                }
                return _launchVelocity;
            }
        }
        
        public Vector3 launchDirection
        {
            get
            {
                if (_isDirty)
                {
                    CalculateLaunchPhysics();
                }
                return _launchDirection;
            }
        }
        
        /// <summary>
        /// Calculates launch physics based on trebuchet parameters.
        /// Uses simplified trebuchet physics formulas.
        /// </summary>
        public void CalculateLaunchPhysics()
        {
            // Calculate launch velocity based on trebuchet mechanics
            // Formula: v = sqrt(2 * m_counterweight * g * h * efficiency) / m_projectile
            // Simplified for gameplay: v = (pullback * counterweight * sling) / constant
            
            float pullbackRatio = _armPullbackStrength / 100f;
            float counterweightFactor = _counterweightMass / 50f;
            float slingFactor = _slingLength / 2f;
            
            // Base velocity calculation
            _launchVelocity = 20f * pullbackRatio * counterweightFactor * slingFactor * EFFICIENCY;
            
            // Calculate launch direction based on angle
            float angleRad = _releaseAngle * Mathf.Deg2Rad;
            _launchDirection = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
            
            _isDirty = false;
        }
        
        /// <summary>
        /// Resets all parameters to default values.
        /// </summary>
        public void ResetToDefaults()
        {
            _armPullbackStrength = 50f;
            _releaseAngle = 45f;
            _counterweightMass = 50f;
            _slingLength = 2f;
            _isDirty = true;
        }
        
        /// <summary>
        /// Creates a copy of the parameters.
        /// </summary>
        public TrebuchetParameters Clone()
        {
            return new TrebuchetParameters
            {
                _armPullbackStrength = _armPullbackStrength,
                _releaseAngle = _releaseAngle,
                _counterweightMass = _counterweightMass,
                _slingLength = _slingLength,
                _launchVelocity = _launchVelocity,
                _launchDirection = _launchDirection,
                _isDirty = _isDirty
            };
        }
        
        /// <summary>
        /// Gets the arm rotation based on pullback strength.
        /// </summary>
        public float GetArmRotation()
        {
            float pullbackRatio = _armPullbackStrength / 100f;
            return -pullbackRatio * 45f; // Negative for backward rotation
        }
        
        /// <summary>
        /// Gets the expected range for current parameters.
        /// </summary>
        public float GetExpectedRange()
        {
            CalculateLaunchPhysics();
            float angleRad = _releaseAngle * Mathf.Deg2Rad;
            return (_launchVelocity * _launchVelocity * Mathf.Sin(2f * angleRad)) / GRAVITY;
        }
        
        /// <summary>
        /// Gets the expected maximum height for current parameters.
        /// </summary>
        public float GetExpectedMaxHeight()
        {
            CalculateLaunchPhysics();
            float angleRad = _releaseAngle * Mathf.Deg2Rad;
            return (_launchVelocity * _launchVelocity * Mathf.Sin(angleRad) * Mathf.Sin(angleRad)) / (2f * GRAVITY);
        }
        
        /// <summary>
        /// Gets the expected time of flight for current parameters.
        /// </summary>
        public float GetExpectedTimeOfFlight()
        {
            CalculateLaunchPhysics();
            float angleRad = _releaseAngle * Mathf.Deg2Rad;
            return (2f * _launchVelocity * Mathf.Sin(angleRad)) / GRAVITY;
        }
        
        /// <summary>
        /// Serializes parameters to a string for saving.
        /// </summary>
        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }
        
        /// <summary>
        /// Deserializes parameters from a string.
        /// </summary>
        public static TrebuchetParameters Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new TrebuchetParameters();
            }
            
            return JsonUtility.FromJson<TrebuchetParameters>(json);
        }
        
        /// <summary>
        /// Gets a summary of the parameters for debugging.
        /// </summary>
        public string GetSummary()
        {
            return $"Trebuchet Parameters - Pullback: {_armPullbackStrength:F1}%, Angle: {_releaseAngle:F1}°, " +
                   $"Counterweight: {_counterweightMass:F1}kg, Sling: {_slingLength:F1}m, " +
                   $"Velocity: {launchVelocity:F1}m/s, Range: {GetExpectedRange():F1}m";
        }
    }
    
    /// <summary>
    /// Defines the available trebuchet upgrades.
    /// </summary>
    [System.Serializable]
    public class TrebuchetUpgrade
    {
        public string upgradeId;
        public string upgradeName;
        [TextArea] public string description;
        public int unlockLevel;
        public int cost;
        public bool isUnlocked;
        
        [Header("Parameter Bonuses")]
        public float maxPullbackBonus = 0f;
        public float maxAngleBonus = 0f;
        public float maxCounterweightBonus = 0f;
        public float maxSlingLengthBonus = 0f;
        public float velocityMultiplier = 1f;
        
        [Header("Visual")]
        public GameObject visualModel;
        public Material upgradeMaterial;
    }
}
