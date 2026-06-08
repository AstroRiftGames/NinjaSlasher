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
    private bool gameplayMusicStarted;
    private bool _pendingVictory;
    private PlayerController _playerController;

    public LevelSession CurrentSession => currentSession;
    public bool HasActiveSession => currentSession != null && !currentSession.IsComplete && !currentSession.IsFailed;
    public bool IsSessionRunning => currentSession != null && currentSession.IsRunning;
    public bool IsSessionPaused => currentSession != null && currentSession.State == LevelSessionState.Paused;
    public bool IsLevelActive => isLevelActive;
    public bool CanProcessGameplay => IsSessionRunning;

    public override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        GameEvents.OnAllEnemiesDefeated += OnAllEnemiesDefeated;
        GameEvents.OnDashEnded += OnDashEnded;
        GameEvents.OnLevelTimeExpired += OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus += OnComboTimeBonus;

        UIEvents.OnRestartLevelRequested += OnRestartLevelRequested;
        UIEvents.OnRetryButtonPressed += OnRetryButtonPressed;
        UIEvents.OnQuitToMenuPressed += OnQuitToMenuPressed;
        SubscribeToPauseController();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameEvents.OnAllEnemiesDefeated -= OnAllEnemiesDefeated;
        GameEvents.OnDashEnded -= OnDashEnded;
        GameEvents.OnLevelTimeExpired -= OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus -= OnComboTimeBonus;

        UIEvents.OnRestartLevelRequested -= OnRestartLevelRequested;
        UIEvents.OnRetryButtonPressed -= OnRetryButtonPressed;
        UIEvents.OnQuitToMenuPressed -= OnQuitToMenuPressed;
        UnsubscribeFromPauseController();
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

        // Fallback silencioso corregido: ahora loggea warning para que la contaminación
        // sea detectable. Si este warning aparece, revisar la convención de nombre de escena.
        Debug.LogWarning($"[LevelSessionManager] No se pudo extraer levelId de '{sceneName}'. " +
                         $"Usando fallback 1, lo que puede contaminar analytics si no es nivel 1.");
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
        ResolvePlayerControllerReference();
        EnsureGameplayMusicStarted();

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

        if (!TryAuthorizeLevelAttempt())
        {
            LogDebug("[LevelSessionManager] StartLevel blocked by life validation.");
            return;
        }

        currentSession.Start();
        timerService.Start();

        EnsureGameplayMusicStarted();

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.RecordLevelStart(
                currentSession.LevelId,
                PowerUpManager.Instance?.GetActivePowerUpsString() ?? ""
            );
        }

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

    public void EnsureGameplayMusicStarted()
    {
        if (gameplayMusicStarted)
            return;

        gameplayMusicStarted = TryPlayGameplayMusic();
    }

    private void OnAllEnemiesDefeated(LevelStats stats)
    {
        if (!IsSessionRunning) return;

        _pendingVictory = true;
        LogDebug("[LevelSessionManager] All enemies defeated -> pending victory");
        TryCompletePendingVictory();
    }

    private void CompleteLevel()
    {
        if (!IsSessionRunning) return;

        _pendingVictory = false;
        timerService.Stop();
        currentSession.Complete();

        EvaluateAndSave();
    }

    private void OnDashEnded()
    {
        if (!_pendingVictory) return;

        LogDebug("[LevelSessionManager] Dash ended -> retry pending victory");
        TryCompletePendingVictory();
    }

    private void OnLevelTimeExpired()
    {
        if (!IsSessionRunning) return;

        FailLevel("timeExpired");
    }

    public void FailLevel(string reason)
    {
        LogDebug($"[LSM] FailLevel | reason={reason} | HasActiveSession={HasActiveSession} | session={currentSession != null} | IsFailed={currentSession?.IsFailed} | IsComplete={currentSession?.IsComplete}");
        if (_pendingVictory)
        {
            LogDebug($"[LevelSessionManager] Pending victory cancelled by defeat | reason={reason}");
        }
        _pendingVictory = false;
        if (!CanFailCurrentSession()) return;

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
        GameEvents.RaiseLevelEnded(LevelResult.Defeat);
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
        ProgressionUnlockResult progressionResult = LevelProgressionManager.Instance?.HandleLevelCompletion(currentSession.LevelId, result.starsEarned);
        UIEvents.SetVictoryContext(BuildVictoryContext(currentSession, result.starsEarned, progressionResult));

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.RecordLevelCompleted(
                currentSession.LevelId,
                result.starsEarned,
                stats.timeTaken
            );

            AnalyticsManager.Instance.RecordLevelMechanicsSummary(
                currentSession.LevelId,
                "completed",
                GetAttemptNumber(currentSession.LevelId),
                stats.movesUsed,
                stats.maxComboLevelReached,
                stats.enemiesDefeated,
                stats.totalEnemies,
                stats.reflectedProjectileKills,
                stats.maxEnemiesKilledInSingleAttack,
                PowerUpManager.Instance?.GetActivePowerUpsString() ?? ""
            );
        }

        GameEvents.RaiseLevelCompleted(stats);
        GameEvents.RaiseLevelEnded(LevelResult.Victory);
    }

    private VictoryContext BuildVictoryContext(LevelSession session, int starsEarned, ProgressionUnlockResult progressionResult)
    {
        bool isBossLevel = session != null &&
                           session.Configuration != null &&
                           session.Configuration.unlockRequirements != null &&
                           session.Configuration.unlockRequirements.isBossLevel;

        var context = new VictoryContext
        {
            Variant = isBossLevel ? VictoryModalVariant.BossClear : VictoryModalVariant.Normal,
            IsBossLevel = isBossLevel,
            StarsEarned = starsEarned
        };

        if (isBossLevel && session.Configuration.bossPreGameData != null)
            context.BossVictoryMessage = session.Configuration.bossPreGameData.victoryMessage;

        if (progressionResult != null && progressionResult.UnlockedNewArea)
        {
            context.UnlockedNewArea = true;
            context.UnlockedAreaId = progressionResult.UnlockedAreaId;
            context.UnlockedAreaName = progressionResult.UnlockedAreaName;
        }

        return context;
    }

    private void OnComboTimeBonus(float bonusSeconds)
    {
        if (!IsSessionRunning) return;

        timerService?.AddTime(bonusSeconds);
    }

    private void TryCompletePendingVictory()
    {
        if (!_pendingVictory) return;

        if (!CanConfirmVictoryNow())
            return;

        LogDebug("[LevelSessionManager] Pending victory confirmed");
        CompleteLevel();
    }

    private bool CanConfirmVictoryNow()
    {
        if (!IsSessionRunning)
        {
            LogDebug("[LevelSessionManager] Pending victory blocked because session is not running");
            return false;
        }

        if (GameManager.Instance != null && GameManager.Instance.PlayerHasDied)
        {
            LogDebug("[LevelSessionManager] Pending victory blocked because player has died");
            return false;
        }

        if (trackingService == null || trackingService.GetRemainingEnemies() > 0)
        {
            LogDebug("[LevelSessionManager] Pending victory blocked because enemies still remain");
            return false;
        }

        if (!TryResolvePlayerControllerReference())
        {
            LogDebug("[LevelSessionManager] Pending victory blocked because PlayerController was not found");
            return false;
        }

        if (_playerController.IsDashing)
        {
            LogDebug("[LevelSessionManager] Pending victory blocked because player is dashing");
            return false;
        }

        if (_playerController.IsDeadOrDying)
        {
            LogDebug("[LevelSessionManager] Pending victory blocked because player is dead or dying");
            return false;
        }

        return true;
    }

    public void RegisterMove()
    {
        if (!IsSessionRunning) return;
        trackingService?.RegisterMove();
    }

    public void RegisterParryKill()
    {
        if (!IsSessionRunning) return;
        trackingService?.RegisterParryKill();
    }

    public void RegisterEnemyKilled(Enemy enemy)
    {
        if (!IsSessionRunning) return;
        trackingService?.RegisterEnemyKilled(enemy);
    }

    public void RegisterSpawnedEnemy(Enemy enemy)
    {
        if (!IsSessionRunning) return;
        trackingService?.RegisterSpawnedEnemy(enemy);
    }

    public ObjectiveProgressData GetCurrentProgress()
    {
        if (currentSession == null || objectiveService == null)
        {
            return new ObjectiveProgressData();
        }

        return objectiveService.GetProgressData(currentSession.CurrentStats);
    }

    private int GetAttemptNumber(int levelId)
    {
        if (SaveManager.Instance == null || levelId <= 0) return 1;
        return SaveManager.Instance.GetGameData().GetLevelProgress(levelId).totalAttempts + 1;
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

    public bool CanStartLevelAttempt(bool includePendingExitCost = false)
    {
        LifeManager lifeManager = LifeManager.Instance;
        if (lifeManager == null)
        {
            Debug.LogWarning("[LevelSessionManager] LifeManager no disponible para validar inicio/reinicio de nivel.");
            return false;
        }

        return includePendingExitCost
            ? lifeManager.CanPlayAfterConfirmingPendingDeduction()
            : lifeManager.CanPlay();
    }

    public bool TryAuthorizeLevelAttempt(bool includePendingExitCost = false)
    {
        if (CanStartLevelAttempt(includePendingExitCost))
            return true;

        UIEvents.RequestShowNoLivesModal();
        return false;
    }

    private bool CanFailCurrentSession()
    {
        if (currentSession == null || currentSession.IsComplete || currentSession.IsFailed)
            return false;

        return currentSession.State == LevelSessionState.Running ||
               currentSession.State == LevelSessionState.Paused;
    }

    private void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused)
        {
            PauseLevel();
        }
        else
        {
            ResumeLevel();
        }
    }

    private void SubscribeToPauseController()
    {
        if (PauseController.Instance == null)
            return;

        PauseController.Instance.PauseStateChanged -= OnPauseStateChanged;
        PauseController.Instance.PauseStateChanged += OnPauseStateChanged;
        OnPauseStateChanged(PauseController.Instance.IsPaused);
    }

    private void UnsubscribeFromPauseController()
    {
        if (PauseController.Instance == null)
            return;

        PauseController.Instance.PauseStateChanged -= OnPauseStateChanged;
    }

    private void OnRetryButtonPressed()
    {
        if (!IsLevelScene(SceneManager.GetActiveScene().name) || LifeManager.Instance == null)
            return;

        if (!TryAuthorizeLevelAttempt())
            return;

        CloseSessionForSceneChange();
        UIEvents.RequestSceneTransition(SceneManager.GetActiveScene().name);
    }

    private void OnRestartLevelRequested()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!IsLevelScene(sceneName))
            return;

        bool includePendingExitCost = LifeManager.Instance != null && LifeManager.Instance.HasPendingDeduction();

        if (!TryAuthorizeLevelAttempt(includePendingExitCost))
            return;

        CloseSessionForSceneChange();

        UIEvents.RequestSceneTransition(SceneManager.GetActiveScene().name);
    }

    private void OnQuitToMenuPressed()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.RecordScreenTransition(currentScene, "LevelSelection");
        }

        CloseSessionForSceneChange();
        SaveManager.Instance?.SaveData();
        UIEvents.RequestLoadLevelSelectorScene();
    }

    private void CloseSessionForSceneChange()
    {
        if (LifeManager.Instance != null && LifeManager.Instance.HasPendingDeduction())
        {
            LifeManager.Instance.OnLevelExit();
        }

        CleanupSession();
    }

    private void CleanupSession()
    {
        if (currentSession != null)
        {
            trackingService?.Dispose();
            timerService?.Dispose();
            currentSession?.Clear();
            GameEvents.RaiseLevelSessionClosed();

            currentSession = null;
            trackingService = null;
            timerService = null;
            objectiveService = null;
            _pendingVictory = false;
            _playerController = null;

            isLevelActive = false;
            gameplayMusicStarted = false;
        }
        else
        {
            _pendingVictory = false;
            _playerController = null;
        }
    }

    private void ResolvePlayerControllerReference()
    {
        if (TryResolvePlayerControllerReference())
            return;

        Debug.LogWarning("[LevelSessionManager] PlayerController no encontrado al inicializar la sesión.");
    }

    private bool TryResolvePlayerControllerReference()
    {
        if (_playerController != null)
            return true;

        _playerController = Object.FindFirstObjectByType<PlayerController>();
        return _playerController != null;
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log(message);
    }

    private bool TryPlayGameplayMusic()
    {
        var config = currentSession?.Configuration;
        if (config == null || AudioService.Instance == null)
            return false;

        if (config.unlockRequirements.isBossLevel)
        {
            if (config.bossMusic == null)
            {
                return false;
            }

            AudioService.Instance.PlayMusic(config.bossMusic);
            return true;
        }
        else
        {
            if (config.gameplayMusic == null)
            {
                return false;
            }

            AudioService.Instance.PlayMusic(config.gameplayMusic);
            return true;
        }
    }

    protected override void OnDestroy()
    {
        CleanupSession();
        base.OnDestroy();
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
