using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    public LevelController levelController;

    private LevelStats currentStats;

    public override void Awake()
    {
        base.Awake();
        EnemyTracker.OnAllEnemiesDefeated += OnLevelCompleted;
    }

    void Start()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        if (LifeManager.Instance != null)
        {
            LifeManager.Instance.OnLivesChanged += OnLivesChanged;
        }
    }

    public void OnLevelCompleted(LevelStats stats)
    {
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
    }


    public void OnPlayerLose()
    {
        LifeManager.Instance.UseLife();

        if (!LifeManager.Instance.CanPlay())
        {
            UIManager.Instance.ShowNoLivesPanel();
        }
        else
        {
            UIManager.Instance.ShowLifeLostPanel();
        }
    }

    private void OnLivesChanged(int newLives)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLivesUI(newLives);
        }

        Debug.Log($"[GameManager] Vidas actualizadas: {newLives}");
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
}