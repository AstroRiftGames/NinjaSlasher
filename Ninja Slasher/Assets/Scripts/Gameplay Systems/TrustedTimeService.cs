using System;
using System.Collections;
using UnityEngine;

public interface ITrustedTimeProvider
{
    bool IsInitialized { get; }
    bool IsTimeTrusted { get; }
    bool CanApplyOfflineProgress { get; }
    bool HasSuspiciousTimeJump { get; }
    DateTime UtcNow { get; }

    bool TryGetTrustedUtcNow(out DateTime utcNow);
    void RegisterAppStartOrForeground();
    bool TryFreezeElapsedSince(DateTime savedAnchorUtc, double maxElapsedSeconds, out DateTime rebasedAnchorUtc);
    bool TryFreezeRemainingDuration(DateTime savedEndUtc, double maxRemainingSeconds, out DateTime rebasedEndUtc);
}

public struct TrustedTimePersistenceState
{
    public string LastKnownLocalUtc;
    public string LastTrustedUtc;
    public string LastTimeValidationUtc;
    public bool TrustedTimeAvailable;
    public bool SuspiciousTimeDetected;
}

public sealed class TrustedTimeProvider : ITrustedTimeProvider
{
    private const double ClockJumpToleranceSeconds = 15d;
    private const double MaxAcceptedOfflineGapSeconds = 365d * 24d * 60d * 60d;

    private readonly Func<DateTime> _localUtcNowSource;
    private readonly Func<double> _monotonicSecondsSource;
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logWarning;

    private DateTime _trustedUtcAtLastValidation = DateTime.MinValue;
    private DateTime _lastObservedLocalUtc = DateTime.MinValue;
    private DateTime _lastValidationUtc = DateTime.MinValue;
    private DateTime _persistedLastKnownLocalUtc = DateTime.MinValue;
    private DateTime _persistedLastTrustedUtc = DateTime.MinValue;

    private double _monotonicSecondsAtLastValidation;
    private bool _hasPersistedLastKnownLocalUtc;
    private bool _hasPersistedLastTrustedUtc;

    public TrustedTimeProvider(
        Func<DateTime> localUtcNowSource,
        Func<double> monotonicSecondsSource,
        Action<string> logInfo = null,
        Action<string> logWarning = null)
    {
        _localUtcNowSource = localUtcNowSource ?? throw new ArgumentNullException(nameof(localUtcNowSource));
        _monotonicSecondsSource = monotonicSecondsSource ?? throw new ArgumentNullException(nameof(monotonicSecondsSource));
        _logInfo = logInfo;
        _logWarning = logWarning;
    }

    public bool IsInitialized { get; private set; }
    public bool IsTimeTrusted => IsInitialized;
    public bool CanApplyOfflineProgress { get; private set; }
    public bool HasSuspiciousTimeJump { get; private set; }

    public DateTime UtcNow => TryGetTrustedUtcNow(out DateTime utcNow)
        ? utcNow
        : DateTime.MinValue;

    public void Initialize(TrustedTimePersistenceState persistenceState)
    {
        DateTime currentLocalUtc = GetLocalUtcNow();

        _hasPersistedLastKnownLocalUtc =
            persistenceState.TrustedTimeAvailable &&
            DailyAvailabilityTimeUtility.TryParseIsoUtc(persistenceState.LastKnownLocalUtc, out _persistedLastKnownLocalUtc);

        _hasPersistedLastTrustedUtc =
            persistenceState.TrustedTimeAvailable &&
            DailyAvailabilityTimeUtility.TryParseIsoUtc(persistenceState.LastTrustedUtc, out _persistedLastTrustedUtc);

        if (!DailyAvailabilityTimeUtility.TryParseIsoUtc(persistenceState.LastTimeValidationUtc, out _lastValidationUtc))
            _lastValidationUtc = currentLocalUtc;

        _trustedUtcAtLastValidation = currentLocalUtc;
        _lastObservedLocalUtc = currentLocalUtc;
        _monotonicSecondsAtLastValidation = GetMonotonicSeconds();

        HasSuspiciousTimeJump = persistenceState.SuspiciousTimeDetected;
        CanApplyOfflineProgress = EvaluateOfflineContinuity(currentLocalUtc);
        IsInitialized = true;
    }

    public bool TryGetTrustedUtcNow(out DateTime utcNow)
    {
        utcNow = DateTime.MinValue;

        if (!IsInitialized)
            return false;

        ValidateLocalClock();
        utcNow = ComputeTrustedUtcNow(GetMonotonicSeconds());
        return true;
    }

    public void RegisterAppStartOrForeground()
    {
        if (!IsInitialized)
            return;

        ValidateLocalClock();
    }

    public TrustedTimePersistenceState CapturePersistence()
    {
        if (!TryGetTrustedUtcNow(out DateTime trustedUtcNow))
            return default;

        return new TrustedTimePersistenceState
        {
            LastKnownLocalUtc = _lastObservedLocalUtc.ToString("o"),
            LastTrustedUtc = trustedUtcNow.ToString("o"),
            LastTimeValidationUtc = _lastValidationUtc.ToString("o"),
            TrustedTimeAvailable = IsTimeTrusted,
            SuspiciousTimeDetected = HasSuspiciousTimeJump
        };
    }

    public bool TryFreezeElapsedSince(DateTime savedAnchorUtc, double maxElapsedSeconds, out DateTime rebasedAnchorUtc)
    {
        rebasedAnchorUtc = DateTime.MinValue;

        if (savedAnchorUtc <= DateTime.MinValue ||
            !_hasPersistedLastTrustedUtc ||
            !TryGetTrustedUtcNow(out DateTime trustedUtcNow))
        {
            return false;
        }

        double elapsedAtSaveSeconds = (_persistedLastTrustedUtc - savedAnchorUtc).TotalSeconds;
        if (double.IsNaN(elapsedAtSaveSeconds) || double.IsInfinity(elapsedAtSaveSeconds))
            return false;

        double clampedElapsedSeconds = Math.Max(0d, Math.Min(maxElapsedSeconds, elapsedAtSaveSeconds));
        rebasedAnchorUtc = trustedUtcNow.AddSeconds(-clampedElapsedSeconds);
        return true;
    }

    public bool TryFreezeRemainingDuration(DateTime savedEndUtc, double maxRemainingSeconds, out DateTime rebasedEndUtc)
    {
        rebasedEndUtc = DateTime.MinValue;

        if (savedEndUtc <= DateTime.MinValue ||
            !_hasPersistedLastTrustedUtc ||
            !TryGetTrustedUtcNow(out DateTime trustedUtcNow))
        {
            return false;
        }

        double remainingAtSaveSeconds = (savedEndUtc - _persistedLastTrustedUtc).TotalSeconds;
        if (double.IsNaN(remainingAtSaveSeconds) || double.IsInfinity(remainingAtSaveSeconds))
            return false;

        double clampedRemainingSeconds = Math.Max(0d, Math.Min(maxRemainingSeconds, remainingAtSaveSeconds));
        rebasedEndUtc = trustedUtcNow.AddSeconds(clampedRemainingSeconds);
        return true;
    }

    private bool EvaluateOfflineContinuity(DateTime currentLocalUtc)
    {
        if (!_hasPersistedLastKnownLocalUtc || !_hasPersistedLastTrustedUtc)
        {
            _logInfo?.Invoke("[TrustedTimeProvider] Offline progress unavailable because no persisted continuity state was found.");
            return false;
        }

        if (HasSuspiciousTimeJump)
        {
            _logWarning?.Invoke("[TrustedTimeProvider] Offline progress is frozen because the previous session already flagged a suspicious time jump.");
            return false;
        }

        double offlineGapSeconds = (currentLocalUtc - _persistedLastKnownLocalUtc).TotalSeconds;
        if (double.IsNaN(offlineGapSeconds) || double.IsInfinity(offlineGapSeconds))
            return false;

        if (offlineGapSeconds < -ClockJumpToleranceSeconds)
        {
            HasSuspiciousTimeJump = true;
            _logWarning?.Invoke($"[TrustedTimeProvider] Startup detected a backwards local clock jump ({offlineGapSeconds:F1}s). Offline progress will be frozen.");
            return false;
        }

        if (offlineGapSeconds < 0d)
        {
            _logWarning?.Invoke($"[TrustedTimeProvider] Startup detected a small negative local clock drift ({offlineGapSeconds:F1}s). Offline progress will be frozen.");
            return false;
        }

        if (offlineGapSeconds > MaxAcceptedOfflineGapSeconds)
        {
            HasSuspiciousTimeJump = true;
            _logWarning?.Invoke($"[TrustedTimeProvider] Startup detected an excessive local clock gap ({offlineGapSeconds:F1}s). Offline progress will be frozen.");
            return false;
        }

        _logInfo?.Invoke($"[TrustedTimeProvider] Offline progress enabled from persisted local continuity ({offlineGapSeconds:F1}s elapsed).");
        return true;
    }

    private void ValidateLocalClock()
    {
        double currentMonotonicSeconds = GetMonotonicSeconds();
        DateTime trustedUtcNow = ComputeTrustedUtcNow(currentMonotonicSeconds);
        DateTime currentLocalUtc = GetLocalUtcNow();

        double monotonicDeltaSeconds = Math.Max(0d, currentMonotonicSeconds - _monotonicSecondsAtLastValidation);
        DateTime expectedLocalUtc = _lastObservedLocalUtc.AddSeconds(monotonicDeltaSeconds);
        double driftSeconds = (currentLocalUtc - expectedLocalUtc).TotalSeconds;

        if (Math.Abs(driftSeconds) > ClockJumpToleranceSeconds)
        {
            HasSuspiciousTimeJump = true;
            _logWarning?.Invoke($"[TrustedTimeProvider] Suspicious local clock drift detected ({driftSeconds:F1}s). Runtime time will stay on the monotonic timeline.");
        }

        _trustedUtcAtLastValidation = trustedUtcNow;
        _lastObservedLocalUtc = currentLocalUtc;
        _lastValidationUtc = trustedUtcNow;
        _monotonicSecondsAtLastValidation = currentMonotonicSeconds;
    }

    private DateTime ComputeTrustedUtcNow(double currentMonotonicSeconds)
    {
        double deltaSeconds = Math.Max(0d, currentMonotonicSeconds - _monotonicSecondsAtLastValidation);
        return _trustedUtcAtLastValidation.AddSeconds(deltaSeconds);
    }

    private DateTime GetLocalUtcNow() => _localUtcNowSource();
    private double GetMonotonicSeconds() => _monotonicSecondsSource();
}

[DefaultExecutionOrder(-900)]
public sealed class TrustedTimeService : MonoBehaviour
{
    private const string BootstrapObjectName = "TrustedTimeService";

    private Coroutine _bootstrapRoutine;
    private TrustedTimeProvider _provider;

    public ITrustedTimeProvider Provider => _provider;
    public bool IsInitialized => _provider != null && _provider.IsInitialized;

    private static void LogTrustedTimeInfo(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(message);
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrapInstance()
    {
        if (UnityEngine.Object.FindFirstObjectByType<TrustedTimeService>() != null)
            return;

        GameObject bootstrap = new GameObject(BootstrapObjectName);
        bootstrap.AddComponent<TrustedTimeService>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _provider ??= new TrustedTimeProvider(
            () => DateTime.UtcNow,
            () => Time.realtimeSinceStartupAsDouble,
            LogTrustedTimeInfo,
            message => Debug.LogWarning(message));
    }

    private void OnEnable()
    {
        SaveManager.OnDataLoaded += OnSaveDataLoaded;
        Application.focusChanged += OnApplicationFocusChanged;

        if (_bootstrapRoutine == null)
            _bootstrapRoutine = StartCoroutine(InitializeWhenSaveReady());
    }

    private void OnDisable()
    {
        SaveManager.OnDataLoaded -= OnSaveDataLoaded;
        Application.focusChanged -= OnApplicationFocusChanged;

        if (_bootstrapRoutine != null)
        {
            StopCoroutine(_bootstrapRoutine);
            _bootstrapRoutine = null;
        }
    }

    public void PopulatePersistence(GameData data)
    {
        if (data == null || _provider == null || !_provider.IsInitialized)
            return;

        TrustedTimePersistenceState persistenceState = _provider.CapturePersistence();
        ApplyPersistenceState(data, persistenceState);
    }

    private IEnumerator InitializeWhenSaveReady()
    {
        while (SaveManager.Instance == null || !SaveManager.Instance.IsDataLoaded)
            yield return null;

        InitializeFromSave(SaveManager.Instance.GetGameData());
        _bootstrapRoutine = null;
    }

    private void OnSaveDataLoaded(GameData data)
    {
        InitializeFromSave(data);
    }

    private void OnApplicationFocusChanged(bool hasFocus)
    {
        if (hasFocus)
            _provider?.RegisterAppStartOrForeground();
    }

    private void InitializeFromSave(GameData data)
    {
        if (_provider == null)
            return;

        _provider.Initialize(CreatePersistenceState(data));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[TrustedTimeService] Bootstrap | canApplyOfflineProgress={_provider.CanApplyOfflineProgress} | suspicious={_provider.HasSuspiciousTimeJump} | trusted={_provider.IsTimeTrusted}");
#endif

        _provider.RegisterAppStartOrForeground();
    }

    private static TrustedTimePersistenceState CreatePersistenceState(GameData data)
    {
        return new TrustedTimePersistenceState
        {
            LastKnownLocalUtc = data?.lastKnownLocalUtc ?? string.Empty,
            LastTrustedUtc = data?.lastTrustedUtc ?? string.Empty,
            LastTimeValidationUtc = data?.lastTimeValidationUtc ?? string.Empty,
            TrustedTimeAvailable = data != null && data.trustedTimeAvailable,
            SuspiciousTimeDetected = data != null && data.suspiciousTimeDetected
        };
    }

    private static void ApplyPersistenceState(GameData data, TrustedTimePersistenceState persistenceState)
    {
        data.lastKnownLocalUtc = persistenceState.LastKnownLocalUtc ?? string.Empty;
        data.lastTrustedUtc = persistenceState.LastTrustedUtc ?? string.Empty;
        data.lastTimeValidationUtc = persistenceState.LastTimeValidationUtc ?? string.Empty;
        data.trustedTimeAvailable = persistenceState.TrustedTimeAvailable;
        data.suspiciousTimeDetected = persistenceState.SuspiciousTimeDetected;
    }
}
