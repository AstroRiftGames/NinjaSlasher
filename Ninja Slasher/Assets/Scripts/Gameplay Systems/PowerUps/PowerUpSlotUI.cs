using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PowerUpSlotUI : MonoBehaviour
{
    [Header("UI REFERENCES")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI usesText;
    [SerializeField] private Button activateButton;
    [SerializeField] private GameObject activeIndicator;
    [SerializeField] private GameObject _activeFrameOverlay;

    [Header("ACTIVE FRAME")]
    [SerializeField] private Color _activeFrameColor = new Color(1f, 0.84f, 0.2f, 1f);
    [SerializeField] private Vector2 _activeFramePadding = Vector2.zero;
    [SerializeField] private float _activeFrameThickness = 4f;

    [Header("QUANTITY / COST")]
    [SerializeField] private GameObject _quantityContainer;
    [SerializeField] private TextMeshProUGUI _quantityText;

    private PowerUpInventoryItem _item;
    private Action<PowerUpInventoryItem, PowerUpBase> _onInteractCallback;
    private PowerUpBase _powerUpBase;
    private PowerUpType _powerUpType;
    private Image _activeFrameImage;
    private bool _activeFrameOverlayWasGenerated;
    private bool _isPregameSelected;

    private void Awake()
    {
        EnsureActiveFrameOverlay();
    }

    void OnEnable()
    {
        GameEvents.OnPowerUpUsesUpdated += OnUsesUpdated;
        GameEvents.OnPowerUpExpired += OnPowerUpExpired;
        GameEvents.OnCoinsChanged += OnCoinsChanged;
    }

    void OnDisable()
    {
        GameEvents.OnPowerUpUsesUpdated -= OnUsesUpdated;
        GameEvents.OnPowerUpExpired -= OnPowerUpExpired;
        GameEvents.OnCoinsChanged -= OnCoinsChanged;
    }

    public void Setup(PowerUpInventoryItem item, PowerUpBase powerUpBase,
                      Action<PowerUpInventoryItem, PowerUpBase> onInteract)
    {
        _item = item;
        _onInteractCallback = onInteract;
        _powerUpBase = powerUpBase;
        _powerUpType = powerUpBase != null ? powerUpBase.powerUpType : default;

        if (iconImage != null)
            iconImage.sprite = powerUpBase != null ? powerUpBase.icon : null;

        if (nameText != null)
            nameText.text = powerUpBase != null ? powerUpBase.displayName : string.Empty;

        BindInteractButton();

        EnsureActiveFrameOverlay();
        RefreshState();
    }

    public void SetPregameSelected(bool selected)
    {
        _isPregameSelected = selected;
        RefreshState();
    }

    public void Clear()
    {
        _item = null;
        _onInteractCallback = null;
        _powerUpBase = null;
        _powerUpType = default;
        _isPregameSelected = false;

        if (activateButton != null)
        {
            activateButton.onClick.RemoveAllListeners();
            activateButton.interactable = false;
        }

        if (iconImage != null)
            iconImage.sprite = null;

        if (nameText != null)
            nameText.text = string.Empty;

        if (_quantityContainer != null)
            _quantityContainer.SetActive(false);

        if (_quantityText != null)
            _quantityText.text = string.Empty;

        if (usesText != null)
        {
            usesText.text = string.Empty;
            usesText.gameObject.SetActive(false);
        }

        if (activeIndicator != null)
            activeIndicator.SetActive(false);

        if (_activeFrameOverlay != null)
            _activeFrameOverlay.SetActive(false);
    }

    private void BindInteractButton()
    {
        if (activateButton == null)
            return;

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(OnInteractPressed);
    }

    private void RefreshState()
    {
        if (PowerUpManager.Instance == null || SaveManager.Instance == null)
        {
            if (activateButton != null)
                activateButton.interactable = false;
            return;
        }

        if (_item == null || _powerUpBase == null)
        {
            Clear();
            return;
        }

        PowerUpManager powerUpManager = PowerUpManager.Instance;
        bool isActive = powerUpManager != null && powerUpManager.IsPowerUpActive(_powerUpType);

        if (_quantityContainer != null)
            _quantityContainer.SetActive(!isActive);

        if (_quantityText != null && !isActive)
            _quantityText.text = $"{_item.quantity}";

        if (activeIndicator != null)
            activeIndicator.SetActive(isActive);

        if (_activeFrameOverlay != null)
        {
            if (isActive)
            {
                _activeFrameOverlay.SetActive(true);
            }
            else if (_isPregameSelected)
            {
                _activeFrameOverlay.SetActive(true);
            }
            else
            {
                _activeFrameOverlay.SetActive(false);
            }
        }

        if (usesText != null)
        {
            if (isActive)
            {
                int usesRemaining = powerUpManager != null ? powerUpManager.GetRemainingUses(_powerUpType) : 0;
                usesText.gameObject.SetActive(true);
                usesText.text = $"{usesRemaining} usos";
            }
            else
            {
                usesText.gameObject.SetActive(false);
            }
        }

        if (activateButton != null)
            activateButton.interactable = !isActive;
    }

    private void OnInteractPressed()
    {
        _onInteractCallback?.Invoke(_item, _powerUpBase);
    }

    private void OnCoinsChanged(int _) => RefreshState();

    private void OnUsesUpdated(PowerUpType type, int usesRemaining)
    {
        if (type == _powerUpType) RefreshState();
    }

    private void OnPowerUpExpired(PowerUpType type)
    {
        if (type == _powerUpType) RefreshState();
    }

    private void EnsureActiveFrameOverlay()
    {
        if (_activeFrameOverlay != null)
        {
            CacheActiveFrameImage();
            NormalizeActiveFrameOverlayRect();
            _activeFrameOverlay.transform.SetAsLastSibling();

            if (_activeFrameImage != null)
                _activeFrameImage.raycastTarget = false;

            return;
        }

        if (activateButton == null)
            return;

        RectTransform buttonRect = activateButton.transform as RectTransform;
        if (buttonRect == null)
            return;

        GameObject frameObject = new GameObject("ActiveFrameOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.SetParent(buttonRect, false);
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        frameRect.SetAsLastSibling();

        _activeFrameOverlay = frameObject;
        _activeFrameOverlayWasGenerated = true;
        CacheActiveFrameImage();
        ApplyActiveFrameVisuals();
        _activeFrameOverlay.SetActive(false);
    }

    private void CacheActiveFrameImage()
    {
        if (_activeFrameOverlay == null)
            return;

        if (_activeFrameImage == null)
            _activeFrameImage = _activeFrameOverlay.GetComponent<Image>();
    }

    private void ApplyActiveFrameVisuals()
    {
        if (_activeFrameOverlay == null)
            return;

        NormalizeActiveFrameOverlayRect();

        if (!_activeFrameOverlayWasGenerated)
            return;

        _activeFrameOverlay.transform.SetAsLastSibling();

        RectTransform frameRect = _activeFrameOverlay.transform as RectTransform;

        if (_activeFrameImage != null)
        {
            _activeFrameImage.color = new Color(_activeFrameColor.r, _activeFrameColor.g, _activeFrameColor.b, 0f);
            _activeFrameImage.raycastTarget = false;
        }

        Outline outline = _activeFrameOverlay.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = _activeFrameColor;
            outline.effectDistance = new Vector2(_activeFrameThickness, _activeFrameThickness);
            outline.useGraphicAlpha = false;
        }
    }

    private void NormalizeActiveFrameOverlayRect()
    {
        RectTransform frameRect = _activeFrameOverlay.transform as RectTransform;
        if (frameRect == null)
            return;

        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = Vector2.zero;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
    }
}
