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

        Debug.Log("[LevelSessionManager] Inicializado - Persistente entre escenas");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        GameEvents.OnAllEnemiesDefeated += OnAllEnemiesDefeated;
        GameEvents.OnLevelTimeExpired += OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus += OnComboTimeBonus;

        Debug.Log("[LevelSessionManager] Eventos suscritos");
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

        Debug.LogWarning($"[LevelSessionManager] No se pudo extraer ID del nivel de: {sceneName}");
        return 1;
    }

    private void InitializeLevelSession(int levelId)
    {
        CleanupSession();

        LevelConfiguration config = LevelConfigurationManager.Instance?.GetConfigurationForLevel(levelId);

        if (config == null)
        {
            Debug.LogError($"[LevelSessionManager] No se encontró configuración para nivel {levelId}");
            return;
        }

        Debug.Log($"[LevelSessionManager] === INICIALIZANDO NIVEL {levelId} ===");

        currentSession = new LevelSession(config);

        trackingService = new TrackingService(currentSession);
        timerService = new LevelTimerService(currentSession, this);
        objectiveService = new ObjectiveService(config);

        trackingService.Initialize();
        timerService.Initialize();
        currentSession.Initialize();

        isLevelActive = true;

        Debug.Log($"[LevelSessionManager] Sesión de nivel {levelId} creada y lista");
    }

    public void StartLevel()
    {
        if (currentSession == null)
        {
            Debug.LogError("[LevelSessionManager] No hay sesión activa para iniciar");
            return;
        }

        if (currentSession.State != LevelSessionState.Ready)
        {
            Debug.LogWarning($"[LevelSessionManager] No se puede iniciar - Estado: {currentSession.State}");
            return;
        }

        currentSession.Start();
        timerService.Start();

        GameEvents.RaiseLevelStarted();

        Debug.Log($"[LevelSessionManager] Nivel {currentSession.LevelId} INICIADO");
    }

    public void PauseLevel()
    {
        if (!HasActiveSession) return;

        currentSession.Pause();
        timerService.Pause();

        Debug.Log($"[LevelSessionManager] Nivel pausado");
    }

    public void ResumeLevel()
    {
        if (!HasActiveSession) return;

        currentSession.Resume();
        timerService.Resume();

        Debug.Log($"[LevelSessionManager] Nivel resumido");
    }

    private void OnAllEnemiesDefeated(LevelStats stats)
    {
        if (!HasActiveSession) return;

        if (GameManager.Instance != null && GameManager.Instance.PlayerHasDied)
        {
            Debug.LogWarning("[LevelSessionManager] Jugador murió, no se completa el nivel");
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

        Debug.Log($"[LevelSessionManager] ¡NIVEL COMPLETADO!");
    }

    private void OnLevelTimeExpired()
    {
        if (!HasActiveSession) return;

        FailLevel("Tiempo agotado");
    }

    public void FailLevel(string reason)
    {
        if (!HasActiveSession) return;

        timerService.Stop();
        currentSession.Fail(reason);

        GameEvents.RaiseLevelFailed(reason);

        Debug.Log($"[LevelSessionManager] Nivel FALLIDO - Razón: {reason}");
    }

    private void EvaluateAndSave()
    {
        if (currentSession == null || !currentSession.CanEvaluate)
        {
            Debug.LogWarning("[LevelSessionManager] No se puede evaluar la sesión");
            return;
        }

        var stats = currentSession.CurrentStats;
        var result = objectiveService.Evaluate(stats);

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

        Debug.Log($"[LevelSessionManager] Evaluación guardada - {result.starsEarned} estrellas");
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
            Debug.Log($"[LevelSessionManager] Limpiando sesión del nivel {currentSession.LevelId}");

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