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

    void Start()
    {
        GameEvents.OnAdsRemoved += OnAdsRemoved;

        if (AreAdsRemoved())
        {
            Debug.Log("[AdsManager] Ads removed entitlement active. Skipping ads initialization.");
            return;
        }

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
        Debug.Log("[AdsManager] LevelPlay initialized");
        CreateAdUnits();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
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
        if (AreAdsRemoved()) return;

        _rewardedAd?.LoadAd();
        _interstitialAd?.LoadAd();
    }

    public void ShowRewardedAdForExtraLife()
    {
        if (AreAdsRemoved()) return;

        ShowRewardedAd(() =>
        {
            LifeManager.Instance?.AddLife();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AdsManager] Extra life granted");
#endif
        });
    }

    public void ShowRewardedAdForDoubleDailyReward()
    {
        if (AreAdsRemoved()) return;

        ShowRewardedAd(() =>
        {
            DailyRewardSystem.Instance?.DoubleTodaysReward();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AdsManager] Daily reward doubled");
#endif
        });
    }

    [ContextMenu("Show Rewarded Ad")]
    public void ShowRewardedAd()
    {
        if (AreAdsRemoved())
        {
            Debug.Log("[AdsManager] Rewarded ads disabled because Remove Ads is active.");
            return;
        }

        ShowRewardedAd(onRewarded: null);
    }

    [ContextMenu("Show Interstitial Ad")]
    public void ShowInterstitialAd()
    {
        if (AreAdsRemoved())
        {
            Debug.Log("[AdsManager] Interstitial ads disabled because Remove Ads is active.");
            return;
        }

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
        return !AreAdsRemoved() && _rewardedAd != null && _rewardedAd.IsAdReady();
    }

    public bool IsInterstitialAdReady()
    {
        return !AreAdsRemoved() && _interstitialAd != null && _interstitialAd.IsAdReady();
    }

    private void ShowRewardedAd(System.Action onRewarded)
    {
        if (AreAdsRemoved())
        {
            Debug.Log("[AdsManager] Rewarded ads disabled because Remove Ads is active.");
            return;
        }

        if (_rewardedAd != null && _rewardedAd.IsAdReady())
        {
            _pendingRewardCallback = onRewarded;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AdsManager] Showing rewarded ad");
#endif
            _rewardedAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("[AdsManager] Rewarded ad not ready");
            _rewardedAd?.LoadAd();
        }
    }

    private void OnRewardedAdLoaded(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[AdsManager] Rewarded ad loaded. Network: {adInfo.AdNetwork}");
#endif
    }

    private void OnRewardedAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"[AdsManager] Rewarded ad load failed: {error.ErrorMessage}");
        Invoke(nameof(LoadRewardedAd), 10f);
    }

    private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Rewarded ad displayed");
#endif
    }

    private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdsManager] Rewarded ad display failed: {error.ErrorMessage}");
        _pendingRewardCallback = null;
    }

    private void OnRewardedAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[AdsManager] User rewarded: {reward.Name} x{reward.Amount}");
#endif
        _pendingRewardCallback?.Invoke();
        _pendingRewardCallback = null;
    }

    private void OnRewardedAdClosed(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Rewarded ad closed");
#endif
        _pendingRewardCallback = null;
        if (!AreAdsRemoved())
            _rewardedAd?.LoadAd();
    }

    private void OnRewardedAdClicked(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Rewarded ad clicked");
#endif
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
        if (!AreAdsRemoved())
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
        if (AreAdsRemoved()) return;
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
        Debug.Log("[AdsManager] Remove Ads granted. Future ads are disabled.");
        CancelInvoke(nameof(LoadRewardedAd));
        CancelInvoke(nameof(LoadInterstitialAd));
        _pendingRewardCallback = null;
    }

    void OnDestroy()
    {
        GameEvents.OnAdsRemoved -= OnAdsRemoved;

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
