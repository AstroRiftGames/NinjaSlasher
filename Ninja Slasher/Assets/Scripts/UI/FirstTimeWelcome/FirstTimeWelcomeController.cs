using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class FirstTimeWelcomeController : MonoBehaviour
{
    private const string DefaultConfigResourceName = "FirstTimeWelcomeConfig";

    [SerializeField] private FirstTimeWelcomeConfig _config;
    [SerializeField] private FirstTimeWelcomeView _view;
    [SerializeField] private int _startupSettleFrames = 4;

    public event Action<bool> LevelSelectionInputBlockChanged;

    private readonly Dictionary<string, FirstTimeWelcomeTarget> _targetsById = new Dictionary<string, FirstTimeWelcomeTarget>(16);
    private readonly HashSet<string> _missingTargetWarnings = new HashSet<string>();
    private Coroutine _startupRoutine;
    private int _currentStepIndex;
    private bool _isRunning;
    private bool _screenReady;

    private void Awake()
    {
        ResolveConfig();
        ResolveView();
    }

    private void OnEnable()
    {
        if (_screenReady && _startupRoutine == null)
            _startupRoutine = StartCoroutine(TryStartWhenReady());
    }

    private void OnDisable()
    {
        if (_startupRoutine != null)
        {
            StopCoroutine(_startupRoutine);
            _startupRoutine = null;
        }

        UnbindView();
    }

    public void NotifyScreenReady()
    {
        _screenReady = true;

        if (!isActiveAndEnabled || _startupRoutine != null || _isRunning)
            return;

        _startupRoutine = StartCoroutine(TryStartWhenReady());
    }

    private IEnumerator TryStartWhenReady()
    {
        yield return null;

        while (!_screenReady)
            yield return null;

        while (SaveManager.Instance == null || !SaveManager.Instance.IsDataLoaded)
            yield return null;

        GameData data = SaveManager.Instance.GetGameData();
        if (data == null || data.hasSeenFirstTimeWelcome)
        {
            LevelSelectionInputBlockChanged?.Invoke(false);
            _startupRoutine = null;
            yield break;
        }

        LevelSelectionInputBlockChanged?.Invoke(true);
        FirstTimeWelcomeSaveState.BeginWelcomeAttemptForCurrentEntry();
        ResolveConfig();
        if (_config == null || _config.StepCount == 0)
        {
            AbortFlow(_config == null ? "Config is missing." : "Config has no steps.");
            _startupRoutine = null;
            yield break;
        }

        while (!CanOpenWelcome())
        {
            yield return null;
        }

        int settleFrames = Mathf.Max(0, _startupSettleFrames);
        for (int i = 0; i < settleFrames; i++)
            yield return null;

        CacheTargets();
        ResolveView();
        if (_view != null)
        {
            _view.Warmup();
            yield return null;
        }

        StartFlow();
        _startupRoutine = null;
    }

    private bool CanOpenWelcome()
    {
        UIManager uiManager = UIManager.Instance;
        return uiManager == null || uiManager.CanOpenFirstTimeWelcomeFlow();
    }

    private void StartFlow()
    {
        ResolveView();
        if (_view == null)
        {
            AbortFlow("View is missing.");
            return;
        }

        _isRunning = true;
        _currentStepIndex = 0;
        BindView();

        _view.Show();
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (!_isRunning)
            return;

        FirstTimeWelcomeStep step = _config.GetStep(_currentStepIndex);
        if (step == null)
        {
            AbortFlow($"Step {_currentStepIndex} is null.");
            return;
        }

        RectTransform targetRect = null;
        if (!string.IsNullOrWhiteSpace(step.TargetId))
        {
            FirstTimeWelcomeTarget target;
            if (_targetsById.TryGetValue(step.TargetId, out target) && target != null)
            {
                targetRect = target.RectTransform;
            }
            else
            {
                LogMissingTargetOnce(step.TargetId);
            }
        }

        _view.ShowStep(step.Message, targetRect);
    }

    private void HandleNextRequested()
    {
        if (!_isRunning)
            return;

        _currentStepIndex++;
        if (_currentStepIndex >= _config.StepCount)
        {
            CompleteFlow();
            return;
        }

        ShowCurrentStep();
    }

    private void HandleSkipRequested()
    {
        if (!_isRunning)
            return;

        CompleteFlow();
    }

    private void CompleteFlow()
    {
        _isRunning = false;
        UnbindView();

        if (_view != null)
            _view.Hide();

        bool saved = FirstTimeWelcomeSaveState.MarkWelcomeAsSeen();
        LevelSelectionInputBlockChanged?.Invoke(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[FirstTimeWelcomeController] Flow completed. Welcome marked as seen. Saved={saved}");
#endif
        if (!saved)
            Debug.LogWarning("[FirstTimeWelcomeController] Flow completed, but SaveManager was not available. Welcome could not be persisted.");
    }

    private void AbortFlow(string reason)
    {
        _isRunning = false;
        UnbindView();

        if (_view != null && _view.IsVisible)
            _view.Hide();

        FirstTimeWelcomeSaveState.MarkWelcomeAbortedForCurrentEntry();
        LevelSelectionInputBlockChanged?.Invoke(false);
        UIEvents.RaiseLevelSelectorReady();
        Debug.LogWarning($"[FirstTimeWelcomeController] Flow aborted. Reason={reason}. Welcome was not marked as seen.");
    }

    private void CacheTargets()
    {
        _targetsById.Clear();
        _missingTargetWarnings.Clear();

        FirstTimeWelcomeTarget[] targets = FindObjectsByType<FirstTimeWelcomeTarget>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < targets.Length; i++)
        {
            FirstTimeWelcomeTarget target = targets[i];
            if (target == null || string.IsNullOrWhiteSpace(target.Id))
                continue;

            if (!_targetsById.ContainsKey(target.Id))
                _targetsById.Add(target.Id, target);
        }
    }

    private void LogMissingTargetOnce(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId) || _missingTargetWarnings.Contains(targetId))
            return;

        _missingTargetWarnings.Add(targetId);
        Debug.LogWarning($"[FirstTimeWelcomeController] Target id '{targetId}' was not found. Showing text-only step.");
    }

    private void ResolveConfig()
    {
        if (_config != null)
            return;

        _config = Resources.Load<FirstTimeWelcomeConfig>(DefaultConfigResourceName);
    }

    private void ResolveView()
    {
        if (_view != null)
            return;

        _view = GetComponentInChildren<FirstTimeWelcomeView>(true);
        if (_view == null)
            Debug.LogWarning("[FirstTimeWelcomeController] FirstTimeWelcomeView is not assigned.");
    }

    private void BindView()
    {
        if (_view == null)
            return;

        _view.NextRequested -= HandleNextRequested;
        _view.SkipRequested -= HandleSkipRequested;
        _view.NextRequested += HandleNextRequested;
        _view.SkipRequested += HandleSkipRequested;
    }

    private void UnbindView()
    {
        if (_view == null)
            return;

        _view.NextRequested -= HandleNextRequested;
        _view.SkipRequested -= HandleSkipRequested;
    }
}

internal static class FirstTimeWelcomeSaveState
{
    private static bool _welcomeAbortedForCurrentEntry;

    public static string GetTodayUtcKey()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    public static void BeginWelcomeAttemptForCurrentEntry()
    {
        _welcomeAbortedForCurrentEntry = false;
    }

    public static void MarkWelcomeAbortedForCurrentEntry()
    {
        _welcomeAbortedForCurrentEntry = true;
    }

    public static bool MarkWelcomeAsSeen()
    {
        if (SaveManager.Instance == null)
            return false;

        string todayUtc = GetTodayUtcKey();
        SaveManager.Instance.Modify(data =>
        {
            data.hasSeenFirstTimeWelcome = true;
            data.firstTimeWelcomeDailyStartupSuppressedDateUtc = todayUtc;
        });

        _welcomeAbortedForCurrentEntry = false;
        return true;
    }

    public static bool ShouldSuppressDailyStartupToday(GameData data)
    {
        if (data == null)
            return false;

        if (IsWelcomePendingForCurrentEntry(data))
            return true;

        return WasWelcomeCompletedToday(data);
    }

    public static bool IsWelcomePendingForCurrentEntry(GameData data)
    {
        return data != null && !data.hasSeenFirstTimeWelcome && !_welcomeAbortedForCurrentEntry;
    }

    public static bool WasWelcomeCompletedToday(GameData data)
    {
        if (data == null || !data.hasSeenFirstTimeWelcome)
            return false;

        return string.Equals(
            data.firstTimeWelcomeDailyStartupSuppressedDateUtc,
            GetTodayUtcKey(),
            StringComparison.Ordinal);
    }
}
