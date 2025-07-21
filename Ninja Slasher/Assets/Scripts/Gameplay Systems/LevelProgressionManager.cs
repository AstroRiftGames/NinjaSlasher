using System;
using UnityEngine;

[Serializable]
public class LevelProgressionData
{
    public int highestUnlockedLevel = 1;        // Último nivel desbloqueado
    public int highestUnlockedArea = 1;         // Última área desbloqueada
    public int totalStarsEarned = 0;            // Total de estrellas para jefes

    public LevelProgressionData()
    {
        highestUnlockedLevel = 1;
        highestUnlockedArea = 1;
        totalStarsEarned = 0;
    }
}

[Serializable]
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
        isInitialized = true;

        Debug.Log("[LevelProgressionManager] Inicializado con valores por defecto");
    }

    public bool IsLevelUnlocked(int levelId)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[LevelProgressionManager] No inicializado - permitiendo nivel 1 solamente");
            return levelId == 1;
        }

        if (levelId == 1) return true;

        var (highestLevel, _, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);

        if (levelId <= highestLevel)
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
            var (highestLevel, _, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
            return levelId <= highestLevel;
        }

        var config = LevelConfigurationManager.Instance.GetConfigurationForLevel(levelId);
        if (config == null)
        {
            Debug.LogWarning($"[LevelProgressionManager] No se encontró configuración para nivel {levelId}");
            var (highestLevel, _, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
            return levelId <= highestLevel;
        }

        if (config.unlockRequirements != null && config.unlockRequirements.isBossLevel)
        {
            int requiredStars = config.unlockRequirements.minimumStarsRequired;
            var (_, _, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
            return totalStars >= requiredStars;
        }

        return true;
    }

    public bool IsAreaUnlocked(int areaId)
    {
        if (!isInitialized)
        {
            return areaId == 1;
        }

        var (_, highestArea, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
        return areaId <= highestArea;
    }

    public void OnLevelCompleted(int levelId, int starsEarned)
    {
        Debug.Log($"[LevelProgressionManager] Nivel {levelId} completado con {starsEarned} estrellas");

        SaveManager.Instance?.UpdateLevelProgression(levelId, starsEarned);

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
        var (_, currentHighestArea, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);

        if (newAreaId <= totalAreas && newAreaId > currentHighestArea)
        {
            SaveManager.Instance?.UnlockNewArea(newAreaId);
            Debug.Log($"[LevelProgressionManager] ¡Nueva área desbloqueada: {newAreaId}!");

            OnNewAreaUnlocked?.Invoke(newAreaId);
        }
    }

    public LevelProgressionInfo GetProgressionInfo()
    {
        if (!isInitialized)
        {
            return new LevelProgressionInfo
            {
                highestUnlockedLevel = 1,
                highestUnlockedArea = 1,
                totalStars = 0,
                nextBossRequirement = starsRequiredPerBoss.Length > 0 ? starsRequiredPerBoss[0] : 0
            };
        }

        var (highestLevel, highestArea, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);

        return new LevelProgressionInfo
        {
            highestUnlockedLevel = highestLevel,
            highestUnlockedArea = highestArea,
            totalStars = totalStars,
            nextBossRequirement = GetNextBossStarRequirement(highestArea)
        };
    }

    private int GetNextBossStarRequirement(int currentArea)
    {
        if (currentArea <= starsRequiredPerBoss.Length)
        {
            return starsRequiredPerBoss[currentArea - 1];
        }
        return 0;
    }

    public void ShowProgressionStatus()
    {
        var info = GetProgressionInfo();
        Debug.Log($"[LevelProgressionManager] Estado de Progresión:\n" +
                  $"Nivel más alto desbloqueado: {info.highestUnlockedLevel}\n" +
                  $"Área más alta desbloqueada: {info.highestUnlockedArea}\n" +
                  $"Total de estrellas: {info.totalStars}\n" +
                  $"Estrellas requeridas para próximo jefe: {info.nextBossRequirement}");
    }
}