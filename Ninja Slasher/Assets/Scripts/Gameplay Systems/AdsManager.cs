using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : MonoBehaviourSingleton<AdsManager>
{
    [Header("LevelPlay Configuration")]
    [SerializeField] private string _appKey = "233038335";
    [SerializeField] private string _rewardedAdUnitId = "88ic5ya7o0vd1t02";
    [SerializeField] private string _interstitialAdUnitId = "y2is2h4ghz01hst6";

    private LevelPlayRewardedAd _rewardedAd;
    private LevelPlayInterstitialAd _interstitialAd;

    // Callback set at call-site (ShowRewardedAdForExtraLife / ShowRewardedAdForDoubleDailyReward).
    // Replaces the mutable _currentRewardType field to avoid incorrect rewards on rapid requests.
    private System.Action _pendingRewardCallback;

    void Start()
    {
        InitializeLevelPlay();
    }

    void InitializeLevelPlay()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed  += OnInitFailed;

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

        _rewardedAd.OnAdLoaded        += OnRewardedAdLoaded;
        _rewardedAd.OnAdLoadFailed    += OnRewardedAdLoadFailed;
        _rewardedAd.OnAdDisplayed     += OnRewardedAdDisplayed;
        _rewardedAd.OnAdDisplayFailed += OnRewardedAdDisplayFailed;
        _rewardedAd.OnAdRewarded      += OnRewardedAdRewarded;
        _rewardedAd.OnAdClosed        += OnRewardedAdClosed;
        _rewardedAd.OnAdClicked       += OnRewardedAdClicked;

        _interstitialAd = new LevelPlayInterstitialAd(_interstitialAdUnitId);

        _interstitialAd.OnAdLoaded        += OnInterstitialAdLoaded;
        _interstitialAd.OnAdLoadFailed    += OnInterstitialAdLoadFailed;
        _interstitialAd.OnAdDisplayed     += OnInterstitialAdDisplayed;
        _interstitialAd.OnAdDisplayFailed += OnInterstitialAdDisplayFailed;
        _interstitialAd.OnAdClosed        += OnInterstitialAdClosed;
        _interstitialAd.OnAdClicked       += OnInterstitialAdClicked;

        LoadAds();
    }

    private void LoadAds()
    {
        _rewardedAd?.LoadAd();
        _interstitialAd?.LoadAd();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Shows a rewarded ad. If completed, grants an extra life.
    /// The reward callback is captured here, not in a mutable field,
    /// so a rapid second call before the ad finishes does not corrupt the reward.
    /// </summary>
    public void ShowRewardedAdForExtraLife()
    {
        ShowRewardedAd(() =>
        {
            LifeManager.Instance?.AddLife();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[AdsManager] Extra life granted");
#endif
        });
    }

    /// <summary>Shows a rewarded ad. If completed, doubles today's daily reward.</summary>
    public void ShowRewardedAdForDoubleDailyReward()
    {
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
        // Context-menu / legacy path — no reward action.
        ShowRewardedAd(onRewarded: null);
    }

    [ContextMenu("Show Interstitial Ad")]
    public void ShowInterstitialAd()
    {
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

    public bool IsRewardedAdReady()    => _rewardedAd != null && _rewardedAd.IsAdReady();
    public bool IsInterstitialAdReady() => _interstitialAd != null && _interstitialAd.IsAdReady();

    // -------------------------------------------------------------------------
    // Internal show helper
    // -------------------------------------------------------------------------

    private void ShowRewardedAd(System.Action onRewarded)
    {
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

    // -------------------------------------------------------------------------
    // Rewarded ad callbacks
    // -------------------------------------------------------------------------

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
        _pendingRewardCallback = null; // Clear if closed without reward
        _rewardedAd?.LoadAd();
    }

    private void OnRewardedAdClicked(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Rewarded ad clicked");
#endif
    }

    // -------------------------------------------------------------------------
    // Interstitial callbacks
    // -------------------------------------------------------------------------

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
        _interstitialAd?.LoadAd();
    }

    private void OnInterstitialAdClicked(LevelPlayAdInfo adInfo)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[AdsManager] Interstitial ad clicked");
#endif
    }

    // -------------------------------------------------------------------------
    // Helpers / Debug
    // -------------------------------------------------------------------------

    private void LoadRewardedAd()    => _rewardedAd?.LoadAd();
    private void LoadInterstitialAd() => _interstitialAd?.LoadAd();

    [ContextMenu("Launch Test Suite")]
    public void LaunchTestSuite() => LevelPlay.LaunchTestSuite();

    [ContextMenu("Reload All Ads")]
    public void ReloadAllAds() => LoadAds();

    [ContextMenu("Test Show Extra Life Ad")]
    public void TestShowExtraLifeAd() => ShowRewardedAdForExtraLife();

    [ContextMenu("Test Double Daily Reward")]
    public void TestDoubleDailyReward()
    {
        if (DailyRewardSystem.Instance != null)
        {
            DailyRewardSystem.Instance.DoubleTodaysReward();
            Debug.Log("[AdsManager] Daily reward doubled (test)");
        }
    }

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed  -= OnInitFailed;

        if (_rewardedAd != null)
        {
            _rewardedAd.OnAdLoaded        -= OnRewardedAdLoaded;
            _rewardedAd.OnAdLoadFailed    -= OnRewardedAdLoadFailed;
            _rewardedAd.OnAdDisplayed     -= OnRewardedAdDisplayed;
            _rewardedAd.OnAdDisplayFailed -= OnRewardedAdDisplayFailed;
            _rewardedAd.OnAdRewarded      -= OnRewardedAdRewarded;
            _rewardedAd.OnAdClosed        -= OnRewardedAdClosed;
            _rewardedAd.OnAdClicked       -= OnRewardedAdClicked;
            _rewardedAd.DestroyAd();
        }

        if (_interstitialAd != null)
        {
            _interstitialAd.OnAdLoaded        -= OnInterstitialAdLoaded;
            _interstitialAd.OnAdLoadFailed    -= OnInterstitialAdLoadFailed;
            _interstitialAd.OnAdDisplayed     -= OnInterstitialAdDisplayed;
            _interstitialAd.OnAdDisplayFailed -= OnInterstitialAdDisplayFailed;
            _interstitialAd.OnAdClosed        -= OnInterstitialAdClosed;
            _interstitialAd.OnAdClicked       -= OnInterstitialAdClicked;
            _interstitialAd.DestroyAd();
        }
    }
}
