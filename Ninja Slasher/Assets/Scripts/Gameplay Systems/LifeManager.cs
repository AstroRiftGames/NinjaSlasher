using System;
using System.Collections;
using UnityEngine;

public class LifeManager : MonoBehaviourSingleton<LifeManager>
{
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

    private bool HasDebugInfiniteLives =>
        GameConfigManager.IsReady() &&
        GameConfigManager.Config.infiniteLives;

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

        Debug.Log($"[LifeManager] LoadLives | savedLives={data?.currentLives} | savedTimestamp='{data?.lastLifeRegenTime}' | canRegen={data?.canRegenLives} | unlimitedLivesEndUtc={data?.unlimitedLivesEndUtc}");

        DateTime lastRegenUtc = DateTime.UtcNow;
        bool validDate = data != null && DateTime.TryParse(
            data.lastLifeRegenTime, null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out lastRegenUtc);

        if (validDate)
        {
            lastRegenUtc = lastRegenUtc.Kind == DateTimeKind.Utc
                ? lastRegenUtc
                : lastRegenUtc.ToUniversalTime();

            TimeSpan timeDiff = DateTime.UtcNow - lastRegenUtc;
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
            _lastLifeUsedUtc = DateTime.UtcNow;
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
                _lastLifeUsedUtc = DateTime.UtcNow;
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
            else if (DateTime.UtcNow < _unlimitedLivesEndUtc)
            {
                _unlimitedLivesStartUtc = DateTime.UtcNow;
                PersistUnlimitedLivesState();
            }
        }
        else
        {
            _unlimitedLivesStartUtc = DateTime.MinValue;
            _unlimitedLivesEndUtc = DateTime.MinValue;
        }

        CheckOfflineRegeneration();
        _virtualLives = CurrentLives;

        Debug.Log($"[LifeManager] RuntimeLivesFinal | currentLives={CurrentLives} | displayLives={GetDisplayLives()} | timerBaseUtc={_lastLifeUsedUtc:O} | unlimitedLivesActive={HasTimedUnlimitedLives}");
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
        if (HasDebugInfiniteLives) return;
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

            bool wasAtWall = _lifeWallActive && CurrentLives == 0;

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
            _lastLifeUsedUtc = DateTime.UtcNow;
            CurrentLives = MaxLives;
            _virtualLives = CurrentLives;
            Persist("Reset por error");
            EmitDisplayLivesChanged();
        }
    }

    private void CheckOfflineRegeneration()
    {
        if (HasDebugInfiniteLives) return;
        if (CurrentLives >= MaxLives) return;

        try
        {
            int savedLives = CurrentLives;
            double seconds = (DateTime.UtcNow - _lastLifeUsedUtc).TotalSeconds;

            if (seconds < 0 || seconds > (365 * 24 * 60 * 60))
            {
                Debug.LogWarning($"[LifeManager] Tiempo offline inválido: {seconds}s. Reseteando.");
                _lastLifeUsedUtc = DateTime.UtcNow;
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
                _lastLifeUsedUtc = DateTime.UtcNow;
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
            _lastLifeUsedUtc = DateTime.UtcNow;
            CurrentLives = MaxLives;
            _virtualLives = CurrentLives;
            Persist("Reset por error offline");
            EmitDisplayLivesChanged();
        }
    }

    #endregion

    #region PUBLIC API

    public bool IsInitialized => _isInitialized;

    public bool HasTimedUnlimitedLives => DateTime.UtcNow < _unlimitedLivesEndUtc;

    public void ActivateUnlimitedLives(float durationMinutes)
    {
        DateTime baseTime = HasTimedUnlimitedLives ? _unlimitedLivesEndUtc : DateTime.UtcNow;
        _unlimitedLivesStartUtc = DateTime.UtcNow;
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
        return _unlimitedLivesEndUtc - DateTime.UtcNow;
    }

    public float GetUnlimitedLivesFillAmount()
    {
        if (!HasTimedUnlimitedLives)
            return 0f;

        double totalSeconds = (_unlimitedLivesEndUtc - _unlimitedLivesStartUtc).TotalSeconds;
        if (totalSeconds <= 0d)
            return 0f;

        double remainingSeconds = (_unlimitedLivesEndUtc - DateTime.UtcNow).TotalSeconds;
        return Mathf.Clamp01((float)(remainingSeconds / totalSeconds));
    }

    public bool CanPlay()
    {
        if (HasTimedUnlimitedLives) return true;

        if (HasDebugInfiniteLives)
        {
            return true;
        }

        return CurrentLives > 0;
    }

    public bool CanPlayAfterConfirmingPendingDeduction()
    {
        if (HasTimedUnlimitedLives) return true;

        if (HasDebugInfiniteLives)
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

        if (HasDebugInfiniteLives)
        {
            _virtualLives = CurrentLives;
            _levelInProgress = false;
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
        if (HasDebugInfiniteLives)
        {
            _hasVirtualDeduction = false;
            _virtualLives = CurrentLives;
            _levelInProgress = false;
            EmitDisplayLivesChanged();
            return;
        }

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
            if (HasTimedUnlimitedLives || HasDebugInfiniteLives)
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

        if (DateTime.UtcNow < _unlimitedLivesEndUtc)
            return;

        _unlimitedLivesStartUtc = DateTime.MinValue;
        _unlimitedLivesEndUtc = DateTime.MinValue;
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
