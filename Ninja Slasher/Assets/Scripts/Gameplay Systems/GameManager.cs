using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    [Header("Testing")]
    [SerializeField] private string[] testingScenes = { "TestScene" };

    private LevelStats currentStats;
    private bool _playerHasDied;
    private bool _levelStarted = false;
    private bool _levelEnded = false;

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
        _levelStarted = false;
        _levelEnded = false;

        if (!LifeManager.Instance.CanPlay())
        {
            GoToLevelSelection();
            return;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnLevelStarted += OnLevelStarted;
        GameEvents.OnLevelCompleted += OnLevelCompleted;
        GameEvents.OnLevelFailed += OnLevelFailed;
        UIEvents.OnRetryButtonPressed += OnRetryButtonPressed;
        UIEvents.OnQuitToMenuPressed += OnQuitToMenuPressed;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLivesChanged -= OnLivesChanged;
        GameEvents.OnLevelStarted -= OnLevelStarted;
        UIEvents.OnRetryButtonPressed -= OnRetryButtonPressed;
        UIEvents.OnQuitToMenuPressed -= OnQuitToMenuPressed;
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
        if (_levelStarted)
        {
            return;
        }

        LifeManager.Instance.OnLevelStart();
        _levelStarted = true;
    }

    private void OnLevelCompleted(LevelStats stats)
    {
        _isVictory = true;

        if (IsTestingScene()) return;

        GameEvents.RaiseLevelEndedConsumePowerUps();

        if (_levelEnded)
        {
            return;
        }

        currentStats = stats;

        if (_levelStarted)
        {
            LifeManager.Instance.OnLevelCompleted();
            _levelStarted = false;
        }

        _levelEnded = true;

        int currentLevelId = GetCurrentLevelId();

        int starsEarned = stats.starsEarned;

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

        if (_levelEnded)
        {
            return;
        }

        _levelEnded = true;

        if (_levelStarted && AnalyticsManager.Instance != null)
        {
            int currentLevelId = GetCurrentLevelId();
            float attemptTime = 0f;

            if (LevelSessionManager.Instance != null)
            {
                attemptTime = LevelSessionManager.Instance.GetTimeTaken();
            }

            AnalyticsManager.Instance.RecordLevelFailed(
                currentLevelId,
                reason,
                attemptTime
            );
        }

        if (_levelStarted)
        {
            Debug.Log($"[GM] HandleLevelDefeat | reason={reason} | LSM.Instance={LevelSessionManager.Instance != null} | HasActiveSession={LevelSessionManager.Instance?.HasActiveSession}");
            LevelSessionManager.Instance?.FailLevel(reason);
            LifeManager.Instance.UseLife();
            _levelStarted = false;
        }

        StartCoroutine(HandleDefeatUIWithDelay());
    }

    private IEnumerator HandleDefeatUIWithDelay()
    {
        yield return new WaitForSeconds(0.1f);

        // Si el Emergency Bundle está activo, él reemplaza la pantalla de derrota.
        if (EmergencyBundleService.Instance != null && EmergencyBundleService.Instance.HasActiveOffer)
            yield break;

        int currentLives = LifeManager.Instance.GetRealLives();

        if (!LifeManager.Instance.HasTimedUnlimitedLives && currentLives <= 0)
        {
            UIEvents.RequestShowNoLivesOverlay();
        }
        else if (LifeManager.Instance.CanPlay())
        {
            UIEvents.RequestShowDefeatOverlay(currentLives);
        }
        else
        {
            UIEvents.RequestShowNoLivesOverlay();
        }
    }

    public void TriggerLevelDefeat(string reason)
    {
        HandleLevelDefeat(reason);
    }

    public void OnLevelFailed()
    {
        HandleLevelDefeat("timeExpired");
        GameEvents.RaiseLevelEndedConsumePowerUps();
    }

    private void OnLevelFailed(LevelFailedContext ctx)
    {
        HandleLevelDefeat(ctx.Reason);
        GameEvents.RaiseLevelEndedConsumePowerUps();
    }

    public void OnPlayerLose()
    {
        _playerHasDied = true;
        HandleLevelDefeat("playerDeath");
    }

    private void OnLivesChanged(int newLives)
    {
        if (newLives > 0)
        {
            Debug.Log($"[LevelManager] lives updated: {newLives}");
        }
    }

    public void GoToLevelSelection(bool confirmPendingDeduction = true)
    {
        UIEvents.RaiseQuitToMenuPressed();
    }

    private void OnQuitToMenuPressed()
    {
        if (AnalyticsManager.Instance != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            AnalyticsManager.Instance.RecordScreenTransition(currentScene, "LevelSelection");
        }

        if (_levelStarted || LifeManager.Instance.HasPendingDeduction())
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }

        if (AudioService.Instance != null)
        {
            AudioService.Instance.StopAllSFX();
        }

        SaveManager.Instance.SaveData();
    }

    private void OnRetryButtonPressed()
    {
        if (LifeManager.Instance == null)
            return;

        if (!LifeManager.Instance.HasTimedUnlimitedLives && LifeManager.Instance.GetRealLives() <= 0)
        {
            UIEvents.RequestShowNoLivesOverlay();
            return;
        }

        UIEvents.RequestRestartLevel();
    }

    public void RestartLevel()
    {
        if (_levelStarted || LifeManager.Instance.HasPendingDeduction())
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }

        if (!LifeManager.Instance.CanPlay())
        {
            GoToLevelSelection();
            return;
        }

        _playerHasDied = false;
        _levelEnded = false;
        string currentScene = SceneManager.GetActiveScene().name;

        LifeManager.Instance.OnLevelStart();
        _levelStarted = true;

        SceneManager.LoadScene(currentScene);
    }

    private int GetCurrentLevelId()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName.Contains("Level"))
        {
            string levelNumber = sceneName.Replace("Level", "").Replace("_", "");
            if (int.TryParse(levelNumber, out int levelId))
            {
                return levelId;
            }
        }

        return 1;
    }

    private void OnDestroy()
    {
        GameEvents.OnLevelCompleted -= OnLevelCompleted;
        GameEvents.OnLevelFailed -= OnLevelFailed;
        GameEvents.OnLivesChanged -= OnLivesChanged;
        UIEvents.OnRetryButtonPressed -= OnRetryButtonPressed;
        UIEvents.OnQuitToMenuPressed -= OnQuitToMenuPressed;
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
        _levelStarted = false;
        _levelEnded = false;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // TO DO
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _levelStarted)
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }
    }
}
