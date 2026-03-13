using System;
using UnityEngine;
using UnityEngine.Purchasing;

public class StoreService : MonoBehaviourSingleton<StoreService>
{
    [SerializeField] private StoreCatalog _catalog;

    public bool IsIAPReady { get; private set; }

    public event Action OnIAPReady;

    private void OnEnable()
    {
        if (Instance != this) return;

        if (IAPManager.Instance == null)
        {
            return;
        }

        IAPManager.Instance.OnIAPInitialized -= RegisterAll;
        if (IAPManager.Instance.IsInitialized)
        {
            RegisterAll();
        }
        else
        {
            IAPManager.Instance.OnIAPInitialized += RegisterAll;
        }

        IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseCompletedFallback;
        IAPManager.Instance.OnPurchaseCompleted += OnPurchaseCompletedFallback;
    }

    private void OnDisable()
    {
        if (IAPManager.Instance == null) return;

        IAPManager.Instance.OnIAPInitialized -= RegisterAll;
        IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseCompletedFallback;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (IAPManager.Instance == null) return;

        IAPManager.Instance.OnIAPInitialized -= RegisterAll;
        IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseCompletedFallback;
        UnregisterAll();
    }

    private void Start()
    {
        if (Instance != this) return;

        if (_catalog == null)
        {
            Debug.LogError("[StoreService] StoreCatalog not assigned in Inspector.");
            return;
        }

        if (IAPManager.Instance == null)
        {
            return;
        }

        IAPManager.Instance.OnIAPInitialized -= RegisterAll;
        if (IAPManager.Instance.IsInitialized)
        {
            RegisterAll();
        }
        else
        {
            IAPManager.Instance.OnIAPInitialized += RegisterAll;
            IAPManager.Instance.Initialize(_catalog);
        }

        IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseCompletedFallback;
        IAPManager.Instance.OnPurchaseCompleted += OnPurchaseCompletedFallback;

        RecoverPendingPurchase();
    }

    private void RegisterAll()
    {
        if (_catalog == null)
            return;

        int count = 0;

        foreach (var product in _catalog.products)
        {
            if (product.productIds == null) continue;

            foreach (var id in product.productIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                IAPManager.Instance.RegisterPurchaseHandler(id, OnPurchaseCompleted);
            }
        }

        IsIAPReady = true;
        OnIAPReady?.Invoke();
    }

    private void UnregisterAll()
    {
        if (_catalog == null) return;
        foreach (var id in _catalog.GetAllProductIds())
            IAPManager.Instance.UnregisterPurchaseHandler(id);
    }

    private void OnPurchaseCompleted(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;

        var product = _catalog.GetByProductId(productId);
        if (product == null)
            return;

        RewardService.Instance?.Grant(product);

        ClearPendingPurchase();
    }

    private void OnPurchaseCompletedFallback(string productId)
    {
        if (_catalog == null) return;

        var product = _catalog.GetByProductId(productId);

        if (product != null)
            return;

        var data = SaveManager.Instance?.GetGameData();
        if (data == null) return;

        if (string.IsNullOrEmpty(data.pendingPurchaseProductId))
            return;

        if (data.pendingPurchaseProductId != productId)
            return;

        RewardService.Instance?.Grant(product);
        ClearPendingPurchase();
    }

    private void RecoverPendingPurchase()
    {
        if (_catalog == null || SaveManager.Instance == null) return;
        
        if (IAPManager.Instance.PurchaseState == PurchaseState.Processing)
            return;
        var data = SaveManager.Instance.GetGameData();
        if (string.IsNullOrEmpty(data.pendingPurchaseProductId)) return;

        var product = _catalog.GetByProductId(data.pendingPurchaseProductId);
        if (product == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[StoreService] Recovering pending purchase: '{data.pendingPurchaseProductId}'");
#endif

        RewardService.Instance?.Grant(product);
        ClearPendingPurchase();
    }

    private void ClearPendingPurchase()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Modify(d => d.pendingPurchaseProductId = "");
    }

    public void Buy(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return;
        }

        var product = _catalog?.GetByProductId(productId);
        if (product != null &&
            product.rewardType == RewardType.RemoveAds &&
            SaveManager.Instance != null &&
            SaveManager.Instance.GetAdsRemoved())
        {
            Debug.Log("[StoreService] Ads already removed.");
            return;
        }

        IAPManager.Instance?.PurchaseProduct(productId);
    }

    public string GetPrice(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "N/A";
        return IAPManager.Instance?.GetProductPrice(productId) ?? "N/A";
    }

    public StoreProductDefinition GetProduct(string productId)
        => _catalog?.GetByProductId(productId);
}
