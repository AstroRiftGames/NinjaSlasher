using UnityEngine;

public class EmergencyBundleService : MonoBehaviourSingleton<EmergencyBundleService>
{
    [Header("Configuration")]
    [SerializeField] private EmergencyBundleConfig _config;

    [Header("Catalog")]
    [SerializeField] private StoreCatalog _catalog;

    private string                 _activeProductId;
    private StoreProductDefinition _activeProduct;
    private bool                   _offerActive;

    public bool HasActiveOffer => _offerActive;

    private int _normalLossStreak = 0;

    private void OnEnable()
    {
        GameEvents.OnLevelFailed    += OnLevelFailed;
        GameEvents.OnLevelCompleted += OnLevelCompleted;
        GameEvents.OnRewardGranted  += OnRewardGranted;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelFailed    -= OnLevelFailed;
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnRewardGranted  -= OnRewardGranted;
    }

    public void OnOfferDismissed()
    {
        if (!_offerActive) return;
        CloseOffer(purchased: false);
    }

    private void OnLevelFailed(LevelFailedContext ctx)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] OnLevelFailed | IsBoss={ctx.IsBossLevel} | config={(_config != null ? "OK" : "NULL")} | catalog={(_catalog != null ? "OK" : "NULL")}");
#endif

        if (_config == null || _catalog == null || SaveManager.Instance == null) return;

        var data = SaveManager.Instance.GetGameData();

        if (ctx.IsBossLevel)
        {
            data.consecutiveBossLosses++;
            AutoSaveManager.Instance?.ForceSave();
        }
        else
        {
            _normalLossStreak++;
        }

        var evalCtx = ctx;
        evalCtx.ConsecutiveLosses = _normalLossStreak;

        bool shouldOffer = FrustrationEvaluator.ShouldOffer(evalCtx, _config, data);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] streak={_normalLossStreak} | threshold={_config.consecutiveLossThreshold} | ShouldOffer={shouldOffer}");
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
            SaveManager.Instance.GetGameData().consecutiveBossLosses = 0;
            AutoSaveManager.Instance?.ForceSave();
        }
        else
        {
            _normalLossStreak = 0;
        }
    }

    private void OnRewardGranted(StoreProductDefinition product)
    {
        if (!_offerActive) return;
        if (string.IsNullOrEmpty(_activeProductId)) return;
        if (!product.MatchesProductId(_activeProductId)) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] Offer purchased: '{_activeProductId}' → recording activation");
#endif

        AutoSaveManager.Instance?.OnEmergencyBundleActivated();
        CloseOffer(purchased: true);
    }

    private void ShowOffer(LevelFailedContext ctx, GameData data)
    {
        if (_offerActive) return;

        BundleTier tier      = FrustrationEvaluator.SelectTier(ctx, _config, data);
        string     productId = _config.GetProductIdForTier(tier);
        var        product   = _catalog.GetByProductId(productId);

        if (product == null)
        {
            return;
        }

        var offer = new EmergencyBundleOffer
        {
            Tier                 = tier,
            ProductId            = productId,
            LocalizedPrice       = IAPManager.Instance?.GetProductPrice(productId) ?? "—",
            DisplayName          = product.display?.bundleName ?? string.Empty,
            Icon                 = product.display?.icon,
            Reward               = product.bundleReward,
            OfferDurationSeconds = _config.offerDurationMinutes * 60f,
            FailContext          = ctx,
        };

        _activeProductId = productId;
        _activeProduct   = product;
        _offerActive     = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] Showing offer | tier={tier} | productId='{productId}'");
#endif

        UIEvents.RequestShowEmergencyBundleOverlay(offer);
    }

    private void CloseOffer(bool purchased = false)
    {
        _offerActive     = false;
        _activeProductId = null;
        _activeProduct   = null;

        UIEvents.RequestHideEmergencyBundleOverlay();
        ShowDefeatUI();
    }

    private void ShowDefeatUI()
    {
        if (LifeManager.Instance == null || !LifeManager.Instance.CanPlay())
            UIEvents.RequestShowNoLivesOverlay();
        else
            UIEvents.RequestShowDefeatOverlay(LifeManager.Instance.GetRealLives());
    }
}
