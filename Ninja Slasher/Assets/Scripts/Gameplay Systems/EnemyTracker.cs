using UnityEngine;
using System.Collections.Generic;

public class EnemyTracker : MonoBehaviour, ITracker
{
    private List<Enemy> enemies = new();
    private float startTime;
    private int totalEnemies;

    public static event System.Action<LevelStats> OnAllEnemiesDefeated;

    void Start()
    {
        Enemy[] found = FindObjectsOfType<Enemy>();
        enemies.AddRange(found);
        totalEnemies = enemies.Count;
        startTime = Time.time;
    }

    public void OnEnemyKilled(Enemy enemy)
    {
        enemies.Remove(enemy);

        if (enemies.Count <= 0)
        {
            float elapsedTime = Time.time - startTime;
            LevelStats stats = new LevelStats
            {
                timeTaken = elapsedTime,
                enemiesDefeated = totalEnemies,
                totalEnemies = totalEnemies
            };
            OnAllEnemiesDefeated?.Invoke(stats);
        }
    }

    public void ResetTracker()
    {
        enemies.Clear();
        totalEnemies = 0;
        startTime = 0;
    }
}