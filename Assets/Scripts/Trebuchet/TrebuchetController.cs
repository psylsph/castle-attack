using UnityEngine;
using System.Collections;

namespace Siege.Trebuchet
{
    /// <summary>
    /// Controls the trebuchet including parameter adjustments, firing, and visualization.
    /// Handles touch input for mobile controls.
    /// </summary>
    public class TrebuchetController : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] private TrebuchetParameters _parameters;
        
        [Header("Components")]
        [SerializeField] private Transform _armTransform;
        [SerializeField] private Transform _slingTransform;
        [SerializeField] private Transform _launchPoint;
        [SerializeField] private Transform _baseTransform;
        
        [Header("Visuals")]
        [SerializeField] private LineRenderer _ghostArc;
        [SerializeField] private GameObject _projectilePreview;
        [SerializeField] private GameObject _currentProjectileVisual;
        
        [Header("Settings")]
        [SerializeField] private float _minPullback = 0f;
        [SerializeField] private float _maxPullback = 100f;
        [SerializeField] private float _minAngle = 0f;
        [SerializeField] private float _maxAngle = 90f;
        [SerializeField] private float _aimSensitivity = 1f;
        [SerializeField] private float _pullbackSensitivity = 1f;
        
        [Header("Animation")]
        [SerializeField] private float _fireAnimationDuration = 0.5f;
        [SerializeField] private AnimationCurve _fireAnimationCurve;
        
        [Header("Upgrades")]
        [SerializeField] private TrebuchetUpgrade[] _unlockedUpgrades;
        
        private bool _isAiming = false;
        private bool _ghostArcEnabled = true;
        private bool _isFiring = false;
        private Coroutine _fireAnimationCoroutine;
        
        // Events
        public event System.Action<TrebuchetParameters> OnParameterChanged;
        public event System.Action<Ammunition.AmmunitionData> OnFired;
        public event System.Action OnReset;
        
        // Public properties
        public TrebuchetParameters parameters => _parameters;
        public bool isAiming => _isAiming;
        public bool ghostArcEnabled
        {
            get => _ghostArcEnabled;
            set
            {
                _ghostArcEnabled = value;
                UpdateGhostArcVisibility();
            }
        }
        public bool isFiring => _isFiring;
        public Transform launchPoint => _launchPoint;
        
        private void Awake()
        {
            if (_parameters == null)
            {
                _parameters = new TrebuchetParameters();
            }
            
            InitializeComponents();
        }
        
        private void Start()
        {
            UpdateArmPosition();
            UpdateArmRotation();
            UpdateGhostArcVisibility();
        }
        
        private void Update()
        {
            if (_isAiming && _ghostArcEnabled && !_isFiring)
            {
                UpdateTrajectory();
            }
        }
        
        private void InitializeComponents()
        {
            // Find components if not assigned
            if (_armTransform == null)
            {
                _armTransform = transform.Find("Arm");
            }
            
            if (_slingTransform == null)
            {
                _slingTransform = transform.Find("Sling");
            }
            
            if (_launchPoint == null)
            {
                _launchPoint = transform.Find("LaunchPoint");
                if (_launchPoint == null && _slingTransform != null)
                {
                    _launchPoint = _slingTransform;
                }
            }
            
            if (_baseTransform == null)
            {
                _baseTransform = transform;
            }
            
            // Setup ghost arc
            if (_ghostArc != null)
            {
                _ghostArc.startWidth = 0.1f;
                _ghostArc.endWidth = 0.05f;
                _ghostArc.material = new Material(Shader.Find("Sprites/Default"));
                _ghostArc.startColor = new Color(1f, 1f, 1f, 0.5f);
                _ghostArc.endColor = new Color(1f, 1f, 1f, 0.2f);
            }
        }
        
        #region Parameter Control
        
        /// <summary>
        /// Sets the arm pullback strength.
        /// </summary>
        public void SetPullback(float value)
        {
            float clampedValue = Mathf.Clamp(value, _minPullback, _maxPullback);
            _parameters.armPullbackStrength = clampedValue * _pullbackSensitivity;
            _parameters.CalculateLaunchPhysics();
            
            UpdateArmPosition();
            OnParameterChanged?.Invoke(_parameters);
            
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.PARAMETER_CHANGED, "pullback");
        }
        
        /// <summary>
        /// Sets the release angle.
        /// </summary>
        public void SetAngle(float value)
        {
            float clampedValue = Mathf.Clamp(value, _minAngle, _maxAngle);
            _parameters.releaseAngle = clampedValue * _aimSensitivity;
            _parameters.CalculateLaunchPhysics();
            
            UpdateArmRotation();
            OnParameterChanged?.Invoke(_parameters);
            
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.PARAMETER_CHANGED, "angle");
        }
        
        /// <summary>
        /// Sets the counterweight mass.
        /// </summary>
        public void SetCounterweight(float value)
        {
            _parameters.counterweightMass = value;
            _parameters.CalculateLaunchPhysics();
            OnParameterChanged?.Invoke(_parameters);
        }
        
        /// <summary>
        /// Sets the sling length.
        /// </summary>
        public void SetSlingLength(float value)
        {
            _parameters.slingLength = value;
            _parameters.CalculateLaunchPhysics();
            UpdateSlingLength();
            OnParameterChanged?.Invoke(_parameters);
        }
        
        #endregion
        
        #region Firing
        
        /// <summary>
        /// Fires the trebuchet with the specified ammunition.
        /// </summary>
        public void Fire(Ammunition.AmmunitionData ammo)
        {
            if (_isFiring || ammo == null)
            {
                Debug.LogWarning("[TrebuchetController] Cannot fire: already firing or invalid ammo");
                return;
            }
            
            Debug.Log($"[TrebuchetController] Firing with {ammo.type}");
            
            _isFiring = true;
            
            // Spawn projectile
            GameObject projectileObj = Instantiate(ammo.projectilePrefab, _launchPoint.position, Quaternion.identity);
            Ammunition.Projectile projectile = projectileObj.GetComponent<Ammunition.Projectile>();
            
            if (projectile != null)
            {
                projectile.Initialize(_parameters.launchVelocity, _parameters.launchDirection, ammo);
            }
            
            // Play fire animation
            if (_fireAnimationCoroutine != null)
            {
                StopCoroutine(_fireAnimationCoroutine);
            }
            _fireAnimationCoroutine = StartCoroutine(FireAnimationCoroutine());
            
            // Notify game manager
            Core.GameManager.Instance?.OnProjectileFired();
            
            // Trigger events
            OnFired?.Invoke(ammo);
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.TREBUCHET_FIRED, ammo);
            
            // Play sound
            PlayFireSound(ammo);
        }
        
        private IEnumerator FireAnimationCoroutine()
        {
            float elapsed = 0f;
            Quaternion startRotation = _armTransform.localRotation;
            Quaternion endRotation = Quaternion.Euler(0f, 0f, 45f);
            
            while (elapsed < _fireAnimationDuration)
            {
                float t = elapsed / _fireAnimationDuration;
                float curveValue = _fireAnimationCurve != null ? _fireAnimationCurve.Evaluate(t) : t;
                
                _armTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, curveValue);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            _armTransform.localRotation = endRotation;
            
            // Reset after delay
            yield return new WaitForSeconds(0.5f);
            
            ResetToDefaultPosition();
            _isFiring = false;
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateArmPosition()
        {
            if (_armTransform != null)
            {
                float pullbackRatio = _parameters.armPullbackStrength / _maxPullback;
                float armRotation = _parameters.GetArmRotation();
                _armTransform.localRotation = Quaternion.Euler(0f, 0f, armRotation);
            }
        }
        
        private void UpdateArmRotation()
        {
            if (_baseTransform != null)
            {
                _baseTransform.rotation = Quaternion.Euler(0f, 0f, _parameters.releaseAngle);
            }
        }
        
        private void UpdateSlingLength()
        {
            if (_slingTransform != null)
            {
                // Visual representation of sling length
                float scale = _parameters.slingLength / 2f;
                _slingTransform.localScale = new Vector3(1f, scale, 1f);
            }
        }
        
        private void UpdateTrajectory()
        {
            if (_ghostArc == null || _launchPoint == null) return;
            
            // Get current level environment
            Level.EnvironmentVariables environment = Core.GameManager.Instance?.CurrentLevel?.environment;
            if (environment == null)
            {
                environment = new Level.EnvironmentVariables();
            }
            
            // Check for simplified physics mode
            bool simplifiedPhysics = Save.SaveManager.Instance?.SettingsData?.simplifiedPhysics ?? false;
            
            Vector3[] points;
            if (simplifiedPhysics)
            {
                points = Physics.TrajectoryPredictor.PredictSimplified(
                    _launchPoint.position,
                    _parameters.launchVelocity,
                    _parameters.launchDirection,
                    environment
                );
            }
            else
            {
                points = Physics.TrajectoryPredictor.Predict(
                    _launchPoint.position,
                    _parameters.launchVelocity,
                    _parameters.launchDirection,
                    environment
                );
            }
            
            _ghostArc.positionCount = points.Length;
            _ghostArc.SetPositions(points);
            
            // Show projectile at impact point
            if (_projectilePreview != null)
            {
                _projectilePreview.transform.position = points[points.Length - 1];
                _projectilePreview.SetActive(true);
            }
        }
        
        private void UpdateGhostArcVisibility()
        {
            if (_ghostArc != null)
            {
                _ghostArc.enabled = _ghostArcEnabled;
            }
            
            if (_projectilePreview != null)
            {
                _projectilePreview.SetActive(_ghostArcEnabled);
            }
        }
        
        #endregion
        
        #region Reset
        
        /// <summary>
        /// Resets the trebuchet to default position.
        /// </summary>
        public void ResetToDefaultPosition()
        {
            StopAllCoroutines();
            _isFiring = false;
            
            if (_armTransform != null)
            {
                _armTransform.localRotation = Quaternion.identity;
            }
            
            if (_baseTransform != null)
            {
                _baseTransform.rotation = Quaternion.Euler(0f, 0f, _parameters.releaseAngle);
            }
            
            OnReset?.Invoke();
            Core.EventManager.Instance?.TriggerEvent(Core.GameEvents.TREBUCHET_RESET);
        }
        
        /// <summary>
        /// Resets all parameters to default values.
        /// </summary>
        public void ResetParameters()
        {
            _parameters.ResetToDefaults();
            UpdateArmPosition();
            UpdateArmRotation();
            UpdateSlingLength();
            UpdateTrajectory();
            OnParameterChanged?.Invoke(_parameters);
        }
        
        #endregion
        
        #region Upgrades
        
        /// <summary>
        /// Applies an upgrade to the trebuchet.
        /// </summary>
        public void ApplyUpgrade(TrebuchetUpgrade upgrade)
        {
            if (upgrade == null) return;
            
            // Apply parameter bonuses
            _maxPullback += upgrade.maxPullbackBonus;
            _maxAngle += upgrade.maxAngleBonus;
            _parameters.counterweightMass += upgrade.maxCounterweightBonus;
            _parameters.slingLength += upgrade.maxSlingLengthBonus;
            
            // Apply visual changes
            if (upgrade.visualModel != null)
            {
                // Replace visual model (implementation depends on prefab structure)
            }
            
            if (upgrade.upgradeMaterial != null)
            {
                // Apply material to trebuchet components
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.material = upgrade.upgradeMaterial;
                }
            }
            
            Debug.Log($"[TrebuchetController] Applied upgrade: {upgrade.upgradeName}");
        }
        
        #endregion
        
        #region Audio
        
        private void PlayFireSound(Ammunition.AmmunitionData ammo)
        {
            if (ammo?.fireSound != null)
            {
                AudioSource.PlayClipAtPoint(ammo.fireSound, transform.position);
            }
        }
        
        #endregion
        
        #region Aiming State
        
        /// <summary>
        /// Sets the aiming state.
        /// </summary>
        public void SetAiming(bool aiming)
        {
            _isAiming = aiming;
            
            if (!aiming)
            {
                // Hide ghost arc when not aiming
                if (_ghostArc != null)
                {
                    _ghostArc.positionCount = 0;
                }
                
                if (_projectilePreview != null)
                {
                    _projectilePreview.SetActive(false);
                }
            }
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (_launchPoint != null)
            {
                // Draw launch direction
                Gizmos.color = Color.yellow;
                Vector3 direction = _parameters?.launchDirection ?? Vector3.up;
                Gizmos.DrawLine(_launchPoint.position, _launchPoint.position + direction * 2f);
                
                // Draw launch point
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_launchPoint.position, 0.1f);
            }
        }
        
        #endregion
    }
}
