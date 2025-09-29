using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviourSingleton<LevelManager>
{
    public LevelController levelController;

    [Header("Testing")]
    [SerializeField] private string[] testingScenes = { "TestScene" };

    private LevelStats currentStats;
    private bool _playerHasDied;
    private bool _levelStarted = false;

    public bool PlayerHasDied => _playerHasDied;

    public override void Awake()
    {
        base.Awake();

        if (!IsTestingScene())
        {
            HookEnemyEvents();
        }
    }

    private void Start()
    {
        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged += OnLivesChanged;

        _playerHasDied = false;
        _levelStarted = false;

        if (!LifeManager.Instance.CanPlay())
        {
            GoToLevelSelection();
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Level"))
        {
            if (!IsTestingScene())
                HookEnemyEvents();

            if (!_levelStarted && LifeManager.Instance.CanPlay())
                StartLevel();
        }
    }

    private void HookEnemyEvents()
    {
        EnemyTracker.OnAllEnemiesDefeated -= OnLevelCompleted;
        EnemyTracker.OnAllEnemiesDefeated += OnLevelCompleted;
    }

    private void StartLevel()
    {
        if (_levelStarted || !LifeManager.Instance.CanPlay()) return;

        _playerHasDied = false;

        ParryKillTracker.Reset();
        //MoveTracker.ResetTracker();

        _levelStarted = true;
        LifeManager.Instance.OnLevelStart();
    }

    public void OnLevelCompleted(LevelStats stats)
    {
        if (IsTestingScene()) return;

        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        currentStats = stats;

        stats.timeTaken = levelController.TimeTaken;
        stats.movesUsed = MoveTracker.TotalMoves;
        stats.parryKillDone = ParryKillTracker.KillWithParryPerformed;

        ParryKillTracker.Reset();
        levelController.StopTimer();

        int starsEarned = levelController.Evaluate(stats);
        int currentLevelId = GetCurrentLevelId();

        if (_levelStarted)
        {
            LifeManager.Instance.OnLevelCompleted();
            _levelStarted = false;
        }

        UIManager.Instance.ShowHideResultsCanvas();
    }

    private void HandleLevelDefeat(string reason = "unknown")
    {
        AudioManager.Instance.PlaySFX(SFXClip.UI_Defeat);

        if (levelController != null)
        {
            levelController.StopTimer(true);
        }

        if (_levelStarted && AnalyticsManager.Instance != null)
        {
            int currentLevelId = GetCurrentLevelId();
            float attemptTime = levelController?.TimeTaken ?? 0f;
            AnalyticsManager.Instance.RecordLevelFailed(
                currentLevelId,
                reason,
                attemptTime
            );
        }

        if (_levelStarted)
        {
            LifeManager.Instance.UseLife();
            _levelStarted = false;
        }

        StartCoroutine(HandleDefeatUIWithDelay());
    }

    private IEnumerator HandleDefeatUIWithDelay()
    {
        float soundDuration = AudioManager.Instance.GetSFXDuration(SFXClip.UI_Defeat);

        if (soundDuration <= 0f)
        {
            soundDuration = 1.5f;
        }

        yield return new WaitForSeconds(soundDuration);

        if (LifeManager.Instance.GetRealLives() <= 0)
        {
            GoToLevelSelection();
        }
        else if (LifeManager.Instance.CanPlay())
        {
            UIManager.Instance.ShowLifeLostPanel();
        }
        else
        {
            UIManager.Instance.ShowNoLivesPanel();
        }
    }

    public void TriggerLevelDefeat(string reason)
    {
        HandleLevelDefeat(reason);
    }

    public void OnLevelFailed()
    {
        HandleLevelDefeat("timeExpired");
    }

    public void OnPlayerLose()
    {
        _playerHasDied = true;
        HandleLevelDefeat("playerDeath");
    }

    private void OnLivesChanged(int newLives)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLivesUI(newLives);
        }

        if (newLives > 0)
        {
            var gameplayUI = FindObjectOfType<GameplayUIManager>();
            if (gameplayUI != null)
            {
                Debug.Log("[GAMEMANAGER] Vidas recuperadas");
            }
        }
    }

    public void GoToLevelSelection(bool confirmPendingDeduction = true)
    {
        if (AnalyticsManager.Instance != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            AnalyticsManager.Instance.RecordScreenTransition(currentScene, "LevelSelection");
        }

        if (confirmPendingDeduction && (_levelStarted || LifeManager.Instance.HasPendingDeduction()))
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }

        SaveManager.Instance.SaveData();
        SceneManager.sceneLoaded += HandleScreenflowLoaded;
        SceneManager.LoadScene("SplashScreen");
        AudioManager.Instance.PlayMusic(MusicClip.MainMenu, true);
    }

    private void HandleScreenflowLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SplashScreen") return;

        UIManager.Instance.ShowLevelSelector();
        SceneManager.sceneLoaded -= HandleScreenflowLoaded;
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
        EnemyTracker.OnAllEnemiesDefeated -= OnLevelCompleted;

        if (LifeManager.Instance != null)
            LifeManager.Instance.OnLivesChanged -= OnLivesChanged;
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

    private void OnApplicationPause(bool pauseStatus)
    {

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