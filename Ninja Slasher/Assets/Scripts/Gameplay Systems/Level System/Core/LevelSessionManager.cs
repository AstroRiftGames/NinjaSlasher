using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSessionManager : MonoBehaviourSingleton<LevelSessionManager>
{
    [Header("DEBUG")]
    [SerializeField] private bool enableDebugLogs = true;

    private LevelSession currentSession;
    private TrackingService trackingService;
    private LevelTimerService timerService;
    private ObjectiveService objectiveService;

    private bool isLevelActive;

    public LevelSession CurrentSession => currentSession;
    public bool HasActiveSession => currentSession != null && !currentSession.IsComplete && !currentSession.IsFailed;
    public bool IsLevelActive => isLevelActive;

    public override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        GameEvents.OnAllEnemiesDefeated += OnAllEnemiesDefeated;
        GameEvents.OnLevelTimeExpired += OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus += OnComboTimeBonus;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameEvents.OnAllEnemiesDefeated -= OnAllEnemiesDefeated;
        GameEvents.OnLevelTimeExpired -= OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus -= OnComboTimeBonus;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsLevelScene(scene.name))
        {
            int levelId = ExtractLevelId(scene.name);
            InitializeLevelSession(levelId);
        }
        else
        {
            CleanupSession();
        }
    }

    private bool IsLevelScene(string sceneName)
    {
        return sceneName.StartsWith("Level_") || sceneName.Contains("Level");
    }

    private int ExtractLevelId(string sceneName)
    {
        string cleanName = sceneName.Replace("Level_", "").Replace("Level", "");

        if (int.TryParse(cleanName, out int levelId))
        {
            return levelId;
        }

        return 1;
    }

    private void InitializeLevelSession(int levelId)
    {
        CleanupSession();

        LevelConfiguration config = LevelConfigurationManager.Instance?.GetConfigurationForLevel(levelId);

        if (config == null)
        {
            return;
        }

        currentSession = new LevelSession(config);

        trackingService = new TrackingService(currentSession);
        trackingService.Initialize();
        timerService = new LevelTimerService(currentSession, this);

        try
        {
            objectiveService = new ObjectiveService(config);

            if (objectiveService == null)
            {
                Debug.LogError($"[LevelSessionManager] ObjectiveService es null después de crearlo para nivel {levelId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelSessionManager] Stack trace: {e.StackTrace}");
        }

        timerService.Initialize();
        currentSession.Initialize();

        isLevelActive = true;
    }

    public void StartLevel()
    {
        if (currentSession == null)
        {
            return;
        }

        if (currentSession.State != LevelSessionState.Ready)
        {
            return;
        }

        currentSession.Start();
        timerService.Start();

        PlayGameplayMusic();

        GameEvents.RaiseLevelStarted();
    }

    public void PauseLevel()
    {
        if (!HasActiveSession) return;

        currentSession.Pause();
        timerService.Pause();
    }

    public void ResumeLevel()
    {
        if (!HasActiveSession) return;

        currentSession.Resume();
        timerService.Resume();
    }

    private void OnAllEnemiesDefeated(LevelStats stats)
    {
        if (!HasActiveSession) return;

        if (GameManager.Instance != null && GameManager.Instance.PlayerHasDied)
        {
            return;
        }

        CompleteLevel();
    }

    private void CompleteLevel()
    {
        if (!HasActiveSession) return;

        timerService.Stop();
        currentSession.Complete();

        EvaluateAndSave();
    }

    private void OnLevelTimeExpired()
    {
        if (!HasActiveSession) return;

        FailLevel("Tiempo agotado");
    }

    public void FailLevel(string reason)
    {
        Debug.Log($"[LSM] FailLevel | reason={reason} | HasActiveSession={HasActiveSession} | session={currentSession != null} | IsFailed={currentSession?.IsFailed} | IsComplete={currentSession?.IsComplete}");
        if (!HasActiveSession) return;

        timerService.Stop();
        currentSession.Fail(reason);

        var context = new LevelFailedContext
        {
            Reason            = reason,
            LevelId           = currentSession.LevelId,
            IsBossLevel       = currentSession.Configuration?.unlockRequirements.isBossLevel ?? false,
            ConsecutiveLosses = (LifeManager.Instance != null ? LifeManager.Instance.ConsecutiveLosses : 0) + 1,
            LivesRemaining    = LifeManager.Instance != null ? LifeManager.Instance.GetRealLives() : 0,
        };

        GameEvents.RaiseLevelFailed(context);

        Debug.Log("[EBS] No funca");
    }

    private void EvaluateAndSave()
    {
        if (currentSession == null || !currentSession.CanEvaluate)
        {
            return;
        }

        if (objectiveService == null)
        {
            return;
        }

        var stats = currentSession.CurrentStats;

        if (ComboManager.Instance != null)
        {
            stats.maxComboActiveDuration = ComboManager.Instance.MaxComboDuration;
            stats.maxComboLevelReached   = ComboManager.Instance.MaxComboLevelReached;
        }

        var result = objectiveService.Evaluate(stats);

        stats.starsEarned = result.starsEarned;

        SaveManager.Instance?.SaveLevelProgress(currentSession.LevelId, result, stats);
        LevelProgressionManager.Instance?.HandleLevelCompletion(currentSession.LevelId, result.starsEarned);

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.RecordLevelCompleted(
                currentSession.LevelId,
                result.starsEarned,
                stats.timeTaken
            );
        }

        GameEvents.RaiseLevelCompleted(stats);
    }

    private void OnComboTimeBonus(float bonusSeconds)
    {
        if (!HasActiveSession) return;

        timerService?.AddTime(bonusSeconds);
    }

    public void RegisterMove()
    {
        if (!HasActiveSession) return;
        trackingService?.RegisterMove();
    }

    public void RegisterParryKill()
    {
        if (!HasActiveSession) return;
        trackingService?.RegisterParryKill();
    }

    public void RegisterEnemyKilled(Enemy enemy)
    {
        if (!HasActiveSession) return;
        trackingService?.RegisterEnemyKilled(enemy);
    }

    public ObjectiveProgressData GetCurrentProgress()
    {
        if (currentSession == null || objectiveService == null)
        {
            return new ObjectiveProgressData();
        }

        return objectiveService.GetProgressData(currentSession.CurrentStats);
    }

    public LevelStats GetCurrentStats()
    {
        return currentSession?.CurrentStats ?? new LevelStats();
    }

    public float GetCurrentTime()
    {
        return timerService?.CurrentTime ?? 0f;
    }

    public float GetTimeTaken()
    {
        return timerService?.TimeTaken ?? 0f;
    }

    public bool IsPaused()
    {
        return timerService?.IsPaused ?? false;
    }

    private void CleanupSession()
    {
        if (currentSession != null)
        {
            trackingService?.Dispose();
            timerService?.Dispose();
            currentSession?.Clear();

            currentSession = null;
            trackingService = null;
            timerService = null;
            objectiveService = null;

            isLevelActive = false;
        }
    }

    private void PlayGameplayMusic()
    {
        var config = currentSession?.Configuration;
        if (config == null) return;

        if (config.unlockRequirements.isBossLevel)
        {
            if (config.bossMusic == null)
            {
                Debug.LogWarning(
                    $"[LevelSessionManager] Nivel {currentSession.LevelId} ({config.name}): " +
                    $"isBossLevel=true pero el campo 'bossMusic' no está asignado. " +
                    $"Asigna un AudioEvent en el LevelConfiguration SO de este nivel.",
                    config);
                return;
            }

            AudioService.Instance.PlayMusic(config.bossMusic);
        }
        else
        {
            if (config.gameplayMusic == null)
            {
                Debug.LogWarning(
                    $"[LevelSessionManager] Nivel {currentSession.LevelId} ({config.name}): " +
                    $"El campo 'gameplayMusic' no está asignado. " +
                    $"Asigna un AudioEvent en el LevelConfiguration SO de este nivel.",
                    config);
                return;
            }

            AudioService.Instance.PlayMusic(config.gameplayMusic);
        }
    }

    private void OnDestroy()
    {
        CleanupSession();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Mostrar Estado Actual")]
    private void DebugShowCurrentState()
    {
        if (currentSession == null)
        {
            Debug.Log("=== NO HAY SESIÓN ACTIVA ===");
            return;
        }

        Debug.Log("=== ESTADO DE SESIÓN ACTUAL ===");
        Debug.Log($"Nivel ID: {currentSession.LevelId}");
        Debug.Log($"Estado: {currentSession.State}");
        Debug.Log($"Tiempo transcurrido: {timerService?.TimeTaken:F2}s");
        Debug.Log($"Enemigos: {trackingService?.GetDefeatedEnemies()}/{trackingService?.GetTotalEnemies()}");
        Debug.Log($"Movimientos: {trackingService?.GetMovesCount()}");
        Debug.Log($"Parry kill: {trackingService?.HasParryKill()}");
        Debug.Log("================================");
    }

    [ContextMenu("Debug/Completar Nivel Forzado")]
    private void DebugForceComplete()
    {
        if (HasActiveSession)
        {
            CompleteLevel();
        }
    }

    [ContextMenu("Debug/Fallar Nivel Forzado")]
    private void DebugForceFail()
    {
        if (HasActiveSession)
        {
            FailLevel("Debug forzado");
        }
    }
#endif
}