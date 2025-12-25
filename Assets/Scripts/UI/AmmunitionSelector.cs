using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Siege.UI
{
    /// <summary>
    /// Manages ammunition selection UI.
    /// Allows player to choose from available ammunition types.
    /// </summary>
    public class AmmunitionSelector : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Transform _container;
        [SerializeField] private GameObject _ammunitionSlotPrefab;
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _normalColor = Color.white;
        
        [Header("Settings")]
        [SerializeField] private bool _autoSelect = true;
        
        private List<AmmunitionSlot> _slots = new List<AmmunitionSlot>();
        private Ammunition.AmmunitionType _selectedType = Ammunition.AmmunitionType.Stone;
        private Dictionary<Ammunition.AmmunitionType, int> _ammunitionCounts = new Dictionary<Ammunition.AmmunitionType, int>();
        
        // Events
        public event System.Action<Ammunition.AmmunitionType> OnAmmunitionSelected;
        
        public Ammunition.AmmunitionType SelectedType => _selectedType;
        
        private void Start()
        {
            InitializeSlots();
        }
        
        /// <summary>
        /// Initializes the ammunition selector.
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[AmmunitionSelector] Initialized.");
        }
        
        /// <summary>
        /// Loads ammunition from level data.
        /// </summary>
        public void LoadAmmunition(List<AmmunitionAllocation> ammunition)
        {
            ClearSlots();
            _ammunitionCounts.Clear();
            
            foreach (AmmunitionAllocation allocation in ammunition)
            {
                if (allocation.quantity > 0)
                {
                    _ammunitionCounts[allocation.type] = allocation.quantity;
                    CreateSlot(allocation.type, allocation.quantity);
                }
            }
            
            // Auto-select first available ammo
            if (_autoSelect && _slots.Count > 0)
            {
                SelectAmmunition(_slots[0].type);
            }
        }
        
        /// <summary>
        /// Clears all ammunition slots.
        /// </summary>
        public void ClearSlots()
        {
            foreach (AmmunitionSlot slot in _slots)
            {
                if (slot != null && slot.gameObject != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            
            _slots.Clear();
        }
        
        /// <summary>
        /// Creates an ammunition slot.
        /// </summary>
        private void CreateSlot(Ammunition.AmmunitionType type, int quantity)
        {
            if (_ammunitionSlotPrefab == null)
            {
                Debug.LogWarning("[AmmunitionSelector] No slot prefab assigned!");
                return;
            }
            
            GameObject slotObj = Instantiate(_ammunitionSlotPrefab, _container);
            AmmunitionSlot slot = slotObj.GetComponent<AmmunitionSlot>();
            
            if (slot == null)
            {
                slot = slotObj.AddComponent<AmmunitionSlot>();
            }
            
            slot.Initialize(type, quantity, this);
            _slots.Add(slot);
        }
        
        /// <summary>
        /// Selects an ammunition type.
        /// </summary>
        public void SelectAmmunition(Ammunition.AmmunitionType type)
        {
            // Check if type is available
            if (!_ammunitionCounts.ContainsKey(type) || _ammunitionCounts[type] <= 0)
            {
                Debug.LogWarning($"[AmmunitionSelector] {type} not available!");
                return;
            }
            
            _selectedType = type;
            
            // Update slot visuals
            foreach (AmmunitionSlot slot in _slots)
            {
                slot.SetSelected(slot.type == type);
            }
            
            OnAmmunitionSelected?.Invoke(type);
            
            Debug.Log($"[AmmunitionSelector] Selected: {type}");
        }
        
        /// <summary>
        /// Gets the currently selected ammunition type.
        /// </summary>
        public Ammunition.AmmunitionType GetSelectedType()
        {
            return _selectedType;
        }
        
        /// <summary>
        /// Consumes one unit of the selected ammunition.
        /// </summary>
        public void ConsumeAmmunition()
        {
            if (_ammunitionCounts.ContainsKey(_selectedType))
            {
                _ammunitionCounts[_selectedType]--;
                
                // Update slot display
                AmmunitionSlot slot = _slots.Find(s => s.type == _selectedType);
                if (slot != null)
                {
                    slot.SetQuantity(_ammunitionCounts[_selectedType]);
                }
                
                // If out of ammo, select next available
                if (_ammunitionCounts[_selectedType] <= 0)
                {
                    SelectNextAvailable();
                }
            }
        }
        
        /// <summary>
        /// Selects the next available ammunition type.
        /// </summary>
        private void SelectNextAvailable()
        {
            foreach (AmmunitionSlot slot in _slots)
            {
                if (_ammunitionCounts.ContainsKey(slot.type) && _ammunitionCounts[slot.type] > 0)
                {
                    SelectAmmunition(slot.type);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Checks if an ammunition type is available.
        /// </summary>
        public bool IsAmmunitionAvailable(Ammunition.AmmunitionType type)
        {
            return _ammunitionCounts.ContainsKey(type) && _ammunitionCounts[type] > 0;
        }
        
        /// <summary>
        /// Gets the count for an ammunition type.
        /// </summary>
        public int GetAmmunitionCount(Ammunition.AmmunitionType type)
        {
            return _ammunitionCounts.ContainsKey(type) ? _ammunitionCounts[type] : 0;
        }
    }
    
    /// <summary>
    /// Represents a single ammunition slot in the UI.
    /// </summary>
    public class AmmunitionSlot : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _quantityText;
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        
        [Header("Colors")]
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _disabledColor = Color.gray;
        
        public Ammunition.AmmunitionType type { get; private set; }
        private AmmunitionSelector _selector;
        private Ammunition.AmmunitionData _ammoData;
        
        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
            
            if (_background == null)
            {
                _background = GetComponent<Image>();
            }
        }
        
        private void Start()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }
        
        /// <summary>
        /// Initializes the ammunition slot.
        /// </summary>
        public void Initialize(Ammunition.AmmunitionType ammoType, int quantity, AmmunitionSelector selector)
        {
            type = ammoType;
            _selector = selector;
            
            // Load ammunition data
            _ammoData = LoadAmmunitionData(ammoType);
            
            // Update UI
            UpdateDisplay(quantity);
            
            // Load icon
            if (_icon != null && _ammoData != null)
            {
                _icon.sprite = _ammoData.icon;
            }
            
            // Set name
            if (_nameText != null)
            {
                _nameText.text = ammoType.ToString();
            }
        }
        
        /// <summary>
        /// Loads ammunition data from Resources.
        /// </summary>
        private Ammunition.AmmunitionData LoadAmmunitionData(Ammunition.AmmunitionType type)
        {
            string path = $"Ammunition/{type}";
            return Resources.Load<Ammunition.AmmunitionData>(path);
        }
        
        /// <summary>
        /// Updates the slot display.
        /// </summary>
        public void UpdateDisplay(int quantity)
        {
            if (_quantityText != null)
            {
                _quantityText.text = quantity.ToString();
            }
            
            // Disable if no ammo
            if (quantity <= 0 && _button != null)
            {
                _button.interactable = false;
                if (_background != null)
                {
                    _background.color = _disabledColor;
                }
            }
        }
        
        /// <summary>
        /// Sets the quantity display.
        /// </summary>
        public void SetQuantity(int quantity)
        {
            UpdateDisplay(quantity);
        }
        
        /// <summary>
        /// Sets the selected state.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_background != null)
            {
                _background.color = selected ? _selectedColor : _normalColor;
            }
        }
        
        private void OnClick()
        {
            _selector?.SelectAmmunition(type);
        }
    }
}
