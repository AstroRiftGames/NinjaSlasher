using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    [Header("Testing")]
    [SerializeField] private string[] testingScenes = { "TestScene" };

    private LevelStats currentStats;
    private bool _playerHasDied;
    private bool _pausedByFocusLoss = false;

    public bool PlayerHasDied => _playerHasDied;

    private bool _isVictory = false;
    public bool IsVictory => _isVictory;

    public override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        GameEvents.OnLivesChanged += OnLivesChanged;

        _playerHasDied = false;
    }

    private void OnEnable()
    {
        GameEvents.OnLevelStarted += OnLevelStarted;
        GameEvents.OnLevelCompleted += OnLevelCompleted;
        GameEvents.OnLevelFailed += OnLevelFailed;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLivesChanged -= OnLivesChanged;
        GameEvents.OnLevelStarted -= OnLevelStarted;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        if (scene.name.Contains("Level"))
        {
            ResetLevelState();
        }
    }

    private void OnLevelStarted()
    {
        _playerHasDied = false;
        _isVictory = false;
        LifeManager.Instance.OnLevelStart();
    }

    private void OnLevelCompleted(LevelStats stats)
    {
        _isVictory = true;

        if (IsTestingScene()) return;

        GameEvents.RaiseLevelEndedConsumePowerUps();

        currentStats = stats;
        LifeManager.Instance.OnLevelCompleted();

        StartCoroutine(HandleVictoryWithDelay());
    }

    private IEnumerator HandleVictoryWithDelay()
    {
        yield return new WaitForSeconds(0.1f);

        UIEvents.RequestShowVictoryModal();
    }

    private void HandleLevelDefeat(string reason = "unknown")
    {
        _isVictory = false;

        if (AnalyticsManager.Instance != null)
        {
            int currentLevelId = GetLevelIdForAnalytics();
            float attemptTime = LevelSessionManager.Instance?.GetTimeTaken() ?? 0f;

            int attemptNumber = 1;
            if (SaveManager.Instance != null && currentLevelId > 0)
            {
                var progress = SaveManager.Instance.GetGameData().GetLevelProgress(currentLevelId);
                attemptNumber = progress.totalAttempts + 1;
                int capturedLevelId = currentLevelId;
                SaveManager.Instance.Modify(d => d.GetLevelProgress(capturedLevelId).totalAttempts++);
            }

            AnalyticsManager.Instance.RecordLevelFailed(
                currentLevelId,
                reason,
                attemptTime,
                attemptNumber
            );

            var defeatStats = LevelSessionManager.Instance?.GetCurrentStats() ?? new LevelStats();
            AnalyticsManager.Instance.RecordLevelMechanicsSummary(
                currentLevelId,
                "failed",
                attemptNumber,
                defeatStats.movesUsed,
                defeatStats.maxComboLevelReached,
                defeatStats.enemiesDefeated,
                defeatStats.totalEnemies,
                defeatStats.reflectedProjectileKills,
                defeatStats.maxEnemiesKilledInSingleAttack,
                PowerUpManager.Instance?.GetActivePowerUpsString() ?? ""
            );
        }

        LifeManager.Instance.UseLife();

        StartCoroutine(HandleDefeatUIWithDelay());
    }

    private IEnumerator HandleDefeatUIWithDelay()
    {
        yield return new WaitForSeconds(0.1f);

        if (EmergencyBundleService.Instance != null && EmergencyBundleService.Instance.HasActiveOffer)
            yield break;

        int currentLives = LifeManager.Instance.GetRealLives();
        bool canPlay = LifeManager.Instance.CanPlay();

        Debug.Log($"[GameManager] Resolve defeat UI | realLives={currentLives} | canPlay={canPlay} | unlimitedLives={LifeManager.Instance.HasTimedUnlimitedLives}");

        if (!canPlay)
        {
            UIEvents.RequestShowNoLivesOverlay();
        }
        else
        {
            UIEvents.RequestShowDefeatOverlay(currentLives);
        }
    }

    public void TriggerLevelDefeat(string reason)
    {
        LevelSessionManager.Instance?.FailLevel(reason);
    }

    private void OnLevelFailed(LevelFailedContext ctx)
    {
        HandleLevelDefeat(ctx.Reason);
        GameEvents.RaiseLevelEndedConsumePowerUps();
    }

    public void OnPlayerLose()
    {
        _playerHasDied = true;
        LevelSessionManager.Instance?.FailLevel("playerDeath");
    }

    private void OnLivesChanged(int newLives)
    {
        if (newLives > 0)
        {
            Debug.Log($"[LevelManager] lives updated: {newLives}");
        }
    }

    private int GetLevelIdForAnalytics()
    {
        if (LevelSessionManager.Instance?.CurrentSession != null)
            return LevelSessionManager.Instance.CurrentSession.LevelId;

        int fallback = GetCurrentLevelId();
        Debug.LogWarning($"[GameManager] levelId obtenido por Regex fallback ({fallback}). LevelSessionManager no disponible.");
        return fallback;
    }

    private int GetCurrentLevelId()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        var match = Regex.Match(sceneName, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int levelId))
            return levelId;

        return 1;
    }

    private void OnDestroy()
    {
        GameEvents.OnLevelStarted -= OnLevelStarted;
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLivesChanged -= OnLivesChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private bool IsTestingScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        foreach (string testScene in testingScenes)
        {
            if (currentScene == testScene)
                return true;
        }

        return false;
    }

    public void ResetLevelState()
    {
        _playerHasDied = false;
        _pausedByFocusLoss = false;
        _isVictory = false;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            HandleFocusLost();
        else
            HandleFocusRegained();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            HandleFocusLost();
        else
            HandleFocusRegained();
    }

    private void HandleFocusLost()
    {
        if (LevelSessionManager.Instance == null || !LevelSessionManager.Instance.IsSessionRunning) return;

        if (Time.timeScale > 0f)
        {
            LevelSessionManager.Instance.PauseLevel();
            Time.timeScale = 0f;
            _pausedByFocusLoss = true;
        }
    }

    private void HandleFocusRegained()
    {
        if (!_pausedByFocusLoss) return;

        LevelSessionManager.Instance?.ResumeLevel();
        Time.timeScale = 1f;
        _pausedByFocusLoss = false;
    }
}
