using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrackingService
{
    private LevelSession session;
    private LevelConfiguration config;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private int totalEnemiesAtStart;

    private int movesCount;
    private bool parryKillRegistered;

    private bool _isDashActive = false;
    private int _currentDashKills = 0;

    private bool isActive;

    public TrackingService(LevelSession levelSession)
    {
        session = levelSession;
        config = levelSession.Configuration;
        isActive = false;
    }

    public void Initialize()
    {
        RegisterEnemies();
        RegisterBreakablePlatforms();

        movesCount = 0;
        parryKillRegistered = false;

        SubscribeToEvents();

        isActive = true;

        UpdateSessionStats();
    }

    private void RegisterEnemies()
    {
        Enemy[] foundEnemies = Object.FindObjectsOfType<Enemy>();
        activeEnemies.Clear();

        Scene currentScene = SceneManager.GetActiveScene();

        foreach (Enemy enemy in foundEnemies)
        {
            if (enemy != null && enemy.gameObject != null &&
                enemy.gameObject.scene == currentScene)
            {
                activeEnemies.Add(enemy);
            }
        }

        totalEnemiesAtStart = activeEnemies.Count;

        if (config.levelContext != null)
        {
            config.levelContext.Initialize(totalEnemiesAtStart);
        }
    }

    private void RegisterBreakablePlatforms()
    {
        BreakablePlatform[] platforms = Object.FindObjectsOfType<BreakablePlatform>();
        int count = 0;
        foreach (BreakablePlatform platform in platforms)
        {
            if (platform != null && platform.gameObject.activeInHierarchy)
                count++;
        }
        session.UpdateTotalPlatforms(count);
    }

    private void SubscribeToEvents()
    {
        GameEvents.OnEnemyDefeated += OnEnemyDefeated;
        GameEvents.OnBL4ZTExplosionKills += OnBL4ZTExplosionKills;
        GameEvents.OnBreakablePlatformBroken += OnBreakablePlatformBroken;
        GameEvents.OnDashStarted += OnDashStarted;
        GameEvents.OnDashEnded += OnDashEnded;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnEnemyDefeated -= OnEnemyDefeated;
        GameEvents.OnBL4ZTExplosionKills -= OnBL4ZTExplosionKills;
        GameEvents.OnBreakablePlatformBroken -= OnBreakablePlatformBroken;
        GameEvents.OnDashStarted -= OnDashStarted;
        GameEvents.OnDashEnded -= OnDashEnded;
    }

    public void RegisterMove()
    {
        if (!isActive) return;

        movesCount++;
        session.IncrementMoves();
    }

    public void RegisterParryKill()
    {
        if (!isActive) return;

        parryKillRegistered = true;
        session.RegisterParryKill();
    }

    public void RegisterEnemyKilled(Enemy enemy)
    {
        if (!isActive) return;

        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);

            UpdateSessionStats();

            if (_isDashActive)
            {
                _currentDashKills++;
                session.UpdateMaxSingleAttackKills(_currentDashKills);
            }

            if (activeEnemies.Count == 0)
            {
                OnAllEnemiesDefeated();
            }
        }
    }

    private void OnEnemyDefeated(int enemyCount)
    {
        int defeated = totalEnemiesAtStart - activeEnemies.Count;
        UpdateSessionStats();
    }

    private void OnBL4ZTExplosionKills(int count)
    {
        if (!isActive) return;
        session.AddBL4ZTKills(count);
    }

    private void OnBreakablePlatformBroken()
    {
        if (!isActive) return;
        session.AddPlatformBroken();
    }

    private void OnDashStarted()
    {
        _isDashActive = true;
        _currentDashKills = 0;
    }

    private void OnDashEnded()
    {
        if (_isDashActive)
        {
            session.UpdateMaxSingleAttackKills(_currentDashKills);
            _isDashActive = false;
            _currentDashKills = 0;
        }
    }

    private void OnAllEnemiesDefeated()
    {
        GameEvents.RaiseAllEnemiesDefeated(session.CurrentStats);
    }

    private void UpdateSessionStats()
    {
        int defeated = totalEnemiesAtStart - activeEnemies.Count;
        session.UpdateEnemyCount(defeated, totalEnemiesAtStart);
    }

    public int GetRemainingEnemies() => activeEnemies.Count;
    public int GetTotalEnemies() => totalEnemiesAtStart;
    public int GetDefeatedEnemies() => totalEnemiesAtStart - activeEnemies.Count;
    public int GetMovesCount() => movesCount;
    public bool HasParryKill() => parryKillRegistered;

    public void Reset()
    {
        activeEnemies.Clear();
        totalEnemiesAtStart = 0;
        movesCount = 0;
        parryKillRegistered = false;
        _isDashActive = false;
        _currentDashKills = 0;
        isActive = false;
    }

    public void Dispose()
    {
        UnsubscribeFromEvents();
        Reset();
    }
}