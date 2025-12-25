using UnityEngine;
using System.Collections;

namespace Siege.UI
{
    /// <summary>
    /// Controls the main camera including zoom, pan, and projectile following.
    /// Supports both touch and mouse input.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }
        
        [Header("Camera Settings")]
        [SerializeField] private float _minZoom = 0.5f;
        [SerializeField] private float _maxZoom = 2f;
        [SerializeField] private float _zoomSpeed = 0.1f;
        [SerializeField] private float _panSpeed = 1f;
        [SerializeField] private float _smoothTime = 0.2f;
        
        [Header("Following")]
        [SerializeField] private bool _followProjectile = false;
        [SerializeField] private Transform _projectileTarget;
        [SerializeField] private float _followSmoothTime = 0.1f;
        [SerializeField] private float _followDelay = 0.5f;
        
        [Header("Bounds")]
        [SerializeField] private bool _useBounds = true;
        [SerializeField] private Vector2 _minBounds = new Vector2(-50f, -20f);
        [SerializeField] private Vector2 _maxBounds = new Vector2(50f, 20f);
        
        [Header("Screen Shake")]
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private float _shakeMagnitude = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        
        private Camera _camera;
        private float _currentZoom;
        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private Coroutine _shakeCoroutine;
        private Coroutine _followCoroutine;
        private bool _isInitialized = false;
        
        public Camera mainCamera => _camera;
        public float currentZoom => _currentZoom;
        
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
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            
            _currentZoom = _camera.orthographicSize;
            _targetPosition = transform.position;
            
            _isInitialized = true;
            
            Debug.Log("[CameraController] Initialized.");
        }
        
        private void Update()
        {
            if (!_isInitialized) return;
            
            HandleInput();
            UpdateCameraPosition();
        }
        
        /// <summary>
        /// Initializes the camera controller.
        /// </summary>
        public void Initialize()
        {
            _isInitialized = true;
            Debug.Log("[CameraController] Initialized.");
        }
        
        private void HandleInput()
        {
            // Don't handle input while following projectile
            if (_followProjectile && _projectileTarget != null) return;
            
            // Zoom input
            float zoomInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                Zoom(zoomInput * -_zoomSpeed);
            }
            
            // Pan input (middle mouse button or touch drag)
            if (Input.GetMouseButton(2) || (Input.touchCount == 2))
            {
                Pan();
            }
        }
        
        private void UpdateCameraPosition()
        {
            Vector3 targetPos = _targetPosition;
            
            // Smooth follow
            if (_followProjectile && _projectileTarget != null)
            {
                targetPos = _projectileTarget.position;
            }
            
            // Smooth damp
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);
            
            // Apply bounds
            if (_useBounds)
            {
                Vector3 clampedPos = transform.position;
                clampedPos.x = Mathf.Clamp(clampedPos.x, _minBounds.x, _maxBounds.x);
                clampedPos.y = Mathf.Clamp(clampedPos.y, _minBounds.y, _maxBounds.y);
                transform.position = clampedPos;
            }
            
            // Update zoom
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _currentZoom, Time.deltaTime * 10f);
        }
        
        #region Camera Movement
        
        /// <summary>
        /// Pans the camera based on input.
        /// </summary>
        private void Pan()
        {
            Vector3 panDelta = Vector3.zero;
            
#if UNITY_ANDROID || UNITY_IOS
            // Touch pan
            if (Input.touchCount == 2)
            {
                Vector2 touchDelta = Input.GetTouch(0).deltaPosition - Input.GetTouch(1).deltaPosition;
                panDelta = new Vector3(-touchDelta.x, -touchDelta.y, 0f) * _panSpeed * Time.deltaTime;
            }
#else
            // Mouse pan
            Vector3 mouseDelta = new Vector3(-Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"), 0f);
            panDelta = mouseDelta * _panSpeed * Time.deltaTime * 5f;
#endif
            
            _targetPosition += panDelta;
        }
        
        /// <summary>
        /// Zooms the camera by the specified amount.
        /// </summary>
        public void Zoom(float delta)
        {
            _currentZoom += delta;
            _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
            
            if (_debugMode)
            {
                Debug.Log($"[CameraController] Zoom: {_currentZoom}");
            }
        }
        
        /// <summary>
        /// Sets the zoom level.
        /// </summary>
        public void SetZoom(float zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
        }
        
        /// <summary>
        /// Moves the camera to a target position.
        /// </summary>
        public void PanTo(Vector2 target)
        {
            _targetPosition = new Vector3(target.x, target.y, transform.position.z);
            
            if (_debugMode)
            {
                Debug.Log($"[CameraController] Panning to: {target}");
            }
        }
        
        /// <summary>
        /// Instantly moves the camera to a position.
        /// </summary>
        public void TeleportTo(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            _targetPosition = transform.position;
            _velocity = Vector3.zero;
        }
        
        #endregion
        
        #region Projectile Following
        
        /// <summary>
        /// Starts following a projectile.
        /// </summary>
        public void FollowProjectile(Ammunition.Projectile projectile)
        {
            if (projectile == null) return;
            
            _projectileTarget = projectile.transform;
            _followProjectile = true;
            
            if (_followCoroutine != null)
            {
                StopCoroutine(_followCoroutine);
            }
            _followCoroutine = StartCoroutine(FollowProjectileCoroutine());
            
            if (_debugMode)
            {
                Debug.Log("[CameraController] Following projectile");
            }
        }
        
        /// <summary>
        /// Stops following the projectile and returns to trebuchet.
        /// </summary>
        public void ReturnToTrebuchet()
        {
            if (_followCoroutine != null)
            {
                StopCoroutine(_followCoroutine);
            }
            
            _followProjectile = false;
            _projectileTarget = null;
            
            // Pan to trebuchet
            if (Trebuchet.TrebuchetController.Instance != null)
            {
                PanTo(Trebuchet.TrebuchetController.Instance.transform.position);
            }
            
            if (_debugMode)
            {
                Debug.Log("[CameraController] Returned to trebuchet");
            }
        }
        
        private IEnumerator FollowProjectileCoroutine()
        {
            // Wait for delay before following
            yield return new WaitForSeconds(_followDelay);
            
            while (_followProjectile && _projectileTarget != null)
            {
                // Check if projectile is destroyed
                Ammunition.Projectile projectile = _projectileTarget.GetComponent<Ammunition.Projectile>();
                if (projectile == null || projectile.HasImpacted)
                {
                    break;
                }
                
                // Follow projectile
                _targetPosition = _projectileTarget.position;
                
                yield return null;
            }
            
            // Return to trebuchet after impact
            ReturnToTrebuchet();
        }
        
        #endregion
        
        #region Screen Shake
        
        /// <summary>
        /// Shakes the camera for dramatic effect.
        /// </summary>
        public void ShakeCamera(float duration = 0.3f, float magnitude = 0.5f)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }
            _shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
        }
        
        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            Vector3 originalPosition = transform.position;
            
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                
                transform.position = originalPosition + new Vector3(x, y, 0f);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = originalPosition;
        }
        
        #endregion
        
        #region Bounds
        
        /// <summary>
        /// Sets the camera bounds.
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            _minBounds = min;
            _maxBounds = max;
        }
        
        /// <summary>
        /// Enables or disables bounds.
        /// </summary>
        public void SetBoundsEnabled(bool enabled)
        {
            _useBounds = enabled;
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!_debugMode) return;
            
            // Draw bounds
            if (_useBounds)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = new Vector3((_minBounds.x + _maxBounds.x) / 2f, (_minBounds.y + _maxBounds.y) / 2f, 0f);
                Vector3 size = new Vector3(_maxBounds.x - _minBounds.x, _maxBounds.y - _minBounds.y, 0f);
                Gizmos.DrawWireCube(center, size);
            }
            
            // Draw target position
            if (_followProjectile && _projectileTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_targetPosition, 0.5f);
            }
        }
        
        #endregion
    }
}
