using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PowerUpSlotUI : MonoBehaviour
{
    [Header("UI REFERENCES")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI usesText;
    [SerializeField] private Button activateButton;
    [SerializeField] private GameObject activeIndicator;

    private PowerUpInventoryItem _item;
    private Action<PowerUpInventoryItem> _onActivateCallback;
    private PowerUpType _powerUpType;

    void OnEnable()
    {
        GameEvents.OnPowerUpUsesUpdated += OnUsesUpdated;
        GameEvents.OnPowerUpExpired += OnPowerUpExpired;
    }

    void OnDisable()
    {
        GameEvents.OnPowerUpUsesUpdated -= OnUsesUpdated;
        GameEvents.OnPowerUpExpired -= OnPowerUpExpired;
    }

    public void Setup(PowerUpInventoryItem item, PowerUpBase powerUpBase, Action<PowerUpInventoryItem> onActivate)
    {
        _item = item;
        _onActivateCallback = onActivate;
        _powerUpType = powerUpBase.powerUpType;

        iconImage.sprite = powerUpBase.icon;
        nameText.text = powerUpBase.displayName;
        quantityText.text = $"x{item.quantity}";

        activateButton.interactable = item.quantity > 0;

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(OnActivatePressed);

        UpdateActiveState();
    }

    private void OnActivatePressed()
    {
        if (_item.quantity > 0)
        {
            AudioManager.Instance.PlaySFX(SFXClip.UI_PowerUp);
            _onActivateCallback?.Invoke(_item);

            UpdateActiveState();
        }
    }

    private void UpdateActiveState()
    {
        bool isActive = PowerUpManager.Instance.IsPowerUpActive(_powerUpType);

        if (activeIndicator != null)
        {
            activeIndicator.SetActive(isActive);
        }

        if (isActive && usesText != null)
        {
            int usesRemaining = PowerUpManager.Instance.GetRemainingUses(_powerUpType);
            usesText.gameObject.SetActive(true);
            usesText.text = $"{usesRemaining} usos";
        }
        else if (usesText != null)
        {
            usesText.gameObject.SetActive(false);
        }
    }

    private void OnUsesUpdated(PowerUpType type, int usesRemaining)
    {
        if (type == _powerUpType)
        {
            UpdateActiveState();
        }
    }

    private void OnPowerUpExpired(PowerUpType type)
    {
        if (type == _powerUpType)
        {
            UpdateActiveState();
        }
    }
}