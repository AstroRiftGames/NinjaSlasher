using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public LevelController levelController;

    private LevelStats currentStats;

    void Start()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            RestartLevel();
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

    public void RestartLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }
}