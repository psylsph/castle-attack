using UnityEngine;

namespace Siege.UI
{
    /// <summary>
    /// Handles touch input for mobile controls.
    /// Supports drag, swipe, and tap gestures.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _dragSensitivity = 1f;
        [SerializeField] private float _swipeThreshold = 50f;
        [SerializeField] private float _tapMaxDistance = 10f;
        [SerializeField] private float _tapMaxTime = 0.3f;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        
        private bool _isEnabled = true;
        private bool _isDragging = false;
        private Vector2 _dragStartPos;
        private float _dragStartTime;
        private Vector2 _currentDragDelta;
        
        // Events
        public event System.Action<Vector2> OnDrag;
        public event System.Action<Vector2> OnSwipeUp;
        public event System.Action<Vector2> OnSwipeDown;
        public event System.Action<Vector2> OnSwipeLeft;
        public event System.Action<Vector2> OnSwipeRight;
        public event System.Action<Vector2> OnTap;
        public event System.Action OnTouchBegan;
        public event System.Action OnTouchEnded;
        
        public bool isEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
        
        private void Update()
        {
            if (!_isEnabled) return;
            
            HandleTouchInput();
        }
        
        /// <summary>
        /// Initializes the touch controls.
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[TouchControls] Initialized.");
        }
        
        /// <summary>
        /// Enables or disables touch controls.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _isDragging = false;
            
            if (_debugMode)
            {
                Debug.Log($"[TouchControls] {(enabled ? "Enabled" : "Disabled")}");
            }
        }
        
        private void HandleTouchInput()
        {
#if UNITY_ANDROID || UNITY_IOS
            HandleMobileTouch();
#else
            HandleDesktopInput();
#endif
        }
        
        private void HandleMobileTouch()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnTouchBeganAction(touch.position);
                        break;
                        
                    case TouchPhase.Moved:
                        OnTouchMovedAction(touch.position);
                        break;
                        
                    case TouchPhase.Ended:
                        OnTouchEndedAction(touch.position);
                        break;
                        
                    case TouchPhase.Canceled:
                        OnTouchCanceledAction();
                        break;
                }
            }
        }
        
        private void HandleDesktopInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnTouchBeganAction(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                OnTouchMovedAction(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                OnTouchEndedAction(Input.mousePosition);
            }
        }
        
        private void OnTouchBeganAction(Vector2 position)
        {
            _dragStartPos = position;
            _dragStartTime = Time.time;
            _isDragging = true;
            _currentDragDelta = Vector2.zero;
            
            OnTouchBegan?.Invoke();
            
            if (_debugMode)
            {
                Debug.Log($"[TouchControls] Touch began at: {position}");
            }
        }
        
        private void OnTouchMovedAction(Vector2 position)
        {
            if (!_isDragging) return;
            
            _currentDragDelta = position - _dragStartPos;
            
            OnDrag?.Invoke(_currentDragDelta * _dragSensitivity);
            
            if (_debugMode)
            {
                Debug.Log($"[TouchControls] Dragging: {_currentDragDelta}");
            }
        }
        
        private void OnTouchEndedAction(Vector2 position)
        {
            if (!_isDragging) return;
            
            Vector2 totalDelta = position - _dragStartPos;
            float dragDuration = Time.time - _dragStartTime;
            
            // Check for tap
            if (totalDelta.magnitude < _tapMaxDistance && dragDuration < _tapMaxTime)
            {
                OnTap?.Invoke(position);
                
                if (_debugMode)
                {
                    Debug.Log($"[TouchControls] Tap at: {position}");
                }
            }
            // Check for swipe
            else if (totalDelta.magnitude > _swipeThreshold)
            {
                DetectSwipe(totalDelta);
            }
            
            _isDragging = false;
            OnTouchEnded?.Invoke();
            
            if (_debugMode)
            {
                Debug.Log($"[TouchControls] Touch ended at: {position}");
            }
        }
        
        private void OnTouchCanceledAction()
        {
            _isDragging = false;
            _currentDragDelta = Vector2.zero;
            
            OnTouchEnded?.Invoke();
        }
        
        private void DetectSwipe(Vector2 delta)
        {
            // Determine swipe direction based on which axis has greater magnitude
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // Horizontal swipe
                if (delta.x > 0)
                {
                    OnSwipeRight?.Invoke(delta);
                    if (_debugMode) Debug.Log($"[TouchControls] Swipe right: {delta}");
                }
                else
                {
                    OnSwipeLeft?.Invoke(delta);
                    if (_debugMode) Debug.Log($"[TouchControls] Swipe left: {delta}");
                }
            }
            else
            {
                // Vertical swipe
                if (delta.y > 0)
                {
                    OnSwipeUp?.Invoke(delta);
                    if (_debugMode) Debug.Log($"[TouchControls] Swipe up: {delta}");
                }
                else
                {
                    OnSwipeDown?.Invoke(delta);
                    if (_debugMode) Debug.Log($"[TouchControls] Swipe down: {delta}");
                }
            }
        }
        
        /// <summary>
        /// Gets the current drag delta.
        /// </summary>
        public Vector2 GetDragDelta()
        {
            return _currentDragDelta;
        }
        
        /// <summary>
        /// Checks if currently dragging.
        /// </summary>
        public bool IsDragging()
        {
            return _isDragging;
        }
        
        /// <summary>
        /// Gets the drag start position.
        /// </summary>
        public Vector2 GetDragStartPos()
        {
            return _dragStartPos;
        }
    }
}
