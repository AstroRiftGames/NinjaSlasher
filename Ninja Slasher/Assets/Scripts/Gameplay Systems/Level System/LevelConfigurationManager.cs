using UnityEngine;

public class LevelConfigurationManager : MonoBehaviourSingleton<LevelConfigurationManager>
{
    [Header("DATABASE SETTINGS")]
    public LevelConfiguration[] levelConfigurations;

    public LevelConfiguration GetConfigurationForLevel(int levelId)
    {
        foreach (var config in levelConfigurations)
        {
            if (config.levelId == levelId)
                return config;
        }

        Debug.LogWarning($"[LEVEL CONFIGURATION] No se encontró configuraciOn para el nivel {levelId}");
        return null;
    }

    public LevelConfiguration GetConfigurationForCurrentLevel()
    {
        int currentLevelId = GetCurrentLevelId();
        return GetConfigurationForLevel(currentLevelId);
    }

    private int GetCurrentLevelId()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

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
}
