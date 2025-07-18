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
    [SerializeField] private int[] starsRequiredPerBoss = { 5, 15, 30, 50, 75 }; // Estrellas requeridas para cada jefe

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
        // Inicializar con valores por defecto seguros
        progressionData = new LevelProgressionData();
        isInitialized = true;

        Debug.Log("[LevelProgressionManager] Inicializado con valores por defecto");
    }

    private void LoadProgressionData()
    {
        // Verificar que SaveManager esté disponible
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[LevelProgressionManager] SaveManager no disponible - usando valores por defecto");
            return;
        }

        try
        {
            // Obtener datos del SaveManager existente
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

        // Encontrar el nivel más alto que tiene al menos 1 estrella
        foreach (var levelStars in saveData.levelStars)
        {
            if (levelStars.Value >= 1)
            {
                highestCompletedLevel = Mathf.Max(highestCompletedLevel, levelStars.Key);
            }
        }

        // El siguiente nivel después del más alto completado está desbloqueado
        progressionData.highestUnlockedLevel = highestCompletedLevel + 1;

        // Calcular área desbloqueada
        progressionData.highestUnlockedArea = Mathf.Min(
            ((highestCompletedLevel - 1) / levelsPerArea) + 1,
            totalAreas
        );

        Debug.Log($"[LevelProgressionManager] Nivel más alto desbloqueado: {progressionData.highestUnlockedLevel}");
        Debug.Log($"[LevelProgressionManager] Área más alta desbloqueada: {progressionData.highestUnlockedArea}");
        Debug.Log($"[LevelProgressionManager] Total estrellas: {progressionData.totalStarsEarned}");
    }

    // Verificar si un nivel específico está desbloqueado
    public bool IsLevelUnlocked(int levelId)
    {
        // Verificar que esté inicializado
        if (!isInitialized || progressionData == null)
        {
            Debug.LogWarning("[LevelProgressionManager] No inicializado - permitiendo nivel 1 solamente");
            return levelId == 1;
        }

        // Nivel 1 siempre desbloqueado
        if (levelId == 1) return true;

        // Verificar si el nivel está dentro del rango desbloqueado
        if (levelId <= progressionData.highestUnlockedLevel)
        {
            return IsLevelAccessible(levelId);
        }

        return false;
    }

    private bool IsLevelAccessible(int levelId)
    {
        // Verificar que LevelConfigurationManager esté disponible
        if (LevelConfigurationManager.Instance == null)
        {
            Debug.LogWarning("[LevelProgressionManager] LevelConfigurationManager no disponible");
            return levelId <= progressionData.highestUnlockedLevel;
        }

        // Obtener configuración del nivel
        var config = LevelConfigurationManager.Instance.GetConfigurationForLevel(levelId);
        if (config == null)
        {
            Debug.LogWarning($"[LevelProgressionManager] No se encontró configuración para nivel {levelId}");
            return levelId <= progressionData.highestUnlockedLevel;
        }

        // Si es un nivel de jefe, verificar requisitos de estrellas
        if (config.unlockRequirements != null && config.unlockRequirements.isBossLevel)
        {
            int requiredStars = config.unlockRequirements.minimumStarsRequired;
            return progressionData.totalStarsEarned >= requiredStars;
        }

        // Niveles normales solo requieren haber completado el anterior
        return true;
    }

    // Verificar si un área está desbloqueada
    public bool IsAreaUnlocked(int areaId)
    {
        if (!isInitialized || progressionData == null)
        {
            return areaId == 1; // Solo área 1 por defecto
        }

        return areaId <= progressionData.highestUnlockedArea;
    }

    // Llamar cuando se completa un nivel para actualizar progresión
    public void OnLevelCompleted(int levelId, int starsEarned)
    {
        Debug.Log($"[LevelProgressionManager] Nivel {levelId} completado con {starsEarned} estrellas");

        // Recargar datos de progresión
        LoadProgressionData();

        // Verificar si se desbloqueó nueva área (al completar un jefe)
        if (LevelConfigurationManager.Instance != null)
        {
            var config = LevelConfigurationManager.Instance.GetConfigurationForLevel(levelId);
            if (config?.unlockRequirements != null && config.unlockRequirements.isBossLevel)
            {
                CheckAreaUnlock(config.unlockRequirements.areaId);
            }
        }

        // Notificar cambios de progresión
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

    // Obtener información de progresión para UI
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

    // Debug: Resetear progresión
    [ContextMenu("Reset Progression")]
    public void ResetProgression()
    {
        progressionData = new LevelProgressionData();
        Debug.Log("[Progression] Progresión reseteada");
    }

    // Debug: Desbloquear todo
    [ContextMenu("Unlock All")]
    public void UnlockAll()
    {
        progressionData.highestUnlockedLevel = levelsPerArea * totalAreas;
        progressionData.highestUnlockedArea = totalAreas;
        Debug.Log("[Progression] Todo desbloqueado");
    }
}