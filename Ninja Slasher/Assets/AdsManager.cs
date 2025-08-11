using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : MonoBehaviour
{
    [Header("LevelPlay Configuration")]
    [SerializeField] private string _appKey = "233038335";
    [SerializeField] private string _rewardedAdUnitId = "88ic5ya7o0vd1t02";
    [SerializeField] private string _interstitialAdUnitId = "y2is2h4ghz01hst6";

    [Header("Debug")]
    [SerializeField] private bool _enableTestSuite = true;

    private LevelPlayRewardedAd _rewardedAd;
    private LevelPlayInterstitialAd _interstitialAd;

    void Start()
    {
        InitializeLevelPlay();
    }

    void InitializeLevelPlay()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        if (_enableTestSuite)
        {
            LevelPlay.SetMetaData("is_test_suite", "enable");
        }

        Debug.Log("Inicializando LevelPlay");
        LevelPlay.Init(_appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay inicializado correctamente!");

        CreateAdUnits();

        if (_enableTestSuite)
        {
            LevelPlay.LaunchTestSuite();
        }
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"Error inicializando LevelPlay: {error.ErrorMessage}");
        Debug.LogError($"Código de error: {error.ErrorCode}");
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
        Debug.Log("loading ads...");
        _rewardedAd?.LoadAd();
        _interstitialAd?.LoadAd();
    }

    [ContextMenu("Show Rewarded Ad")]
    public void ShowRewardedAd()
    {
        if (_rewardedAd != null && _rewardedAd.IsAdReady())
        {
            Debug.Log("showing Rewarded Ad");
            _rewardedAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("Rewarded Ad isn't ready");
            _rewardedAd?.LoadAd();
        }
    }

    [ContextMenu("Show Interstitial Ad")]
    public void ShowInterstitialAd()
    {
        if (_interstitialAd != null && _interstitialAd.IsAdReady())
        {
            Debug.Log("showing Interstitial Ad");
            _interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("Interstitial Ad isn't ready");
            _interstitialAd?.LoadAd();
        }
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

    private void OnRewardedAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"Rewarded Ad loaded. Network: {adInfo.adNetwork}");
    }

    private void OnRewardedAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"Error loading Rewarded Ad: {error.ErrorMessage}");
        Invoke(nameof(LoadRewardedAd), 10f);
    }

    private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad showed");
    }

    private void OnRewardedAdDisplayFailed(LevelPlayAdDisplayInfoError error)
    {
        Debug.LogError($"Error showing Rewarded Ad: {error.LevelPlayError.ErrorMessage}");
    }

    private void OnRewardedAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"¡user rewarded!");
        Debug.Log($"reward: {reward.Name} - ampunt: {reward.Amount}");

        GiveReward(reward);
    }

    private void OnRewardedAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad closed");
        _rewardedAd?.LoadAd();
    }

    private void OnRewardedAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad clicked");
    }

    private void OnInterstitialAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"Interstitial Ad loaded. Network: {adInfo.adNetwork}");
    }

    private void OnInterstitialAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"❌ Error loading Interstitial Ad: {error.ErrorMessage}");
        Invoke(nameof(LoadInterstitialAd), 30f);
    }

    private void OnInterstitialAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad showed");
    }

    private void OnInterstitialAdDisplayFailed(LevelPlayAdDisplayInfoError error)
    {
        Debug.LogError($"Error showing Interstitial Ad: {error.LevelPlayError.ErrorMessage}");
    }

    private void OnInterstitialAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad closed");
        _interstitialAd?.LoadAd();
    }

    private void OnInterstitialAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad clicked");
    }

    private void LoadRewardedAd()
    {
        _rewardedAd?.LoadAd();
    }

    private void LoadInterstitialAd()
    {
        _interstitialAd?.LoadAd();
    }

    private void GiveReward(LevelPlayReward reward)
    {
        Debug.Log($"Otorgando reward: {reward.Name} x{reward.Amount}");

        // lógica de recompensa
    }

    public bool IsRewardedAdReady()
    {
        return _rewardedAd != null && _rewardedAd.IsAdReady();
    }

    public bool IsInterstitialAdReady()
    {
        return _interstitialAd != null && _interstitialAd.IsAdReady();
    }

    void OnDestroy()
    {
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