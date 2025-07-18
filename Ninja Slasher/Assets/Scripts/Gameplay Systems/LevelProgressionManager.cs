using System;
using UnityEngine;

[System.Serializable]
public class LevelProgressionData
{
    public int highestUnlockedLevel = 1;        // Último nivel desbloqueado
    public int highestUnlockedArea = 1;         // Última área desbloqueada
    public int totalStarsEarned = 0;            // Total de estrellas para jefes

    // Constructor por defecto - solo nivel 1 desbloqueado
    public LevelProgressionData()
    {
        highestUnlockedLevel = 1;
        highestUnlockedArea = 1;
        totalStarsEarned = 0;
    }
}

[System.Serializable]
public class LevelProgressionInfo
{
    public int highestUnlockedLevel;
    public int highestUnlockedArea;
    public int totalStars;
    public int nextBossRequirement;
}

public class LevelProgressionManager : MonoBehaviourSingleton<LevelProgressionManager>
{
    [Header("PROGRESSION SETTINGS")]
    [SerializeField] private int levelsPerArea = 10;
    [SerializeField] private int totalAreas = 5;

    [Header("BOSS REQUIREMENTS")]
    [SerializeField] private int[] starsRequiredPerBoss = { 5, 15, 30, 50, 75 };

    private LevelProgressionData progressionData;
    private bool isInitialized = false;

    public Action OnProgressionUpdated;
    public Action<int> OnNewAreaUnlocked;

    public override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        progressionData = new LevelProgressionData();
        isInitialized = true;

        Debug.Log("[LevelProgressionManager] Inicializado con valores por defecto");
    }

    private void LoadProgressionData()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[LevelProgressionManager] SaveManager no disponible - usando valores por defecto");
            return;
        }

        try
        {
            var saveData = SaveManager.Instance.GetGameData();

            if (saveData != null)
            {
                progressionData.totalStarsEarned = saveData.totalStars;
                CalculateUnlockedContent(saveData);
                Debug.Log("[LevelProgressionManager] Datos cargados correctamente");
            }
            else
            {
                Debug.LogWarning("[LevelProgressionManager] SaveData es null - usando valores por defecto");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelProgressionManager] Error cargando datos: {e.Message}");
        }
    }

    private void CalculateUnlockedContent(GameData saveData)
    {
        if (saveData == null || saveData.levelStars == null)
        {
            Debug.LogWarning("[LevelProgressionManager] SaveData inválido");
            return;
        }

        int highestCompletedLevel = 0;

        foreach (var levelStars in saveData.levelStars)
        {
            if (levelStars.Value >= 1)
            {
                highestCompletedLevel = Mathf.Max(highestCompletedLevel, levelStars.Key);
            }
        }

        progressionData.highestUnlockedLevel = highestCompletedLevel + 1;

        progressionData.highestUnlockedArea = Mathf.Min(
            ((highestCompletedLevel - 1) / levelsPerArea) + 1,
            totalAreas
        );

        Debug.Log($"[LevelProgressionManager] Nivel más alto desbloqueado: {progressionData.highestUnlockedLevel}");
        Debug.Log($"[LevelProgressionManager] Área más alta desbloqueada: {progressionData.highestUnlockedArea}");
        Debug.Log($"[LevelProgressionManager] Total estrellas: {progressionData.totalStarsEarned}");
    }

    public bool IsLevelUnlocked(int levelId)
    {
        if (!isInitialized || progressionData == null)
        {
            Debug.LogWarning("[LevelProgressionManager] No inicializado - permitiendo nivel 1 solamente");
            return levelId == 1;
        }

        if (levelId == 1) return true;

        if (levelId <= progressionData.highestUnlockedLevel)
        {
            return IsLevelAccessible(levelId);
        }

        return false;
    }

    private bool IsLevelAccessible(int levelId)
    {
        if (LevelConfigurationManager.Instance == null)
        {
            Debug.LogWarning("[LevelProgressionManager] LevelConfigurationManager no disponible");
            return levelId <= progressionData.highestUnlockedLevel;
        }

        var config = LevelConfigurationManager.Instance.GetConfigurationForLevel(levelId);
        if (config == null)
        {
            Debug.LogWarning($"[LevelProgressionManager] No se encontró configuración para nivel {levelId}");
            return levelId <= progressionData.highestUnlockedLevel;
        }

        if (config.unlockRequirements != null && config.unlockRequirements.isBossLevel)
        {
            int requiredStars = config.unlockRequirements.minimumStarsRequired;
            return progressionData.totalStarsEarned >= requiredStars;
        }

        return true;
    }

    public bool IsAreaUnlocked(int areaId)
    {
        if (!isInitialized || progressionData == null)
        {
            return areaId == 1;
        }

        return areaId <= progressionData.highestUnlockedArea;
    }

    public void OnLevelCompleted(int levelId, int starsEarned)
    {
        Debug.Log($"[LevelProgressionManager] Nivel {levelId} completado con {starsEarned} estrellas");

        LoadProgressionData();

        if (LevelConfigurationManager.Instance != null)
        {
            var config = LevelConfigurationManager.Instance.GetConfigurationForLevel(levelId);
            if (config?.unlockRequirements != null && config.unlockRequirements.isBossLevel)
            {
                CheckAreaUnlock(config.unlockRequirements.areaId);
            }
        }

        OnProgressionUpdated?.Invoke();
    }

    private void CheckAreaUnlock(int completedAreaId)
    {
        int newAreaId = completedAreaId + 1;
        if (newAreaId <= totalAreas && newAreaId > progressionData.highestUnlockedArea)
        {
            progressionData.highestUnlockedArea = newAreaId;
            Debug.Log($"[LevelProgressionManager] ¡Nueva área desbloqueada: {newAreaId}!");

            OnNewAreaUnlocked?.Invoke(newAreaId);
        }
    }

    public LevelProgressionInfo GetProgressionInfo()
    {
        if (!isInitialized || progressionData == null)
        {
            return new LevelProgressionInfo
            {
                highestUnlockedLevel = 1,
                highestUnlockedArea = 1,
                totalStars = 0,
                nextBossRequirement = starsRequiredPerBoss.Length > 0 ? starsRequiredPerBoss[0] : 0
            };
        }

        return new LevelProgressionInfo
        {
            highestUnlockedLevel = progressionData.highestUnlockedLevel,
            highestUnlockedArea = progressionData.highestUnlockedArea,
            totalStars = progressionData.totalStarsEarned,
            nextBossRequirement = GetNextBossStarRequirement()
        };
    }

    private int GetNextBossStarRequirement()
    {
        int currentArea = progressionData.highestUnlockedArea;
        if (currentArea <= starsRequiredPerBoss.Length)
        {
            return starsRequiredPerBoss[currentArea - 1];
        }
        return 0;
    }
}