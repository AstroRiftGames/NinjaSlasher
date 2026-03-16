using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SaveManager : MonoBehaviourSingleton<SaveManager>
{
    [Header("Save Settings")]
    //[SerializeField] private bool debugMode = true;
    [SerializeField] private bool autoMigration = true;

    private static string SaveFileName = "ninja_save.json";
    private const string USER_DATA_FOLDER = "UserData";
    private const int MAX_POWERUP_STACK = 99;
    private const int MAX_POWERUP_USES  = 99;

    private string saveFilePath;
    private GameData gameData;
    private string currentUserId = "";
    private bool isDataLoaded = false;
    public bool IsDataLoaded => isDataLoaded;
    private bool hasAuthIntegration = false;

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

        StartCoroutine(WaitForAuthIntegration());
    }

    private System.Collections.IEnumerator WaitForAuthIntegration()
    {
        while (LoginManager.Instance == null)
            yield return null;

        TrySetupAuthIntegration();
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

            //if (debugMode)
            //    Debug.Log("[SaveManager] Authentication integration enabled");
        }
        //else
        //{
        //    if (debugMode)
        //        Debug.Log("[SaveManager] No authentication available, using offline mode");
        //}
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
                if (dto != null && (dto.levelStars != null || dto.highestUnlockedLevel != 0 || !string.IsNullOrEmpty(dto.lastPlayDate)))
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
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load '{path}': {e.Message}");
            return false;
        }
    }

    private void MigrateToAuthenticatedUser()
    {
        string authenticatedUserId = GetAuthenticatedUserId();
        if (authenticatedUserId == currentUserId) return;

        //if (debugMode)
        //    Debug.Log($"[SaveManager] Migrating from {currentUserId} to {authenticatedUserId}");

        GameData offlineData = gameData;

        currentUserId = authenticatedUserId;
        string userFolder = Path.Combine(Application.persistentDataPath, USER_DATA_FOLDER, currentUserId);
        if (!Directory.Exists(userFolder)) Directory.CreateDirectory(userFolder);
        saveFilePath = Path.Combine(userFolder, SaveFileName);

        if (TryLoadFromCurrentPath())
        {
            //if (debugMode) Debug.Log("[SaveManager] Loaded existing authenticated user data");
            if (offlineData != null)
            {
                MergeGameData(offlineData, gameData);
                SaveData();
                //if (debugMode) Debug.Log("[SaveManager] Merged offline changes into authenticated save");
            }
        }
        else if (autoMigration && offlineData != null)
        {
            // Si no existe, migramos el estado actual tal cual
            gameData = offlineData;
            SaveData();
            //if (debugMode) Debug.Log("[SaveManager] Migrated data to authenticated user");
        }

        OnDataLoaded?.Invoke(gameData);
        PowerUpManager.Instance?.ReloadFromSave();
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

        target.adsRemoved = target.adsRemoved || source.adsRemoved;

        if (source.lastPlayDate > target.lastPlayDate) target.lastPlayDate = source.lastPlayDate;

        if (source.levelObjectives != null)
        {
            if (target.levelObjectives == null) target.levelObjectives = new Dictionary<int, List<int>>();
            foreach (var kv in source.levelObjectives)
            {
                if (!target.levelObjectives.ContainsKey(kv.Key))
                    target.levelObjectives[kv.Key] = new List<int>(kv.Value);
                else
                    foreach (var id in kv.Value)
                        if (!target.levelObjectives[kv.Key].Contains(id))
                            target.levelObjectives[kv.Key].Add(id);
            }
        }

        if (source.levelProgressData != null)
        {
            if (target.levelProgressData == null) target.levelProgressData = new Dictionary<int, LevelProgressData>();
            foreach (var kv in source.levelProgressData)
            {
                if (!target.levelProgressData.ContainsKey(kv.Key))
                {
                    target.levelProgressData[kv.Key] = kv.Value;
                }
                else
                {
                    var dst = target.levelProgressData[kv.Key];
                    var s   = kv.Value;
                    if (s.maxStarsEarned > dst.maxStarsEarned) dst.maxStarsEarned = s.maxStarsEarned;
                    if (s.isCompleted) dst.isCompleted = true;
                    if (s.firstCompletedDate != DateTime.MinValue &&
                        (dst.firstCompletedDate == DateTime.MinValue || s.firstCompletedDate < dst.firstCompletedDate))
                        dst.firstCompletedDate = s.firstCompletedDate;
                    if (s.bestTimeSeconds < dst.bestTimeSeconds) dst.bestTimeSeconds = s.bestTimeSeconds;
                    if (s.bestMoves < dst.bestMoves) dst.bestMoves = s.bestMoves;
                    if (s.parryKillAchieved) dst.parryKillAchieved = true;
                    foreach (var objId in s.completedObjectiveIds)
                        if (!dst.completedObjectiveIds.Contains(objId))
                            dst.completedObjectiveIds.Add(objId);
                }
            }
        }

        target.totalGamesPlayed  += source.totalGamesPlayed;
        target.totalEnemiesKilled += source.totalEnemiesKilled;
        target.totalPlayTime     += source.totalPlayTime;
        target.bestCombo          = Mathf.Max(target.bestCombo, source.bestCombo);

        target.consecutiveLevelWins = Mathf.Max(target.consecutiveLevelWins, source.consecutiveLevelWins);
        target.lastCompletedLevel   = Mathf.Max(target.lastCompletedLevel, source.lastCompletedLevel);
    }

    #endregion

    #region Authentication Integration

    private void OnAuthenticationChanged(bool isAuthenticated)
    {
        //if (debugMode)
        //    Debug.Log($"[SaveManager] Authentication state changed: {isAuthenticated}");

        if (isAuthenticated)
        {
            MigrateToAuthenticatedUser();
        }
    }

    private void OnUserSignedIn(string playerId)
    {
        //if (debugMode)
        //    Debug.Log($"[SaveManager] User signed in with ID: {playerId}");

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
            : 50; // default: 10 levels × 5 areas
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

        // UpdateLevelProgress(levelId + 1)
        int nextLevel = levelId + 1;
        if (nextLevel > data.highestUnlockedLevel)
            data.highestUnlockedLevel = nextLevel;

        // UpdateStars(levelId, stars)
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

        // UpdateGameStats(enemiesKilled, maxCombo, playTime, gameCompleted: true)
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

    public void ShowProgressionDebug()
    {
        var data = GetGameData();
        Debug.Log($"[SaveManager] ESTADO ACTUAL:\n" +
                  $"- Usuario: {currentUserId}\n" +
                  $"- Autenticado: {IsUserAuthenticated()}\n" +
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

            string userDataRoot = Path.Combine(Application.persistentDataPath, USER_DATA_FOLDER);
            if (Directory.Exists(userDataRoot))
            {
                foreach (var dir in Directory.GetDirectories(userDataRoot))
                {
                    string f = Path.Combine(dir, SaveFileName);
                    SafeDeleteFile(f);
                    SafeDeleteFile(f + ".bak");
                    SafeDeleteFile(f + ".tmp");

                    TryDeleteDirectoryIfEmpty(dir);
                }
            }
                     
            gameData = new GameData();
            InitializeNewGameData();
            isDataLoaded = true;
            SaveData();

            //if (debugMode)
            //{
            //    Debug.Log($"[SaveManager] Deep local reset completed.\n" +
            //              $"Root: {rootFile}\nUserData: {userDataRoot}\nCurrentPath: {saveFilePath}");
            //}

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
            //if (debugMode) Debug.Log($"[SaveManager] Could not clean empty dir '{dir}': {e.Message}");
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
