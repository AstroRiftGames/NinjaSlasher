using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IAPTestButton : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string productId;

    [Header("UI References")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI priceText;

    [SerializeField] UIAudioContext _audioContext;

    private void Start()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
            buyButton.interactable = false;
        }

        UpdateStatus("Initializing IAP...");

        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnIAPInitialized += OnIAPReady;
            IAPManager.Instance.OnIAPInitializationFailed += OnIAPFailed;
            IAPManager.Instance.OnPurchaseCompleted += OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
        }
        else
        {
            UpdateStatus("ERROR: IAPManager not found");
        }
    }

    private void OnIAPReady()
    {
        UpdateStatus("IAP Ready!");

        if (buyButton != null)
        {
            buyButton.interactable = true;
        }

        var product = IAPManager.Instance.GetProduct(productId);
        if (product != null)
        {
            if (priceText != null)
            {
                priceText.text = product.metadata.localizedPriceString;
            }
            Debug.Log($"[IAPTest] Product found: {product.metadata.localizedTitle} - {product.metadata.localizedPriceString}");
        }
        else
        {
            Debug.LogWarning($"[IAPTest] Product not found: {productId}");
            UpdateStatus($"Product {productId} not found");
        }
    }

    private void OnIAPFailed(string error)
    {
        UpdateStatus($"Error IAP: {error}");
        Debug.LogError($"[IAPTest] Inicialization failed: {error}");
    }

    private void OnBuyClicked()
    {
        if (IAPManager.Instance.IsInitialized)
        {
            UpdateStatus("Buying");
            buyButton.interactable = false;

            IAPManager.Instance.PurchaseProduct(productId);
        }
        else
        {
            UpdateStatus("IAP not initialized");
        }
    }

    private void OnPurchaseSuccess(string purchasedProductId)
    {
        if (purchasedProductId == productId)
        {
            UpdateStatus($"Purchase completed: {purchasedProductId}");
            Debug.Log($"[IAPTest] Purchase completed: {purchasedProductId}");

            //AudioManager.Instance.PlaySFX(SFXClip.Reward_Coins);
            AudioService.Instance.PlaySFX(_audioContext.Audio.rewardCoins);

            // Aca se otorgaria la recompensa al jugador
            // Metodo.AddBentos(100);
        }

        buyButton.interactable = true;
    }

    private void OnPurchaseFailed(string failedProductId, string reason)
    {
        if (failedProductId == productId)
        {
            UpdateStatus($"Purchase failed: {reason}");
            Debug.LogError($"[IAPTest] Purchase failed: {failedProductId} - {reason}");
        }

        buyButton.interactable = true;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        //Debug.Log($"[IAPTest] {message}");
    }

    private void OnDestroy()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnIAPInitialized -= OnIAPReady;
            IAPManager.Instance.OnIAPInitializationFailed -= OnIAPFailed;
            IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(OnBuyClicked);
        }
    }
}