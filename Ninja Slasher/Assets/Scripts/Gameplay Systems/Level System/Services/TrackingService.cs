using System.Collections.Generic;
using UnityEngine;

public class TrackingService
{
    private LevelSession session;
    private LevelConfiguration config;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private int totalEnemiesAtStart;

    private int movesCount;
    private bool parryKillRegistered;

    private bool isActive;

    public TrackingService(LevelSession levelSession)
    {
        session = levelSession;
        config = levelSession.Configuration;
        isActive = false;

        Debug.Log($"[TrackingService] Servicio creado para nivel {session.LevelId}");
    }

    public void Initialize()
    {
        RegisterEnemies();

        movesCount = 0;
        parryKillRegistered = false;

        SubscribeToEvents();

        isActive = true;

        UpdateSessionStats();

        Debug.Log($"[TrackingService] Inicializado - {totalEnemiesAtStart} enemigos registrados");
    }

    private void RegisterEnemies()
    {
        Enemy[] foundEnemies = Object.FindObjectsOfType<Enemy>();
        activeEnemies.Clear();
        activeEnemies.AddRange(foundEnemies);
        totalEnemiesAtStart = activeEnemies.Count;

        if (config.levelContext != null)
        {
            config.levelContext.Initialize(totalEnemiesAtStart);
        }

        Debug.Log($"[TrackingService] {totalEnemiesAtStart} enemigos encontrados en la escena");
    }

    private void SubscribeToEvents()
    {
        GameEvents.OnEnemyDefeated += OnEnemyDefeated;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnEnemyDefeated -= OnEnemyDefeated;
    }

    public void RegisterMove()
    {
        if (!isActive) return;

        movesCount++;
        session.IncrementMoves();

        Debug.Log($"[TrackingService] Movimiento registrado - Total: {movesCount}");
    }

    public void RegisterParryKill()
    {
        if (!isActive) return;

        if (!parryKillRegistered)
        {
            parryKillRegistered = true;
            session.RegisterParryKill();

            Debug.Log($"[TrackingService] Parry kill registrado");
        }
    }

    public void RegisterEnemyKilled(Enemy enemy)
    {
        if (!isActive) return;

        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);

            int defeated = totalEnemiesAtStart - activeEnemies.Count;
            UpdateSessionStats();

            Debug.Log($"[TrackingService] Enemigo eliminado - Progreso: {defeated}/{totalEnemiesAtStart}");

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

    private void OnAllEnemiesDefeated()
    {
        Debug.Log($"[TrackingService] ¡Todos los enemigos eliminados!");

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
        isActive = false;

        Debug.Log($"[TrackingService] Servicio reseteado");
    }

    public void Dispose()
    {
        UnsubscribeFromEvents();
        Reset();

        Debug.Log($"[TrackingService] Servicio destruido");
    }
}