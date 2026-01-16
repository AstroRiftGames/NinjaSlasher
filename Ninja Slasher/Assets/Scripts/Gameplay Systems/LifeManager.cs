using System;
using UnityEngine;

public class LifeManager : MonoBehaviourSingleton<LifeManager>
{
    public int CurrentLives { get; private set; }
    private DateTime _lastLifeUsedUtc;

    [Header("VIRTUAL LIFE DEDUCTION")]
    private int _virtualLives;
    private bool _hasVirtualDeduction = false;

    // DEPRECATED
    //[Header("ADS CONFIGURATION")]
    //[SerializeField] private int lossesRequiredForAd = 2;
    //[SerializeField] private bool enableConsecutiveLossAds = true;
    //[SerializeField] private bool enableNoLivesAds = true;
    //[Header("LIVES SETTINGS")]
    //[SerializeField] private int _maxLives = 5;
    //[SerializeField] private int _startingLives = 5;
    //[SerializeField] private int _lifeRechargeSeconds = 1800;

    private int MaxLives => GameConfigManager.Config.maxLives;
    private int StartingLives => GameConfigManager.Config.startingLives;
    private int LifeRechargeSeconds => GameConfigManager.Config.lifeRechargeSeconds;
    private int LossesRequiredForAd => GameConfigManager.Config.lossesRequiredForAd;
    private bool EnableConsecutiveLossAds => GameConfigManager.Config.enableConsecutiveLossAds;
    private bool EnableNoLivesAds => GameConfigManager.Config.enableNoLivesAds;

    private int totalLivesLostThisSession = 0;

    private int currentConsecutiveLosses = 0;

    private bool _levelInProgress = false;

    public event Action<int> OnLivesChanged;

    public override void Awake()
    {
        base.Awake();
        InitializeFromSave();
        LoadAdsProgress();
    }

    private void Start()
    {
        OnLivesChanged?.Invoke(GetDisplayLives());
    }

    private void Update()
    {
        UpdateLifeRecharge();
    }

    private void InitializeFromSave()
    {
        var data = SaveManager.Instance?.GetGameData();

        DateTime lastRegenUtc = DateTime.UtcNow;
        bool validDate = data != null && DateTime.TryParse(data.lastLifeRegenTime, out lastRegenUtc);

        bool isCorruptOrFirstTime =
            data == null ||
            data.currentLives < 0 ||
            data.currentLives > MaxLives ||
            !validDate;

        if (isCorruptOrFirstTime)
        {
            CurrentLives = Mathf.Clamp(StartingLives, 0, MaxLives);
            _lastLifeUsedUtc = DateTime.UtcNow;
            Persist("Init (default)");
        }
        else
        {
            CurrentLives = Mathf.Clamp(data.currentLives, 0, MaxLives);
            _lastLifeUsedUtc = DateTime.SpecifyKind(lastRegenUtc, DateTimeKind.Utc);
        }

        CheckOfflineRegeneration();
        _virtualLives = CurrentLives;
    }

    private void Persist(string reason = "Autosave")
    {
        bool hasTimer = CurrentLives < MaxLives;
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnLivesChanged(CurrentLives, _lastLifeUsedUtc, hasTimer);
        }
        else
        {
            SaveManager.Instance?.UpdateLives(CurrentLives, _lastLifeUsedUtc, hasTimer);
        }
#if UNITY_EDITOR
        Debug.Log($"[LifeManager] Persist -> {reason}. Lives={CurrentLives}, lastUsedUtc={_lastLifeUsedUtc:O}, timer={(hasTimer ? "ON" : "OFF")}");
#endif
    }

    private void UpdateLifeRecharge()
    {
        if (CurrentLives >= MaxLives) return;

        double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
        if (seconds < LifeRechargeSeconds) return;

        int toGenerate = Mathf.FloorToInt((float)seconds / LifeRechargeSeconds);
        int newLives = Mathf.Min(CurrentLives + toGenerate, MaxLives);

        _lastLifeUsedUtc = _lastLifeUsedUtc.AddSeconds(toGenerate * LifeRechargeSeconds);
        CurrentLives = newLives;

        if (_hasVirtualDeduction)
            _virtualLives = Mathf.Max(0, CurrentLives - 1);
        else
            _virtualLives = CurrentLives;

        if (AnalyticsManager.Instance != null && toGenerate > 0)
        {
            AnalyticsManager.Instance.RecordLifeRestored(
                CurrentLives,
                "timeRegeneration"
            );
        }

        Persist("Vida regenerada");
        EmitDisplayLivesChanged();
    }


    private void CheckOfflineRegeneration()
    {
        if (CurrentLives >= MaxLives) return;

        double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
        if (seconds < LifeRechargeSeconds) return;

        int toGenerate = Mathf.FloorToInt((float)seconds / LifeRechargeSeconds);
        int newLives = Mathf.Min(CurrentLives + toGenerate, MaxLives);

        _lastLifeUsedUtc = _lastLifeUsedUtc.AddSeconds(toGenerate * LifeRechargeSeconds);
        CurrentLives = newLives;

        _virtualLives = _hasVirtualDeduction ? Mathf.Max(0, CurrentLives - 1) : CurrentLives;

        Persist("Vidas offline regeneradas");
        EmitDisplayLivesChanged();
    }

    public bool CanPlay()
    {
        if (GameConfigManager.IsReady() && GameConfigManager.Config.infiniteLives)
        {
            return true;
        }

        return CurrentLives > 0;
    }

    public void OnLevelStart()
    {
        _hasVirtualDeduction = false;

        if (CurrentLives > 0)
        {
            _virtualLives = Mathf.Max(0, CurrentLives - 1);
            _hasVirtualDeduction = true;
            _levelInProgress = true;

            EmitDisplayLivesChanged();
        }
    }

    public void UseLife()
    {
        var context = PowerUpManager.Instance?.context;
        if (context != null && context.SecondChanceActive)
        {
            EmitDisplayLivesChanged();
            return;
        }

        if (_hasVirtualDeduction)
        {
            CurrentLives = Mathf.Clamp(_virtualLives, 0, MaxLives);
            _lastLifeUsedUtc = DateTime.UtcNow;

            _hasVirtualDeduction = false;
            _levelInProgress = false;

            totalLivesLostThisSession++;
            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.RecordLifeLost(
                    CurrentLives,
                    totalLivesLostThisSession,
                    "levelFailed"
                );
            }

            CheckLifeLossAds();

            Persist("Vida perdida (confirmada)");
            EmitDisplayLivesChanged();
        }
        else
        {
            if (CurrentLives <= 0) return;

            CurrentLives = Mathf.Max(0, CurrentLives - 1);
            _lastLifeUsedUtc = DateTime.UtcNow;
            _virtualLives = CurrentLives;

            totalLivesLostThisSession++;
            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.RecordLifeLost(
                    CurrentLives,
                    totalLivesLostThisSession,
                    "directUse"
                );
            }

            CheckLifeLossAds();

            Persist("Vida perdida (directa)");
            EmitDisplayLivesChanged();
        }
    }

    public void OnLevelCompleted()
    {
        if (_hasVirtualDeduction)
        {
            _virtualLives = CurrentLives;
            _hasVirtualDeduction = false;
            _levelInProgress = false;

            ResetLossCounter();

            EmitDisplayLivesChanged();
        }
    }

    public void OnLevelExit()
    {
        if (_hasVirtualDeduction)
        {
            CurrentLives = Mathf.Clamp(_virtualLives, 0, MaxLives);
            _lastLifeUsedUtc = DateTime.UtcNow;

            _hasVirtualDeduction = false;
            _levelInProgress = false;

            Persist("Vida perdida por abandono");
            EmitDisplayLivesChanged();
        }
    }

    public int GetDisplayLives() => _hasVirtualDeduction ? _virtualLives : CurrentLives;
    public int GetRealLives() => CurrentLives;

    public void AddLife()
    {
        if (CurrentLives >= MaxLives) return;

        CurrentLives++;

        if (_hasVirtualDeduction)
            _virtualLives = Mathf.Max(0, CurrentLives - 1);
        else
            _virtualLives = CurrentLives;

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.RecordLifeRestored(
                CurrentLives,
                "manualAdd"
            );
        }

        Persist("Vida ganada");
        EmitDisplayLivesChanged();
    }

    public void FillAllLives()
    {
        if (CurrentLives >= MaxLives) return;

        int previousLives = CurrentLives;
        CurrentLives = MaxLives;
        _lastLifeUsedUtc = DateTime.UtcNow;

        if (_hasVirtualDeduction)
            _virtualLives = Mathf.Max(0, CurrentLives - 1);
        else
            _virtualLives = CurrentLives;

        Persist("Vidas completas");
        EmitDisplayLivesChanged();
    }

    public float GetRechargeProgress()
    {
        if (CurrentLives >= MaxLives) return 1f;
        double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
        return Mathf.Clamp01((float)(seconds / LifeRechargeSeconds));
    }

    public TimeSpan GetTimeToNextLife()
    {
        if (CurrentLives >= MaxLives) return TimeSpan.Zero;
        double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
        double secondsLeft = LifeRechargeSeconds - seconds;
        return TimeSpan.FromSeconds(Mathf.Max(0, (float)secondsLeft));
    }

    public bool HasPendingDeduction() => _hasVirtualDeduction;

    private void OnApplicationFocus(bool hasFocus) { }

    private void EmitDisplayLivesChanged()
    {
        int displayLives = GetDisplayLives();

        GameEvents.RaiseLivesChanged(displayLives);

        // DEPRECATED
        //OnLivesChanged?.Invoke(displayLives);
    }

    private void LoadAdsProgress()
    {
        currentConsecutiveLosses = PlayerPrefs.GetInt("ConsecutiveLosses", 0);
    }

    private void SaveAdsProgress()
    {
        PlayerPrefs.SetInt("ConsecutiveLosses", currentConsecutiveLosses);
        PlayerPrefs.Save();
    }

    private void CheckLifeLossAds()
    {
        if (EnableNoLivesAds && CurrentLives == 0)
        {
            ShowNoLivesAd();
            ResetLossCounter();
        }
        else if (EnableConsecutiveLossAds)
        {
            currentConsecutiveLosses++;

            if (currentConsecutiveLosses >= LossesRequiredForAd)
            {
                ShowConsecutiveLossAd();
                ResetLossCounter();
            }
        }

        SaveAdsProgress();
    }

    private void ShowConsecutiveLossAd()
    {
        if (AdsManager.Instance != null && AdsManager.Instance.IsInterstitialAdReady())
        {
            AdsManager.Instance.ShowInterstitialAd();
        }
        else
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ReloadAllAds();
            }
        }
    }

    private void ShowNoLivesAd()
    {
        if (AdsManager.Instance != null && AdsManager.Instance.IsInterstitialAdReady())
        {
            AdsManager.Instance.ShowInterstitialAd();
        }
        else
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ReloadAllAds();
            }
        }
    }

    private void ResetLossCounter()
    {
        if (currentConsecutiveLosses > 0)
        {
            Debug.Log($"Contador de derrotas reseteado (era: {currentConsecutiveLosses})");
        }
        currentConsecutiveLosses = 0;
        SaveAdsProgress();
    }
}
