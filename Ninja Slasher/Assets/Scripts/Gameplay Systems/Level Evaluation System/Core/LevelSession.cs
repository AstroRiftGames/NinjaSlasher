using System;
using UnityEngine;

public class LevelSession
{
    public int LevelId { get; private set; }
    public LevelConfiguration Configuration { get; private set; }
    public LevelSessionState State { get; private set; }

    public LevelStats CurrentStats { get; private set; }

    private float sessionStartTime;
    private bool isComplete;
    private bool isFailed;

    public event Action<LevelStats> OnStatsUpdated;
    public event Action<LevelSessionState> OnStateChanged;

    public LevelSession(LevelConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        Configuration = config;
        LevelId = config.levelId;
        State = LevelSessionState.Initializing;

        CurrentStats = new LevelStats
        {
            timeTaken = 0f,
            enemiesDefeated = 0,
            totalEnemies = 0,
            movesUsed = 0,
            parryKillDone = false
        };

        sessionStartTime = Time.time;
        isComplete = false;
        isFailed = false;

        Debug.Log($"[LevelSession] Sesión creada para nivel {LevelId}");
    }

    public void Initialize()
    {
        ChangeState(LevelSessionState.Ready);
        Debug.Log($"[LevelSession] Sesión inicializada para nivel {LevelId}");
    }

    public void Start()
    {
        if (State != LevelSessionState.Ready)
        {
            Debug.LogWarning($"[LevelSession] No se puede iniciar - Estado actual: {State}");
            return;
        }

        sessionStartTime = Time.time;
        ChangeState(LevelSessionState.Running);
        Debug.Log($"[LevelSession] Nivel {LevelId} iniciado");
    }

    public void Pause()
    {
        if (State != LevelSessionState.Running)
        {
            Debug.LogWarning($"[LevelSession] No se puede pausar - Estado actual: {State}");
            return;
        }

        ChangeState(LevelSessionState.Paused);
    }

    public void Resume()
    {
        if (State != LevelSessionState.Paused)
        {
            Debug.LogWarning($"[LevelSession] No se puede resumir - Estado actual: {State}");
            return;
        }

        ChangeState(LevelSessionState.Running);
    }

    public void Complete()
    {
        if (isComplete || isFailed)
        {
            Debug.LogWarning($"[LevelSession] Sesión ya finalizada - Complete: {isComplete}, Failed: {isFailed}");
            return;
        }

        isComplete = true;
        CurrentStats.timeTaken = Time.time - sessionStartTime;
        ChangeState(LevelSessionState.Completed);

        Debug.Log($"[LevelSession] Nivel {LevelId} completado - Tiempo: {CurrentStats.timeTaken:F2}s");
    }

    public void Fail(string reason)
    {
        if (isComplete || isFailed)
        {
            Debug.LogWarning($"[LevelSession] Sesión ya finalizada");
            return;
        }

        isFailed = true;
        CurrentStats.timeTaken = Time.time - sessionStartTime;
        ChangeState(LevelSessionState.Failed);

        Debug.Log($"[LevelSession] Nivel {LevelId} fallido - Razón: {reason}");
    }

    public void UpdateEnemyCount(int defeated, int total)
    {
        CurrentStats.enemiesDefeated = defeated;
        CurrentStats.totalEnemies = total;
        NotifyStatsUpdated();
    }

    public void IncrementMoves()
    {
        CurrentStats.movesUsed++;
        NotifyStatsUpdated();
    }

    public void RegisterParryKill()
    {
        CurrentStats.parryKillDone = true;
        NotifyStatsUpdated();
    }

    public void UpdateTime(float currentTime)
    {
        CurrentStats.timeTaken = currentTime;
        NotifyStatsUpdated();
    }

    public bool IsRunning => State == LevelSessionState.Running;
    public bool IsComplete => isComplete;
    public bool IsFailed => isFailed;
    public bool CanEvaluate => isComplete && !isFailed;

    public float GetElapsedTime()
    {
        if (State == LevelSessionState.Running)
        {
            return Time.time - sessionStartTime;
        }
        return CurrentStats.timeTaken;
    }

    public void Clear()
    {
        ChangeState(LevelSessionState.Disposed);

        CurrentStats = new LevelStats();
        isComplete = false;
        isFailed = false;

        Debug.Log($"[LevelSession] Sesión limpiada para nivel {LevelId}");
    }

    private void ChangeState(LevelSessionState newState)
    {
        if (State == newState) return;

        var oldState = State;
        State = newState;

        OnStateChanged?.Invoke(newState);

        Debug.Log($"[LevelSession] Estado cambiado: {oldState} → {newState}");
    }

    private void NotifyStatsUpdated()
    {
        OnStatsUpdated?.Invoke(CurrentStats);
    }
}

public enum LevelSessionState
{
    Initializing,
    Ready,
    Running,
    Paused,
    Completed,
    Failed,
    Disposed
}