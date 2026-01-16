using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelObjectives
{
    [Header("PRIMARY")]
    public bool mustDefeatAllEnemies = true;

    [Header("SECONDARY")]
    public bool enableTimeChallenge;
    public float maxTimeAllowed = 30f;

    public bool enableMoveLimitChallenge;
    public int maxAllowedMoves = 0;

    public bool enableParryKillChallenge;

    [Header("SETTINGS")]
    public bool useScriptableObjectOverride = false;
    public ObjectiveData objectivesDataOverride;
}

public class LevelStats
{
    public float timeTaken;
    public int enemiesDefeated;
    public int totalEnemies;

    public int movesUsed;
    public bool parryKillDone;
}

[Serializable]
public class ObjectiveStatus
{
    public bool primaryCompleted;
    public bool timeCompleted;
    public bool movesCompleted;
    public bool parryCompleted;

    public int GetSecondaryCount()
    {
        int count = 0;
        if (timeCompleted) count++;
        if (movesCompleted) count++;
        if (parryCompleted) count++;
        return count;
    }
}

[Serializable]
public class ObjectiveEvaluationResult
{
    public int starsEarned = 0;
    public bool primaryCompleted = false;
    public List<ObjectiveData> completedObjectives = new List<ObjectiveData>();
}

[Serializable]
public class ObjectiveProgressData
{
    public List<SingleObjectiveProgress> objectiveProgresses = new List<SingleObjectiveProgress>();
}

[Serializable]
public class SingleObjectiveProgress
{
    public ObjectiveData objective;
    public float progress;
    public bool isCompleted;
    public bool canBeEvaluated;
}

public class LevelController : MonoBehaviour
{
    [Header("LEVEL SETTINGS")]
    public LevelConfiguration levelConfiguration;

    [Header("AUTO-CONFIG")]
    public bool autoLoadConfiguration = true;

    private ObjectiveEvaluator evaluator;
    private float currentTime;
    private float initialDuration;
    private bool levelCompleted = false;
    private bool levelFailed = false;

    // DEPRECATED
    // public Action<float> OnTimeChanged;

    // DEPRECATED
    // public Action OnTimeExpired;

    private bool isTimerPaused = false;
    public float TimeTaken => levelConfiguration.levelDuration - currentTime;

    private void Start()
    {
        InitializeLevel();
    }

    private void OnEnable()
    {
        GameEvents.OnLevelTimeBonus += OnComboTimeBonus;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelTimeBonus -= OnComboTimeBonus;
    }

    private void InitializeLevel()
    {
        if (autoLoadConfiguration && levelConfiguration == null)
        {
            levelConfiguration = LevelConfigurationManager.Instance?.GetConfigurationForCurrentLevel();
        }

        if (levelConfiguration == null)
        {
            Debug.LogError("[LevelController] No se pudo cargar configuración del nivel");
            return;
        }

        evaluator = new ObjectiveEvaluator(levelConfiguration);

        float baseDuration = levelConfiguration.levelDuration;
        float modifiedDuration = ApplyTimePowerUps(baseDuration);

        currentTime = modifiedDuration;
        initialDuration = baseDuration;

        Debug.Log($"[LevelController] Nivel iniciado - Tiempo: {currentTime:F1}s (base: {baseDuration:F1}s)");

        StartCoroutine(TimerCoroutine());

        ShowPreviousProgress();
    }

    private IEnumerator TimerCoroutine()
    {
        while (currentTime > 0 && !levelCompleted)
        {
            if (!isTimerPaused)
            {
                currentTime -= Time.deltaTime;

                GameEvents.RaiseLevelTimeChanged(currentTime);

                if (currentTime <= 0)
                {
                    GameEvents.RaiseLevelTimeExpired();
                    break;
                }
            }

            yield return null;
        }
    }

    public void StopTimer(bool failed = false)
    {
        levelCompleted = true;
        levelFailed = failed;

        Debug.Log($"[LevelController] Timer detenido - Failed: {failed}");
    }

    public int Evaluate(LevelStats stats)
    {
        if (evaluator == null || levelConfiguration == null)
        {
            Debug.LogWarning("[LevelController] No se puede evaluar - falta configuración");
            return 0;
        }

        if (levelFailed)
        {
            Debug.Log("[LevelController] Nivel fallido - 0 estrellas");
            return 0;
        }

        var result = evaluator.Evaluate(stats);

        SaveManager.Instance?.SaveLevelProgress(levelConfiguration.levelId, result, stats);
        LevelProgressionManager.Instance?.HandleLevelCompletion(levelConfiguration.levelId, result.starsEarned);

        foreach (var completed in result.completedObjectives)
        {
            Debug.Log($"[LevelController] Objetivo completado: {completed.objectiveName}");
        }

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.RecordLevelCompleted(
                levelConfiguration.levelId,
                result.starsEarned,
                stats.timeTaken
            );
        }

        Debug.Log($"[LevelController] Evaluación completa - {result.starsEarned} estrellas");
        return result.starsEarned;
    }

    public ObjectiveEvaluationResult GetDetailedEvaluation(LevelStats stats)
    {
        return evaluator?.Evaluate(stats) ?? new ObjectiveEvaluationResult();
    }

    public ObjectiveProgressData GetCurrentProgress()
    {
        if (evaluator == null) return new ObjectiveProgressData();

        var currentStats = new LevelStats
        {
            timeTaken = TimeTaken,
            movesUsed = MoveTracker.TotalMoves,
            parryKillDone = ParryKillTracker.KillWithParryPerformed,
            enemiesDefeated = GetCurrentEnemiesDefeated(),
            totalEnemies = GetTotalEnemies()
        };

        return evaluator.GetProgressData(currentStats);
    }

    private int GetCurrentEnemiesDefeated()
    {
        int totalEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        return GetTotalEnemies() - totalEnemies;
    }

    private int GetTotalEnemies()
    {
        if (levelConfiguration?.levelContext != null)
            return levelConfiguration.levelContext.totalEnemiesInLevel;

        return GameObject.FindGameObjectsWithTag("Enemy").Length;
    }

    public void AddTime(float seconds)
    {
        if (currentTime > 0)
        {
            currentTime += seconds;

            GameEvents.RaiseLevelTimeChanged(currentTime);

            Debug.Log($"[LevelController] +{seconds:F1}s agregados. Tiempo actual: {currentTime:F1}s");
        }
        else
        {
            Debug.LogWarning($"[LevelController] No se puede agregar tiempo - nivel ya terminado");
        }
    }

    private float ApplyTimePowerUps(float baseDuration)
    {
        float modifiedDuration = baseDuration;

        var powerUpContext = PowerUpManager.Instance?.context;
        if (powerUpContext != null && powerUpContext.ExtraTimeActive)
        {
            float extraPercent = powerUpContext.ExtraTimePercent;
            float bonusTime = baseDuration * extraPercent;
            modifiedDuration += bonusTime;

            Debug.Log($"[LevelController] Power-up ExtraTime activo: +{bonusTime:F1}s ({extraPercent * 100:F0}%)");
        }

        return modifiedDuration;
    }

    public bool CanEvaluateObjectives()
    {
        return levelCompleted && !levelFailed;
    }

    public void MarkLevelAsFailed()
    {
        levelFailed = true;
        levelCompleted = true;

        Debug.Log("[LevelController] Nivel marcado como fallido");

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.TriggerLevelDefeat("levelMarkedAsFailed");
        }
    }

    public LevelProgressData GetLevelProgressSummary()
    {
        if (SaveManager.Instance == null || levelConfiguration == null)
            return new LevelProgressData();

        return SaveManager.Instance.GetLevelProgressData(levelConfiguration.levelId);
    }

    private void ShowPreviousProgress()
    {
        if (SaveManager.Instance == null || levelConfiguration == null) return;

        var previousProgress = SaveManager.Instance.GetLevelProgressData(levelConfiguration.levelId);

        if (previousProgress.completedObjectiveIds.Count > 0)
        {
            Debug.Log($"[LevelController] Objetivos previos completados ({previousProgress.completedObjectiveIds.Count}):");
            foreach (var objectiveId in previousProgress.completedObjectiveIds)
            {
                Debug.Log($"  - {objectiveId}");
            }
        }
    }

    public void PauseTimer()
    {
        isTimerPaused = true;
        Debug.Log("[LevelController] Timer pausado");
    }

    public void ResumeTimer()
    {
        isTimerPaused = false;
        Debug.Log("[LevelController] Timer reanudado");
    }

    public bool IsTimerPaused()
    {
        return isTimerPaused;
    }

    private void OnComboTimeBonus(float bonusSeconds)
    {
        AddTime(bonusSeconds);
        Debug.Log($"[LevelController] Bonus de combo aplicado: +{bonusSeconds:F1}s");
    }
}