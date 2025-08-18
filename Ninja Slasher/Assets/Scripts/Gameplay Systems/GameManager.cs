using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    public LevelController levelController;
    private string[] testingScenes = { "TestScene" };
    private LevelStats currentStats;
    private bool _playerHasDied;
    private bool _levelStarted = false;

    public bool PlayerHasDied => _playerHasDied;

    public override void Awake()
    {
        base.Awake();
        if (!IsTestingScene())
        {
            EnemyTracker.OnAllEnemiesDefeated += OnLevelCompleted;
        }
    }

    void Start()
    {
        if (LifeManager.Instance != null)
        {
            LifeManager.Instance.OnLivesChanged += OnLivesChanged;
        }

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
        if (scene.name.Contains("Level") && !_levelStarted && LifeManager.Instance.CanPlay())
        {
            StartLevel();
        }
    }

    private void StartLevel()
    {
        if (!_levelStarted && LifeManager.Instance.CanPlay())
        {
            _levelStarted = true;
            LifeManager.Instance.OnLevelStart();
        }
    }

    public void OnLevelCompleted(LevelStats stats)
    {
        if (IsTestingScene())
            return;

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

#if UNITY_EDITOR
        ShowLevelCompletionSummary(currentLevelId);
#endif

        if (_levelStarted)
        {
            LifeManager.Instance.OnLevelCompleted();
            _levelStarted = false;
        }

        GoToLevelSelection();
    }

    public void OnLevelFailed()
    {
        if (LifeManager.Instance != null)
        {
            LifeManager.Instance.UseLife();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLifeLostPanel();
        }
    }

    public void OnPlayerLose()
    {
        _playerHasDied = true;

        if (_levelStarted)
        {
            LifeManager.Instance.UseLife();
            _levelStarted = false;
        }

        if (LifeManager.Instance.GetRealLives() <= 0)
        {
            GoToLevelSelection();
            return;
        }

        if (LifeManager.Instance.CanPlay())
        {
            UIManager.Instance.ShowLifeLostPanel();
        }
        else
        {
            UIManager.Instance.ShowNoLivesPanel();
        }
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

    public void GoToLevelSelection()
    {
        if (_levelStarted || LifeManager.Instance.HasPendingDeduction())
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }

        SaveManager.Instance.SaveData();
        SceneManager.sceneLoaded += HandleScreenflowLoaded;
        SceneManager.LoadScene("ScreenflowTest");
    }

    private void HandleScreenflowLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "ScreenflowTest") return;

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
            //UIManager.Instance.ShowNoLivesPanel();
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

    void OnDestroy()
    {
        EnemyTracker.OnAllEnemiesDefeated -= OnLevelCompleted;

        if (LifeManager.Instance != null)
        {
            LifeManager.Instance.OnLivesChanged -= OnLivesChanged;
        }
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
        if (pauseStatus && _levelStarted)
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _levelStarted)
        {
            LifeManager.Instance.OnLevelExit();
            _levelStarted = false;
        }
    }

    private void ShowLevelCompletionSummary(int levelId)
    {
        var summary = levelController?.GetLevelProgressSummary();
        if (summary == null) return;

        Debug.Log($"[GameManager] Resumen del Nivel {levelId}:\n" +
                  $"Completado: {summary.isCompleted}\n" +
                  $"Estrellas: {summary.maxStarsEarned}/3\n" +
                  $"Objetivos: {summary.completedObjectiveIds.Count}\n" +
                  $"Mejor tiempo: {(summary.bestTimeSeconds < float.MaxValue ? summary.bestTimeSeconds.ToString("F2") + "s" : "N/A")}\n" +
                  $"Mejores movimientos: {(summary.bestMoves < int.MaxValue ? summary.bestMoves.ToString() : "N/A")}\n" +
                  $"Parry Kill logrado: {(summary.parryKillAchieved ? "Sí" : "No")}");
    }
}