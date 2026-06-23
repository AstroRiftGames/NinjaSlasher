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

    private bool CanApplyOfflineLifeProgress(ITrustedTimeProvider timeProvider)
    {
        return timeProvider != null &&
               timeProvider.IsInitialized &&
               timeProvider.CanApplyOfflineProgress &&
               !timeProvider.HasSuspiciousTimeJump;
    }

    private void UpdateVirtualLivesFromCurrentLives()
    {
        _virtualLives = _hasVirtualDeduction
            ? Mathf.Max(0, CurrentLives - 1)
            : CurrentLives;
    }

    private void UpdateRechargeAnchorAfterLifeSpent(int previousLives, DateTime currentUtc)
    {
        if (CurrentLives >= MaxLives)
        {
            _lastLifeUsedUtc = DateTime.MinValue;
            return;
        }

        if (previousLives >= MaxLives || _lastLifeUsedUtc <= DateTime.MinValue)
            _lastLifeUsedUtc = currentUtc;
    }

    private bool TrySynchronizeLivesWithTrustedTime(
        string persistReason,
        bool emitChange = true,
        bool requireOfflineValidation = false)
    {
        if (requireOfflineValidation && !CanApplyOfflineLifeProgress(TimeProvider))
            return false;

        if (CurrentLives >= MaxLives)
        {
            if (_lastLifeUsedUtc <= DateTime.MinValue)
                return false;

            _lastLifeUsedUtc = DateTime.MinValue;
            Persist(persistReason);
            return false;
        }

        if (!TryGetCurrentUtcNow(out DateTime currentUtc))
            return false;

        if (_lastLifeUsedUtc <= DateTime.MinValue)
        {
            _lastLifeUsedUtc = currentUtc;
            Persist("Repair missing life timer");
            return false;
        }

        try
        {
            double elapsedSeconds = (currentUtc - _lastLifeUsedUtc).TotalSeconds;

            if (elapsedSeconds < 0d || elapsedSeconds > MaxSupportedElapsedSeconds)
            {
                Debug.LogWarning($"[LifeManager] Tiempo de recarga invÃ¡lido: {elapsedSeconds}s. Reseteando timer.");
                _lastLifeUsedUtc = currentUtc;
                Persist("Reset por tiempo invÃ¡lido");
                return false;
            }

            int recoveredLives = Mathf.FloorToInt((float)(elapsedSeconds / LifeRechargeSeconds));
            if (recoveredLives <= 0)
                return false;

            int missingLives = MaxLives - CurrentLives;
            if (recoveredLives > missingLives)
                recoveredLives = missingLives;

            int previousLives = CurrentLives;
            CurrentLives += recoveredLives;

            if (CurrentLives >= MaxLives)
            {
                _lastLifeUsedUtc = DateTime.MinValue;
            }
            else
            {
                double consumedSeconds = recoveredLives * (double)LifeRechargeSeconds;
                _lastLifeUsedUtc = _lastLifeUsedUtc.AddSeconds(consumedSeconds);
            }

            UpdateVirtualLivesFromCurrentLives();

            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.RecordLifeRestored(
                    CurrentLives,
                    LifeRestoreSource.TimeRegeneration);
            }

            if (_lifeWallActive && previousLives == 0 && CurrentLives > 0)
            {
                _lifeWallActive = false;
                int wallLevelId = LevelSessionManager.Instance?.CurrentSession?.LevelId ?? 0;
                AnalyticsManager.Instance?.RecordLifeWallResolved(LifeWallOutcome.Waited, wallLevelId);
            }

            Persist(persistReason);
            if (emitChange)
                EmitDisplayLivesChanged();
            return true;
        }
        catch (ArgumentOutOfRangeException e)
        {
            Debug.LogError($"[LifeManager] Error al sincronizar vidas: {e.Message}. Reseteando timer.");
            _lastLifeUsedUtc = currentUtc;
            Persist("Reset por error");
            return false;
        }
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LifeManager] LoadLives | savedLives={data?.currentLives} | savedTimestamp='{data?.lastLifeRegenTime}' | canRegen={data?.canRegenLives} | unlimitedLivesEndUtc={data?.unlimitedLivesEndUtc} | canApplyOfflineProgress={timeProvider != null && timeProvider.CanApplyOfflineProgress}");
#endif

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LifeManager] InitializeFromSave | validTimestamp={validDate} | shouldResetToStartingLives={shouldResetToStartingLives} | parsedTimestamp={lastRegenUtc:O} | dataNull={data == null} | hasValidLives={hasValidLives}");
#endif

        if (shouldResetToStartingLives)
        {
            CurrentLives = Mathf.Clamp(StartingLives, 0, MaxLives);
            _lastLifeUsedUtc = CurrentLives < MaxLives ? currentUtc : DateTime.MinValue;
            Persist("Init (default)");
        }
        else
        {
            CurrentLives = Mathf.Clamp(data.currentLives, 0, MaxLives);
            if (CurrentLives >= MaxLives)
            {
                _lastLifeUsedUtc = DateTime.MinValue;
            }
            else if (validDate)
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

        if (CanApplyOfflineLifeProgress(timeProvider))
        {
            CheckOfflineRegeneration();
        }
        else
        {
            ApplyConservativeOfflineTimePolicy(currentUtc, timeProvider);
        }

        UpdateVirtualLivesFromCurrentLives();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LifeManager] RuntimeLivesFinal | currentLives={CurrentLives} | displayLives={GetDisplayLives()} | timerBaseUtc={_lastLifeUsedUtc:O} | unlimitedLivesActive={HasTimedUnlimitedLives}");
#endif
    }

    private void ApplyConservativeOfflineTimePolicy(DateTime currentUtc, ITrustedTimeProvider timeProvider)
    {
        if (CurrentLives >= MaxLives)
        {
            _lastLifeUsedUtc = DateTime.MinValue;
        }
        else if (_lastLifeUsedUtc <= DateTime.MinValue)
        {
            _lastLifeUsedUtc = currentUtc;
        }
        else
        {
            double maxFrozenElapsedSeconds = Math.Max(0d, LifeRechargeSeconds - 1d);
            if (timeProvider != null && timeProvider.TryFreezeElapsedSince(_lastLifeUsedUtc, maxFrozenElapsedSeconds, out DateTime rebasedLifeAnchorUtc))
            {
                _lastLifeUsedUtc = rebasedLifeAnchorUtc;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[LifeManager] Offline life regeneration frozen | rebasedAnchorUtc={_lastLifeUsedUtc:O}");
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LifeManager] Offline unlimited lives frozen | remainingSeconds={remainingUnlimitedSeconds:F0} | rebasedEndUtc={_unlimitedLivesEndUtc:O}");
#endif
    }

    private void ClearUnlimitedLivesState()
    {
        _unlimitedLivesStartUtc = DateTime.MinValue;
        _unlimitedLivesEndUtc = DateTime.MinValue;
    }

    private void Persist(string reason = "Autosave")
    {
        bool hasTimer = CurrentLives < MaxLives && _lastLifeUsedUtc > DateTime.MinValue;
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
        TrySynchronizeLivesWithTrustedTime("Vida regenerada");
    }

    private void CheckOfflineRegeneration()
    {
        TrySynchronizeLivesWithTrustedTime(
            "Vidas offline regeneradas",
            emitChange: true,
            requireOfflineValidation: true);
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

    public void ReloadFromSaveForExternalGrant()
    {
        if (SaveManager.Instance == null)
            return;

        InitializeFromSave();
        EmitDisplayLivesChanged();
    }

    public void ActivateUnlimitedLives(float durationMinutes)
    {
        DateTime currentUtc = GetCurrentUtcNowOrFallback();
        DateTime baseTime = HasTimedUnlimitedLives ? _unlimitedLivesEndUtc : currentUtc;
        _unlimitedLivesStartUtc = currentUtc;
        _unlimitedLivesEndUtc = baseTime.AddMinutes(durationMinutes);

        PersistUnlimitedLivesState();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LifeManager] ActivateUnlimitedLives: {durationMinutes} min | " +
                  $"StartUtc={_unlimitedLivesStartUtc:O} | " +
                  $"EndUtc={_unlimitedLivesEndUtc:O} | " +
                  $"HasTimedUnlimitedLives={HasTimedUnlimitedLives} | " +
                  $"SaveManager={(SaveManager.Instance != null ? "OK" : "NULL")}");
#endif

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
        if (GameConfigManager.IsTrailerCaptureModeEnabled()) return true;

        if (HasTimedUnlimitedLives) return true;

        if (GameConfigManager.IsReady() && GameConfigManager.Config.infiniteLives)
        {
            return true;
        }

        return CurrentLives > 0;
    }

    public bool CanPlayAfterConfirmingPendingDeduction()
    {
        if (GameConfigManager.IsTrailerCaptureModeEnabled()) return true;

        if (HasTimedUnlimitedLives) return true;

        if (GameConfigManager.IsReady() && GameConfigManager.Config.infiniteLives)
        {
            return true;
        }

        if (!_hasVirtualDeduction)
            return CurrentLives > 0;

        return _virtualLives > 0;
    }

    public bool CanContinueCurrentAttempt()
    {
        return CanPlayAfterConfirmingPendingDeduction();
    }

    public bool RequiresLifeRecoveryForCurrentAttempt()
    {
        return !CanContinueCurrentAttempt();
    }

    public int GetEffectiveLivesForCurrentAttempt()
    {
        if (GameConfigManager.IsTrailerCaptureModeEnabled())
            return MaxLives;

        return Mathf.Clamp(GetDisplayLives(), 0, MaxLives);
    }

    public int GetPendingDeductionCost()
    {
        if (GameConfigManager.IsTrailerCaptureModeEnabled())
            return 0;

        if (!_hasVirtualDeduction)
            return 0;

        return Mathf.Clamp(CurrentLives - _virtualLives, 0, MaxLives);
    }

    public int GetMaxLives()
    {
        return MaxLives;
    }

    public void OnLevelStart()
    {
        _hasVirtualDeduction = false;

        if (GameConfigManager.IsTrailerCaptureModeEnabled())
        {
            _virtualLives = CurrentLives;
            _levelInProgress = true;
            EmitDisplayLivesChanged();
            return;
        }

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
        if (GameConfigManager.IsTrailerCaptureModeEnabled())
        {
            _hasVirtualDeduction = false;
            _levelInProgress = false;
            EmitDisplayLivesChanged();
            return;
        }

        var context = PowerUpManager.Instance?.context;
        if (context != null && context.SecondChanceActive)
        {
            if (_hasVirtualDeduction)
            {
                _hasVirtualDeduction = false;
                _levelInProgress = false;
            }
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
            int previousLives = CurrentLives;
            CurrentLives = Mathf.Clamp(_virtualLives, 0, MaxLives);
            DateTime currentUtc = GetCurrentUtcNowOrFallback();
            UpdateRechargeAnchorAfterLifeSpent(previousLives, currentUtc);

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

            int previousLives = CurrentLives;
            CurrentLives = Mathf.Max(0, CurrentLives - 1);
            DateTime currentUtc = GetCurrentUtcNowOrFallback();
            UpdateRechargeAnchorAfterLifeSpent(previousLives, currentUtc);
            UpdateVirtualLivesFromCurrentLives();

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
        if (GameConfigManager.IsTrailerCaptureModeEnabled())
        {
            _virtualLives = CurrentLives;
            _hasVirtualDeduction = false;
            _levelInProgress = false;
            EmitDisplayLivesChanged();
            return;
        }

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
                int previousLives = CurrentLives;
                CurrentLives = Mathf.Clamp(_virtualLives, 0, MaxLives);
                DateTime currentUtc = GetCurrentUtcNowOrFallback();
                UpdateRechargeAnchorAfterLifeSpent(previousLives, currentUtc);

                _hasVirtualDeduction = false;
                _levelInProgress = false;

                Persist("Vida perdida por abandono");
                EmitDisplayLivesChanged();
            }
        }
    }

    public int GetDisplayLives() => GameConfigManager.IsTrailerCaptureModeEnabled()
        ? MaxLives
        : _hasVirtualDeduction ? _virtualLives : CurrentLives;
    public int GetRealLives() => CurrentLives;

    public void AddLife(LifeRestoreSource source = LifeRestoreSource.Unknown)
    {
        GrantExternalLife(source);
    }

    public void GrantExternalLife(LifeRestoreSource source = LifeRestoreSource.Unknown)
    {
        int livesBefore = CurrentLives;
        if (CurrentLives >= MaxLives) return;

        // Manual grants must not reset recharge progress. The recharge anchor only
        // moves when a real life is consumed, a natural regeneration is applied,
        // or an invalid time state is normalized.
        DateTime rechargeAnchorUtc = _lastLifeUsedUtc;

        CurrentLives++;
        if (CurrentLives >= MaxLives)
            rechargeAnchorUtc = DateTime.MinValue;

        UpdateVirtualLivesFromCurrentLives();

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

        _lastLifeUsedUtc = rechargeAnchorUtc;
        Persist("Vida ganada");
        EmitDisplayLivesChanged();
    }

    public void FillAllLives()
    {
        if (CurrentLives >= MaxLives) return;

        // Manual full refills follow the same rule as single-life grants: do not
        // move the recharge anchor unless the caller explicitly needs a reset.
        CurrentLives = MaxLives;
        _lastLifeUsedUtc = DateTime.MinValue;
        UpdateVirtualLivesFromCurrentLives();

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

    public bool TryGetFullLivesUtc(out DateTime fullLivesUtc)
    {
        fullLivesUtc = DateTime.MinValue;

        if (!_isInitialized)
            return false;

        if (GameConfigManager.IsTrailerCaptureModeEnabled())
            return false;

        if (GameConfigManager.IsReady() && GameConfigManager.Config.infiniteLives)
            return false;

        if (CurrentLives >= MaxLives)
            return false;

        if (HasTimedUnlimitedLives)
            return false;

        if (_lastLifeUsedUtc <= DateTime.MinValue || LifeRechargeSeconds <= 0)
            return false;

        if (!TryGetCurrentUtcNow(out DateTime currentUtc))
            return false;

        int missingLives = MaxLives - CurrentLives;
        TimeSpan timeToNextLife = GetTimeToNextLife();

        double totalSeconds = timeToNextLife.TotalSeconds + (missingLives - 1) * LifeRechargeSeconds;

        if (totalSeconds < 0d || totalSeconds > MaxSupportedElapsedSeconds)
            return false;

        fullLivesUtc = currentUtc.AddSeconds(totalSeconds);
        return true;
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

    private void RefreshStateAfterForeground(string syncReason)
    {
        ITrustedTimeProvider timeProvider = TimeProvider;

        if (CanApplyOfflineLifeProgress(timeProvider))
        {
            TrySynchronizeLivesWithTrustedTime(
                syncReason,
                emitChange: false,
                requireOfflineValidation: true);
        }
        else if (TryGetCurrentUtcNow(out DateTime currentUtc))
        {
            ApplyConservativeOfflineTimePolicy(currentUtc, timeProvider);
        }

        UpdateUnlimitedLivesState(false);
        EmitDisplayLivesChanged();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !_isInitialized)
            return;

        RefreshStateAfterForeground("Sync en foco");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus || !_isInitialized)
            return;

        RefreshStateAfterForeground("Sync al reanudar");
    }

    private void EmitDisplayLivesChanged()
    {
        int displayLives = GetDisplayLives();
        GameEvents.RaiseLivesChanged(displayLives);
    }

    private bool UpdateUnlimitedLivesState(bool emitChange = true)
    {
        if (_unlimitedLivesEndUtc == DateTime.MinValue)
            return false;

        DateTime currentUtc = GetCurrentUtcNowOrFallback();
        if (currentUtc > DateTime.MinValue && currentUtc < _unlimitedLivesEndUtc)
            return false;

        ClearUnlimitedLivesState();
        PersistUnlimitedLivesState();
        if (emitChange)
            EmitDisplayLivesChanged();

        return true;
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
