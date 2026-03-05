using UnityEngine;
using UnityEngine.Purchasing;

public class EmergencyBundleService : MonoBehaviourSingleton<EmergencyBundleService>
{
    [Header("Configuration")]
    [SerializeField] private StoreConfig _config;

    private string _activeProductId;
    private BundleRewardData _activeReward;
    private bool _offerActive;

    public bool HasActiveOffer => _offerActive;

    public void OnOfferDismissed()
    {
        if (!_offerActive) return;
        CloseOffer(purchased: false);
    }

    private int _normalLossStreak = 0;

    #region LIFECYCLE

    private void OnEnable()
    {
        GameEvents.OnLevelFailed    += OnLevelFailed;
        GameEvents.OnLevelCompleted += OnLevelCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelFailed    -= OnLevelFailed;
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
    }

    private void Start()
    {
        RecoverPendingPurchase();
    }

    private void RecoverPendingPurchase()
    {
        if (_config == null || SaveManager.Instance == null) return;

        var data = SaveManager.Instance.GetGameData();
        if (string.IsNullOrEmpty(data.pendingPurchaseProductId)) return;

        var reward = _config.GetRewardByProductId(data.pendingPurchaseProductId);
        if (reward == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] Recovering pending purchase: {data.pendingPurchaseProductId}");
#endif
        GrantRewards(reward);
        data.pendingPurchaseProductId = "";
        SaveManager.Instance.SaveData();
    }

    private void OnDestroy()
    {
        if (_offerActive && !string.IsNullOrEmpty(_activeProductId))
            IAPManager.Instance?.UnregisterPurchaseHandler(_activeProductId);
    }

    #endregion

    #region LEVEL EVENTS

    private void OnLevelFailed(LevelFailedContext ctx)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] OnLevelFailed | IsBoss={ctx.IsBossLevel}, config={_config != null}, save={SaveManager.Instance != null}");
#endif

        if (_config == null || SaveManager.Instance == null) return;

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
        Debug.Log($"[EBS] streak={_normalLossStreak} threshold={_config.consecutiveLossThreshold} | ShouldOffer={shouldOffer}");
#endif

        if (!shouldOffer) return;

        ShowOffer(evalCtx, data);
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

    #endregion

    #region OFFER MANAGEMENT

    private void ShowOffer(LevelFailedContext ctx, GameData data)
    {
        if (_offerActive) return;

        BundleTier tier         = FrustrationEvaluator.SelectTier(ctx, _config, data);
        string productId        = _config.GetProductId(tier);
        BundleRewardData reward = _config.GetReward(tier);
        BundleDisplayData disp  = _config.GetDisplay(tier);

        var offer = new EmergencyBundleOffer
        {
            Tier                 = tier,
            ProductId            = productId,
            LocalizedPrice       = IAPManager.Instance?.GetProductPrice(productId) ?? "—",
            DisplayName          = disp?.bundleName ?? string.Empty,
            Icon                 = disp?.icon,
            Reward               = reward,
            OfferDurationSeconds = _config.offerDurationMinutes * 60f,
            FailContext          = ctx,
        };

        _activeProductId = productId;
        _activeReward    = reward;
        _offerActive     = true;

        IAPManager.Instance?.RegisterPurchaseHandler(productId, OnBundlePurchased);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] RequestShowEmergencyBundleOverlay | tier={tier} | productId='{productId}'");
#endif

        UIEvents.RequestShowEmergencyBundleOverlay(offer);
    }

    #endregion

    #region IAP CALLBACKS

    private void OnBundlePurchased(PurchaseEventArgs args)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] Purchase confirmed: productId='{args.purchasedProduct.definition.id}' | Granting rewards...");
#endif
        GrantRewards(_activeReward);
        AutoSaveManager.Instance?.OnEmergencyBundleActivated();
        CloseOffer(purchased: true);
    }

    public void GrantRewards(BundleRewardData reward)
    {
        if (reward == null)
        {
            Debug.LogWarning("[EBS] GrantRewards: reward is NULL — nothing granted.");
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EBS] GrantRewards | unlimitedLives={reward.unlimitedLives} " +
                  $"duration={reward.unlimitedLivesDurationMinutes}min " +
                  $"regularLives={reward.regularLivesCount} " +
                  $"coins={reward.coins} " +
                  $"LifeManager={(LifeManager.Instance != null ? "OK" : "NULL")}");
#endif

        if (reward.unlimitedLives && reward.unlimitedLivesDurationMinutes > 0f)
        {
            LifeManager.Instance?.ActivateUnlimitedLives(reward.unlimitedLivesDurationMinutes);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EBS] Reward granted: UNLIMITED LIVES for {reward.unlimitedLivesDurationMinutes} min");
#endif
        }
        else if (reward.regularLivesCount > 0)
        {
            for (int i = 0; i < reward.regularLivesCount; i++)
                LifeManager.Instance?.AddLife();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EBS] Reward granted: +{reward.regularLivesCount} lives | New total={LifeManager.Instance?.GetRealLives()}");
#endif
        }

        if (reward.powerUps != null)
        {
            foreach (var entry in reward.powerUps)
            {
                if (entry.quantity > 0)
                {
                    AutoSaveManager.Instance?.OnPowerUpObtained(entry.type, entry.quantity);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[EBS] Reward granted: +{entry.quantity}x {entry.type} (inventory: {SaveManager.Instance?.GetPowerUpCount(entry.type)})");
#endif
                }
            }
        }

        if (reward.coins > 0)
        {
            SaveManager.Instance?.AddCoins(reward.coins);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EBS] Reward granted: +{reward.coins} coins | Wallet={SaveManager.Instance?.GetCoins()}");
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[EBS] All rewards granted successfully.");
#endif
    }

    private void CloseOffer(bool purchased = false)
    {
        if (!string.IsNullOrEmpty(_activeProductId))
            IAPManager.Instance?.UnregisterPurchaseHandler(_activeProductId);

        _offerActive     = false;
        _activeProductId = null;
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

    #endregion

    #region DEBUG

    [ContextMenu("Diagnose Setup")]
    private void DiagnoseSetup()
    {
        Debug.Log("[EBS] ========= DIAGNÓSTICO =========");
        Debug.Log($"  EmergencyBundleService en escena: OK (este log lo confirma)");
        Debug.Log($"  _config asignado:      {(_config != null ? "OK" : "FALLO ← arrastra el ScriptableObject en el Inspector")}");
        Debug.Log($"  SaveManager:           {(SaveManager.Instance != null ? "OK" : "NULL (solo disponible en Play)")}");
        Debug.Log($"  UIEvents listener:     {(UIEvents.HasEmergencyBundleOverlayListener() ? "OK" : "FALLO ← UIManager no suscripto o _emergencyBundleOverlay no asignado")}");
        Debug.Log($"  _normalLossStreak:     {_normalLossStreak}");
        if (_config != null)
        {
            Debug.Log($"  consecutiveLossThreshold: {_config.consecutiveLossThreshold}");
            Debug.Log($"  dailyCap: {_config.dailyCap}  cooldownHours: {_config.cooldownHours}");
            Debug.Log($"  small primary productId:  '{_config.smallBundle?.PrimaryProductId}'");
            Debug.Log($"  medium primary productId: '{_config.mediumBundle?.PrimaryProductId}'");
            Debug.Log($"  large primary productId:  '{_config.largeBundle?.PrimaryProductId}'");
        }
        Debug.Log("[EBS] =================================");
    }

    [ContextMenu("Simulate Level Failed (Normal)")]
    private void DebugSimulateFail()
    {
        var ctx = new LevelFailedContext
        {
            Reason = "debug", LevelId = 1, IsBossLevel = false,
            ConsecutiveLosses = _config != null ? _config.consecutiveLossThreshold : 3,
            LivesRemaining = 2,
        };
        OnLevelFailed(ctx);
    }

    [ContextMenu("Simulate Level Failed (Boss, sin vidas)")]
    private void DebugSimulateBossFailNoLives()
    {
        var ctx = new LevelFailedContext
        {
            Reason = "debug", LevelId = 10, IsBossLevel = true,
            ConsecutiveLosses = 1, LivesRemaining = 0,
        };
        OnLevelFailed(ctx);
    }

    #endregion
}
