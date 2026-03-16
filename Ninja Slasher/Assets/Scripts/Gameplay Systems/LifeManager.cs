using System;
using System.Collections;
using UnityEngine;

public class LifeManager : MonoBehaviourSingleton<LifeManager>
{
    public int CurrentLives { get; private set; }

    public int ConsecutiveLosses => currentConsecutiveLosses;

    private DateTime _lastLifeUsedUtc;
    private DateTime _unlimitedLivesEndUtc = DateTime.MinValue;

    [Header("VIRTUAL LIFE DEDUCTION")]
    private int _virtualLives;
    private bool _hasVirtualDeduction = false;

    private int? _cachedMaxLives;
    private int? _cachedStartingLives;
    private int? _cachedLifeRechargeSeconds;
    private int? _cachedLossesRequiredForAd;
    private bool? _cachedEnableConsecutiveLossAds;
    private bool? _cachedEnableNoLivesAds;

    private int MaxLives
    {
        get
        {
            if (_cachedMaxLives.HasValue)
                return _cachedMaxLives.Value;

            if (GameConfigManager.IsReady())
            {
                _cachedMaxLives = GameConfigManager.Config.maxLives;
                return _cachedMaxLives.Value;
            }

            return 5;
        }
    }

    private int StartingLives
    {
        get
        {
            if (_cachedStartingLives.HasValue)
                return _cachedStartingLives.Value;

            if (GameConfigManager.IsReady())
            {
                _cachedStartingLives = GameConfigManager.Config.startingLives;
                return _cachedStartingLives.Value;
            }

            return 5;
        }
    }

    private int LifeRechargeSeconds
    {
        get
        {
            if (_cachedLifeRechargeSeconds.HasValue)
                return _cachedLifeRechargeSeconds.Value;

            if (GameConfigManager.IsReady())
            {
                _cachedLifeRechargeSeconds = GameConfigManager.Config.lifeRechargeSeconds;
                return _cachedLifeRechargeSeconds.Value;
            }

            return 1800;
        }
    }

    private int LossesRequiredForAd
    {
        get
        {
            if (_cachedLossesRequiredForAd.HasValue)
                return _cachedLossesRequiredForAd.Value;

            if (GameConfigManager.IsReady())
            {
                _cachedLossesRequiredForAd = GameConfigManager.Config.lossesRequiredForAd;
                return _cachedLossesRequiredForAd.Value;
            }

            return 2;
        }
    }

    private bool EnableConsecutiveLossAds
    {
        get
        {
            if (_cachedEnableConsecutiveLossAds.HasValue)
                return _cachedEnableConsecutiveLossAds.Value;

            if (GameConfigManager.IsReady())
            {
                _cachedEnableConsecutiveLossAds = GameConfigManager.Config.enableConsecutiveLossAds;
                return _cachedEnableConsecutiveLossAds.Value;
            }

            return true;
        }
    }

    private bool EnableNoLivesAds
    {
        get
        {
            if (_cachedEnableNoLivesAds.HasValue)
                return _cachedEnableNoLivesAds.Value;

            if (GameConfigManager.IsReady())
            {
                _cachedEnableNoLivesAds = GameConfigManager.Config.enableNoLivesAds;
                return _cachedEnableNoLivesAds.Value;
            }

            return true;
        }
    }

    private int totalLivesLostThisSession = 0;
    private int currentConsecutiveLosses = 0;
    private bool _levelInProgress = false;
    private bool _isInitialized = false;

    public event Action<int> OnLivesChanged;

    #region INITIALIZATION

    public override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        while (!GameConfigManager.IsReady() ||
               SaveManager.Instance == null)
        {
            yield return null;
        }

        yield return null;

        InitializeFromSave();
        LoadAdsProgress();

        _isInitialized = true;

        OnLivesChanged?.Invoke(GetDisplayLives());
    }

    #endregion

    #region UPDATE

    private void Update()
    {
        if (!_isInitialized)
            return;

        UpdateLifeRecharge();
    }

    #endregion

    #region SAVE/LOAD

    private void InitializeFromSave()
    {
        var data = SaveManager.Instance?.GetGameData();

        DateTime lastRegenUtc = DateTime.UtcNow;
        bool validDate = data != null && DateTime.TryParse(data.lastLifeRegenTime, out lastRegenUtc);

        if (validDate)
        {
            TimeSpan timeDiff = DateTime.UtcNow - lastRegenUtc;
            if (timeDiff.TotalDays < -1 || timeDiff.TotalDays > 365)
            {
                Debug.LogWarning($"[LifeManager] Fecha de regeneración corrupta: {lastRegenUtc}. Reseteando.");
                validDate = false;
            }
        }

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

        if (data != null && data.unlimitedLivesEndUtc > 0)
            _unlimitedLivesEndUtc = DateTimeOffset.FromUnixTimeSeconds(data.unlimitedLivesEndUtc).UtcDateTime;

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
    }

    #endregion

    #region LIFE REGENERATION

    private void UpdateLifeRecharge()
    {
        if (CurrentLives >= MaxLives) return;

        try
        {
            double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;

            if (seconds < 0 || seconds > (365 * 24 * 60 * 60))
            {
                Debug.LogWarning($"[LifeManager] Tiempo desde última vida inválido: {seconds}s. Reseteando.");
                _lastLifeUsedUtc = DateTime.UtcNow;
                Persist("Reset por tiempo inválido");
                return;
            }

            if (seconds < LifeRechargeSeconds) return;

            int toGenerate = Mathf.FloorToInt((float)seconds / LifeRechargeSeconds);
            int newLives = Mathf.Min(CurrentLives + toGenerate, MaxLives);

            double secondsToAdd = toGenerate * LifeRechargeSeconds;
            if (secondsToAdd > int.MaxValue)
            {
                Debug.LogWarning("[LifeManager] Overflow detectado. Llenando vidas.");
                CurrentLives = MaxLives;
                _lastLifeUsedUtc = DateTime.UtcNow;
                _virtualLives = CurrentLives;
                Persist("Reset por overflow");
                EmitDisplayLivesChanged();
                return;
            }

            _lastLifeUsedUtc = _lastLifeUsedUtc.AddSeconds(secondsToAdd);
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
        catch (ArgumentOutOfRangeException e)
        {
            Debug.LogError($"[LifeManager] Error en UpdateLifeRecharge: {e.Message}. Reseteando.");
            _lastLifeUsedUtc = DateTime.UtcNow;
            CurrentLives = MaxLives;
            _virtualLives = CurrentLives;
            Persist("Reset por error");
            EmitDisplayLivesChanged();
        }
    }

    private void CheckOfflineRegeneration()
    {
        if (CurrentLives >= MaxLives) return;

        try
        {
            double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;

            if (seconds < 0 || seconds > (365 * 24 * 60 * 60))
            {
                Debug.LogWarning($"[LifeManager] Tiempo offline inválido: {seconds}s. Reseteando.");
                _lastLifeUsedUtc = DateTime.UtcNow;
                return;
            }

            if (seconds < LifeRechargeSeconds) return;

            int toGenerate = Mathf.FloorToInt((float)seconds / LifeRechargeSeconds);
            int newLives = Mathf.Min(CurrentLives + toGenerate, MaxLives);

            double secondsToAdd = toGenerate * LifeRechargeSeconds;
            if (secondsToAdd > int.MaxValue)
            {
                CurrentLives = MaxLives;
                _lastLifeUsedUtc = DateTime.UtcNow;
                _virtualLives = CurrentLives;
                Persist("Vidas completas (overflow offline)");
                EmitDisplayLivesChanged();
                return;
            }

            _lastLifeUsedUtc = _lastLifeUsedUtc.AddSeconds(secondsToAdd);
            CurrentLives = newLives;

            _virtualLives = _hasVirtualDeduction ? Mathf.Max(0, CurrentLives - 1) : CurrentLives;

            Persist("Vidas offline regeneradas");
            EmitDisplayLivesChanged();
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.LogError($"[LifeManager] Error en CheckOfflineRegeneration: {e.Message}. Reseteando.");
            _lastLifeUsedUtc = DateTime.UtcNow;
            CurrentLives = MaxLives;
            _virtualLives = CurrentLives;
            Persist("Reset por error offline");
            EmitDisplayLivesChanged();
        }
    }

    #endregion

    #region PUBLIC API

    public bool HasTimedUnlimitedLives => DateTime.UtcNow < _unlimitedLivesEndUtc;

    public void ActivateUnlimitedLives(float durationMinutes)
    {
        DateTime baseTime = HasTimedUnlimitedLives ? _unlimitedLivesEndUtc : DateTime.UtcNow;
        _unlimitedLivesEndUtc = baseTime.AddMinutes(durationMinutes);

        if (SaveManager.Instance != null)
        {
            long endUtcSeconds = new DateTimeOffset(_unlimitedLivesEndUtc).ToUnixTimeSeconds();
            SaveManager.Instance.Modify(d => d.unlimitedLivesEndUtc = endUtcSeconds);
        }

        Debug.Log($"[LifeManager] ActivateUnlimitedLives: {durationMinutes} min | " +
                  $"EndUtc={_unlimitedLivesEndUtc:O} | " +
                  $"HasTimedUnlimitedLives={HasTimedUnlimitedLives} | " +
                  $"SaveManager={(SaveManager.Instance != null ? "OK" : "NULL")}");

        EmitDisplayLivesChanged();
    }

    public TimeSpan GetUnlimitedLivesRemainingTime()
    {
        if (!HasTimedUnlimitedLives) return TimeSpan.Zero;
        return _unlimitedLivesEndUtc - DateTime.UtcNow;
    }

    public bool CanPlay()
    {
        if (HasTimedUnlimitedLives) return true;

        if (GameConfigManager.IsReady() && GameConfigManager.Config.infiniteLives)
        {
            return true;
        }

        return CurrentLives > 0;
    }

    public void OnLevelStart()
    {
        _hasVirtualDeduction = false;

        if (!HasTimedUnlimitedLives && CurrentLives > 0)
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

        if (HasTimedUnlimitedLives)
        {
            Debug.Log($"[LifeManager] UseLife BLOCKED — unlimited lives active. Remaining={GetUnlimitedLivesRemainingTime():mm\\:ss}");
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
            if (HasTimedUnlimitedLives)
            {
                _virtualLives = CurrentLives;
                _hasVirtualDeduction = false;
                _levelInProgress = false;
                EmitDisplayLivesChanged();
            }
            else
            {
                CurrentLives = Mathf.Clamp(_virtualLives, 0, MaxLives);
                _lastLifeUsedUtc = DateTime.UtcNow;

                _hasVirtualDeduction = false;
                _levelInProgress = false;

                Persist("Vida perdida por abandono");
                EmitDisplayLivesChanged();
            }
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

        try
        {
            double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;
            if (seconds < 0 || seconds > (365 * 24 * 60 * 60))
                return 0f;

            return Mathf.Clamp01((float)(seconds / LifeRechargeSeconds));
        }
        catch
        {
            return 0f;
        }
    }

    public TimeSpan GetTimeToNextLife()
    {
        if (CurrentLives >= MaxLives) return TimeSpan.Zero;

        try
        {
            double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;

            if (seconds < 0 || seconds > (365 * 24 * 60 * 60))
                return TimeSpan.Zero;

            double secondsLeft = LifeRechargeSeconds - seconds;
            return TimeSpan.FromSeconds(Mathf.Max(0, (float)secondsLeft));
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    public bool HasPendingDeduction() => _hasVirtualDeduction;

    #endregion

    #region EVENTS

    private void OnApplicationFocus(bool hasFocus) { }

    private void EmitDisplayLivesChanged()
    {
        int displayLives = GetDisplayLives();
        GameEvents.RaiseLivesChanged(displayLives);
    }

    #endregion

    #region ADS

    private void LoadAdsProgress()
    {
        if (SaveManager.Instance != null)
            currentConsecutiveLosses = SaveManager.Instance.GetGameData().consecutiveLosses;
        else
            currentConsecutiveLosses = PlayerPrefs.GetInt("ConsecutiveLosses", 0);
    }

    private void SaveAdsProgress()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.Modify(d => d.consecutiveLosses = currentConsecutiveLosses);
        else
        {
            PlayerPrefs.SetInt("ConsecutiveLosses", currentConsecutiveLosses);
            PlayerPrefs.Save();
        }
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
            Debug.Log($"[LifeManager] Contador de derrotas reseteado (era: {currentConsecutiveLosses})");
        }
        currentConsecutiveLosses = 0;
        SaveAdsProgress();
    }

    #endregion
}