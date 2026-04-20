using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    [Header("Presentation")]
    [SerializeField] private float _endOfLevelSettleDelay = 0.45f;

    private LevelStats currentStats;
    private bool _playerHasDied;
    private bool _pausedByFocusLoss = false;

    public bool PlayerHasDied => _playerHasDied;

    private bool _isVictory = false;
    public bool IsVictory => _isVictory;
    private Coroutine _endOfLevelPresentationCoroutine;

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
        GameEvents.OnLevelEnded += OnLevelEnded;
        GameEvents.OnLevelFailed += OnLevelFailed;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        StopEndOfLevelPresentation();
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelEnded -= OnLevelEnded;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLivesChanged -= OnLivesChanged;
        GameEvents.OnLevelStarted -= OnLevelStarted;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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

        GameEvents.RaiseLevelEndedConsumePowerUps();

        currentStats = stats;
        LifeManager.Instance.OnLevelCompleted();

    }

    private void OnLevelEnded(LevelResult result)
    {
        StartEndOfLevelPresentation(result);
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

    }

    private void StartEndOfLevelPresentation(LevelResult result)
    {
        StopEndOfLevelPresentation();
        _endOfLevelPresentationCoroutine = StartCoroutine(HandleEndOfLevelPresentation(result));
    }

    private IEnumerator HandleEndOfLevelPresentation(LevelResult result)
    {
        yield return new WaitForSecondsRealtime(_endOfLevelSettleDelay);
        _endOfLevelPresentationCoroutine = null;

        if (result == LevelResult.Victory)
        {
            GameEvents.RaiseLevelResultReady(LevelResult.Victory);
            yield break;
        }

        if (EmergencyBundleService.Instance != null && EmergencyBundleService.Instance.HasActiveOffer)
            yield break;

        LevelResult finalResult = LifeManager.Instance != null && !LifeManager.Instance.CanPlay()
            ? LevelResult.NoLives
            : LevelResult.Defeat;

        int currentLives = LifeManager.Instance != null ? LifeManager.Instance.GetRealLives() : 0;
        bool canPlay = LifeManager.Instance != null && LifeManager.Instance.CanPlay();

        Debug.Log($"[GameManager] Resolve defeat UI | realLives={currentLives} | canPlay={canPlay} | unlimitedLives={LifeManager.Instance.HasTimedUnlimitedLives}");
        GameEvents.RaiseLevelResultReady(finalResult);
    }

    private void StopEndOfLevelPresentation()
    {
        if (_endOfLevelPresentationCoroutine == null)
            return;

        StopCoroutine(_endOfLevelPresentationCoroutine);
        _endOfLevelPresentationCoroutine = null;
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
        StopEndOfLevelPresentation();
        GameEvents.OnLevelStarted -= OnLevelStarted;
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelEnded -= OnLevelEnded;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLivesChanged -= OnLivesChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ResetLevelState()
    {
        StopEndOfLevelPresentation();
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

        bool isManagedBackgroundPauseActive = PauseController.Instance.IsPauseSourceActive(PauseSource.ApplicationBackground);

        if (!isManagedBackgroundPauseActive)
        {
            LevelSessionManager.Instance.PauseLevel();
            PauseController.Instance.RequestPause(PauseSource.ApplicationBackground);
            _pausedByFocusLoss = true;
        }
    }

    private void HandleFocusRegained()
    {
        if (!_pausedByFocusLoss) return;

        LevelSessionManager.Instance?.ResumeLevel();
        PauseController.Instance.ReleasePause(PauseSource.ApplicationBackground);
        _pausedByFocusLoss = false;
    }
}
