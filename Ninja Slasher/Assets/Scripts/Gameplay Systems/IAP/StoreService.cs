using System;
using UnityEngine;
using UnityEngine.Purchasing;

public enum StorePurchaseProcessingDecision
{
    Complete,
    Pending,
    RejectAndComplete
}

public readonly struct StorePurchaseProcessingResult
{
    public readonly StorePurchaseProcessingDecision Decision;
    public readonly string ProcessedPurchaseKey;
    public readonly string Message;

    public StorePurchaseProcessingResult(StorePurchaseProcessingDecision decision, string processedPurchaseKey, string message)
    {
        Decision = decision;
        ProcessedPurchaseKey = processedPurchaseKey;
        Message = message;
    }

    public static StorePurchaseProcessingResult Complete(string processedPurchaseKey, string message = null)
        => new StorePurchaseProcessingResult(StorePurchaseProcessingDecision.Complete, processedPurchaseKey, message);

    public static StorePurchaseProcessingResult Pending(string processedPurchaseKey, string message)
        => new StorePurchaseProcessingResult(StorePurchaseProcessingDecision.Pending, processedPurchaseKey, message);

    public static StorePurchaseProcessingResult RejectAndComplete(string message)
        => new StorePurchaseProcessingResult(StorePurchaseProcessingDecision.RejectAndComplete, null, message);
}

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

        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
    }

    private void OnDisable()
    {
        if (IAPManager.Instance == null) return;

        IAPManager.Instance.OnIAPInitialized -= RegisterAll;
        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (IAPManager.Instance == null) return;

        IAPManager.Instance.OnIAPInitialized -= RegisterAll;
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

        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;

        ClearStalePendingPurchaseRequest();
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

    private StorePurchaseProcessingResult OnPurchaseCompleted(PurchaseEventArgs args)
    {
        if (!TryValidateSuccessfulPurchase(args, out StoreProductDefinition product, out string productId, out string transactionId, out string dedupeKey, out string validationError))
        {
            Debug.LogError($"[StoreService] Purchase success callback rejected: {validationError}");
            ShowPurchaseResult(BuildFailedResult(null));
            ClearPendingVisualFeedback();
            ClearPendingPurchase();
            return StorePurchaseProcessingResult.RejectAndComplete(validationError);
        }

        if (!string.IsNullOrWhiteSpace(dedupeKey) && SaveManager.Instance != null && SaveManager.Instance.HasGrantedPurchaseTransaction(dedupeKey))
        {
            Debug.LogWarning($"[StoreService] Duplicate purchase ignored across sessions: '{dedupeKey}' for '{productId}'.");
            ClearPendingVisualFeedback();
            ClearPendingPurchase();
            return StorePurchaseProcessingResult.Complete(dedupeKey, "Purchase was already granted previously.");
        }

        string source = (!string.IsNullOrEmpty(_pendingVisualProductId) && _pendingVisualProductId == productId)
            ? "shop"
            : "paywall";

        bool rewardGranted = TryGrantReward(product, productId, dedupeKey, out string rewardError);

        if (rewardGranted)
        {
            TryRaisePurchaseFeedback(product, productId);
            ShowPurchaseResult(BuildSuccessResult(product));
            AnalyticsManager.Instance?.RecordPurchaseCompleted(
                productId,
                AnalyticsManager.ProductCategoryStr(product.category),
                source,
                transactionId
            );

            ClearPendingVisualFeedback();
            ClearPendingPurchase();
            return StorePurchaseProcessingResult.Complete(dedupeKey, "Purchase granted durably.");
        }

        Debug.LogError($"[StoreService] Purchase completed but reward persistence failed for '{productId}': {rewardError}");
        ShowPurchaseResult(BuildRewardApplicationFailedResult(product));
        ClearPendingVisualFeedback();
        return StorePurchaseProcessingResult.Pending(dedupeKey, rewardError);
    }

    private void ClearPendingPurchase()
    {
        SaveManager.Instance?.ClearPendingPurchaseProductId();
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
            ShowPurchaseResult(BuildUnavailableResult(null));
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
            ShowPurchaseResult(BuildUnavailableResult(product));
            return;
        }

        Product storeProduct = IAPManager.Instance.GetProduct(resolvedProductId);
        if (storeProduct == null || !storeProduct.availableToPurchase)
        {
            ShowPurchaseResult(BuildUnavailableResult(product));
            return;
        }

        _pendingVisualProductId = resolvedProductId;
        _pendingRewardFeedbackOrigin = feedbackOrigin;
        Debug.Log($"[StoreService] Purchase start requested | productId='{resolvedProductId}' | source='shop' | hasFeedbackOrigin={feedbackOrigin != null}");

        string category = AnalyticsManager.ProductCategoryStr(product.category);
        AnalyticsManager.Instance?.RecordPurchaseStarted(resolvedProductId, category, "shop");

        PurchaseStartResult startResult = IAPManager.Instance.PurchaseProduct(resolvedProductId);
        if (startResult != PurchaseStartResult.Started)
        {
            ClearPendingVisualFeedback();
            ShowPurchaseResult(BuildPurchaseStartRejectedResult(product, startResult));
        }
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

    private void OnPurchaseFailed(string productId, PurchaseFailureReason reason)
    {
        StoreProductDefinition product = _catalog?.GetByProductId(productId);
        StorePurchaseResultRequest request = reason switch
        {
            PurchaseFailureReason.UserCancelled => BuildCancelledResult(product),
            PurchaseFailureReason.ProductUnavailable => BuildUnavailableResult(product),
            PurchaseFailureReason.PurchasingUnavailable => BuildUnavailableResult(product),
            _ => BuildFailedResult(product)
        };

        Debug.LogWarning($"[StoreService] Purchase failed result | productId='{productId}' | reason={reason} | mappedResult={request.ResultType}");
        ShowPurchaseResult(request);
        ClearPendingVisualFeedback();
        ClearPendingPurchase();
    }

    private static bool TryGrantReward(StoreProductDefinition product, string productId, string purchaseKey, out string error)
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
            Debug.Log($"[StoreService] Granting purchase reward durably | productId='{productId}' | purchaseKey='{purchaseKey}' | rewardType={product.rewardType}");
            return RewardService.Instance.TryGrantDurably(product, purchaseKey, out error);
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
            ProductId = product != null ? product.PrimaryProductId : null,
            ResultType = StorePurchaseResultType.Success,
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
            ProductId = product != null ? product.PrimaryProductId : null,
            ResultType = StorePurchaseResultType.Cancelled,
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
            ProductId = product != null ? product.PrimaryProductId : null,
            ResultType = StorePurchaseResultType.Failed,
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
            ProductId = product != null ? product.PrimaryProductId : null,
            ResultType = StorePurchaseResultType.Unavailable,
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
            ProductId = product != null ? product.PrimaryProductId : null,
            ResultType = StorePurchaseResultType.ApplyRewardFailed,
            Title = "Error al aplicar compra",
            Message = "La compra se completó, pero no se pudo aplicar la recompensa.",
            ConfirmButtonText = "Aceptar",
            Icon = product != null ? product.ConfirmationIcon : null
        };
    }

    private static StorePurchaseResultRequest BuildPurchaseStartRejectedResult(StoreProductDefinition product, PurchaseStartResult startResult)
    {
        return startResult switch
        {
            PurchaseStartResult.NotInitialized => BuildUnavailableResult(product),
            PurchaseStartResult.ProductUnavailable => BuildUnavailableResult(product),
            PurchaseStartResult.AlreadyProcessing => new StorePurchaseResultRequest
            {
                ProductId = product != null ? product.PrimaryProductId : null,
                ResultType = StorePurchaseResultType.Failed,
                Title = "Compra en curso",
                Message = "Ya hay una compra en curso.",
                ConfirmButtonText = "Aceptar",
                Icon = product != null ? product.ConfirmationIcon : null
            },
            _ => BuildFailedResult(product)
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

    private static void ShowPurchaseResult(StorePurchaseResultRequest request)
    {
        if (request == null)
            return;

        EmergencyBundleService.Instance?.OnStorePurchaseResultShown(request);
        UIEvents.RequestShowStorePurchaseResult(request);
    }

    private void ClearStalePendingPurchaseRequest()
    {
        if (SaveManager.Instance == null)
            return;

        GameData data = SaveManager.Instance.GetGameData();
        if (data == null || string.IsNullOrEmpty(data.pendingPurchaseProductId))
            return;

        Debug.LogWarning($"[StoreService] Clearing stale pending purchase request without granting reward | productId='{data.pendingPurchaseProductId}'");
        ClearPendingPurchase();
    }

    private bool TryValidateSuccessfulPurchase(
        PurchaseEventArgs args,
        out StoreProductDefinition product,
        out string productId,
        out string transactionId,
        out string dedupeKey,
        out string error)
    {
        product = null;
        productId = null;
        transactionId = null;
        dedupeKey = null;
        error = null;

        if (args?.purchasedProduct == null)
        {
            error = "PurchaseEventArgs or purchased product is null.";
            return false;
        }

        productId = args.purchasedProduct.definition?.id;
        transactionId = args.purchasedProduct.transactionID;
        string receipt = args.purchasedProduct.receipt;

        if (string.IsNullOrWhiteSpace(productId))
        {
            error = "Successful purchase callback arrived without product id.";
            return false;
        }

        product = _catalog?.GetByProductId(productId);
        if (product == null)
        {
            error = $"Unknown purchased product '{productId}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(receipt))
        {
            error = $"Purchase '{productId}' has no receipt.";
            return false;
        }

        bool hasUsableDedupeKey = PurchaseDedupeKeyUtility.TryBuild(transactionId, receipt, out dedupeKey);
        if (!hasUsableDedupeKey)
        {
            error = $"Purchase '{productId}' has no usable dedupe key.";
            return false;
        }

        if (product.productType == ProductType.Consumable && string.IsNullOrWhiteSpace(dedupeKey))
        {
            error = $"Consumable purchase '{productId}' has no usable dedupe key.";
            return false;
        }

        Debug.Log($"[StoreService] Purchase success callback validated | productId='{productId}' | tx='{transactionId}' | purchaseKey='{dedupeKey}' | hasReceipt=true");
        return true;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"[StoreService] OnApplicationPause | paused={pauseStatus} | pendingVisualProductId='{_pendingVisualProductId}' | purchaseState={IAPManager.Instance?.PurchaseState}");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[StoreService] OnApplicationFocus | hasFocus={hasFocus} | pendingVisualProductId='{_pendingVisualProductId}' | purchaseState={IAPManager.Instance?.PurchaseState}");
    }
}
