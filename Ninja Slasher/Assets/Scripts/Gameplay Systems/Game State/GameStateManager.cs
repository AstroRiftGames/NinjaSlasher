using System;
using UnityEngine;

public class GameStateManager : MonoBehaviourSingleton<GameStateManager>
{
    [Header("Diagnostics")]
    [SerializeField] private bool _verboseLogging = false;
    [SerializeField] private GameState _currentState = GameState.Boot;

    private GameState _previousState = GameState.Boot;

    public event Action<GameStateChanged> OnStateChanged;

    public GameState CurrentState => _currentState;
    public GameState PreviousState => _previousState;

    public override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        _currentState = GameState.Boot;
        _previousState = GameState.Boot;
    }

    public bool TrySetState(GameState newState, string reason, UnityEngine.Object source = null)
    {
        if (_currentState == newState)
            return false;

        GameState oldState = _currentState;
        _previousState = oldState;
        _currentState = newState;

        GameStateChanged payload = new GameStateChanged(
            oldState,
            newState,
            string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason,
            ResolveSourceName(source),
            Time.frameCount,
            Time.realtimeSinceStartup,
            DateTime.UtcNow);

        if (ShouldLogTransitions())
        {
            Debug.Log(
                $"[GameState] {payload.PreviousState} -> {payload.CurrentState} | " +
                $"Reason: {payload.Reason} | Source: {payload.SourceName}");
        }

        OnStateChanged?.Invoke(payload);
        return true;
    }

    private bool ShouldLogTransitions()
    {
        return _verboseLogging || Debug.isDebugBuild || Application.isEditor;
    }

    private static string ResolveSourceName(UnityEngine.Object source)
    {
        if (source == null)
            return "Unknown";

        return source.GetType().Name;
    }
}
