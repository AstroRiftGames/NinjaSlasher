using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SaveManager : MonoBehaviourSingleton<SaveManager>
{
    [Header("Save Settings")]
    //[SerializeField] private bool debugMode = true;

    private static string SaveFileName = "ninja_save.json";
    private const string LocalUserId = "local_device";
    private const int MAX_POWERUP_STACK = 99;
    private const int MAX_POWERUP_USES  = 99;
    private const int DEFAULT_MAX_LIVES = 5;
    private const int DEFAULT_STARTING_LIVES = 5;

    private string saveFilePath;
    private GameData gameData;
    private string currentUserId = "";
    private bool isDataLoaded = false;
    public bool IsDataLoaded => isDataLoaded;

    private bool _resetInProgress;
    public bool ResetInProgress => _resetInProgress;

    private bool isSaving;

    public static event Action<GameData> OnDataLoaded;
    public static event Action<GameData> OnDataSaved;

    internal void BeginReset() => _resetInProgress = true;
    internal void EndReset() => _resetInProgress = false;

    public override void Awake()
    {
        base.Awake();

        InitializeOfflineMode();
    }

    private void InitializeOfflineMode()
    {
        currentUserId = LocalUserId;
        saveFilePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        LoadData();
    }

    #region Data Loading and Migration

    public void LoadData()
    {
        //if (debugMode)
        //    Debug.Log($"[SaveManager] Loading data from: {saveFilePath}");

        if (TryLoadFromCurrentPath())
        {
            isDataLoaded = true;
            OnDataLoaded?.Invoke(gameData);
            PowerUpManager.Instance?.ReloadFromSave();
            return;
        }

        //if (debugMode)
        //    Debug.Log("[SaveManager] Creating new game data");

        gameData = new GameData();
        InitializeNewGameData();
        SaveData();
        isDataLoaded = true;
        OnDataLoaded?.Invoke(gameData);
        PowerUpManager.Instance?.ReloadFromSave();
    }

    private bool TryLoadFromCurrentPath()
    {
        string backupPath = saveFilePath + ".bak";

        if (TryLoadFromFile(saveFilePath)) return true;

        if (File.Exists(backupPath))
        {
            Debug.LogWarning("[SaveManager] Save principal no disponible o corrupto. Recuperando desde backup.");
            if (TryLoadFromFile(backupPath))
            {
                try { File.Copy(backupPath, saveFilePath, overwrite: true); } catch { /* no crítico */ }
                return true;
            }
        }

        return false;
    }

    private bool TryLoadFromFile(string path)
    {
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path);

            bool loadedWithDto = false;
            try
            {
                var dto = JsonUtility.FromJson<GameDataDTO>(json);
                bool hasDtoMarker = json.Contains("\"saveVersion\"");
                if (hasDtoMarker && dto != null)
                {
                    gameData = GameDataMapper.FromDto(dto);
                    loadedWithDto = true;
                }
            }
            catch (Exception dtoEx) { Debug.LogWarning($"[SaveManager] DTO deserialization failed, falling back to legacy: {dtoEx.Message}"); }

            if (!loadedWithDto)
            {
                gameData = JsonUtility.FromJson<GameData>(json);
            }

            ValidateAndInitializeProgressionData();
            Debug.Log($"[SaveManager] LoadLives | path='{path}' | lives={gameData.currentLives} | timestamp='{gameData.lastLifeRegenTime}' | canRegen={gameData.canRegenLives} | unlimitedLivesEndUtc={gameData.unlimitedLivesEndUtc}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load '{path}': {e.Message}");
            return false;
        }
    }

    #endregion

    #region Original SaveManager Methods

    public void SaveData()
    {
        if (isSaving) return;

        if (gameData == null)
        {
            Debug.LogWarning("[SaveManager] Attempted to save null gameData");
            return;
        }

        isSaving = true;
        try
        {
            gameData.lastPlayDate = DateTime.Now;

            string directory = Path.GetDirectoryName(saveFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var dto = GameDataMapper.ToDto(gameData);
            string json = JsonUtility.ToJson(dto, true);

            Debug.Log($"[SaveManager] SaveData | path='{saveFilePath}' | lives={gameData.currentLives} | timestamp='{gameData.lastLifeRegenTime}' | canRegen={gameData.canRegenLives} | unlimitedLivesEndUtc={gameData.unlimitedLivesEndUtc}");

            string tempPath   = saveFilePath + ".tmp";
            string backupPath = saveFilePath + ".bak";

            File.WriteAllText(tempPath, json);

            if (File.Exists(saveFilePath))
                File.Replace(tempPath, saveFilePath, backupPath);
            else
                File.Move(tempPath, saveFilePath);

            OnDataSaved?.Invoke(gameData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save data: {e.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    public void Modify(Action<GameData> mutation)
    {
        mutation(GetGameData());
        SaveData();
    }

    private void ValidateAndInitializeProgressionData()
    {
        if (gameData == null) return;

        if (gameData.highestUnlockedLevel <= 1 && gameData.levelStars != null && gameData.levelStars.Count > 0)
        {
            RecalculateProgressionFromStars();
        }

        if (gameData.highestUnlockedLevel < 1) gameData.highestUnlockedLevel = 1;
        if (gameData.highestUnlockedArea < 1) gameData.highestUnlockedArea = 1;

        int maxLevel = GameConfigManager.IsReady()
            ? GameConfigManager.Config.levelsPerArea * GameConfigManager.Config.totalAreas
            : 50;
        gameData.highestUnlockedLevel = Mathf.Clamp(gameData.highestUnlockedLevel, 1, maxLevel);

        gameData.coins = Mathf.Max(0, gameData.coins);

        if (gameData.powerUpInventory != null)
            foreach (var item in gameData.powerUpInventory)
                item.quantity = Mathf.Clamp(item.quantity, 0, MAX_POWERUP_STACK);

        if (gameData.activePowerUps != null)
            foreach (var item in gameData.activePowerUps)
                item.usesRemaining = Mathf.Clamp(item.usesRemaining, 0, MAX_POWERUP_USES);

        if (gameData.levelProgressData == null)
        {
            gameData.levelProgressData = new Dictionary<int, LevelProgressData>();
        }

        if (gameData.tutorialStates == null)
        {
            gameData.tutorialStates = new Dictionary<string, int>();
        }

        if (gameData.tutorialStepIndices == null)
        {
            gameData.tutorialStepIndices = new Dictionary<string, int>();
        }

        RepairLifeDataIfNeeded();
    }

    private void RecalculateProgressionFromStars()
    {
        if (gameData?.levelStars == null) return;

        int highestCompletedLevel = 0;

        foreach (var levelStar in gameData.levelStars)
        {
            if (levelStar.Value >= 1)
            {
                highestCompletedLevel = Mathf.Max(highestCompletedLevel, levelStar.Key);
            }
        }

        if (highestCompletedLevel > 0)
        {
            gameData.highestUnlockedLevel = highestCompletedLevel + 1;
            gameData.highestUnlockedArea = Mathf.Min(((highestCompletedLevel - 1) / 10) + 1, 5);
        }
    }

    private void InitializeNewGameData()
    {
        if (gameData == null) return;

        gameData.highestUnlockedLevel = 1;
        gameData.highestUnlockedArea = 1;
        gameData.totalStars = 0;

        if (gameData.levelProgressData == null)
        {
            gameData.levelProgressData = new Dictionary<int, LevelProgressData>();
        }

        if (gameData.tutorialStates == null)
        {
            gameData.tutorialStates = new Dictionary<string, int>();
        }

        if (gameData.tutorialStepIndices == null)
        {
            gameData.tutorialStepIndices = new Dictionary<string, int>();
        }

        InitializeLifeDataForNewSave();
    }

    public GameData GetGameData()
    {
        if (gameData == null)
        {
            Debug.LogWarning("[SaveManager] gameData is null, creating new instance");
            gameData = new GameData();
            InitializeNewGameData();
        }
        return gameData;
    }

    public void UpdateLives(int lives, DateTime lastRegen, bool canRegen)
    {
        Debug.Log($"[SaveManager] SaveLives | lives={lives} | timestamp='{lastRegen:o}' | canRegen={canRegen} | kind={lastRegen.Kind}");
        var data = GetGameData();
        data.currentLives = lives;
        data.lastLifeRegenTime = lastRegen.ToString("o");
        data.canRegenLives = canRegen;
        SaveData();
    }

    public void UpdateStars(int level, int stars)
    {
        var data = GetGameData();
        if (data == null) return;

        int previousStars = 0;

        if (data.levelStars.ContainsKey(level))
        {
            previousStars = data.levelStars[level];
            if (data.levelStars[level] < stars)
            {
                data.totalStars += (stars - data.levelStars[level]);
            }
            data.levelStars[level] = stars;
        }
        else
        {
            data.levelStars.Add(level, stars);
            data.totalStars += stars;
        }

        if (stars >= 1 && previousStars == 0)
        {
            if (level >= data.highestUnlockedLevel)
            {
                data.highestUnlockedLevel = level + 1;
            }

            int highestCompletedLevel = 0;
            foreach (var levelStar in data.levelStars)
            {
                if (levelStar.Value >= 1)
                {
                    highestCompletedLevel = Mathf.Max(highestCompletedLevel, levelStar.Key);
                }
            }

            int calculatedArea = Mathf.Min(((highestCompletedLevel - 1) / 10) + 1, 5);
            if (calculatedArea > data.highestUnlockedArea)
            {
                data.highestUnlockedArea = calculatedArea;
            }
        }

        SaveData();
    }

    public void UpdateHighestUnlockedLevel(int newHighestLevel)
    {
        var data = GetGameData();

        if (newHighestLevel > data.highestUnlockedLevel)
        {
            data.highestUnlockedLevel = newHighestLevel;
            SaveData();
        }
    }

    [Obsolete("Use SetAudioSettings(musicVolume, sfxVolume) to avoid a double disk write.")]
    public void SetMusicVolume(float volume)
    {
        var data = GetGameData();
        data.musicVolume = Mathf.Clamp01(volume);
        SaveData();
    }

    [Obsolete("Use SetAudioSettings(musicVolume, sfxVolume) to avoid a double disk write.")]
    public void SetSFXVolume(float volume)
    {
        var data = GetGameData();
        data.sfxVolume = Mathf.Clamp01(volume);
        SaveData();
    }

    public void SetAudioSettings(float musicVolume, float sfxVolume)
    {
        var data = GetGameData();
        data.musicVolume = Mathf.Clamp01(musicVolume);
        data.sfxVolume   = Mathf.Clamp01(sfxVolume);
        SaveData();
    }

    public void SetAudioToggleState(bool musicEnabled, bool sfxEnabled)
    {
        var data = GetGameData();
        data.musicEnabled = musicEnabled;
        data.sfxEnabled   = sfxEnabled;
        SaveData();
    }

    public void SaveDailyRewardData(string dailyRewardJson, bool updateLastRewardTimestamp = true)
    {
        var data = GetGameData();
        data.dailyRewardData = dailyRewardJson;
        if (updateLastRewardTimestamp)
            data.lastRewardTimestamp = DateTime.Now.ToString("o");
        SaveData();
    }

    public string GetDailyRewardData()
    {
        var data = GetGameData();
        return data.dailyRewardData;
    }

    public bool GetAdsRemoved()
    {
        return GetGameData().adsRemoved;
    }

    public void SetAdsRemoved(bool value)
    {
        var data = GetGameData();
        if (data.adsRemoved == value) return;

        data.adsRemoved = value;
        SaveData();
    }

    public void UpdateLevelProgress(int level)
    {
        var data = GetGameData();
        if (level > data.highestUnlockedLevel)
        {
            data.highestUnlockedLevel = level;
        }
        SaveData();
    }

    public void UpdateLevelProgression(int levelId, int starsEarned)
    {
        UpdateStars(levelId, starsEarned);
    }

    public void SaveLevelProgress(int levelId, ObjectiveEvaluationResult result, LevelStats stats)
    {
        var data = GetGameData();
        data.UpdateLevelProgress(levelId, result, stats);
        SaveData();
    }

    public int GetTutorialState(string tutorialId)
    {
        return GetGameData().GetTutorialState(tutorialId);
    }

    public int GetTutorialStepIndex(string tutorialId)
    {
        return GetGameData().GetTutorialStepIndex(tutorialId);
    }

    public void SaveTutorialProgress(string tutorialId, int state, int stepIndex)
    {
        var data = GetGameData();
        data.SetTutorialProgress(tutorialId, state, stepIndex);
        SaveData();
    }

    public LevelProgressData GetLevelProgressData(int levelId)
    {
        var data = GetGameData();
        return data.GetLevelProgress(levelId);
    }

    public void UnlockNewArea(int areaId)
    {
        var data = GetGameData();

        if (areaId > data.highestUnlockedArea)
        {
            data.highestUnlockedArea = areaId;

            if (!data.unlockedAreas.Contains(areaId))
            {
                data.unlockedAreas.Add(areaId);
            }

            SaveData();
        }
    }

    public (int highestLevel, int highestArea, int totalStars) GetProgressionData()
    {
        var data = GetGameData();
        return (data.highestUnlockedLevel, data.highestUnlockedArea, data.totalStars);
    }

    public bool IsObjectiveCompleted(int levelId, ObjectiveData objective)
    {
        var data = GetGameData();
        if (data == null || objective == null) return false;
        return data.IsObjectiveCompleted(levelId, objective.StableId)
            || data.IsObjectiveCompleted(levelId, objective.name);
    }

    public void UnlockArea(int areaId)
    {
        var data = GetGameData();
        if (areaId > data.currentArea)
        {
            data.currentArea = areaId;
        }
        SaveData();
    }

    public int GetPowerUpCount(PowerUpType powerUpType)
    {
        var item = GetGameData().powerUpInventory.Find(i => i.type == powerUpType);
        return item?.quantity ?? 0;
    }

    public bool ConsumePowerUp(PowerUpType powerUpType, int quantity = 1)
    {
        var data = GetGameData();
        var item = data.powerUpInventory.Find(i => i.type == powerUpType);
        if (item == null || item.quantity < quantity) return false;
        item.quantity -= quantity;
        item.lastUpdated = DateTime.Now;
        if (item.quantity == 0) data.powerUpInventory.Remove(item);
        SaveData();
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        GetGameData().coins += amount;
        SaveData();
        GameEvents.RaiseCoinsChanged(GetGameData().coins);
    }

    public int GetCoins() => GetGameData().coins;

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || GetGameData().coins < amount) return false;
        GetGameData().coins -= amount;
        SaveData();
        GameEvents.RaiseCoinsChanged(GetGameData().coins);
        return true;
    }

    public void AddPowerUpToInventory(PowerUpType powerUpType, int quantity = 1)
    {
        var data = GetGameData();
        var existingItem = data.powerUpInventory.Find(item => item.type == powerUpType);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
            existingItem.lastUpdated = DateTime.Now;
        }
        else
        {
            data.powerUpInventory.Add(new PowerUpInventoryItem(powerUpType, quantity));
        }
        SaveData();
    }

    public void RemovePowerUpFromInventory(PowerUpType powerUpType, int quantity = 1)
    {
        var data = GetGameData();
        var existingItem = data.powerUpInventory.Find(item => item.type == powerUpType);
        if (existingItem != null)
        {
            existingItem.quantity = Mathf.Max(0, existingItem.quantity - quantity);
            existingItem.lastUpdated = DateTime.Now;
            if (existingItem.quantity == 0)
            {
                data.powerUpInventory.Remove(existingItem);
            }
        }
        SaveData();
    }

    public void ActivatePowerUp(PowerUpType powerUpType, int uses)
    {
        var data = GetGameData();

        var inventoryItem = data.powerUpInventory.Find(item => item.type == powerUpType);
        if (inventoryItem != null)
        {
            inventoryItem.quantity = Mathf.Max(0, inventoryItem.quantity - 1);
            inventoryItem.lastUpdated = DateTime.Now;
            if (inventoryItem.quantity == 0)
                data.powerUpInventory.Remove(inventoryItem);
        }

        var existingActivePowerUp = data.activePowerUps.Find(p => p.type == powerUpType);
        if (existingActivePowerUp != null)
            existingActivePowerUp.usesRemaining += uses;
        else
            data.activePowerUps.Add(new PowerUpData(powerUpType, uses));

        SaveData();
    }

    public void DeactivatePowerUp(PowerUpType powerUpType)
    {
        var data = GetGameData();
        var powerUpToRemove = data.activePowerUps.Find(p => p.type == powerUpType);
        if (powerUpToRemove != null)
        {
            data.activePowerUps.Remove(powerUpToRemove);
            SaveData();
        }
    }

    public void UpdatePowerUpUses(PowerUpType powerUpType, int usesRemaining)
    {
        var data = GetGameData();
        var powerUpData = data.activePowerUps.Find(p => p.type == powerUpType);

        if (powerUpData != null)
        {
            powerUpData.usesRemaining = usesRemaining;

            if (usesRemaining <= 0)
            {
                data.activePowerUps.Remove(powerUpData);
            }

            SaveData();
        }
    }

    public void UpdateGameStats(int enemiesKilled = 0, int combo = 0, float playTime = 0f, bool gameCompleted = false)
    {
        var data = GetGameData();

        if (gameCompleted)
        {
            data.totalGamesPlayed++;
        }

        if (enemiesKilled > 0)
        {
            data.totalEnemiesKilled += enemiesKilled;
        }

        if (combo > data.bestCombo)
        {
            data.bestCombo = combo;
        }

        if (playTime > 0f)
        {
            data.totalPlayTime += playTime;
        }

        SaveData();
    }

    public void SaveLevelCompletion(int levelId, int stars, int enemiesKilled, int maxCombo, float playTime)
    {
        var data = GetGameData();

        int nextLevel = levelId + 1;
        if (nextLevel > data.highestUnlockedLevel)
            data.highestUnlockedLevel = nextLevel;

        int previousStars = 0;
        if (data.levelStars.ContainsKey(levelId))
        {
            previousStars = data.levelStars[levelId];
            if (data.levelStars[levelId] < stars)
                data.totalStars += stars - data.levelStars[levelId];
            data.levelStars[levelId] = stars;
        }
        else
        {
            data.levelStars.Add(levelId, stars);
            data.totalStars += stars;
        }

        if (stars >= 1 && previousStars == 0)
        {
            if (levelId >= data.highestUnlockedLevel)
                data.highestUnlockedLevel = levelId + 1;

            int highestCompletedLevel = 0;
            foreach (var kv in data.levelStars)
                if (kv.Value >= 1)
                    highestCompletedLevel = Mathf.Max(highestCompletedLevel, kv.Key);

            int calculatedArea = Mathf.Min(((highestCompletedLevel - 1) / 10) + 1, 5);
            if (calculatedArea > data.highestUnlockedArea)
                data.highestUnlockedArea = calculatedArea;
        }

        data.totalGamesPlayed++;
        if (enemiesKilled > 0) data.totalEnemiesKilled += enemiesKilled;
        if (maxCombo > data.bestCombo) data.bestCombo = maxCombo;
        if (playTime > 0f) data.totalPlayTime += playTime;

        SaveData();
    }

    public void RecordEmergencyBundleActivation()
    {
        var data = GetGameData();
        long nowUtc  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long today   = nowUtc / 86400;
        long lastDay = data.emergencyBundleLastActivationUtc / 86400;

        data.emergencyBundleUsesToday = (data.emergencyBundleLastActivationUtc == 0 || today != lastDay)
                                        ? 1
                                        : data.emergencyBundleUsesToday + 1;

        data.emergencyBundleLastActivationUtc = nowUtc;
        data.pendingPurchaseProductId = "";
        SaveData();
    }

    public void SaveOnApplicationEvent()
    {
        if (_resetInProgress) return;
        SaveData();
    }

    private void InitializeLifeDataForNewSave()
    {
        int maxLives = GetConfiguredMaxLives();
        int startingLives = Mathf.Clamp(GetConfiguredStartingLives(), 0, maxLives);

        gameData.currentLives = startingLives;
        gameData.lastLifeRegenTime = DateTime.UtcNow.ToString("o");
        gameData.canRegenLives = startingLives < maxLives;
    }

    private void RepairLifeDataIfNeeded()
    {
        int maxLives = GetConfiguredMaxLives();
        int currentLives = gameData.currentLives;

        if (currentLives < 0 || currentLives > maxLives)
        {
            int clampedLives = Mathf.Clamp(currentLives, 0, maxLives);
            Debug.LogWarning($"[SaveManager] RepairLifeData | clamping lives from {currentLives} to {clampedLives}");
            gameData.currentLives = clampedLives;
        }

        if (!TryParseLifeTimestampUtc(gameData.lastLifeRegenTime, out DateTime parsedUtc))
        {
            parsedUtc = DateTime.UtcNow;
            Debug.LogWarning($"[SaveManager] RepairLifeData | invalid timestamp '{gameData.lastLifeRegenTime}'. Replacing with '{parsedUtc:o}'");
        }

        gameData.lastLifeRegenTime = parsedUtc.ToString("o");
        gameData.canRegenLives = gameData.currentLives < maxLives;
    }

    private bool TryParseLifeTimestampUtc(string value, out DateTime parsedUtc)
    {
        parsedUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            return false;

        parsedUtc = parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
        return true;
    }

    private int GetConfiguredMaxLives()
    {
        if (GameConfigManager.IsReady())
            return Mathf.Max(1, GameConfigManager.Config.maxLives);

        return DEFAULT_MAX_LIVES;
    }

    private int GetConfiguredStartingLives()
    {
        if (GameConfigManager.IsReady())
            return GameConfigManager.Config.startingLives;

        return DEFAULT_STARTING_LIVES;
    }

    public void ShowProgressionDebug()
    {
        var data = GetGameData();
        Debug.Log($"[SaveManager] ESTADO ACTUAL:\n" +
                  $"- Usuario: {currentUserId}\n" +
                  $"- Nivel m�s alto: {data.highestUnlockedLevel}\n" +
                  $"- �rea m�s alta: {data.highestUnlockedArea}\n" +
                  $"- Estrellas totales: {data.totalStars}\n" +
                  $"- �reas desbloqueadas: [{string.Join(", ", data.unlockedAreas)}]");
    }

    public void DeleteSaveData()
    {
        ResetAllLocalSaves(notify: true);
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Print User Info")]
    public void DebugPrintUserInfo()
    {
        Debug.Log($"Current User ID: {currentUserId}");
        Debug.Log($"Save File Path: {saveFilePath}");
        Debug.Log($"Data Loaded: {isDataLoaded}");
    }

    [ContextMenu("Force Reload Data")]
    public void DebugReloadData()
    {
        LoadData();
    }

    [ContextMenu("Add Test Stars")]
    public void AddTestStars()
    {
        var data = GetGameData();
        data.levelStars[1] = 3;
        data.levelStars[2] = 3;
        data.levelStars[3] = 1;
        data.totalStars = 7;
        data.highestUnlockedLevel = 4;
        SaveData();


        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated?.Invoke();
        }

        Debug.Log("Test stars added and UI refreshed");
    }

    [ContextMenu("DEBUG/Print DailyReward Fields")]
    public void DebugPrintDailyRewardFields()
    {
        var d = GetGameData();
        Debug.Log($"[DEBUG] dailyRewardData(len): {(d.dailyRewardData == null ? 0 : d.dailyRewardData.Length)}");
        Debug.Log($"[DEBUG] lastRewardTimestamp: {d.lastRewardTimestamp}");
    }

    public void ResetAllLocalSaves(bool notify = true)
    {
        try
        {
            string rootFile = Path.Combine(Application.persistentDataPath, SaveFileName);
            SafeDeleteFile(rootFile);
            SafeDeleteFile(rootFile + ".bak");
            SafeDeleteFile(rootFile + ".tmp");

            gameData = new GameData();
            InitializeNewGameData();
            isDataLoaded = true;
            SaveData();

            if (notify)
            {
                OnDataLoaded?.Invoke(gameData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] ResetAllLocalSaves error: {e.Message}");
        }
    }

    private void SafeDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                //if (debugMode) Debug.Log($"[SaveManager] Deleted file: {path}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to delete '{path}': {e.Message}");
        }
    }

    [ContextMenu("DEBUG: Delete All Data")]
    public void DebugDeleteAllData()
    {
        Debug.LogWarning("[SaveManager] BORRANDO TODOS LOS DATOS");

        ResetAllLocalSaves(notify: true);

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("[SaveManager] Todos los datos locales y PlayerPrefs han sido eliminados.");

        if (Application.isPlaying)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    #endregion
}
