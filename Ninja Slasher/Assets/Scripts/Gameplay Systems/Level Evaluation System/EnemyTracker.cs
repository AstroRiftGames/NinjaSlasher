using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class EnemyTracker : MonoBehaviour, ITracker
{
    private List<Enemy> enemies = new();
    private float startTime;
    private int totalEnemies;

    public static event Action<LevelStats> OnAllEnemiesDefeated;

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
            if (LevelManager.Instance != null && LevelManager.Instance.PlayerHasDied)
            {
                return;
            }

            StartCoroutine(PlayVictorySoundAndShowResults());
        }
    }

    private IEnumerator PlayVictorySoundAndShowResults()
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Victory);

        float soundDuration = AudioManager.Instance.GetSFXDuration(SFXClip.UI_Victory);

        if (soundDuration <= 0f)
        {
            soundDuration = 2.0f;
        }

        yield return new WaitForSeconds(soundDuration);

        float elapsedTime = Time.time - startTime;
        LevelStats stats = new LevelStats
        {
            timeTaken = elapsedTime,
            enemiesDefeated = totalEnemies,
            totalEnemies = totalEnemies
        };

        OnAllEnemiesDefeated?.Invoke(stats);
    }

    public void ResetTracker()
    {
        enemies.Clear();
        totalEnemies = 0;
        startTime = 0;
    }
}