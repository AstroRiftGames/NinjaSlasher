using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviourSingleton<SaveManager>
{
    private static string SaveFileName = "ninja_save.json";
    private string saveFilePath;
    private GameData gameData;

    public override void Awake()
    {
        base.Awake();
        saveFilePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        LoadData();
    }

    public void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            try
            {
                gameData = JsonUtility.FromJson<GameData>(json);

                ValidateAndInitializeProgressionData();
            }
            catch
            {
                Debug.LogWarning("Save corrupto. Se crea uno nuevo.");
                gameData = new GameData();
                InitializeNewGameData();
            }
        }
        else
        {
            gameData = new GameData();
            InitializeNewGameData();
        }
    }

    public void SaveData()
    {
        if (gameData == null) return;
        gameData.lastPlayDate = DateTime.Now;
        string json = JsonUtility.ToJson(gameData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[SaveManager] Datos guardados en: {saveFilePath} (vidas: {gameData.currentLives})");
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

        Debug.Log($"[SaveManager] Datos de progresión validados: Nivel {gameData.highestUnlockedLevel}, Área {gameData.highestUnlockedArea}");
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

            Debug.Log($"[SaveManager] Progresión recalculada desde estrellas existentes: Nivel {gameData.highestUnlockedLevel}, Área {gameData.highestUnlockedArea}");
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

        Debug.Log("[SaveManager] Nuevos datos de juego inicializados");
    }

    public GameData GetGameData() => gameData;

    public void UpdateLives(int lives, DateTime lastRegen, bool canRegen)
    {
        gameData.currentLives = lives;
        gameData.lastLifeRegenTime = lastRegen.ToString("o");
        gameData.canRegenLives = canRegen;
        SaveData();
    }

    public void UpdateStars(int level, int stars)
    {
        if (gameData == null) return;

        int previousStars = 0;

        if (gameData.levelStars.ContainsKey(level))
        {
            previousStars = gameData.levelStars[level];
            if (gameData.levelStars[level] < stars)
            {
                gameData.totalStars += (stars - gameData.levelStars[level]);
            }
            gameData.levelStars[level] = stars;
        }
        else
        {
            gameData.levelStars.Add(level, stars);
            gameData.totalStars += stars;
        }

        if (stars >= 1 && previousStars == 0)
        {
            if (level >= gameData.highestUnlockedLevel)
            {
                gameData.highestUnlockedLevel = level + 1;
            }

            int highestCompletedLevel = 0;
            foreach (var levelStar in gameData.levelStars)
            {
                if (levelStar.Value >= 1)
                {
                    highestCompletedLevel = Mathf.Max(highestCompletedLevel, levelStar.Key);
                }
            }

            int calculatedArea = Mathf.Min(((highestCompletedLevel - 1) / 10) + 1, 5);
            if (calculatedArea > gameData.highestUnlockedArea)
            {
                gameData.highestUnlockedArea = calculatedArea;
                Debug.Log($"[SaveManager] Nueva área calculada desbloqueada: {calculatedArea}");
            }

            Debug.Log($"[SaveManager] Progresión actualizada: nivel {level} completado, siguiente nivel desbloqueado: {gameData.highestUnlockedLevel}");
        }

        SaveData();
    }

    public void SetMusicVolume(float volume)
    {
        gameData.musicVolume = Mathf.Clamp01(volume);
        SaveData();
    }

    public void SetSFXVolume(float volume)
    {
        gameData.sfxVolume = Mathf.Clamp01(volume);
        SaveData();
    }

    public void SaveDailyRewardData(string dailyRewardJson)
    {
        gameData.dailyRewardData = dailyRewardJson;
        SaveData();
    }

    public string GetDailyRewardData()
    {
        return gameData.dailyRewardData;
    }

    public void UpdateLevelProgress(int level)
    {
        if (level > gameData.highestUnlockedLevel)
        {
            gameData.highestUnlockedLevel = level;
        }
        SaveData();
    }

    public void SaveLevelProgress(int levelId, ObjectiveEvaluationResult result, LevelStats stats)
    {
        if (gameData == null) return;

        gameData.UpdateLevelProgress(levelId, result, stats);

        UpdateStars(levelId, result.starsEarned);

        SaveData();

        Debug.Log($"[SaveManager] Progreso completo del nivel {levelId} guardado: {result.starsEarned} estrellas, {result.completedObjectives.Count} objetivos");
    }

    public LevelProgressData GetLevelProgressData(int levelId)
    {
        if (gameData == null) return new LevelProgressData(levelId);
        return gameData.GetLevelProgress(levelId);
    }

    public void UpdateLevelProgression(int levelId, int starsEarned)
    {
        if (gameData == null) return;

        if (levelId >= gameData.highestUnlockedLevel)
        {
            gameData.highestUnlockedLevel = levelId + 1;
        }

        SaveData();
        Debug.Log($"[SaveManager] Progresión actualizada: nivel más alto = {gameData.highestUnlockedLevel}");
    }

    public void UnlockNewArea(int areaId)
    {
        if (gameData == null) return;

        if (areaId > gameData.highestUnlockedArea)
        {
            gameData.highestUnlockedArea = areaId;
            SaveData();
            Debug.Log($"[SaveManager] Nueva área desbloqueada: {areaId}");
        }
    }

    public (int highestLevel, int highestArea, int totalStars) GetProgressionData()
    {
        if (gameData == null) return (1, 1, 0);
        return (gameData.highestUnlockedLevel, gameData.highestUnlockedArea, gameData.totalStars);
    }

    public bool IsObjectiveCompleted(int levelId, ObjectiveData objective)
    {
        if (gameData == null || objective == null) return false;
        return gameData.IsObjectiveCompleted(levelId, objective.name);
    }

    public void UnlockArea(int areaId)
    {
        if (areaId > gameData.currentArea)
        {
            gameData.currentArea = areaId;
        }
        SaveData();
    }

    public void AddPowerUpToInventory(PowerUpType powerUpType, int quantity = 1)
    {
        var existingItem = gameData.powerUpInventory.Find(item => item.type == powerUpType);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
            existingItem.lastUpdated = DateTime.Now;
        }
        else
        {
            gameData.powerUpInventory.Add(new PowerUpInventoryItem(powerUpType, quantity));
        }
        SaveData();
    }

    public void RemovePowerUpFromInventory(PowerUpType powerUpType, int quantity = 1)
    {
        var existingItem = gameData.powerUpInventory.Find(item => item.type == powerUpType);
        if (existingItem != null)
        {
            existingItem.quantity = Mathf.Max(0, existingItem.quantity - quantity);
            existingItem.lastUpdated = DateTime.Now;
            if (existingItem.quantity == 0)
            {
                gameData.powerUpInventory.Remove(existingItem);
            }
        }
        SaveData();
    }

    public void ActivatePowerUp(PowerUpType powerUpType, float duration)
    {
        RemovePowerUpFromInventory(powerUpType, 1);

        var existingActivePowerUp = gameData.activePowerUps.Find(p => p.type == powerUpType);
        if (existingActivePowerUp != null)
        {
            gameData.activePowerUps.Remove(existingActivePowerUp);
        }

        gameData.activePowerUps.Add(new PowerUpData
        {
            type = powerUpType,
            activationTime = DateTime.Now,
            duration = duration
        });
        SaveData();
    }

    public void DeactivatePowerUp(PowerUpType powerUpType)
    {
        var powerUpToRemove = gameData.activePowerUps.Find(p => p.type == powerUpType);
        if (powerUpToRemove != null)
        {
            gameData.activePowerUps.Remove(powerUpToRemove);
            SaveData();
            Debug.Log($"[SaveManager] Power-up {powerUpType} desactivado y removido del guardado");
        }
        else
        {
            Debug.LogWarning($"[SaveManager] No se encontró power-up activo {powerUpType} para desactivar");
        }
    }

    public void UpdateActivePowerUps()
    {
        bool hasChanges = false;
        var currentTime = DateTime.Now;

        for (int i = gameData.activePowerUps.Count - 1; i >= 0; i--)
        {
            var powerUp = gameData.activePowerUps[i];
            var timeElapsed = (currentTime - powerUp.activationTime).TotalSeconds;

            if (timeElapsed >= powerUp.duration)
            {
                gameData.activePowerUps.RemoveAt(i);
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
        if (gameCompleted)
        {
            gameData.totalGamesPlayed++;
        }

        if (enemiesKilled > 0)
        {
            gameData.totalEnemiesKilled += enemiesKilled;
        }

        if (combo > gameData.bestCombo)
        {
            gameData.bestCombo = combo;
        }

        if (playTime > 0f)
        {
            gameData.totalPlayTime += playTime;
        }

        SaveData();
    }

    public void SaveOnApplicationEvent()
    {
        UpdateActivePowerUps();
        SaveData();
    }

    public void DeleteSaveData()
    {
        if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
        gameData = new GameData();
        SaveData();
        Debug.Log("[SaveManager] Save reseteado");
    }
}
