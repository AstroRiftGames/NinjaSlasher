using System;
using System.Collections;
using UnityEngine;

public class LifeManager : MonoBehaviourSingleton<LifeManager>
{
    private const double MaxSupportedElapsedSeconds = 365d * 24d * 60d * 60d;

    [SerializeField] private TrustedTimeService _trustedTimeService;

    public int CurrentLives { get; private set; }

    public int ConsecutiveLosses => currentConsecutiveLosses;

    private DateTime _lastLifeUsedUtc;
    private DateTime _unlimitedLivesStartUtc = DateTime.MinValue;
    private DateTime _unlimitedLivesEndUtc = DateTime.MinValue;

    [Header("VIRTUAL LIFE DEDUCTION")]
    private int _virtualLives;
    private bool _hasVirtualDeduction = false;

    private int? _cachedMaxLives;
    private int? _cachedStartingLives;
    private int? _cachedLifeRechargeSeconds;

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

    private int totalLivesLostThisSession = 0;
    private int currentConsecutiveLosses = 0;
    private bool _levelInProgress = false;
    private bool _isInitialized = false;

    private bool _lifeWallActive = false;

    private ITrustedTimeProvider _timeProvider;

    private ITrustedTimeProvider TimeProvider
    {
        get
        {
            if (_timeProvider != null)
                return _timeProvider;

            if (_trustedTimeService == null)
                _trustedTimeService = FindFirstObjectByType<TrustedTimeService>();

            _timeProvider = _trustedTimeService != null
                ? _trustedTimeService.Provider
                : null;

            return _timeProvider;
        }
    }

    private bool TryGetCurrentUtcNow(out DateTime utcNow)
    {
        utcNow = DateTime.MinValue;
        return TimeProvider != null && TimeProvider.TryGetTrustedUtcNow(out utcNow);
    }

    private DateTime GetCurrentUtcNowOrFallback()
    {
        if (TryGetCurrentUtcNow(out DateTime utcNow))
            return utcNow;

        if (_lastLifeUsedUtc > DateTime.MinValue)
            return _lastLifeUsedUtc;

        return DateTime.MinValue;
    }

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
               SaveManager.Instance == null ||
               TimeProvider == null ||
               !TimeProvider.IsInitialized)
        {
            yield return null;
        }

        yield return null;

        InitializeFromSave();

        _isInitialized = true;

        GameEvents.RaiseLivesChanged(GetDisplayLives());
    }

    #endregion

    #region UPDATE

    private void Update()
    {
        if (!_isInitialized)
            return;

        UpdateLifeRecharge();
        UpdateUnlimitedLivesState();
    }

    #endregion

    #region SAVE/LOAD

    private void InitializeFromSave()
    {
        var data = SaveManager.Instance?.GetGameData();
        ITrustedTimeProvider timeProvider = TimeProvider;
        DateTime currentUtc = GetCurrentUtcNowOrFallback();

        Debug.Log($"[LifeManager] LoadLives | savedLives={data?.currentLives} | savedTimestamp='{data?.lastLifeRegenTime}' | canRegen={data?.canRegenLives} | unlimitedLivesEndUtc={data?.unlimitedLivesEndUtc} | canApplyOfflineProgress={timeProvider != null && timeProvider.CanApplyOfflineProgress}");

        DateTime lastRegenUtc = currentUtc;
        bool validDate = data != null && DateTime.TryParse(
            data.lastLifeRegenTime, null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out lastRegenUtc);

        if (validDate)
        {
            lastRegenUtc = lastRegenUtc.Kind == DateTimeKind.Utc
                ? lastRegenUtc
                : lastRegenUtc.ToUniversalTime();

            TimeSpan timeDiff = currentUtc - lastRegenUtc;
            if (timeDiff.TotalDays < -1 || timeDiff.TotalDays > 365)
            {
                Debug.LogWarning($"[LifeManager] Fecha de regeneración corrupta: {lastRegenUtc}. Reseteando.");
                validDate = false;
            }
        }

        bool hasValidLives =
            data != null &&
            data.currentLives >= 0 &&
            data.currentLives <= MaxLives;

        bool shouldResetToStartingLives =
            data == null ||
            !hasValidLives;

        Debug.Log($"[LifeManager] InitializeFromSave | validTimestamp={validDate} | shouldResetToStartingLives={shouldResetToStartingLives} | parsedTimestamp={lastRegenUtc:O} | dataNull={data == null} | hasValidLives={hasValidLives}");

        if (shouldResetToStartingLives)
        {
            CurrentLives = Mathf.Clamp(StartingLives, 0, MaxLives);
            _lastLifeUsedUtc = currentUtc;
            Persist("Init (default)");
        }
        else
        {
            CurrentLives = Mathf.Clamp(data.currentLives, 0, MaxLives);
            if (validDate)
            {
                _lastLifeUsedUtc = lastRegenUtc;
            }
            else
            {
                _lastLifeUsedUtc = currentUtc;
                Persist("Repair missing life timestamp");
            }
        }

        if (data != null && data.unlimitedLivesEndUtc > 0)
        {
            _unlimitedLivesEndUtc = DateTimeOffset.FromUnixTimeSeconds(data.unlimitedLivesEndUtc).UtcDateTime;

            if (data.unlimitedLivesStartUtc > 0)
            {
                _unlimitedLivesStartUtc = DateTimeOffset.FromUnixTimeSeconds(data.unlimitedLivesStartUtc).UtcDateTime;
            }
            else if (currentUtc < _unlimitedLivesEndUtc)
            {
                _unlimitedLivesStartUtc = currentUtc;
                PersistUnlimitedLivesState();
            }
        }
        else
        {
            _unlimitedLivesStartUtc = DateTime.MinValue;
            _unlimitedLivesEndUtc = DateTime.MinValue;
        }

        if (timeProvider != null && timeProvider.CanApplyOfflineProgress)
        {
            CheckOfflineRegeneration();
        }
        else
        {
            ApplyConservativeOfflineTimePolicy(currentUtc, timeProvider);
        }

        _virtualLives = CurrentLives;

        Debug.Log($"[LifeManager] RuntimeLivesFinal | currentLives={CurrentLives} | displayLives={GetDisplayLives()} | timerBaseUtc={_lastLifeUsedUtc:O} | unlimitedLivesActive={HasTimedUnlimitedLives}");
    }

    private void ApplyConservativeOfflineTimePolicy(DateTime currentUtc, ITrustedTimeProvider timeProvider)
    {
        if (CurrentLives < MaxLives)
        {
            double maxFrozenElapsedSeconds = Math.Max(0d, LifeRechargeSeconds - 1d);
            if (timeProvider != null && timeProvider.TryFreezeElapsedSince(_lastLifeUsedUtc, maxFrozenElapsedSeconds, out DateTime rebasedLifeAnchorUtc))
            {
                _lastLifeUsedUtc = rebasedLifeAnchorUtc;
                Debug.Log($"[LifeManager] Offline life regeneration frozen | rebasedAnchorUtc={_lastLifeUsedUtc:O}");
            }
            else
            {
                _lastLifeUsedUtc = currentUtc;
                Debug.LogWarning("[LifeManager] Offline life regeneration disabled because trusted continuity was unavailable. Timer progress was reset conservatively.");
            }
        }

        if (_unlimitedLivesEndUtc <= DateTime.MinValue)
            return;

        DateTime savedUnlimitedStartUtc = _unlimitedLivesStartUtc;
        DateTime savedUnlimitedEndUtc = _unlimitedLivesEndUtc;

        if (timeProvider == null ||
            !timeProvider.TryFreezeRemainingDuration(savedUnlimitedEndUtc, MaxSupportedElapsedSeconds, out DateTime rebasedUnlimitedEndUtc))
        {
            ClearUnlimitedLivesState();
            Debug.LogWarning("[LifeManager] Unlimited lives expired conservatively because trusted continuity was unavailable.");
            return;
        }

        double remainingUnlimitedSeconds = (rebasedUnlimitedEndUtc - currentUtc).TotalSeconds;
        if (remainingUnlimitedSeconds <= 0d)
        {
            ClearUnlimitedLivesState();
            return;
        }

        _unlimitedLivesEndUtc = rebasedUnlimitedEndUtc;

        if (savedUnlimitedStartUtc > DateTime.MinValue)
        {
            double savedTotalDurationSeconds = (savedUnlimitedEndUtc - savedUnlimitedStartUtc).TotalSeconds;
            if (savedTotalDurationSeconds > 0d && savedTotalDurationSeconds <= MaxSupportedElapsedSeconds)
            {
                _unlimitedLivesStartUtc = _unlimitedLivesEndUtc.AddSeconds(-savedTotalDurationSeconds);
            }
            else
            {
                _unlimitedLivesStartUtc = currentUtc;
            }
        }
        else
        {
            _unlimitedLivesStartUtc = currentUtc;
        }

        Debug.Log($"[LifeManager] Offline unlimited lives frozen | remainingSeconds={remainingUnlimitedSeconds:F0} | rebasedEndUtc={_unlimitedLivesEndUtc:O}");
    }

    private void ClearUnlimitedLivesState()
    {
        _unlimitedLivesStartUtc = DateTime.MinValue;
        _unlimitedLivesEndUtc = DateTime.MinValue;
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
        if (!TryGetCurrentUtcNow(out DateTime currentUtc)) return;

        try
        {
            double seconds = (currentUtc - _lastLifeUsedUtc).TotalSeconds;

            if (seconds < 0 || seconds > MaxSupportedElapsedSeconds)
            {
                Debug.LogWarning($"[LifeManager] Tiempo desde última vida inválido: {seconds}s. Reseteando.");
                _lastLifeUsedUtc = currentUtc;
                Persist("Reset por tiempo inválido");
                return;
            }

            if (seconds < LifeRechargeSeconds) return;

            int toGenerate = Mathf.FloorToInt((float)seconds / LifeRechargeSeconds);
            int newLives = Mathf.Min(CurrentLives + toGenerate, MaxLives);

            bool wasAtWall = _lifeWallActive && CurrentLives == 0;

            double secondsToAdd = toGenerate * LifeRechargeSeconds;
            if (secondsToAdd > int.MaxValue)
            {
                Debug.LogWarning("[LifeManager] Overflow detectado. Llenando vidas.");
                CurrentLives = MaxLives;
                _lastLifeUsedUtc = currentUtc;
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
                    LifeRestoreSource.TimeRegeneration
                );
            }

            if (wasAtWall && CurrentLives > 0)
            {
                _lifeWallActive = false;
                int wallLevelId = LevelSessionManager.Instance?.CurrentSession?.LevelId ?? 0;
                AnalyticsManager.Instance?.RecordLifeWallResolved(LifeWallOutcome.Waited, wallLevelId);
            }

            Persist("Vida regenerada");
            EmitDisplayLivesChanged();
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.LogError($"[LifeManager] Error en UpdateLifeRecharge: {e.Message}. Reseteando.");
            _lastLifeUsedUtc = currentUtc;
            CurrentLives = MaxLives;
            _virtualLives = CurrentLives;
            Persist("Reset por error");
            EmitDisplayLivesChanged();
        }
    }

    private void CheckOfflineRegeneration()
    {
        if (CurrentLives >= MaxLives) return;
        if (!TryGetCurrentUtcNow(out DateTime currentUtc)) return;

        try
        {
            int savedLives = CurrentLives;
            double seconds = (currentUtc - _lastLifeUsedUtc).TotalSeconds;

            if (seconds < 0 || seconds > MaxSupportedElapsedSeconds)
            {
                Debug.LogWarning($"[LifeManager] Tiempo offline inválido: {seconds}s. Reseteando.");
                _lastLifeUsedUtc = currentUtc;
                return;
            }

            if (seconds < LifeRechargeSeconds)
            {
                Debug.Log($"[LifeManager] OfflineRegen | savedLives={savedLives} | elapsedSeconds={seconds:F0} | generated=0 | resultLives={CurrentLives} | nextTimestamp='{_lastLifeUsedUtc:O}'");
                return;
            }

            int toGenerate = Mathf.FloorToInt((float)seconds / LifeRechargeSeconds);
            int newLives = Mathf.Min(CurrentLives + toGenerate, MaxLives);

            double secondsToAdd = toGenerate * LifeRechargeSeconds;
            if (secondsToAdd > int.MaxValue)
            {
                CurrentLives = MaxLives;
                _lastLifeUsedUtc = currentUtc;
                _virtualLives = CurrentLives;
                Persist("Vidas completas (overflow offline)");
                EmitDisplayLivesChanged();
                return;
            }

            _lastLifeUsedUtc = _lastLifeUsedUtc.AddSeconds(secondsToAdd);
            CurrentLives = newLives;

            _virtualLives = _hasVirtualDeduction ? Mathf.Max(0, CurrentLives - 1) : CurrentLives;

            Debug.Log($"[LifeManager] OfflineRegen | savedLives={savedLives} | elapsedSeconds={seconds:F0} | generated={toGenerate} | resultLives={CurrentLives} | nextTimestamp='{_lastLifeUsedUtc:O}'");

            Persist("Vidas offline regeneradas");
            EmitDisplayLivesChanged();
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.LogError($"[LifeManager] Error en CheckOfflineRegeneration: {e.Message}. Reseteando.");
            _lastLifeUsedUtc = currentUtc;
            CurrentLives = MaxLives;
            _virtualLives = CurrentLives;
            Persist("Reset por error offline");
            EmitDisplayLivesChanged();
        }
    }

    #endregion

    #region PUBLIC API

    public bool IsInitialized => _isInitialized;

    public bool HasTimedUnlimitedLives
    {
        get
        {
            DateTime currentUtc = GetCurrentUtcNowOrFallback();
            return currentUtc > DateTime.MinValue && currentUtc < _unlimitedLivesEndUtc;
        }
    }

    public void ActivateUnlimitedLives(float durationMinutes)
    {
        DateTime currentUtc = GetCurrentUtcNowOrFallback();
        DateTime baseTime = HasTimedUnlimitedLives ? _unlimitedLivesEndUtc : currentUtc;
        _unlimitedLivesStartUtc = currentUtc;
        _unlimitedLivesEndUtc = baseTime.AddMinutes(durationMinutes);

        PersistUnlimitedLivesState();

        Debug.Log($"[LifeManager] ActivateUnlimitedLives: {durationMinutes} min | " +
                  $"StartUtc={_unlimitedLivesStartUtc:O} | " +
                  $"EndUtc={_unlimitedLivesEndUtc:O} | " +
                  $"HasTimedUnlimitedLives={HasTimedUnlimitedLives} | " +
                  $"SaveManager={(SaveManager.Instance != null ? "OK" : "NULL")}");

        EmitDisplayLivesChanged();
    }

    public TimeSpan GetUnlimitedLivesRemainingTime()
    {
        if (!HasTimedUnlimitedLives) return TimeSpan.Zero;
        return _unlimitedLivesEndUtc - GetCurrentUtcNowOrFallback();
    }

    public float GetUnlimitedLivesFillAmount()
    {
        if (!HasTimedUnlimitedLives)
            return 0f;

        double totalSeconds = (_unlimitedLivesEndUtc - _unlimitedLivesStartUtc).TotalSeconds;
        if (totalSeconds <= 0d)
            return 0f;

        double remainingSeconds = (_unlimitedLivesEndUtc - GetCurrentUtcNowOrFallback()).TotalSeconds;
        return Mathf.Clamp01((float)(remainingSeconds / totalSeconds));
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

    public bool CanPlayAfterConfirmingPendingDeduction()
    {
        if (HasTimedUnlimitedLives) return true;

        if (GameConfigManager.IsReady() && GameConfigManager.Config.infiniteLives)
        {
            return true;
        }

        if (!_hasVirtualDeduction)
            return CurrentLives > 0;

        return _virtualLives > 0;
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
            _lastLifeUsedUtc = GetCurrentUtcNowOrFallback();

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

            IncrementLossCounter();

            Persist("Vida perdida (confirmada)");
            EmitDisplayLivesChanged();

            if (CurrentLives == 0 && !_lifeWallActive)
            {
                _lifeWallActive = true;
                int wallLevelId = LevelSessionManager.Instance?.CurrentSession?.LevelId ?? 0;
                AnalyticsManager.Instance?.RecordLifeWallEncountered(wallLevelId);
            }
        }
        else
        {
            if (CurrentLives <= 0) return;

            CurrentLives = Mathf.Max(0, CurrentLives - 1);
            _lastLifeUsedUtc = GetCurrentUtcNowOrFallback();
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

            IncrementLossCounter();

            Persist("Vida perdida (directa)");
            EmitDisplayLivesChanged();

            if (CurrentLives == 0 && !_lifeWallActive)
            {
                _lifeWallActive = true;
                int wallLevelId = LevelSessionManager.Instance?.CurrentSession?.LevelId ?? 0;
                AnalyticsManager.Instance?.RecordLifeWallEncountered(wallLevelId);
            }
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
                _lastLifeUsedUtc = GetCurrentUtcNowOrFallback();

                _hasVirtualDeduction = false;
                _levelInProgress = false;

                Persist("Vida perdida por abandono");
                EmitDisplayLivesChanged();
            }
        }
    }

    public int GetDisplayLives() => _hasVirtualDeduction ? _virtualLives : CurrentLives;
    public int GetRealLives() => CurrentLives;

    public void AddLife(LifeRestoreSource source = LifeRestoreSource.Unknown)
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
                source
            );
        }

        if (_lifeWallActive)
        {
            _lifeWallActive = false;
            int wallLevelId = LevelSessionManager.Instance?.CurrentSession?.LevelId ?? 0;
            AnalyticsManager.Instance?.RecordLifeWallResolved(WallOutcomeFromSource(source), wallLevelId);
        }

        Persist("Vida ganada");
        EmitDisplayLivesChanged();
    }

    public void FillAllLives()
    {
        if (CurrentLives >= MaxLives) return;

        CurrentLives = MaxLives;
        _lastLifeUsedUtc = GetCurrentUtcNowOrFallback();

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
            if (!TryGetCurrentUtcNow(out DateTime currentUtc))
                return 0f;

            double seconds = (currentUtc - _lastLifeUsedUtc).TotalSeconds;
            if (seconds < 0 || seconds > MaxSupportedElapsedSeconds)
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
            if (!TryGetCurrentUtcNow(out DateTime currentUtc))
                return TimeSpan.Zero;

            double seconds = (currentUtc - _lastLifeUsedUtc).TotalSeconds;

            if (seconds < 0 || seconds > MaxSupportedElapsedSeconds)
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

    public void NotifyLifeWallAbandoned()
    {
        if (!_lifeWallActive) return;

        _lifeWallActive = false;
        int wallLevelId = LevelSessionManager.Instance?.CurrentSession?.LevelId ?? 0;
        AnalyticsManager.Instance?.RecordLifeWallResolved(LifeWallOutcome.Abandoned, wallLevelId);
    }

    #endregion

    #region EVENTS

    private void OnApplicationFocus(bool hasFocus) { }

    private void EmitDisplayLivesChanged()
    {
        int displayLives = GetDisplayLives();
        GameEvents.RaiseLivesChanged(displayLives);
    }

    private void UpdateUnlimitedLivesState()
    {
        if (_unlimitedLivesEndUtc == DateTime.MinValue)
            return;

        DateTime currentUtc = GetCurrentUtcNowOrFallback();
        if (currentUtc > DateTime.MinValue && currentUtc < _unlimitedLivesEndUtc)
            return;

        ClearUnlimitedLivesState();
        PersistUnlimitedLivesState();
        EmitDisplayLivesChanged();
    }

    private void PersistUnlimitedLivesState()
    {
        if (SaveManager.Instance == null)
            return;

        long startUtcSeconds = _unlimitedLivesStartUtc > DateTime.MinValue
            ? new DateTimeOffset(_unlimitedLivesStartUtc).ToUnixTimeSeconds()
            : 0L;

        long endUtcSeconds = _unlimitedLivesEndUtc > DateTime.MinValue
            ? new DateTimeOffset(_unlimitedLivesEndUtc).ToUnixTimeSeconds()
            : 0L;

        SaveManager.Instance.Modify(d =>
        {
            d.unlimitedLivesStartUtc = startUtcSeconds;
            d.unlimitedLivesEndUtc = endUtcSeconds;
        });
    }

    #endregion

    #region ADS

    private void IncrementLossCounter()
    {
        currentConsecutiveLosses++;
    }

    private void ResetLossCounter()
    {
        if (currentConsecutiveLosses > 0)
        {
            Debug.Log($"[LifeManager] Contador de derrotas reseteado (era: {currentConsecutiveLosses})");
        }
        currentConsecutiveLosses = 0;
    }

    private static LifeWallOutcome WallOutcomeFromSource(LifeRestoreSource source) => source switch
    {
        LifeRestoreSource.AdReward        => LifeWallOutcome.AdReward,
        LifeRestoreSource.IapPurchase     => LifeWallOutcome.IapPurchase,
        LifeRestoreSource.EmergencyBundle => LifeWallOutcome.IapPurchase,
        _                                 => LifeWallOutcome.Waited
    };

    #endregion
}
