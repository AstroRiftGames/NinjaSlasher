using UnityEngine;
using System.Collections.Generic;
using System;

public class EnemyTracker : MonoBehaviour, ITracker
{
    private List<Enemy> enemies = new();
    private float startTime;
    private int totalEnemies;
    private bool levelEnded = false;

    [Obsolete]
    public static event Action<LevelStats> OnAllEnemiesDefeated;

    void Start()
    {
        Enemy[] found = FindObjectsOfType<Enemy>();
        enemies.AddRange(found);
        totalEnemies = enemies.Count;
        startTime = Time.time;
        levelEnded = false;

        Debug.Log($"[EnemyTracker] Nivel iniciado con {totalEnemies} enemigos");
    }

    public void OnEnemyKilled(Enemy enemy)
    {
        enemies.Remove(enemy);

        int remainingEnemies = enemies.Count;

        GameEvents.RaiseEnemyDefeated(totalEnemies - remainingEnemies);

        Debug.Log($"[EnemyTracker] Enemigo eliminado. Restantes: {remainingEnemies}/{totalEnemies}");

        if (remainingEnemies <= 0)
        {
            HandleAllEnemiesDefeated();
        }
    }

    private void HandleAllEnemiesDefeated()
    {
        if (levelEnded)
        {
            Debug.LogWarning("[EnemyTracker] Nivel ya terminado, ignorando invocación adicional");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.PlayerHasDied)
        {
            Debug.LogWarning("[EnemyTracker] Jugador murió, no se completa el nivel");
            return;
        }

        levelEnded = true;

        float elapsedTime = Time.time - startTime;
        LevelStats stats = new LevelStats
        {
            timeTaken = elapsedTime,
            enemiesDefeated = totalEnemies,
            totalEnemies = totalEnemies
        };

        Debug.Log($"[EnemyTracker] Todos los enemigos derrotados Tiempo: {elapsedTime:F2}s");

        GameEvents.RaiseAllEnemiesDefeated(stats);

        // DEPRECATED
        // OnAllEnemiesDefeated?.Invoke(stats);
    }

    public void ResetTracker()
    {
        enemies.Clear();
        totalEnemies = 0;
        startTime = 0;
        levelEnded = false;

        Debug.Log("[EnemyTracker] Tracker reseteado");
    }

    public int GetRemainingEnemies() => enemies.Count;
    public int GetTotalEnemies() => totalEnemies;
    public float GetElapsedTime() => Time.time - startTime;
    public bool IsLevelComplete() => levelEnded;
}