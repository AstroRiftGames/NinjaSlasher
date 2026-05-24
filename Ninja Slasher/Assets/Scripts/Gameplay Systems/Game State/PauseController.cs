using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviourSingleton<PauseController>
{
    [SerializeField] private bool _verboseLogging = false;

    [Header("Diagnostics")]
    [SerializeField] private List<PauseSource> _activePauseSourcesDebug = new();

    private readonly HashSet<PauseSource> _activePauseSources = new();
    private bool _hasAppliedPauseState;
    private bool _lastAppliedPauseState;

    public bool IsPaused => _activePauseSources.Count > 0;
    public bool HasActivePauseSources => _activePauseSources.Count > 0;
    public IReadOnlyCollection<PauseSource> ActivePauseSources => _activePauseSources;
    public event Action<bool> PauseStateChanged;

    public override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        RefreshDebugView();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        UIEvents.OnPauseButtonPressed += OnPauseButtonPressed;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;

        ApplyPauseState("OnEnable");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UIEvents.OnPauseButtonPressed -= OnPauseButtonPressed;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    public bool RequestPause(PauseSource source)
    {
        if (source == PauseSource.None)
            return false;

        bool added = _activePauseSources.Add(source);
        if (!added)
            return false;

        LogSourceChange("+", source);
        RefreshDebugView();
        ApplyPauseState("RequestPause");
        return true;
    }

    public bool ReleasePause(PauseSource source)
    {
        if (source == PauseSource.None)
            return false;

        bool removed = _activePauseSources.Remove(source);
        if (!removed)
            return false;

        LogSourceChange("-", source);
        RefreshDebugView();
        ApplyPauseState("ReleasePause");
        return true;
    }

    public bool IsPauseSourceActive(PauseSource source)
    {
        return source != PauseSource.None && _activePauseSources.Contains(source);
    }

    public bool CanAcceptUserPauseRequest()
    {
        return LevelSessionManager.Instance != null && LevelSessionManager.Instance.IsSessionRunning;
    }

    private void ApplyPauseState(string reason)
    {
        bool isPaused = IsPaused;
        float targetTimeScale = isPaused ? 0f : 1f;
        bool pauseStateChanged = !_hasAppliedPauseState || _lastAppliedPauseState != isPaused;

        _hasAppliedPauseState = true;
        _lastAppliedPauseState = isPaused;

        if (!Mathf.Approximately(Time.timeScale, targetTimeScale))
        {
            Time.timeScale = targetTimeScale;

            if (ShouldLog())
                Debug.Log($"[Pause] Applied {targetTimeScale:0.##} | Reason: {reason} | Active Sources: [{FormatActiveSources()}]");
        }

        if (pauseStateChanged)
            PauseStateChanged?.Invoke(isPaused);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsPaused)
            return;

        if (ShouldLog())
            Debug.Log($"[Pause] Reapplying managed pause after scene load | Scene={scene.name}");

        ApplyPauseState("SceneLoaded");
    }

    private void OnGameStateChanged(GameStateChanged change)
    {
        if (!IsPaused)
            return;

        ApplyPauseState($"GameStateChanged:{change.CurrentState}");
    }

    private void OnPauseButtonPressed()
    {
        if (!CanAcceptUserPauseRequest())
        {
            if (ShouldLog())
                Debug.Log("[Pause] User pause request ignored because the session is not running.");

            return;
        }

        UIEvents.RequestTogglePauseOverlay();
    }

    private void RefreshDebugView()
    {
        _activePauseSourcesDebug = _activePauseSources
            .OrderBy(source => source)
            .ToList();
    }

    private void LogSourceChange(string operation, PauseSource source)
    {
        if (!ShouldLog())
            return;

        Debug.Log($"[Pause] {operation} {source} | Active Sources: [{FormatActiveSources()}]");
    }

    private bool ShouldLog()
    {
        return _verboseLogging || Debug.isDebugBuild || Application.isEditor;
    }

    private string FormatActiveSources()
    {
        return _activePauseSources.Count > 0
            ? string.Join(", ", _activePauseSources.OrderBy(value => value))
            : "none";
    }
}
