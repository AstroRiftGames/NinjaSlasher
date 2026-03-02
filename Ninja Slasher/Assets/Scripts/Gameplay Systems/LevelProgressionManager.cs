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
    // DEPRECATED
    //[Header("PROGRESSION SETTINGS")]
    //[SerializeField] private int levelsPerArea = 10;
    //[SerializeField] private int totalAreas = 5;
    //[Header("ADS CONFIGURATION")]
    //[SerializeField] private int levelsRequiredForAd = 3;
    //[SerializeField] private bool enableConsecutiveLevelAds = true;
    //[SerializeField] private bool enableAreaUnlockAds = true;
    //[Header("BOSS REQUIREMENTS")]
    //[SerializeField] private int[] starsRequiredPerBoss = { 5, 15, 30, 50, 75 };

    private int LevelsPerArea => GameConfigManager.Config.levelsPerArea;
    private int TotalAreas => GameConfigManager.Config.totalAreas;
    private int LevelsRequiredForAd => GameConfigManager.Config.levelsRequiredForAd;
    private bool EnableConsecutiveLevelAds => GameConfigManager.Config.enableConsecutiveLevelAds;
    private bool EnableAreaUnlockAds => GameConfigManager.Config.enableAreaUnlockAds;

    private bool isInitialized = false;

    public Action OnProgressionUpdated;
    public Action<int> OnNewAreaUnlocked;

    public override void Awake()
    {
        var _ = SaveManager.Instance;
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        isInitialized = true;

        //if (GameConfigManager.IsReady() && GameConfigManager.Config.unlockAllLevelsOnStart)
        //{
        //    UnlockAllLevelsForDebug();
        //}
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

    private void CheckConsecutiveLevelAd(int levelId, int starsEarned)
    {
        if (!EnableConsecutiveLevelAds || SaveManager.Instance == null)
            return;

        var gameData = SaveManager.Instance.GetGameData();
        int currentConsecutiveWins = gameData.consecutiveLevelWins;
        int lastCompletedLevel = gameData.lastCompletedLevel;

        if (lastCompletedLevel == -1 || levelId == lastCompletedLevel + 1)
        {
            currentConsecutiveWins++;
            gameData.consecutiveLevelWins = currentConsecutiveWins;
            gameData.lastCompletedLevel = levelId;

            if (currentConsecutiveWins >= LevelsRequiredForAd)
            {
                ShowConsecutiveLevelAd();
                ResetConsecutiveCounter();
            }
        }
        else
        {
            ResetConsecutiveCounter();
            gameData.consecutiveLevelWins = 1;
            gameData.lastCompletedLevel = levelId;
        }

        SaveManager.Instance.SaveData();
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
        if (SaveManager.Instance == null) return;

        var gameData = SaveManager.Instance.GetGameData();

        if (gameData.consecutiveLevelWins > 0)
        {
            Debug.Log($"[LevelProgressionManager] Contador consecutivo reseteado (era: {gameData.consecutiveLevelWins})");
        }

        gameData.consecutiveLevelWins = 0;
        gameData.lastCompletedLevel = -1;
        SaveManager.Instance.SaveData();
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

        if (newAreaId <= TotalAreas && newAreaId > currentHighestArea)
        {
            SaveManager.Instance?.UnlockNewArea(newAreaId);
            OnNewAreaUnlocked?.Invoke(newAreaId);

            if (EnableAreaUnlockAds)
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
                nextBossRequirement = GameConfigManager.Config.starsRequiredPerBoss.Length > 0 ? GameConfigManager.Config.starsRequiredPerBoss[0] : 0
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
        int maxLevel = LevelsPerArea * TotalAreas;
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
        if (GameConfigManager.IsReady())
        {
            return GameConfigManager.Config.GetStarsRequiredForBoss(currentArea);
        }

        Debug.LogWarning($"[LevelProgressionManager] GameConfigManager no está listo");
        return 0;
    }

    //DEPRECATED
    //private int GetNextBossStarRequirement(int currentArea)
    //{
    //    if (currentArea <= starsRequiredPerBoss.Length)
    //    {
    //        return starsRequiredPerBoss[currentArea - 1];
    //    }
    //    return 0;
    //}

    //private void UnlockAllLevelsForDebug()
    //{
    //    Debug.Log("[LevelProgressionManager] DEBUG MODE: Desbloqueando todos los niveles...");

    //    SimulateUnlockUpToArea(TotalAreas);

    //    Debug.Log($"[LevelProgressionManager] {TotalAreas * LevelsPerArea} niveles desbloqueados");
    //}

#if UNITY_EDITOR
    //[ContextMenu("Debug/Simular Desbloqueo Area 1")]
    //private void SimulateUnlockArea1() => SimulateAreaUnlock(1);

    //[ContextMenu("Debug/Simular Desbloqueo Area 2")]
    //private void SimulateUnlockArea2() => SimulateAreaUnlock(2);

    //[ContextMenu("Debug/Simular Desbloqueo Area 3")]
    //private void SimulateUnlockArea3() => SimulateAreaUnlock(3);

    //[ContextMenu("Debug/Simular Desbloqueo Area 4")]
    //private void SimulateUnlockArea4() => SimulateAreaUnlock(4);

    //[ContextMenu("Debug/Simular Desbloqueo Area 5")]
    //private void SimulateUnlockArea5() => SimulateAreaUnlock(5);

    //[ContextMenu("Debug/Simular Desbloqueo de TODAS las Areas")]
    //private void SimulateUnlockAllAreas()
    //{
    //    for (int area = 1; area <= TotalAreas; area++)
    //    {
    //        SimulateAreaUnlock(area, logDetails: false);
    //    }

    //    Debug.Log($"[LevelProgressionManager] TODAS LAS AREAS DESBLOQUEADAS (1-{TotalAreas})");
    //    Debug.Log($"[LevelProgressionManager] Total de niveles desbloqueados: {TotalAreas * LevelsPerArea}");
    //    Debug.Log($"[LevelProgressionManager] Estrellas totales simuladas: {SaveManager.Instance.GetGameData().totalStars}");

    //    OnProgressionUpdated?.Invoke();
    //}

    //public void SimulateAreaUnlock(int areaId, bool logDetails = true)
    //{
    //    if (SaveManager.Instance == null)
    //    {
    //        return;
    //    }

    //    if (areaId < 1 || areaId > TotalAreas)
    //    {
    //        return;
    //    }

    //    var gameData = SaveManager.Instance.GetGameData();

    //    int firstLevelInArea = ((areaId - 1) * LevelsPerArea) + 1;
    //    int lastLevelInArea = areaId * LevelsPerArea;

    //    if (logDetails)
    //    {
    //        Debug.Log($"[LevelProgressionManager] SIMULANDO DESBLOQUEO AREA {areaId}");
    //        Debug.Log($"[LevelProgressionManager] Niveles: {firstLevelInArea} - {lastLevelInArea}");
    //    }

    //    int starsAddedThisArea = 0;

    //    for (int levelId = firstLevelInArea; levelId <= lastLevelInArea; levelId++)
    //    {
    //        int starsForLevel = UnityEngine.Random.Range(1, 4);

    //        if (gameData.levelStars.ContainsKey(levelId))
    //        {
    //            starsForLevel = Mathf.Max(gameData.levelStars[levelId], starsForLevel);
    //        }

    //        gameData.levelStars[levelId] = starsForLevel;
    //        starsAddedThisArea += starsForLevel;

    //        if (logDetails)
    //        {
    //            Debug.Log($"[LevelProgressionManager] Nivel {levelId}: {starsForLevel}");
    //        }
    //    }

    //    RecalculateTotalStars(gameData);

    //    if (!gameData.unlockedAreas.Contains(areaId))
    //    {
    //        gameData.unlockedAreas.Add(areaId);
    //    }

    //    if (areaId > gameData.highestUnlockedArea)
    //    {
    //        gameData.highestUnlockedArea = areaId;
    //    }

    //    if (lastLevelInArea > gameData.highestUnlockedLevel)
    //    {
    //        gameData.highestUnlockedLevel = lastLevelInArea;
    //    }

    //    SaveManager.Instance.SaveData();

    //    if (logDetails)
    //    {
    //        Debug.Log($"[LevelProgressionManager] Area {areaId} desbloqueada completamente");
    //        Debug.Log($"[LevelProgressionManager] Estrellas ganadas en area: {starsAddedThisArea}");
    //        Debug.Log($"[LevelProgressionManager] Total de estrellas acumuladas: {gameData.totalStars}");
    //    }

    //    OnProgressionUpdated?.Invoke();
    //}

    //public void SimulateUnlockUpToArea(int targetArea)
    //{
    //    if (targetArea < 1 || targetArea > TotalAreas)
    //    {
    //        Debug.LogError($"[LevelProgressionManager] Area objetivo invalida: {targetArea}");
    //        return;
    //    }

    //    Debug.Log($"[LevelProgressionManager] DESBLOQUEANDO HASTA AREA {targetArea}");

    //    for (int area = 1; area <= targetArea; area++)
    //    {
    //        SimulateAreaUnlock(area, logDetails: false);
    //    }

    //    Debug.Log($"[LevelProgressionManager] Areas 1-{targetArea} desbloqueadas");
    //    Debug.Log($"[LevelProgressionManager] Total estrellas: {SaveManager.Instance.GetGameData().totalStars}");

    //    OnProgressionUpdated?.Invoke();
    //}

    //private void RecalculateTotalStars(GameData gameData)
    //{
    //    int total = 0;
    //    foreach (var kvp in gameData.levelStars)
    //    {
    //        total += kvp.Value;
    //    }
    //    gameData.totalStars = total;
    //}

    //[ContextMenu("Debug/Resetear Progreso Area 1")]
    //private void ResetArea1Progress() => ResetAreaProgress(1);

    //[ContextMenu("Debug/Resetear Progreso Area 2")]
    //private void ResetArea2Progress() => ResetAreaProgress(2);

    //public void ResetAreaProgress(int areaId)
    //{
    //    if (SaveManager.Instance == null)
    //    {
    //        Debug.LogError("[LevelProgressionManager] SaveManager no disponible");
    //        return;
    //    }

    //    if (areaId < 1 || areaId > TotalAreas)
    //    {
    //        Debug.LogError($"[LevelProgressionManager] Area invalida: {areaId}");
    //        return;
    //    }

    //    var gameData = SaveManager.Instance.GetGameData();

    //    int firstLevelInArea = ((areaId - 1) * LevelsPerArea) + 1;
    //    int lastLevelInArea = areaId * LevelsPerArea;

    //    Debug.Log($"[LevelProgressionManager] Reseteando area {areaId} (niveles {firstLevelInArea}-{lastLevelInArea})");

    //    for (int levelId = firstLevelInArea; levelId <= lastLevelInArea; levelId++)
    //    {
    //        if (gameData.levelStars.ContainsKey(levelId))
    //        {
    //            gameData.levelStars.Remove(levelId);
    //        }

    //        if (gameData.levelObjectives.ContainsKey(levelId))
    //        {
    //            gameData.levelObjectives.Remove(levelId);
    //        }

    //        if (gameData.levelProgressData.ContainsKey(levelId))
    //        {
    //            gameData.levelProgressData.Remove(levelId);
    //        }
    //    }

    //    RecalculateTotalStars(gameData);

    //    if (areaId > 1 && gameData.unlockedAreas.Contains(areaId))
    //    {
    //        gameData.unlockedAreas.Remove(areaId);
    //    }

    //    SaveManager.Instance.SaveData();

    //    Debug.Log($"[LevelProgressionManager] Area {areaId} reseteada");
    //    Debug.Log($"[LevelProgressionManager] Estrellas totales restantes: {gameData.totalStars}");

    //    OnProgressionUpdated?.Invoke();
    //}

    //[ContextMenu("Debug/Mostrar Progreso")]
    //private void ShowCurrentProgress()
    //{
    //    if (SaveManager.Instance == null)
    //    {
    //        Debug.LogError("[LevelProgressionManager] SaveManager no disponible");
    //        return;
    //    }

    //    var gameData = SaveManager.Instance.GetGameData();

    //    Debug.Log("[PROGRESO ACTUAL]");
    //    Debug.Log($"Area mas alta desbloqueada: {gameData.highestUnlockedArea}");
    //    Debug.Log($"Nivel mas alto desbloqueado: {gameData.highestUnlockedLevel}");
    //    Debug.Log($"Total de estrellas: {gameData.totalStars}");
    //    Debug.Log($"Areas desbloqueadas: {string.Join(", ", gameData.unlockedAreas)}");

    //    Debug.Log("\n Desglose por area");
    //    for (int area = 1; area <= TotalAreas; area++)
    //    {
    //        int firstLevel = ((area - 1) * LevelsPerArea) + 1;
    //        int lastLevel = area * LevelsPerArea;
    //        int starsInArea = 0;
    //        int levelsCompleted = 0;

    //        for (int levelId = firstLevel; levelId <= lastLevel; levelId++)
    //        {
    //            if (gameData.levelStars.TryGetValue(levelId, out int stars))
    //            {
    //                starsInArea += stars;
    //                levelsCompleted++;
    //            }
    //        }

    //        bool isUnlocked = gameData.unlockedAreas.Contains(area);
    //        string status = isUnlocked ? "DESBLOQUEADA" : "BLOQUEADA";

    //        Debug.Log($"Area {area} ({status}): {levelsCompleted}/{LevelsPerArea} niveles | {starsInArea} ⭐");
    //    }
    //}

#endif
}