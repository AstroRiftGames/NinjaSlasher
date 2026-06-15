using System;
using UnityEngine;
using UnityEngine.Purchasing;

public class StoreService : MonoBehaviourSingleton<StoreService>
{
    [SerializeField] private StoreCatalog _catalog;

    public bool IsIAPReady { get; private set; }

    public event Action OnIAPReady;

    private string _pendingVisualProductId;
    private RectTransform _pendingRewardFeedbackOrigin;

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
        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
    }

    private void OnDisable()
    {
        if (IAPManager.Instance == null) return;

        IAPManager.Instance.OnIAPInitialized -= RegisterAll;
        IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseCompletedFallback;
        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (IAPManager.Instance == null) return;

        IAPManager.Instance.OnIAPInitialized -= RegisterAll;
        IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseCompletedFallback;
        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
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
        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;

        RecoverPendingPurchase();
    }

    private void RegisterAll()
    {
        if (_catalog == null)
            return;

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[StoreService] Store products registered. UI can refresh localized prices now.");
#endif
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

        string source = (!string.IsNullOrEmpty(_pendingVisualProductId) && _pendingVisualProductId == productId)
            ? "shop"
            : "paywall";

        string transactionId = args.purchasedProduct.transactionID ?? "";

        bool rewardGranted = TryGrantReward(product, out string rewardError);

        if (rewardGranted)
        {
            TryRaisePurchaseFeedback(product, productId);
            UIEvents.RequestShowStorePurchaseResult(BuildSuccessResult(product));
        }
        else
        {
            Debug.LogError($"[StoreService] Purchase completed but reward application failed for '{productId}': {rewardError}");
            UIEvents.RequestShowStorePurchaseResult(BuildRewardApplicationFailedResult(product));
        }

        ClearPendingVisualFeedback();
        ClearPendingPurchase();

        AnalyticsManager.Instance?.RecordPurchaseCompleted(
            productId,
            AnalyticsManager.ProductCategoryStr(product.category),
            source,
            transactionId
        );
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
        Buy(productId, null);
    }

    public void Buy(string productId, RectTransform feedbackOrigin)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return;
        }

        string resolvedProductId = ResolveProductId(productId);
        var product = _catalog?.GetByProductId(resolvedProductId);

        if (product == null)
        {
            Debug.LogWarning($"[StoreService] Buy requested for unknown product id '{productId}'.");
            UIEvents.RequestShowStorePurchaseResult(BuildUnavailableResult(null));
            return;
        }

        if (product != null &&
            product.rewardType == RewardType.RemoveAds &&
            SaveManager.Instance != null &&
            SaveManager.Instance.GetAdsRemoved())
        {
            return;
        }

        if (IAPManager.Instance == null || !IAPManager.Instance.IsInitialized)
        {
            UIEvents.RequestShowStorePurchaseResult(BuildUnavailableResult(product));
            return;
        }

        Product storeProduct = IAPManager.Instance.GetProduct(resolvedProductId);
        if (storeProduct == null || !storeProduct.availableToPurchase)
        {
            UIEvents.RequestShowStorePurchaseResult(BuildUnavailableResult(product));
            return;
        }

        _pendingVisualProductId = resolvedProductId;
        _pendingRewardFeedbackOrigin = feedbackOrigin;

        string category = AnalyticsManager.ProductCategoryStr(product.category);
        AnalyticsManager.Instance?.RecordPurchaseStarted(resolvedProductId, category, "shop");

        IAPManager.Instance?.PurchaseProduct(resolvedProductId);
    }

    public string GetPrice(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "N/A";

        string resolvedProductId = ResolveProductId(productId);
        return IAPManager.Instance?.GetProductPrice(resolvedProductId) ?? "N/A";
    }

    public StoreProductDefinition GetProduct(string productId)
        => _catalog?.GetByProductId(ResolveProductId(productId));

    private string ResolveProductId(string productId)
    {
        if (_catalog == null)
            return productId;

        string resolvedProductId = _catalog.NormalizeProductId(productId);

        if (!string.Equals(resolvedProductId, productId, StringComparison.Ordinal))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StoreService] Remapped legacy product id '{productId}' -> '{resolvedProductId}'.");
#endif
        }

        return resolvedProductId;
    }

    private void TryRaisePurchaseFeedback(StoreProductDefinition product, string purchasedProductId)
    {
        if (product == null)
            return;

        if (string.IsNullOrEmpty(_pendingVisualProductId) || _pendingVisualProductId != purchasedProductId)
            return;

        if (_pendingRewardFeedbackOrigin == null)
        {
            ClearPendingVisualFeedback();
            return;
        }

        StorePurchaseFeedbackRequest request = BuildPurchaseFeedbackRequest(product, purchasedProductId, _pendingRewardFeedbackOrigin);
        if (request == null || !request.HasRewards)
            return;

        GameEvents.RaiseStorePurchaseFeedbackRequested(request);
        ClearPendingVisualFeedback();
    }

    private StorePurchaseFeedbackRequest BuildPurchaseFeedbackRequest(StoreProductDefinition product, string purchasedProductId, RectTransform sourceTransform)
    {
        if (product == null || sourceTransform == null)
            return null;

        StorePurchaseFeedbackRequest request = new StorePurchaseFeedbackRequest
        {
            ProductId = purchasedProductId,
            SourceTransform = sourceTransform
        };

        switch (product.rewardType)
        {
            case RewardType.Coins:
                if (product.coinAmount > 0)
                {
                    request.Rewards.Add(new StoreRewardFeedbackEntry
                    {
                        RewardType = StoreRewardFeedbackType.Coins,
                        Amount = product.coinAmount
                    });
                }
                break;

            case RewardType.Bundle:
                AppendBundleFeedback(request, product.bundleReward);
                break;
        }

        return request.HasRewards ? request : null;
    }

    private void AppendBundleFeedback(StorePurchaseFeedbackRequest request, BundleRewardData reward)
    {
        if (request == null || reward == null)
            return;

        if (reward.coins > 0)
        {
            request.Rewards.Add(new StoreRewardFeedbackEntry
            {
                RewardType = StoreRewardFeedbackType.Coins,
                Amount = reward.coins
            });
        }

        if (reward.unlimitedLives && reward.unlimitedLivesDurationMinutes > 0f)
        {
            request.Rewards.Add(new StoreRewardFeedbackEntry
            {
                RewardType = StoreRewardFeedbackType.UnlimitedLives,
                DurationMinutes = reward.unlimitedLivesDurationMinutes
            });
        }
    }

    private void ClearPendingVisualFeedback()
    {
        _pendingVisualProductId = null;
        _pendingRewardFeedbackOrigin = null;
    }

    private void OnPurchaseFailed(string productId, string reason)
    {
        StoreProductDefinition product = _catalog?.GetByProductId(productId);
        StorePurchaseResultRequest request = IsCancelledPurchase(reason)
            ? BuildCancelledResult(product)
            : BuildFailedResult(product);

        UIEvents.RequestShowStorePurchaseResult(request);
        ClearPendingVisualFeedback();
    }

    private static bool IsCancelledPurchase(string reason)
    {
        return !string.IsNullOrWhiteSpace(reason) &&
               reason.IndexOf("UserCancelled", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryGrantReward(StoreProductDefinition product, out string error)
    {
        error = null;

        if (product == null)
        {
            error = "Product definition is null.";
            return false;
        }

        if (RewardService.Instance == null)
        {
            error = "RewardService instance is not available.";
            return false;
        }

        try
        {
            RewardService.Instance.Grant(product);
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static StorePurchaseResultRequest BuildSuccessResult(StoreProductDefinition product)
    {
        return new StorePurchaseResultRequest
        {
            Title = "Compra completada",
            Message = BuildSuccessMessage(product),
            ConfirmButtonText = "Aceptar",
            Icon = product != null ? product.ConfirmationIcon : null,
            PlaySuccessAudio = true
        };
    }

    private static StorePurchaseResultRequest BuildCancelledResult(StoreProductDefinition product)
    {
        return new StorePurchaseResultRequest
        {
            Title = "Compra cancelada",
            Message = "Compra cancelada.",
            ConfirmButtonText = "Aceptar",
            Icon = product != null ? product.ConfirmationIcon : null
        };
    }

    private static StorePurchaseResultRequest BuildFailedResult(StoreProductDefinition product)
    {
        return new StorePurchaseResultRequest
        {
            Title = "Error de compra",
            Message = "No se pudo completar la compra.",
            ConfirmButtonText = "Aceptar",
            Icon = product != null ? product.ConfirmationIcon : null
        };
    }

    private static StorePurchaseResultRequest BuildUnavailableResult(StoreProductDefinition product)
    {
        return new StorePurchaseResultRequest
        {
            Title = "Producto no disponible",
            Message = "Producto no disponible.",
            ConfirmButtonText = "Aceptar",
            Icon = product != null ? product.ConfirmationIcon : null
        };
    }

    private static StorePurchaseResultRequest BuildRewardApplicationFailedResult(StoreProductDefinition product)
    {
        return new StorePurchaseResultRequest
        {
            Title = "Error al aplicar compra",
            Message = "La compra se completó, pero no se pudo aplicar la recompensa.",
            ConfirmButtonText = "Aceptar",
            Icon = product != null ? product.ConfirmationIcon : null
        };
    }

    private static string BuildSuccessMessage(StoreProductDefinition product)
    {
        if (product == null)
            return "Se agregaron tus recompensas.";

        return product.rewardType switch
        {
            RewardType.Coins when product.coinAmount > 0 => $"Se agregaron {product.coinAmount} monedas.",
            RewardType.RemoveAds => "Los anuncios intersticiales fueron eliminados.",
            _ => "Se agregaron tus recompensas."
        };
    }
}
