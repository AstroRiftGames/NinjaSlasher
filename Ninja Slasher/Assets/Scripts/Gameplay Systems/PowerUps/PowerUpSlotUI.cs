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

    [SerializeField] private GameObject _costContainer;
    [SerializeField] private TextMeshProUGUI _costText;

    private PowerUpInventoryItem _item;
    private Action<PowerUpInventoryItem> _onActivateCallback;
    private Action<PowerUpInventoryItem> _onPurchaseCallback;
    private PowerUpType _powerUpType;
    private int _cost;

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
                      Action<PowerUpInventoryItem> onActivate,
                      Action<PowerUpInventoryItem> onPurchase = null)
    {
        _item = item;
        _onActivateCallback = onActivate;
        _onPurchaseCallback = onPurchase;
        _powerUpType = powerUpBase.powerUpType;
        _cost = powerUpBase.cost;

        iconImage.sprite = powerUpBase.icon;
        nameText.text = powerUpBase.displayName;

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(() =>
        {
            if (_item.quantity > 0) OnActivatePressed();
            else OnBuyPressed();
        });

        RefreshState();
    }

    private void RefreshState()
    {
        if (_item == null) return;

        bool isActive = PowerUpManager.Instance.IsPowerUpActive(_powerUpType);
        bool hasStock = _item.quantity > 0;
        bool showCost = !hasStock && !isActive;
        bool canAfford = PowerUpPurchaseService.CanAfford(_cost);

        if (_quantityContainer != null)
            _quantityContainer.SetActive(!showCost);

        if (_quantityText != null && !showCost)
            _quantityText.text = $"x{_item.quantity}";

        if (_costContainer != null)
            _costContainer.SetActive(showCost);

        if (_costText != null && showCost)
            _costText.text = _cost.ToString();

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

        if (hasStock && !isActive)
            activateButton.interactable = true;
        else if (hasStock && isActive)
            activateButton.interactable = false;
        else if (!hasStock && canAfford)
            activateButton.interactable = true;
        else
            activateButton.interactable = false;
    }

    private void OnActivatePressed()
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_PowerUp);
        _onActivateCallback?.Invoke(_item);
        RefreshState();
    }

    private void OnBuyPressed()
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_PowerUp);
        _onPurchaseCallback?.Invoke(_item);
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
