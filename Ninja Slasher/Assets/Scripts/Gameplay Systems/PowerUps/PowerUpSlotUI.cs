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

    [Header("QUANTITY / COST")]
    [SerializeField] private GameObject _quantityContainer;
    [SerializeField] private TextMeshProUGUI _quantityText;

    private PowerUpInventoryItem _item;
    private Action<PowerUpInventoryItem, PowerUpBase> _onInteractCallback;
    private PowerUpBase _powerUpBase;
    private PowerUpType _powerUpType;

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
        _powerUpType = powerUpBase.powerUpType;

        iconImage.sprite = powerUpBase.icon;
        nameText.text = powerUpBase.displayName;

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(OnInteractPressed);

        RefreshState();
    }

    private void RefreshState()
    {
        if (_item == null) return;

        bool isActive = PowerUpManager.Instance.IsPowerUpActive(_powerUpType);

        if (_quantityContainer != null)
            _quantityContainer.SetActive(!isActive);

        if (_quantityText != null && !isActive)
            _quantityText.text = $"{_item.quantity}";

        if (activeIndicator != null)
            activeIndicator.SetActive(isActive);

        if (usesText != null)
        {
            if (isActive)
            {
                int usesRemaining = PowerUpManager.Instance.GetRemainingUses(_powerUpType);
                usesText.gameObject.SetActive(true);
                usesText.text = $"{usesRemaining} usos";
            }
            else
            {
                usesText.gameObject.SetActive(false);
            }
        }

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
}
