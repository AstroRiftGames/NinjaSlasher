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
    }

    public void Initialize()
    {
        ChangeState(LevelSessionState.Ready);
    }

    public void Start()
    {
        if (State != LevelSessionState.Ready)
        {
            return;
        }

        sessionStartTime = Time.time;
        ChangeState(LevelSessionState.Running);
    }

    public void Pause()
    {
        if (State != LevelSessionState.Running)
        {
            return;
        }

        ChangeState(LevelSessionState.Paused);
    }

    public void Resume()
    {
        if (State != LevelSessionState.Paused)
        {
            return;
        }

        ChangeState(LevelSessionState.Running);
    }

    public void Complete()
    {
        if (isComplete || isFailed)
        {
            return;
        }

        isComplete = true;
        CurrentStats.timeTaken = Time.time - sessionStartTime;
        ChangeState(LevelSessionState.Completed);
    }

    public void Fail(string reason)
    {
        if (isComplete || isFailed)
        {
            return;
        }

        isFailed = true;
        CurrentStats.timeTaken = Time.time - sessionStartTime;
        ChangeState(LevelSessionState.Failed);
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
    }

    private void ChangeState(LevelSessionState newState)
    {
        if (State == newState) return;

        var oldState = State;
        State = newState;

        OnStateChanged?.Invoke(newState);
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