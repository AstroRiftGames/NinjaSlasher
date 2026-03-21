using UnityEngine;
using Unity.Services.LevelPlay;
using System;

public class AdsManager : MonoBehaviourSingleton<AdsManager>
{
    [Header("LevelPlay Configuration")]
    [SerializeField] private string _appKey = "233038335";
    [SerializeField] private string _rewardedAdUnitId = "88ic5ya7o0vd1t02";
    [SerializeField] private string _interstitialAdUnitId = "y2is2h4ghz01hst6";

    private LevelPlayRewardedAd _rewardedAd;
    private LevelPlayInterstitialAd _interstitialAd;

    private Action _pendingRewardCallback;
    private string _pendingRewardContext;
    private bool _pendingRewardShowRequest;

    private bool _interstitialPending = false;
    private bool _isLevelPlayInitialized = false;
    private bool _levelPlayInitFailed = false;

    public event Action OnRewardedAdReadinessChanged;

    void Start()
    {
        GameEvents.OnAdsRemoved += OnAdsRemoved;
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
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Initializing LevelPlay");
#endif
        LevelPlay.Init(_appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        _isLevelPlayInitialized = true;
        _levelPlayInitFailed = false;
        Debug.Log("[AdsManager] LevelPlay initialized");
        CreateAdUnits();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        _isLevelPlayInitialized = false;
        _levelPlayInitFailed = true;
        Debug.LogError($"[AdsManager] LevelPlay init failed ({error.ErrorCode}): {error.ErrorMessage}");
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
            LifeManager.Instance?.AddLife();
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
    public void ShowInterstitialAd()
    {
        if (AreAdsRemoved())
        {
            Debug.Log("[AdsManager] Interstitial ads disabled because Remove Ads is active.");
            return;
        }

        _interstitialPending = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Interstitial ad deferred — esperando OnResultsScreenFullyShown");
#endif
    }

    private void OnResultsActionTaken()
    {
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
            Debug.Log("[AdsManager] Showing interstitial ad");
#endif
            _interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("[AdsManager] Interstitial ad not ready");
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
        if (_rewardedAd != null && _rewardedAd.IsAdReady())
        {
            _pendingRewardCallback = onRewarded;
            _pendingRewardContext = context;
            _pendingRewardShowRequest = false;
            Debug.Log($"[AdsManager] Showing rewarded ad | context={context}");
            _rewardedAd.ShowAd();
        }
        else
        {
            _pendingRewardCallback = onRewarded;
            _pendingRewardContext = context;
            _pendingRewardShowRequest = true;

            string reason = _rewardedAd == null
                ? "rewarded unit not created yet"
                : "rewarded ad not ready";

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
        Debug.Log($"[AdsManager] Rewarded ad displayed | context={_pendingRewardContext}");
    }

    private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdsManager] Rewarded ad display failed | context={_pendingRewardContext} | error={error.ErrorMessage}");
        CancelPendingRewardedRequest("display failed");
        OnRewardedAdReadinessChanged?.Invoke();
    }

    private void OnRewardedAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"[AdsManager] Reward received | context={_pendingRewardContext} | reward={reward.Name} x{reward.Amount}");
        _pendingRewardCallback?.Invoke();
        _pendingRewardCallback = null;
    }

    private void OnRewardedAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsManager] Rewarded ad closed | context={_pendingRewardContext}");
        CancelPendingRewardedRequest("ad closed");
        _rewardedAd?.LoadAd();
    }

    private void OnRewardedAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdsManager] Rewarded ad clicked | context={_pendingRewardContext}");
    }

    private void OnInterstitialAdLoaded(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[AdsManager] Interstitial ad loaded. Network: {adInfo.AdNetwork}");
#endif
    }

    private void OnInterstitialAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"[AdsManager] Interstitial ad load failed: {error.ErrorMessage}");
        Invoke(nameof(LoadInterstitialAd), 30f);
    }

    private void OnInterstitialAdDisplayed(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Interstitial ad displayed");
#endif
    }

    private void OnInterstitialAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdsManager] Interstitial ad display failed: {error.ErrorMessage}");
    }

    private void OnInterstitialAdClosed(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Interstitial ad closed");
#endif
        if (!AreAdsRemoved())
            _interstitialAd?.LoadAd();
    }

    private void OnInterstitialAdClicked(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Interstitial ad clicked");
#endif
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
        return !_levelPlayInitFailed;
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
    }

    void OnDestroy()
    {
        GameEvents.OnAdsRemoved -= OnAdsRemoved;
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
