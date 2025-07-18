using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    public LevelController levelController;
    private string[] testingScenes = { "TestScene" };
    private LevelStats currentStats;
    private bool _playerHasDied;

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
        Debug.Log($"Nivel completado. Estrellas obtenidas: {starsEarned}");

        int currentLevelId = GetCurrentLevelId();
        SaveManager.Instance.UpdateStars(currentLevelId, starsEarned);

        if (starsEarned >= 1)
        {
            LevelProgressionManager.Instance?.OnLevelCompleted(currentLevelId, starsEarned);
        }

        GoToLevelSelection();
    }

    public void OnPlayerLose()
    {
        _playerHasDied = true;

        if (LifeManager.Instance.CurrentLives <= 0)
        {
            GoToLevelSelection();
            return;
        }

        LifeManager.Instance.UseLife();

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
                Debug.Log("[GameManager] Vidas recuperadas");
            }
        }
    }

    public void GoToLevelSelection()
    {
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
        if (!LifeManager.Instance.CanPlay())
        {
            Debug.LogWarning("[GameManager] Intento de reiniciar nivel sin vidas disponibles");
            UIManager.Instance.ShowNoLivesPanel();
            return;
        }

        _playerHasDied = false;

        string currentScene = SceneManager.GetActiveScene().name;
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

}