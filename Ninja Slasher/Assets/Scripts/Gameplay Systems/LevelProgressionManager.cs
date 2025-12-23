using System;
using UnityEngine;

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

    [Header("ADS CONFIGURATION")]
    [SerializeField] private int levelsRequiredForAd = 3;
    [SerializeField] private bool enableConsecutiveLevelAds = true;
    [SerializeField] private bool enableAreaUnlockAds = true;

    private int currentConsecutiveWins = 0;
    private int lastCompletedLevel = -1;

    private bool isInitialized = false;

    public Action OnProgressionUpdated;
    public Action<int> OnNewAreaUnlocked;

    public override void Awake()
    {
        var _ = SaveManager.Instance;
        base.Awake();
        Initialize();
        LoadConsecutiveProgress();
    }

    private void Initialize()
    {
        isInitialized = true;
    }

    private void OnEnable()
    {
        SaveManager.OnDataLoaded += HandleDataLoaded;
    }

    private void OnDisable()
    {
        SaveManager.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded(GameData _)
    {
        OnProgressionUpdated?.Invoke();
    }

    private void LoadConsecutiveProgress()
    {
        currentConsecutiveWins = PlayerPrefs.GetInt("ConsecutiveWins", 0);
        lastCompletedLevel = PlayerPrefs.GetInt("LastCompletedLevel", -1);
    }

    private void SaveConsecutiveProgress()
    {
        PlayerPrefs.SetInt("ConsecutiveWins", currentConsecutiveWins);
        PlayerPrefs.SetInt("LastCompletedLevel", lastCompletedLevel);
        PlayerPrefs.Save();
    }

    private void CheckConsecutiveLevelAd(int levelId, int starsEarned)
    {
        if (!enableConsecutiveLevelAds)
            return;

        if (lastCompletedLevel == -1 || levelId == lastCompletedLevel + 1)
        {
            currentConsecutiveWins++;
            lastCompletedLevel = levelId;

            if (currentConsecutiveWins >= levelsRequiredForAd)
            {
                ShowConsecutiveLevelAd();
                ResetConsecutiveCounter();
            }
        }
        else
        {
            ResetConsecutiveCounter();
            currentConsecutiveWins = 1;
            lastCompletedLevel = levelId;
        }

        SaveConsecutiveProgress();
    }

    private void ShowConsecutiveLevelAd()
    {
        if (AdsManager.Instance != null && AdsManager.Instance.IsInterstitialAdReady())
        {
            AdsManager.Instance.ShowInterstitialAd();
        }
        else
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ReloadAllAds();
            }
        }
    }

    private void ShowAreaUnlockAd(int newAreaId)
    {
        if (AdsManager.Instance != null && AdsManager.Instance.IsInterstitialAdReady())
        {
            AdsManager.Instance.ShowInterstitialAd();
        }
        else
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ReloadAllAds();
            }
        }
    }

    private void ResetConsecutiveCounter()
    {
        if (currentConsecutiveWins > 0)
        {
            Debug.Log($"Contador consecutivo reseteado (era: {currentConsecutiveWins})");
        }
        currentConsecutiveWins = 0;
        lastCompletedLevel = -1;
    }

    public bool IsLevelUnlocked(int levelId)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (levelId == 1) return true;

        var (highestLevel, _, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
        bool isUnlocked = levelId <= highestLevel && IsLevelAccessible(levelId);

        return isUnlocked;
    }

    private bool IsLevelAccessible(int levelId)
    {
        if (LevelConfigurationManager.Instance == null)
        {
            var (highestLevel, _, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
            return levelId <= highestLevel;
        }

        var config = LevelConfigurationManager.Instance.GetConfigurationForLevel(levelId);
        if (config == null)
        {
            var (highestLevel, _, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
            return levelId <= highestLevel;
        }

        if (config.unlockRequirements != null && config.unlockRequirements.isBossLevel)
        {
            int requiredStars = config.unlockRequirements.minimumStarsRequired;
            var (_, _, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
            bool hasEnoughStars = totalStars >= requiredStars;
            return hasEnoughStars;
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

    private void CheckAreaUnlock(int completedAreaId)
    {
        int newAreaId = completedAreaId + 1;
        var (_, currentHighestArea, _) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);

        if (newAreaId <= totalAreas && newAreaId > currentHighestArea)
        {
            SaveManager.Instance?.UnlockNewArea(newAreaId);
            OnNewAreaUnlocked?.Invoke(newAreaId);

            if (enableAreaUnlockAds)
            {
                ShowAreaUnlockAd(newAreaId);
            }
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

    public void HandleLevelCompletion(int levelId, int starsEarned)
    {
        SaveManager.Instance?.UpdateLevelProgression(levelId, starsEarned);

        CheckConsecutiveLevelAd(levelId, starsEarned);

        var (currentHighest, currentArea, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);

        if (levelId >= currentHighest)
        {
            int nextLevel = levelId + 1;

            if (ShouldUnlockNextLevel(nextLevel))
            {
                SaveManager.Instance?.UpdateHighestUnlockedLevel(nextLevel);
            }
        }

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

    private bool ShouldUnlockNextLevel(int nextLevel)
    {
        int maxLevel = levelsPerArea * totalAreas;
        if (nextLevel > maxLevel)
        {
            return false;
        }

        if (LevelConfigurationManager.Instance != null)
        {
            var config = LevelConfigurationManager.Instance.GetConfigurationForLevel(nextLevel);
            if (config == null)
            {
                return false;
            }

            if (config.unlockRequirements != null && config.unlockRequirements.isBossLevel)
            {
                var (_, _, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
                bool hasEnoughStars = totalStars >= config.unlockRequirements.minimumStarsRequired;
                return hasEnoughStars;
            }
        }

        return true;
    }

    private int GetNextBossStarRequirement(int currentArea)
    {
        if (currentArea <= starsRequiredPerBoss.Length)
        {
            return starsRequiredPerBoss[currentArea - 1];
        }
        return 0;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Simular Desbloqueo Área 1")]
    private void SimulateUnlockArea1() => SimulateAreaUnlock(1);

    [ContextMenu("Debug/Simular Desbloqueo Área 2")]
    private void SimulateUnlockArea2() => SimulateAreaUnlock(2);

    [ContextMenu("Debug/Simular Desbloqueo Área 3")]
    private void SimulateUnlockArea3() => SimulateAreaUnlock(3);

    [ContextMenu("Debug/Simular Desbloqueo Área 4")]
    private void SimulateUnlockArea4() => SimulateAreaUnlock(4);

    [ContextMenu("Debug/Simular Desbloqueo Área 5")]
    private void SimulateUnlockArea5() => SimulateAreaUnlock(5);

    [ContextMenu("Debug/Simular Desbloqueo de TODAS las Áreas")]
    private void SimulateUnlockAllAreas()
    {
        for (int area = 1; area <= totalAreas; area++)
        {
            SimulateAreaUnlock(area, logDetails: false);
        }

        Debug.Log($"[LevelProgressionManager] TODAS LAS ÁREAS DESBLOQUEADAS (1-{totalAreas})");
        Debug.Log($"[LevelProgressionManager] Total de niveles desbloqueados: {totalAreas * levelsPerArea}");
        Debug.Log($"[LevelProgressionManager] Estrellas totales simuladas: {SaveManager.Instance.GetGameData().totalStars}");

        OnProgressionUpdated?.Invoke();
    }

    public void SimulateAreaUnlock(int areaId, bool logDetails = true)
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        if (areaId < 1 || areaId > totalAreas)
        {
            return;
        }

        var gameData = SaveManager.Instance.GetGameData();

        int firstLevelInArea = ((areaId - 1) * levelsPerArea) + 1;
        int lastLevelInArea = areaId * levelsPerArea;

        if (logDetails)
        {
            Debug.Log($"[LevelProgressionManager] === SIMULANDO DESBLOQUEO ÁREA {areaId} ===");
            Debug.Log($"[LevelProgressionManager] Niveles: {firstLevelInArea} - {lastLevelInArea}");
        }

        int starsAddedThisArea = 0;

        for (int levelId = firstLevelInArea; levelId <= lastLevelInArea; levelId++)
        {
            int starsForLevel = UnityEngine.Random.Range(1, 4);

            if (gameData.levelStars.ContainsKey(levelId))
            {
                starsForLevel = Mathf.Max(gameData.levelStars[levelId], starsForLevel);
            }

            gameData.levelStars[levelId] = starsForLevel;
            starsAddedThisArea += starsForLevel;

            if (logDetails)
            {
                Debug.Log($"[LevelProgressionManager]   Nivel {levelId}: {starsForLevel}");
            }
        }

        RecalculateTotalStars(gameData);

        if (!gameData.unlockedAreas.Contains(areaId))
        {
            gameData.unlockedAreas.Add(areaId);
        }

        if (areaId > gameData.highestUnlockedArea)
        {
            gameData.highestUnlockedArea = areaId;
        }

        if (lastLevelInArea > gameData.highestUnlockedLevel)
        {
            gameData.highestUnlockedLevel = lastLevelInArea;
        }

        SaveManager.Instance.SaveData();

        if (logDetails)
        {
            Debug.Log($"[LevelProgressionManager] Área {areaId} desbloqueada completamente");
            Debug.Log($"[LevelProgressionManager] Estrellas ganadas en área: {starsAddedThisArea}");
            Debug.Log($"[LevelProgressionManager] Total de estrellas acumuladas: {gameData.totalStars}");
            Debug.Log($"[LevelProgressionManager] ===================================");
        }

        OnProgressionUpdated?.Invoke();
    }

    public void SimulateUnlockUpToArea(int targetArea)
    {
        if (targetArea < 1 || targetArea > totalAreas)
        {
            Debug.LogError($"[LevelProgressionManager] Área objetivo inválida: {targetArea}");
            return;
        }

        Debug.Log($"[LevelProgressionManager] === DESBLOQUEANDO HASTA ÁREA {targetArea} ===");

        for (int area = 1; area <= targetArea; area++)
        {
            SimulateAreaUnlock(area, logDetails: false);
        }

        Debug.Log($"[LevelProgressionManager] Áreas 1-{targetArea} desbloqueadas");
        Debug.Log($"[LevelProgressionManager] Total estrellas: {SaveManager.Instance.GetGameData().totalStars}");

        OnProgressionUpdated?.Invoke();
    }

    private void RecalculateTotalStars(GameData gameData)
    {
        int total = 0;
        foreach (var kvp in gameData.levelStars)
        {
            total += kvp.Value;
        }
        gameData.totalStars = total;
    }

    [ContextMenu("Debug/Resetear Progreso Área 1")]
    private void ResetArea1Progress() => ResetAreaProgress(1);

    [ContextMenu("Debug/Resetear Progreso Área 2")]
    private void ResetArea2Progress() => ResetAreaProgress(2);

    public void ResetAreaProgress(int areaId)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LevelProgressionManager] SaveManager no disponible");
            return;
        }

        if (areaId < 1 || areaId > totalAreas)
        {
            Debug.LogError($"[LevelProgressionManager] Área inválida: {areaId}");
            return;
        }

        var gameData = SaveManager.Instance.GetGameData();

        int firstLevelInArea = ((areaId - 1) * levelsPerArea) + 1;
        int lastLevelInArea = areaId * levelsPerArea;

        Debug.Log($"[LevelProgressionManager] Reseteando área {areaId} (niveles {firstLevelInArea}-{lastLevelInArea})");

        for (int levelId = firstLevelInArea; levelId <= lastLevelInArea; levelId++)
        {
            if (gameData.levelStars.ContainsKey(levelId))
            {
                gameData.levelStars.Remove(levelId);
            }

            if (gameData.levelObjectives.ContainsKey(levelId))
            {
                gameData.levelObjectives.Remove(levelId);
            }

            if (gameData.levelProgressData.ContainsKey(levelId))
            {
                gameData.levelProgressData.Remove(levelId);
            }
        }

        RecalculateTotalStars(gameData);

        if (areaId > 1 && gameData.unlockedAreas.Contains(areaId))
        {
            gameData.unlockedAreas.Remove(areaId);
        }

        SaveManager.Instance.SaveData();

        Debug.Log($"[LevelProgressionManager] Área {areaId} reseteada");
        Debug.Log($"[LevelProgressionManager] Estrellas totales restantes: {gameData.totalStars}");

        OnProgressionUpdated?.Invoke();
    }

    [ContextMenu("Debug/Mostrar Progreso Actual")]
    private void ShowCurrentProgress()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LevelProgressionManager] SaveManager no disponible");
            return;
        }

        var gameData = SaveManager.Instance.GetGameData();

        Debug.Log("========== PROGRESO ACTUAL ==========");
        Debug.Log($"Área más alta desbloqueada: {gameData.highestUnlockedArea}");
        Debug.Log($"Nivel más alto desbloqueado: {gameData.highestUnlockedLevel}");
        Debug.Log($"Total de estrellas: {gameData.totalStars}");
        Debug.Log($"Áreas desbloqueadas: {string.Join(", ", gameData.unlockedAreas)}");

        Debug.Log("\n--- Desglose por Área ---");
        for (int area = 1; area <= totalAreas; area++)
        {
            int firstLevel = ((area - 1) * levelsPerArea) + 1;
            int lastLevel = area * levelsPerArea;
            int starsInArea = 0;
            int levelsCompleted = 0;

            for (int levelId = firstLevel; levelId <= lastLevel; levelId++)
            {
                if (gameData.levelStars.TryGetValue(levelId, out int stars))
                {
                    starsInArea += stars;
                    levelsCompleted++;
                }
            }

            bool isUnlocked = gameData.unlockedAreas.Contains(area);
            string status = isUnlocked ? "DESBLOQUEADA" : "BLOQUEADA";

            Debug.Log($"Área {area} ({status}): {levelsCompleted}/{levelsPerArea} niveles | {starsInArea} ⭐");
        }

        Debug.Log("=====================================");
    }

#endif
}