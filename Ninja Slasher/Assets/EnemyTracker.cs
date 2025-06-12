using UnityEngine;
using System.Collections.Generic;

public class EnemyTracker : MonoBehaviour
{
    private List<Enemy> enemies = new();
    private float startTime;
    private int totalEnemies;

    [SerializeField] private GameManager gameManager;

    void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

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
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        float elapsedTime = Time.time - startTime;

        LevelStats stats = new LevelStats
        {
            timeTaken = elapsedTime,
            enemiesDefeated = totalEnemies,
            totalEnemies = totalEnemies
        };

        gameManager.OnLevelCompleted(stats);
    }
}
