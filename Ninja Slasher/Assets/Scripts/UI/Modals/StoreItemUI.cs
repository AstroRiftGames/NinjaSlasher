using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemUI : MonoBehaviour
{
    [Tooltip("Must match a product ID in StoreCatalog.")]
    [SerializeField] private string _productId;

    [SerializeField] private Button   _buyButton;
    [SerializeField] private TMP_Text _priceLabel;

    private void OnEnable()
    {
        _buyButton?.onClick.AddListener(OnBuyClicked);

        if (StoreService.Instance == null) return;

        if (StoreService.Instance.IsIAPReady)
            RefreshPrice();
        else
            StoreService.Instance.OnIAPReady += RefreshPrice;
    }

    private void OnDisable()
    {
        _buyButton?.onClick.RemoveListener(OnBuyClicked);

        if (StoreService.Instance != null)
            StoreService.Instance.OnIAPReady -= RefreshPrice;
    }

    private void RefreshPrice()
    {
        if (_priceLabel == null) return;
        _priceLabel.text = StoreService.Instance?.GetPrice(_productId) ?? "N/A";
    }

    private void OnBuyClicked()
    {
        if (string.IsNullOrEmpty(_productId)) return;
        StoreService.Instance?.Buy(_productId);
    }
}
