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
    }

    public void OnPlayerLose()
    {
        LifeManager.Instance.UseLife();
        UIManager.Instance.UpdateLivesUI(LifeManager.Instance.CurrentLives);
        UIManager.Instance.ShowLifeLostPanel();
    }

    public void GoToLevelSelection()
    {
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

    void OnDestroy()
    {
        EnemyTracker.OnAllEnemiesDefeated -= OnLevelCompleted;
    }
}