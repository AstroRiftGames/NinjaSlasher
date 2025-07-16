using System;
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
            }
            catch
            {
                Debug.LogWarning("Save corrupto. Se crea uno nuevo.");
                gameData = new GameData();
            }
        }
        else
        {
            gameData = new GameData();
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
        if (gameData.levelStars.ContainsKey(level))
        {
            if (gameData.levelStars[level] < stars)
                gameData.totalStars += (stars - gameData.levelStars[level]);
            gameData.levelStars[level] = stars;
        }
        else
        {
            gameData.levelStars.Add(level, stars);
            gameData.totalStars += stars;
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

    public void LogCurrentGameState()
    {
        Debug.Log($"[SaveManager] Estado actual:");
        Debug.Log($"- Nivel más alto: {gameData.highestUnlockedLevel}");
        Debug.Log($"- Área actual: {gameData.currentArea}");
        Debug.Log($"- Estrellas totales: {gameData.totalStars}");
        Debug.Log($"- Vidas actuales: {gameData.currentLives}");
        Debug.Log($"- Power-ups activos: {gameData.activePowerUps.Count}");
        Debug.Log($"- Power-ups en inventario: {gameData.powerUpInventory.Count}");
        Debug.Log($"- Último guardado: {gameData.lastPlayDate}");
    }
}
