using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SaveManager : MonoBehaviourSingleton<SaveManager>
{
    [Header("Save Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool autoMigration = true;

    private static string SaveFileName = "ninja_save.json";
    private const string USER_DATA_FOLDER = "UserData";

    private string saveFilePath;
    private GameData gameData;
    private string currentUserId = "";
    private bool isDataLoaded = false;
    public bool IsDataLoaded => isDataLoaded;
    private bool hasAuthIntegration = false;

    private bool _resetInProgress;
    public bool ResetInProgress => _resetInProgress;

    public static event Action<GameData> OnDataLoaded;
    public static event Action<GameData> OnDataSaved;

    internal void BeginReset() => _resetInProgress = true;
    internal void EndReset() => _resetInProgress = false;

    public override void Awake()
    {
        base.Awake();

        InitializeOfflineMode();

        Invoke(nameof(TrySetupAuthIntegration), 0.1f);
    }

    private void InitializeOfflineMode()
    {
        currentUserId = "local_" + SystemInfo.deviceUniqueIdentifier;
        saveFilePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        LoadData();
    }

    private void TrySetupAuthIntegration()
    {
        if (LoginManager.Instance != null)
        {
            LoginManager.OnAuthenticationStateChanged += OnAuthenticationChanged;
            LoginManager.OnSignInCompleted += OnUserSignedIn;
            hasAuthIntegration = true;

            if (IsUserAuthenticated())
            {
                MigrateToAuthenticatedUser();
            }

            if (debugMode)
                Debug.Log("[SaveManager] Authentication integration enabled");
        }
        else
        {
            if (debugMode)
                Debug.Log("[SaveManager] No authentication available, using offline mode");
        }
    }

    void OnDestroy()
    {
        if (hasAuthIntegration && LoginManager.Instance != null)
        {
            LoginManager.OnAuthenticationStateChanged -= OnAuthenticationChanged;
            LoginManager.OnSignInCompleted -= OnUserSignedIn;
        }
    }

    #region Data Loading and Migration

    public void LoadData()
    {
        if (debugMode)
            Debug.Log($"[SaveManager] Loading data from: {saveFilePath}");

        if (TryLoadFromCurrentPath())
        {
            isDataLoaded = true;
            OnDataLoaded?.Invoke(gameData);
            return;
        }

        if (debugMode)
            Debug.Log("[SaveManager] Creating new game data");

        gameData = new GameData();
        InitializeNewGameData();
        SaveData();
        isDataLoaded = true;
        OnDataLoaded?.Invoke(gameData);
    }

    private bool TryLoadFromCurrentPath()
    {
        if (!File.Exists(saveFilePath)) return false;

        try
        {
            string json = File.ReadAllText(saveFilePath);

            bool loadedWithDto = false;
            try
            {
                var dto = JsonUtility.FromJson<GameDataDTO>(json);
                if (dto != null && (dto.levelStars != null || dto.highestUnlockedLevel != 0 || !string.IsNullOrEmpty(dto.lastPlayDate)))
                {
                    gameData = GameDataMapper.FromDto(dto);
                    loadedWithDto = true;
                }
            }
            catch { /* ignorar */ }

            if (!loadedWithDto)
            {
                gameData = JsonUtility.FromJson<GameData>(json);
            }

            ValidateAndInitializeProgressionData();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load save file: {e.Message}");
            return false;
        }
    }

    private void MigrateToAuthenticatedUser()
    {
        string authenticatedUserId = GetAuthenticatedUserId();
        if (authenticatedUserId == currentUserId) return;

        if (debugMode)
            Debug.Log($"[SaveManager] Migrating from {currentUserId} to {authenticatedUserId}");

        GameData offlineData = gameData;

        currentUserId = authenticatedUserId;
        string userFolder = Path.Combine(Application.persistentDataPath, USER_DATA_FOLDER, currentUserId);
        if (!Directory.Exists(userFolder)) Directory.CreateDirectory(userFolder);
        saveFilePath = Path.Combine(userFolder, SaveFileName);

        if (TryLoadFromCurrentPath())
        {
            if (debugMode) Debug.Log("[SaveManager] Loaded existing authenticated user data");
            if (offlineData != null)
            {
                MergeGameData(offlineData, gameData);
                SaveData();
                if (debugMode) Debug.Log("[SaveManager] Merged offline changes into authenticated save");
            }
        }
        else if (autoMigration && offlineData != null)
        {
            // Si no existe, migramos el estado actual tal cual
            gameData = offlineData;
            SaveData();
            if (debugMode) Debug.Log("[SaveManager] Migrated data to authenticated user");
        }

        OnDataLoaded?.Invoke(gameData);
    }

    [Serializable]
    private class DailyRewardMirror
    {
        public string lastClaimDate;
    }

    private DateTime ParseIsoOrDefault(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return DateTime.MinValue;
        if (DateTime.TryParse(iso, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) return dt;
        if (DateTime.TryParse(iso, out dt)) return dt;
        return DateTime.MinValue;
    }

    private void MergeGameData(GameData source, GameData target)
    {
        if (source == null || target == null) return;

        target.highestUnlockedLevel = Mathf.Max(target.highestUnlockedLevel, source.highestUnlockedLevel);
        target.highestUnlockedArea = Mathf.Max(target.highestUnlockedArea, source.highestUnlockedArea);
        target.currentArea = Mathf.Max(target.currentArea, source.currentArea);

        if (source.levelStars != null)
        {
            if (target.levelStars == null) target.levelStars = new Dictionary<int, int>();
            foreach (var kv in source.levelStars)
            {
                if (!target.levelStars.ContainsKey(kv.Key))
                    target.levelStars[kv.Key] = kv.Value;
                else
                    target.levelStars[kv.Key] = Mathf.Max(target.levelStars[kv.Key], kv.Value);
            }
        }
        target.totalStars = 0;
        if (target.levelStars != null)
            foreach (var kv in target.levelStars) target.totalStars += Mathf.Max(0, kv.Value);

        if (source.unlockedAreas != null)
        {
            if (target.unlockedAreas == null) target.unlockedAreas = new List<int>();
            foreach (var a in source.unlockedAreas)
                if (!target.unlockedAreas.Contains(a)) target.unlockedAreas.Add(a);
        }

        if (source.powerUpInventory != null)
        {
            if (target.powerUpInventory == null) target.powerUpInventory = new List<PowerUpInventoryItem>();
            foreach (var item in source.powerUpInventory)
            {
                var dst = target.powerUpInventory.Find(i => i.type == item.type);
                if (dst == null)
                {
                    target.powerUpInventory.Add(new PowerUpInventoryItem(item.type, item.quantity)
                    {
                        lastUpdated = item.lastUpdated
                    });
                }
                else
                {
                    dst.quantity += item.quantity;
                    if (item.lastUpdated > dst.lastUpdated) dst.lastUpdated = item.lastUpdated;
                }
            }
        }

        if (source.activePowerUps != null)
        {
            if (target.activePowerUps == null) target.activePowerUps = new List<PowerUpData>();
            foreach (var p in source.activePowerUps)
            {
                var existing = target.activePowerUps.Find(x => x.type == p.type);
                if (existing == null || p.activationTime > existing.activationTime)
                {
                    if (existing != null) target.activePowerUps.Remove(existing);
                    target.activePowerUps.Add(p);
                }
            }
        }

        try
        {
            var src = string.IsNullOrEmpty(source.dailyRewardData) ? null : JsonUtility.FromJson<DailyRewardMirror>(source.dailyRewardData);
            var dst = string.IsNullOrEmpty(target.dailyRewardData) ? null : JsonUtility.FromJson<DailyRewardMirror>(target.dailyRewardData);
            var srcDate = src != null ? ParseIsoOrDefault(src.lastClaimDate) : DateTime.MinValue;
            var dstDate = dst != null ? ParseIsoOrDefault(dst.lastClaimDate) : DateTime.MinValue;
            if (srcDate > dstDate) target.dailyRewardData = source.dailyRewardData;
        }
        catch { /* si falla parse, dejamos el existente */ }

        if (source.lastPlayDate > target.lastPlayDate) target.lastPlayDate = source.lastPlayDate;
    }

    #endregion

    #region Authentication Integration

    private void OnAuthenticationChanged(bool isAuthenticated)
    {
        if (debugMode)
            Debug.Log($"[SaveManager] Authentication state changed: {isAuthenticated}");

        if (isAuthenticated)
        {
            MigrateToAuthenticatedUser();
        }
    }

    private void OnUserSignedIn(string playerId)
    {
        if (debugMode)
            Debug.Log($"[SaveManager] User signed in with ID: {playerId}");

        MigrateToAuthenticatedUser();
    }

    private string GetAuthenticatedUserId()
    {
        if (IsUserAuthenticated())
        {
            return LoginManager.Instance.PlayerId;
        }
        return currentUserId;
    }

    private bool IsUserAuthenticated()
    {
        try
        {
            return hasAuthIntegration &&
                   LoginManager.Instance != null &&
                   LoginManager.Instance.IsSignedIn;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Original SaveManager Methods (preserved for compatibility)

    public void SaveData()
    {
        if (gameData == null)
        {
            Debug.LogWarning("[SaveManager] Attempted to save null gameData");
            return;
        }

        gameData.lastPlayDate = DateTime.Now;

        try
        {
            string directory = Path.GetDirectoryName(saveFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var dto = GameDataMapper.ToDto(gameData);
            string json = JsonUtility.ToJson(dto, true);

            File.WriteAllText(saveFilePath, json);

            if (debugMode)
                Debug.Log($"[SaveManager] Data saved successfully to: {saveFilePath}");

            OnDataSaved?.Invoke(gameData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save data: {e.Message}");
        }
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

        if (gameData.levelProgressData == null)
        {
            gameData.levelProgressData = new Dictionary<int, LevelProgressData>();
        }
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

    public void SetMusicVolume(float volume)
    {
        var data = GetGameData();
        data.musicVolume = Mathf.Clamp01(volume);
        SaveData();
    }

    public void SetSFXVolume(float volume)
    {
        var data = GetGameData();
        data.sfxVolume = Mathf.Clamp01(volume);
        SaveData();
    }

    public void SaveDailyRewardData(string dailyRewardJson)
    {
        var data = GetGameData();
        data.dailyRewardData = dailyRewardJson;
        data.lastRewardTimestamp = DateTime.Now.ToString("o");
        SaveData();
    }

    public string GetDailyRewardData()
    {
        var data = GetGameData();
        return data.dailyRewardData;
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
        return data.IsObjectiveCompleted(levelId, objective.name);
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

    public void ActivatePowerUp(PowerUpType powerUpType, float duration)
    {
        RemovePowerUpFromInventory(powerUpType, 1);

        var data = GetGameData();
        var existingActivePowerUp = data.activePowerUps.Find(p => p.type == powerUpType);
        if (existingActivePowerUp != null)
        {
            data.activePowerUps.Remove(existingActivePowerUp);
        }

        data.activePowerUps.Add(new PowerUpData
        {
            type = powerUpType,
            activationTime = DateTime.Now,
            duration = duration
        });
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

    public void UpdateActivePowerUps()
    {
        var data = GetGameData();
        bool hasChanges = false;
        var currentTime = DateTime.Now;

        for (int i = data.activePowerUps.Count - 1; i >= 0; i--)
        {
            var powerUp = data.activePowerUps[i];
            var timeElapsed = (currentTime - powerUp.activationTime).TotalSeconds;

            if (timeElapsed >= powerUp.duration)
            {
                data.activePowerUps.RemoveAt(i);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
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

    public void SaveOnApplicationEvent()
    {
        if (_resetInProgress) return;
        SaveData();
    }

    public void ShowProgressionDebug()
    {
        var data = GetGameData();
        Debug.Log($"[SaveManager] ESTADO ACTUAL:\n" +
                  $"- Usuario: {currentUserId}\n" +
                  $"- Autenticado: {IsUserAuthenticated()}\n" +
                  $"- Nivel más alto: {data.highestUnlockedLevel}\n" +
                  $"- Área más alta: {data.highestUnlockedArea}\n" +
                  $"- Estrellas totales: {data.totalStars}\n" +
                  $"- Áreas desbloqueadas: [{string.Join(", ", data.unlockedAreas)}]");
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
        Debug.Log($"Is Authenticated: {IsUserAuthenticated()}");
        Debug.Log($"Save File Path: {saveFilePath}");
        Debug.Log($"Data Loaded: {isDataLoaded}");
        Debug.Log($"Auth Integration: {hasAuthIntegration}");
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

        var debugUI = FindObjectOfType<DebugUIManager>();
        if (debugUI != null)
        {
            debugUI.ShowStarsDebug();
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

            string userDataRoot = Path.Combine(Application.persistentDataPath, USER_DATA_FOLDER);
            if (Directory.Exists(userDataRoot))
            {
                foreach (var dir in Directory.GetDirectories(userDataRoot))
                {
                    string f = Path.Combine(dir, SaveFileName);
                    SafeDeleteFile(f);

                    TryDeleteDirectoryIfEmpty(dir);
                }
            }
                     
            gameData = new GameData();
            InitializeNewGameData();
            isDataLoaded = true;
            SaveData();

            if (debugMode)
            {
                Debug.Log($"[SaveManager] Deep local reset completed.\n" +
                          $"Root: {rootFile}\nUserData: {userDataRoot}\nCurrentPath: {saveFilePath}");
            }

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
                if (debugMode) Debug.Log($"[SaveManager] Deleted file: {path}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to delete '{path}': {e.Message}");
        }
    }

    private void TryDeleteDirectoryIfEmpty(string dir)
    {
        try
        {
            if (!Directory.Exists(dir)) return;
            bool noFiles = Directory.GetFiles(dir).Length == 0;
            bool noDirs = Directory.GetDirectories(dir).Length == 0;
            if (noFiles && noDirs)
                Directory.Delete(dir, recursive: false);
        }
        catch (Exception e)
        {
            if (debugMode) Debug.Log($"[SaveManager] Could not clean empty dir '{dir}': {e.Message}");
        }
    }

    #endregion
}