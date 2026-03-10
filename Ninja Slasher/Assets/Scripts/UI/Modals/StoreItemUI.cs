using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemUI : MonoBehaviour
{
    private const string AdsRemovedLabel = "Ads Removed";

    [Tooltip("Must match a product ID in StoreCatalog.")]
    [SerializeField] private string _productId;

    [SerializeField] private Button _buyButton;
    [SerializeField] private TMP_Text _priceLabel;

    private void OnEnable()
    {
        _buyButton?.onClick.AddListener(OnBuyClicked);
        GameEvents.OnAdsRemoved += RefreshView;

        if (StoreService.Instance == null) return;

        RefreshView();

        if (StoreService.Instance.IsIAPReady)
            return;
        else
            StoreService.Instance.OnIAPReady += RefreshView;
    }

    private void OnDisable()
    {
        _buyButton?.onClick.RemoveListener(OnBuyClicked);
        GameEvents.OnAdsRemoved -= RefreshView;

        if (StoreService.Instance != null)
            StoreService.Instance.OnIAPReady -= RefreshView;
    }

    private void RefreshView()
    {
        var product = StoreService.Instance?.GetProduct(_productId);
        bool adsRemovedPurchased = product != null &&
                                   product.rewardType == RewardType.RemoveAds &&
                                   SaveManager.Instance != null &&
                                   SaveManager.Instance.GetAdsRemoved();

        if (_buyButton != null)
        {
            _buyButton.interactable = !adsRemovedPurchased;
            _buyButton.gameObject.SetActive(!adsRemovedPurchased);
        }

        if (_priceLabel == null) return;

        _priceLabel.text = adsRemovedPurchased
            ? AdsRemovedLabel
            : StoreService.Instance?.GetPrice(_productId) ?? "N/A";
    }

    private void OnBuyClicked()
    {
        if (string.IsNullOrEmpty(_productId)) return;
        StoreService.Instance?.Buy(_productId);
    }
}
