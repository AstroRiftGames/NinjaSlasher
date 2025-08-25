using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PowerUpSlotUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button activateButton;

    private PowerUpInventoryItem _item;
    private Action<PowerUpInventoryItem> _onActivateCallback;

    public void Setup(PowerUpInventoryItem item, PowerUpBase powerUpBase, Action<PowerUpInventoryItem> onActivate)
    {
        _item = item;
        _onActivateCallback = onActivate;

        iconImage.sprite = powerUpBase.icon;
        nameText.text = powerUpBase.displayName;
        quantityText.text = $"{item.quantity}";

        activateButton.interactable = item.quantity > 0;

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(OnActivatePressed);
    }

    private void OnActivatePressed()
    {
        if (_item.quantity > 0)
        {
            AudioManager.Instance.PlaySFX(SFXClip.UI_PowerUp);
            _onActivateCallback?.Invoke(_item);
        }
    }
}
