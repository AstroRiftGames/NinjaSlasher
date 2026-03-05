using UnityEngine;

public class EmergencyBundleService : MonoBehaviourSingleton<EmergencyBundleService>
{
    [Header("Configuration")]
    [SerializeField] private EmergencyBundleConfig _config;

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
        if (IAPManager.Instance != null)
            IAPManager.Instance.OnPurchaseCompleted += OnPurchaseCompleted;

        RecoverPendingPurchase();
    }

    private void RecoverPendingPurchase()
    {
        if (_config == null || SaveManager.Instance == null) return;

        var data = SaveManager.Instance.GetGameData();
        if (string.IsNullOrEmpty(data.pendingPurchaseProductId)) return;

        var reward = _config.GetRewardByProductId(data.pendingPurchaseProductId);
        if (reward == null) return;

        Debug.Log($"[EBS] Recovering pending purchase: {data.pendingPurchaseProductId}");
        GrantRewards(reward);
        data.pendingPurchaseProductId = "";
        SaveManager.Instance.SaveData();
    }

    private void OnDestroy()
    {
        if (IAPManager.Instance != null)
            IAPManager.Instance.OnPurchaseCompleted -= OnPurchaseCompleted;
    }

    #endregion

    #region LEVEL EVENTS

    private void OnLevelFailed(LevelFailedContext ctx)
    {
        Debug.Log($"[EBS] OnLevelFailed | IsBoss={ctx.IsBossLevel}, config={_config != null}, save={SaveManager.Instance != null}");

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
        Debug.Log($"[EBS] streak={_normalLossStreak} threshold={_config.consecutiveLossThreshold} | ShouldOffer={shouldOffer}");

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

        Debug.Log($"[EBS] RequestShowEmergencyBundleOverlay | tier={tier} | productId='{productId}'");

        UIEvents.RequestShowEmergencyBundleOverlay(offer);
    }

    #endregion

    #region IAP CALLBACKS

    private void OnPurchaseCompleted(string productId)
    {
        if (!_offerActive || productId != _activeProductId) return;

        GrantRewards(_activeReward);
        AutoSaveManager.Instance?.OnEmergencyBundleActivated();
        CloseOffer(purchased: true);
    }

    private void GrantRewards(BundleRewardData reward)
    {
        if (reward == null) return;

        if (reward.unlimitedLives && reward.unlimitedLivesDurationMinutes > 0f)
        {
            LifeManager.Instance?.ActivateUnlimitedLives(reward.unlimitedLivesDurationMinutes);
        }
        else if (reward.regularLivesCount > 0)
        {
            for (int i = 0; i < reward.regularLivesCount; i++)
                LifeManager.Instance?.AddLife();
        }

        if (reward.powerUps != null)
        {
            foreach (var entry in reward.powerUps)
            {
                if (entry.quantity > 0)
                    AutoSaveManager.Instance?.OnPowerUpObtained(entry.type, entry.quantity);
            }
        }

    }

    private void CloseOffer(bool purchased = false)
    {
        _offerActive     = false;
        _activeProductId = null;
        UIEvents.RequestHideEmergencyBundleOverlay();

        if (!purchased)
            ShowFallbackDefeatUI();
    }

    private void ShowFallbackDefeatUI()
    {
        int lives = LifeManager.Instance != null ? LifeManager.Instance.GetRealLives() : 0;
        if (lives <= 0 || (LifeManager.Instance != null && !LifeManager.Instance.CanPlay()))
            UIEvents.RequestShowNoLivesOverlay();
        else
            UIEvents.RequestShowDefeatOverlay(lives);
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
            Debug.Log($"  smallBundleProductId: '{_config.smallBundleProductId}'");
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
