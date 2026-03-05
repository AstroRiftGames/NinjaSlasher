using UnityEngine;
using UnityEngine.Purchasing;

public class StorePurchaseHandler : MonoBehaviour
{
    [Header("Store Product IDs")]
    [SerializeField] private string _smallProductId  = "bento_small";
    [SerializeField] private string _mediumProductId = "bento_medium";
    [SerializeField] private string _largeProductId  = "bento_large";

    [Header("Configuration")]
    [SerializeField] private StoreConfig _config;


    private void OnEnable()
    {
        if (IAPManager.Instance == null || _config == null) return;

        if (IAPManager.Instance.IsInitialized)
            RegisterHandlers();
        else
            IAPManager.Instance.OnIAPInitialized += OnIAPReady;
    }

    private void OnDisable()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnIAPInitialized -= OnIAPReady;
            UnregisterHandlers();
        }
    }

    private void OnIAPReady()
    {
        IAPManager.Instance.OnIAPInitialized -= OnIAPReady;
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        Register(_smallProductId);
        Register(_mediumProductId);
        Register(_largeProductId);
    }

    private void UnregisterHandlers()
    {
        Unregister(_smallProductId);
        Unregister(_mediumProductId);
        Unregister(_largeProductId);
    }

    private void Register(string productId)
    {
        if (!string.IsNullOrEmpty(productId))
            IAPManager.Instance.RegisterPurchaseHandler(productId, OnPurchaseCompleted);
    }

    private void Unregister(string productId)
    {
        if (!string.IsNullOrEmpty(productId))
            IAPManager.Instance.UnregisterPurchaseHandler(productId);
    }

    private void OnPurchaseCompleted(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[StorePurchaseHandler] Purchase completed: '{productId}'");
#endif

        var reward = _config?.GetRewardByProductId(productId);
        if (reward == null)
        {
            Debug.LogWarning($"[StorePurchaseHandler] No reward found for productId: '{productId}'");
            return;
        }

        if (EmergencyBundleService.Instance != null)
            EmergencyBundleService.Instance.GrantRewards(reward);
        else
            Debug.LogWarning("[StorePurchaseHandler] EmergencyBundleService not available.");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.GetGameData().pendingPurchaseProductId = "";
            SaveManager.Instance.SaveData();
        }
    }

    public void BuySmallBento()  => Buy(_smallProductId);
    public void BuyMediumBento() => Buy(_mediumProductId);
    public void BuyLargeBento()  => Buy(_largeProductId);

    private void Buy(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            Debug.LogWarning("[StorePurchaseHandler] Product ID is not configured.");
            return;
        }

        IAPManager.Instance?.PurchaseProduct(productId);
    }

    public string GetSmallBentoPrice()  => GetPrice(_smallProductId);
    public string GetMediumBentoPrice() => GetPrice(_mediumProductId);
    public string GetLargeBentoPrice()  => GetPrice(_largeProductId);

    private string GetPrice(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "N/A";
        return IAPManager.Instance?.GetProductPrice(productId) ?? "N/A";
    }
}
