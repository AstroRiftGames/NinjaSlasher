using UnityEngine;

public class EmergencyBundleService : MonoBehaviourSingleton<EmergencyBundleService>
{
    [Header("Configuration")]
    [SerializeField] private EmergencyBundleConfig _config;

    private StoreProductDefinition _activeProduct;
    private BundleTier             _activeTier;
    private bool                   _offerActive;
    private bool                   _purchasePending;
    private bool                   _awaitingPurchaseResultDismiss;
    private bool                   _resumeGameplayFlowAfterResultDismiss;
    private string                 _pendingResultProductId;

    public bool HasActiveOffer => _offerActive;
    public bool IsPurchasePending => _purchasePending;

    private void OnEnable()
    {
        GameEvents.OnLevelFailed    += OnLevelFailed;
        GameEvents.OnLevelCompleted += OnLevelCompleted;
        GameEvents.OnRewardGranted  += OnRewardGranted;
        UIEvents.OnStorePurchaseResultDismissed += OnStorePurchaseResultDismissed;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelFailed    -= OnLevelFailed;
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnRewardGranted  -= OnRewardGranted;
        UIEvents.OnStorePurchaseResultDismissed -= OnStorePurchaseResultDismissed;
    }

    public void OnOfferDismissed()
    {
        if (!_offerActive || _purchasePending) return;
        CloseOffer(PaywallOutcome.Dismissed);
    }

    public void OnOfferExpired()
    {
        if (!_offerActive || _purchasePending) return;
        CloseOffer(PaywallOutcome.Expired);
    }

    public void OnOfferClosedForTransition()
    {
        if (!_offerActive || _purchasePending || _awaitingPurchaseResultDismiss) return;
        ClearActiveOffer();
    }

    public bool TryBeginPurchase(string productId)
    {
        if (!_offerActive || _activeProduct == null || _purchasePending)
            return false;

        if (!IsTrackedProduct(productId))
            return false;

        _purchasePending = true;
        _awaitingPurchaseResultDismiss = false;
        _resumeGameplayFlowAfterResultDismiss = false;
        _pendingResultProductId = null;
        return true;
    }

    public void OnStorePurchaseResultShown(StorePurchaseResultRequest request)
    {
        if (request == null || !_offerActive || !IsTrackedProduct(request.ProductId))
            return;

        _purchasePending = false;
        _pendingResultProductId = request.ProductId;

        switch (request.ResultType)
        {
            case StorePurchaseResultType.Cancelled:
            case StorePurchaseResultType.Failed:
            case StorePurchaseResultType.Unavailable:
                _awaitingPurchaseResultDismiss = true;
                _resumeGameplayFlowAfterResultDismiss = true;
                break;
            case StorePurchaseResultType.ApplyRewardFailed:
                _awaitingPurchaseResultDismiss = true;
                _resumeGameplayFlowAfterResultDismiss = false;
                break;
            default:
                _awaitingPurchaseResultDismiss = false;
                _resumeGameplayFlowAfterResultDismiss = false;
                break;
        }
    }

    private void OnLevelFailed(LevelFailedContext ctx)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] OnLevelFailed | IsBoss={ctx.IsBossLevel} | config={(_config != null ? "OK" : "NULL")}");
#endif

        if (GameConfigManager.IsTrailerCaptureModeEnabled())
            return;

        if (_config == null || SaveManager.Instance == null) return;

        var data = SaveManager.Instance.GetGameData();

        data.consecutiveLosses = Mathf.Max(0, data.consecutiveLosses) + 1;

        if (ctx.IsBossLevel)
        {
            data.consecutiveBossLosses++;
        }

        SaveManager.Instance.SaveData();

        var evalCtx = ctx;
        evalCtx.ConsecutiveLosses = data.consecutiveLosses;

        bool shouldOffer = FrustrationEvaluator.ShouldOffer(evalCtx, _config, data);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] streak={data.consecutiveLosses} | threshold={_config.consecutiveLossThreshold} | ShouldOffer={shouldOffer}");
#endif

        if (shouldOffer) ShowOffer(evalCtx, data);
    }

    private void OnLevelCompleted(LevelStats stats)
    {
        if (SaveManager.Instance == null) return;

        bool isBoss = LevelSessionManager.Instance?.CurrentSession?.Configuration
                          ?.unlockRequirements.isBossLevel ?? false;

        if (isBoss)
        {
            SaveManager.Instance.Modify(d =>
            {
                d.consecutiveLosses = 0;
                d.consecutiveBossLosses = 0;
            });
            AutoSaveManager.Instance?.ForceSave();
        }
        else
        {
            SaveManager.Instance.Modify(d => d.consecutiveLosses = 0);
        }
    }

    private void OnRewardGranted(StoreProductDefinition product)
    {
        if (!_offerActive || _activeProduct == null) return;
        if (product != _activeProduct) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] Offer purchased: '{_activeProduct.PrimaryProductId}' → recording activation");
#endif

        AutoSaveManager.Instance?.OnEmergencyBundleActivated();
        _purchasePending = false;
        _awaitingPurchaseResultDismiss = false;
        _resumeGameplayFlowAfterResultDismiss = false;
        _pendingResultProductId = null;
        CloseOffer(PaywallOutcome.Purchased);
    }

    private void ShowOffer(LevelFailedContext ctx, GameData data)
    {
        if (_offerActive) return;

        BundleTier tier    = FrustrationEvaluator.SelectTier(ctx, _config, data);
        var        product = _config.GetProductForTier(tier);

        if (product == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[EBS] No product assigned for tier {tier} in EmergencyBundleConfig.");
#endif
            return;
        }

        var offer = new EmergencyBundleOffer
        {
            Tier                 = tier,
            Product              = product,
            LocalizedPrice       = IAPManager.Instance?.GetProductPrice(product.PrimaryProductId) ?? "—",
            OfferDurationSeconds = _config.offerDurationMinutes * 60f,
            FailContext          = ctx,
        };

        _activeProduct = product;
        _activeTier    = tier;
        _offerActive   = true;

        AnalyticsManager.Instance?.RecordPaywallShown(
            product.PrimaryProductId,
            BundleTierToString(tier),
            ctx.LevelId,
            ctx.ConsecutiveLosses,
            ctx.IsBossLevel
        );

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] Showing offer | tier={tier} | productId='{product.PrimaryProductId}'");
#endif

        UIEvents.RequestShowEmergencyBundleModal(offer);
    }

    private void CloseOffer(PaywallOutcome outcome)
    {
        AnalyticsManager.Instance?.RecordPaywallResolved(
            outcome,
            _activeProduct?.PrimaryProductId ?? "",
            BundleTierToString(_activeTier)
        );

        ClearActiveOffer();

        UIEvents.RequestHideEmergencyBundleModal();
        ShowDefeatUI();
    }

    private void ClearActiveOffer()
    {
        _offerActive   = false;
        _activeProduct = null;
        _activeTier    = BundleTier.Small;
        _purchasePending = false;
        _awaitingPurchaseResultDismiss = false;
        _resumeGameplayFlowAfterResultDismiss = false;
        _pendingResultProductId = null;
    }

    private void OnStorePurchaseResultDismissed(StorePurchaseResultRequest request)
    {
        if (request == null || !_offerActive || !_awaitingPurchaseResultDismiss)
            return;

        if (!IsTrackedProduct(request.ProductId) || request.ProductId != _pendingResultProductId)
            return;

        bool shouldResumeGameplayFlow = _resumeGameplayFlowAfterResultDismiss;
        ClearActiveOffer();

        if (shouldResumeGameplayFlow)
            ShowDefeatUI();
    }

    private bool IsTrackedProduct(string productId)
    {
        return _activeProduct != null &&
               !string.IsNullOrEmpty(productId) &&
               string.Equals(_activeProduct.PrimaryProductId, productId, System.StringComparison.Ordinal);
    }

    private static string BundleTierToString(BundleTier tier) => tier switch
    {
        BundleTier.Small  => "small",
        BundleTier.Medium => "medium",
        BundleTier.Large  => "large",
        _                 => "unknown"
    };

    private void ShowDefeatUI()
    {
        if (LifeManager.Instance == null || !LifeManager.Instance.CanPlay())
            UIEvents.RequestShowNoLivesModal();
        else
            UIEvents.RequestShowDefeatModal(LifeManager.Instance.GetRealLives());
    }
}
