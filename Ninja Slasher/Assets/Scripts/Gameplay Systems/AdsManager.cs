using UnityEngine;
using Unity.Services.LevelPlay;
using System;

public class AdsManager : MonoBehaviourSingleton<AdsManager>
{
    [Header("LevelPlay Configuration")]
    [SerializeField] private string _appKey = "26126460d";
    [SerializeField] private string _rewardedAdUnitId = "yaociswpoabh9k1w";
    [SerializeField] private string _interstitialAdUnitId = "pqtjob97lz95hfyr";

    private LevelPlayRewardedAd _rewardedAd;
    private LevelPlayInterstitialAd _interstitialAd;

    private Action _pendingRewardCallback;
    private string _pendingRewardContext;
    private bool _pendingRewardShowRequest;
    private bool _rewardedAdShowing;
    private bool _rewardGrantedForCurrentAd;
    private bool _awaitingRewardAfterClose;

    private bool   _interstitialPending = false;
    private string _pendingInterstitialPlacement = "";
    private int    _pendingInterstitialLevelId   = -1;

    private bool _isLevelPlayInitialized = false;
    private bool _levelPlayInitFailed = false;
    private bool _isInitializingLevelPlay = false;
    private int _sessionGamesSinceLastInterstitial = 0;

    public event Action OnRewardedAdReadinessChanged;
    public event Action<string, bool> OnRewardedAdFlowCompleted;

    private int GamesRequiredForInterstitialAd =>
        GameConfigManager.IsReady()
            ? Mathf.Max(1, GameConfigManager.Config.gamesRequiredForInterstitialAd)
            : 3;

    private bool EnableSessionGameplayInterstitialAds =>
        !GameConfigManager.IsReady() || GameConfigManager.Config.enableSessionGameplayInterstitialAds;

    void Start()
    {
        GameEvents.OnAdsRemoved += OnAdsRemoved;
        GameEvents.OnLevelCompleted += OnLevelCompleted;
        GameEvents.OnLevelFailed += OnLevelFailed;
        UIEvents.OnRetryButtonPressed += OnResultsActionTaken;
        UIEvents.OnQuitToMenuPressed  += OnResultsActionTaken;

        InitializeLevelPlay();
    }

    private bool AreAdsRemoved()
    {
        return SaveManager.Instance != null && SaveManager.Instance.GetAdsRemoved();
    }

    void InitializeLevelPlay()
    {
        if (_isLevelPlayInitialized || _isInitializingLevelPlay)
            return;

        _isInitializingLevelPlay = true;
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        Debug.Log("[AdsManager] Initializing LevelPlay");
        LevelPlay.Init(_appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        _isInitializingLevelPlay = false;
        _isLevelPlayInitialized = true;
        _levelPlayInitFailed = false;
        Debug.Log("[AdsManager] LevelPlay initialized");
        CreateAdUnits();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        _isInitializingLevelPlay = false;
        _isLevelPlayInitialized = false;
        _levelPlayInitFailed = true;
        Debug.LogError($"[AdsManager] LevelPlay init failed ({error.ErrorCode}): {error.ErrorMessage}");
        OnRewardedAdReadinessChanged?.Invoke();
        Invoke(nameof(RetryInitializeLevelPlay), 10f);
    }

    private void CreateAdUnits()
    {
        _rewardedAd = new LevelPlayRewardedAd(_rewardedAdUnitId);

        _rewardedAd.OnAdLoaded += OnRewardedAdLoaded;
        _rewardedAd.OnAdLoadFailed += OnRewardedAdLoadFailed;
        _rewardedAd.OnAdDisplayed += OnRewardedAdDisplayed;
        _rewardedAd.OnAdDisplayFailed += OnRewardedAdDisplayFailed;
        _rewardedAd.OnAdRewarded += OnRewardedAdRewarded;
        _rewardedAd.OnAdClosed += OnRewardedAdClosed;
        _rewardedAd.OnAdClicked += OnRewardedAdClicked;

        _interstitialAd = new LevelPlayInterstitialAd(_interstitialAdUnitId);

        _interstitialAd.OnAdLoaded += OnInterstitialAdLoaded;
        _interstitialAd.OnAdLoadFailed += OnInterstitialAdLoadFailed;
        _interstitialAd.OnAdDisplayed += OnInterstitialAdDisplayed;
        _interstitialAd.OnAdDisplayFailed += OnInterstitialAdDisplayFailed;
        _interstitialAd.OnAdClosed += OnInterstitialAdClosed;
        _interstitialAd.OnAdClicked += OnInterstitialAdClicked;

        LoadAds();
    }

    private void LoadAds()
    {
        _rewardedAd?.LoadAd();

        if (!AreAdsRemoved())
            _interstitialAd?.LoadAd();
    }

    public void ShowRewardedAdForExtraLife()
    {
        ShowRewardedAd(() =>
        {
            LifeManager.Instance?.AddLife(LifeRestoreSource.AdReward);
            Debug.Log("[AdsManager] Extra life granted from rewarded ad.");
        }, "extra_life");
    }

    public void ShowRewardedAdForDoubleDailyReward()
    {
        ShowRewardedAd(() =>
        {
            DailyRewardSystem.Instance?.DoubleTodaysReward();
            Debug.Log("[AdsManager] Daily reward doubled from rewarded ad.");
        }, "double_daily_reward");
    }

    [ContextMenu("Show Rewarded Ad")]
    public void ShowRewardedAd()
    {
        ShowRewardedAd(onRewarded: null, context: "manual");
    }

    [ContextMenu("Show Interstitial Ad")]
    public void ShowInterstitialAd(string placement = "")
    {
        if (AreAdsRemoved())
        {
            Debug.Log("[AdsManager] Interstitial ads disabled because Remove Ads is active.");
            return;
        }

        int levelId = LevelSessionManager.Instance?.CurrentSession?.LevelId ?? -1;

        AnalyticsManager.Instance?.RecordInterstitialOpportunity(placement, levelId);

        if (_interstitialAd == null || !_interstitialAd.IsAdReady())
        {
            Debug.LogWarning($"[AdsManager] Interstitial not available | placement={placement}");
            AnalyticsManager.Instance?.RecordInterstitialFailed(placement, "not_available", levelId);
            _interstitialAd?.LoadAd();
            return;
        }

        _pendingInterstitialPlacement = placement;
        _pendingInterstitialLevelId   = levelId;
        _interstitialPending          = true;

        Debug.Log($"[AdsManager] Interstitial deferred | placement={placement}");
    }

    private void OnLevelCompleted(LevelStats stats)
    {
        RegisterSessionGameForInterstitial("level_completed");
    }

    private void OnLevelFailed(LevelFailedContext context)
    {
        RegisterSessionGameForInterstitial("level_failed");
    }

    private void RegisterSessionGameForInterstitial(string placement)
    {
        if (!EnableSessionGameplayInterstitialAds || AreAdsRemoved())
            return;

        _sessionGamesSinceLastInterstitial++;
        Debug.Log($"[AdsManager] Session game counter | placement={placement} | count={_sessionGamesSinceLastInterstitial}/{GamesRequiredForInterstitialAd}");

        if (_sessionGamesSinceLastInterstitial < GamesRequiredForInterstitialAd)
            return;

        _sessionGamesSinceLastInterstitial = 0;
        ShowInterstitialAd(placement);
    }

    private void OnResultsActionTaken()
    {
        if (IsRewardedAdFlowInProgress())
        {
            Debug.Log("[AdsManager] Ignoring results action while rewarded flow is still in progress.");
            return;
        }

        CancelPendingRewardedRequest("results action taken");

        if (!_interstitialPending) return;

        _interstitialPending = false;
        ShowInterstitialAdNow();
    }

    private void ShowInterstitialAdNow()
    {
        if (AreAdsRemoved()) return;

        if (_interstitialAd != null && _interstitialAd.IsAdReady())
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[AdsManager] Showing interstitial | placement={_pendingInterstitialPlacement}");
#endif
            _interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogWarning($"[AdsManager] Interstitial lost readiness | placement={_pendingInterstitialPlacement}");
            AnalyticsManager.Instance?.RecordInterstitialFailed(
                _pendingInterstitialPlacement, "not_ready_at_show", _pendingInterstitialLevelId);
            ClearPendingInterstitial();
            _interstitialAd?.LoadAd();
        }
    }

    public bool IsRewardedAdReady()
    {
        return _rewardedAd != null && _rewardedAd.IsAdReady();
    }

    public bool IsInterstitialAdReady()
    {
        return !AreAdsRemoved() && _interstitialAd != null && _interstitialAd.IsAdReady();
    }

    private void ShowRewardedAd(System.Action onRewarded, string context)
    {
        AnalyticsManager.Instance?.RecordRewardedAdRequested(context);

        if (_rewardedAd != null && _rewardedAd.IsAdReady())
        {
            _pendingRewardCallback = onRewarded;
            _pendingRewardContext = context;
            _pendingRewardShowRequest = false;
            _rewardGrantedForCurrentAd = false;
            _awaitingRewardAfterClose = false;
            CancelInvoke(nameof(FinalizePendingRewardedClose));
            Debug.Log($"[AdsManager] Showing rewarded ad | context={context}");
            _rewardedAd.ShowAd();
        }
        else
        {
            _pendingRewardCallback = onRewarded;
            _pendingRewardContext = context;
            _pendingRewardShowRequest = true;

            string reason;
            if (_levelPlayInitFailed)
            {
                reason = "levelplay init failed";
                RetryInitializeLevelPlay();
            }
            else if (_rewardedAd == null)
            {
                reason = "rewarded unit not created yet";
            }
            else
            {
                reason = "rewarded ad not ready";
            }

            Debug.LogWarning($"[AdsManager] Rewarded ad show deferred | context={context} | reason={reason}");
            _rewardedAd?.LoadAd();
        }
    }

    private void OnRewardedAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsManager] Rewarded ad loaded. Network: {adInfo.AdNetwork}");
        OnRewardedAdReadinessChanged?.Invoke();

        if (_pendingRewardShowRequest && _rewardedAd != null && _rewardedAd.IsAdReady())
        {
            _pendingRewardShowRequest = false;
            Debug.Log($"[AdsManager] Retrying deferred rewarded ad | context={_pendingRewardContext}");
            _rewardedAd.ShowAd();
        }
    }

    private void OnRewardedAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"[AdsManager] Rewarded ad load failed: {error.ErrorMessage}");
        Invoke(nameof(LoadRewardedAd), 10f);
        OnRewardedAdReadinessChanged?.Invoke();
    }

    private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo)
    {
        _rewardedAdShowing = true;
        _interstitialPending = false;
        ClearPendingInterstitial();
        Debug.Log($"[AdsManager] Rewarded ad displayed | context={_pendingRewardContext}");
        AnalyticsManager.Instance?.RecordRewardedAdShown(_pendingRewardContext);
    }

    private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        string context = _pendingRewardContext;
        _rewardedAdShowing = false;
        _rewardGrantedForCurrentAd = false;
        _awaitingRewardAfterClose = false;
        Debug.LogError($"[AdsManager] Rewarded ad display failed | context={_pendingRewardContext} | error={error.ErrorMessage}");
        AnalyticsManager.Instance?.RecordRewardedAdFailed(_pendingRewardContext, error.ErrorMessage);
        CancelPendingRewardedRequest("display failed");
        OnRewardedAdFlowCompleted?.Invoke(context, false);
        OnRewardedAdReadinessChanged?.Invoke();
    }

    private void OnRewardedAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        string context = _pendingRewardContext;
        _rewardGrantedForCurrentAd = true;
        _awaitingRewardAfterClose = false;
        CancelInvoke(nameof(FinalizePendingRewardedClose));
        Debug.Log($"[AdsManager] Reward received | context={_pendingRewardContext} | reward={reward.Name} x{reward.Amount}");
        AnalyticsManager.Instance?.RecordRewardedAdCompleted(_pendingRewardContext);
        _pendingRewardCallback?.Invoke();
        _pendingRewardCallback = null;

        if (!_rewardedAdShowing)
        {
            OnRewardedAdFlowCompleted?.Invoke(context, true);
            CancelPendingRewardedRequest("reward granted after close");
            _rewardGrantedForCurrentAd = false;
        }
    }

    private void OnRewardedAdClosed(LevelPlayAdInfo adInfo)
    {
        string context = _pendingRewardContext;
        bool rewardGranted = _rewardGrantedForCurrentAd;
        _rewardedAdShowing = false;
        Debug.Log($"[AdsManager] Rewarded ad closed | context={_pendingRewardContext}");
        AnalyticsManager.Instance?.RecordRewardedAdClosed(_pendingRewardContext);
        _rewardedAd?.LoadAd();

        if (rewardGranted)
        {
            OnRewardedAdFlowCompleted?.Invoke(context, true);
            CancelPendingRewardedRequest("ad closed after reward");
            _rewardGrantedForCurrentAd = false;
            _awaitingRewardAfterClose = false;
            return;
        }

        _awaitingRewardAfterClose = true;
        CancelInvoke(nameof(FinalizePendingRewardedClose));
        Invoke(nameof(FinalizePendingRewardedClose), 0.75f);
    }

    private void OnRewardedAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsManager] Rewarded ad clicked | context={_pendingRewardContext}");
    }

    private void OnInterstitialAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsManager] Interstitial ad loaded. Network: {adInfo.AdNetwork}");
    }

    private void OnInterstitialAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"[AdsManager] Interstitial ad load failed: {error.ErrorMessage}");
        Invoke(nameof(LoadInterstitialAd), 30f);
    }

    private void OnInterstitialAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsManager] Interstitial displayed | placement={_pendingInterstitialPlacement}");
        AnalyticsManager.Instance?.RecordInterstitialShown(
            _pendingInterstitialPlacement, _pendingInterstitialLevelId);
    }

    private void OnInterstitialAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdsManager] Interstitial display failed | placement={_pendingInterstitialPlacement} | error={error.ErrorMessage}");
        AnalyticsManager.Instance?.RecordInterstitialFailed(
            _pendingInterstitialPlacement, error.ErrorMessage, _pendingInterstitialLevelId);
        ClearPendingInterstitial();
    }

    private void OnInterstitialAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsManager] Interstitial closed | placement={_pendingInterstitialPlacement}");
        AnalyticsManager.Instance?.RecordInterstitialClosed(
            _pendingInterstitialPlacement, _pendingInterstitialLevelId);
        ClearPendingInterstitial();

        if (!AreAdsRemoved())
            _interstitialAd?.LoadAd();
    }

    private void OnInterstitialAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdsManager] Interstitial ad clicked");
    }

    private void ClearPendingInterstitial()
    {
        _pendingInterstitialPlacement = "";
        _pendingInterstitialLevelId   = -1;
    }

    private void LoadRewardedAd()
    {
        _rewardedAd?.LoadAd();
    }

    private void LoadInterstitialAd()
    {
        if (!AreAdsRemoved())
            _interstitialAd?.LoadAd();
    }

    [ContextMenu("Launch Test Suite")]
    public void LaunchTestSuite()
    {
        LevelPlay.LaunchTestSuite();
    }

    [ContextMenu("Reload All Ads")]
    public void ReloadAllAds()
    {
        LoadAds();
    }

    [ContextMenu("Test Show Extra Life Ad")]
    public void TestShowExtraLifeAd()
    {
        ShowRewardedAdForExtraLife();
    }

    [ContextMenu("Test Double Daily Reward")]
    public void TestDoubleDailyReward()
    {
        if (DailyRewardSystem.Instance != null)
        {
            DailyRewardSystem.Instance.DoubleTodaysReward();
            Debug.Log("[AdsManager] Daily reward doubled (test)");
        }
    }

    private void OnAdsRemoved()
    {
        Debug.Log("[AdsManager] Remove Ads granted. Interstitial ads are disabled; rewarded ads remain available.");
        CancelInvoke(nameof(LoadInterstitialAd));
    }

    public bool CanRequestRewardedAd()
    {
        return _rewardedAd != null && _rewardedAd.IsAdReady();
    }

    public bool IsRewardedAdFlowInProgress(string context = null)
    {
        bool hasPendingRequest =
            _pendingRewardCallback != null ||
            _pendingRewardShowRequest ||
            _rewardedAdShowing ||
            _awaitingRewardAfterClose;

        if (!hasPendingRequest)
            return false;

        return string.IsNullOrEmpty(context) || string.Equals(_pendingRewardContext, context, StringComparison.Ordinal);
    }

    public string GetRewardedAvailabilityReason()
    {
        if (_rewardedAd != null && _rewardedAd.IsAdReady())
            return "ready";

        if (_levelPlayInitFailed)
            return "rewarded init failed";

        if (_rewardedAd == null)
            return _isLevelPlayInitialized
                ? "rewarded unit not initialized"
                : "rewarded init pending";

        return "rewarded loading";
    }

    private void CancelPendingRewardedRequest(string reason)
    {
        if (_pendingRewardCallback == null && !_pendingRewardShowRequest && string.IsNullOrEmpty(_pendingRewardContext))
            return;

        Debug.Log($"[AdsManager] Clearing pending rewarded request | context={_pendingRewardContext} | reason={reason}");
        _pendingRewardCallback = null;
        _pendingRewardContext = null;
        _pendingRewardShowRequest = false;
        _rewardedAdShowing = false;
        _awaitingRewardAfterClose = false;
    }

    private void FinalizePendingRewardedClose()
    {
        if (_rewardGrantedForCurrentAd)
            return;

        string context = _pendingRewardContext;
        _awaitingRewardAfterClose = false;
        OnRewardedAdFlowCompleted?.Invoke(context, false);
        CancelPendingRewardedRequest("ad closed without reward");
        OnRewardedAdReadinessChanged?.Invoke();
    }

    private void RetryInitializeLevelPlay()
    {
        if (_isLevelPlayInitialized || _isInitializingLevelPlay)
            return;

        Debug.Log("[AdsManager] Retrying LevelPlay initialization.");
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
        InitializeLevelPlay();
    }

    void OnDestroy()
    {
        GameEvents.OnAdsRemoved -= OnAdsRemoved;
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        UIEvents.OnRetryButtonPressed -= OnResultsActionTaken;
        UIEvents.OnQuitToMenuPressed  -= OnResultsActionTaken;

        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        if (_rewardedAd != null)
        {
            _rewardedAd.OnAdLoaded -= OnRewardedAdLoaded;
            _rewardedAd.OnAdLoadFailed -= OnRewardedAdLoadFailed;
            _rewardedAd.OnAdDisplayed -= OnRewardedAdDisplayed;
            _rewardedAd.OnAdDisplayFailed -= OnRewardedAdDisplayFailed;
            _rewardedAd.OnAdRewarded -= OnRewardedAdRewarded;
            _rewardedAd.OnAdClosed -= OnRewardedAdClosed;
            _rewardedAd.OnAdClicked -= OnRewardedAdClicked;
            _rewardedAd.DestroyAd();
        }

        if (_interstitialAd != null)
        {
            _interstitialAd.OnAdLoaded -= OnInterstitialAdLoaded;
            _interstitialAd.OnAdLoadFailed -= OnInterstitialAdLoadFailed;
            _interstitialAd.OnAdDisplayed -= OnInterstitialAdDisplayed;
            _interstitialAd.OnAdDisplayFailed -= OnInterstitialAdDisplayFailed;
            _interstitialAd.OnAdClosed -= OnInterstitialAdClosed;
            _interstitialAd.OnAdClicked -= OnInterstitialAdClicked;
            _interstitialAd.DestroyAd();
        }
    }
}
