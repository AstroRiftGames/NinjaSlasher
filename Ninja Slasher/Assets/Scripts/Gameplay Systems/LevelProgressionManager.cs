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
        //Debug.Log($"Progreso consecutivo cargado: {currentConsecutiveWins} intentos, último nivel: {lastCompletedLevel}");
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

            Debug.Log($"Nivel {levelId} jugado consecutivamente. Total intentos: {currentConsecutiveWins}/{levelsRequiredForAd}");

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
            Debug.Log($"Secuencia reiniciada. Nuevo inicio en nivel {levelId}");
        }

        SaveConsecutiveProgress();
    }

    private void ShowConsecutiveLevelAd()
    {
        Debug.Log($"¡{levelsRequiredForAd} niveles jugados consecutivamente! Mostrando publicidad intersticial...");

        if (AdsManager.Instance != null && AdsManager.Instance.IsInterstitialAdReady())
        {
            AdsManager.Instance.ShowInterstitialAd();
        }
        else
        {
            Debug.LogWarning("AdsManager no disponible o anuncio intersticial no listo");
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.ReloadAllAds();
            }
        }
    }

    private void ShowAreaUnlockAd(int newAreaId)
    {
        Debug.Log($"¡Nueva área {newAreaId} desbloqueada! Mostrando publicidad de celebración...");

        if (AdsManager.Instance != null && AdsManager.Instance.IsInterstitialAdReady())
        {
            AdsManager.Instance.ShowInterstitialAd();
        }
        else
        {
            Debug.LogWarning("AdsManager no disponible o anuncio intersticial no listo para área desbloqueada");
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