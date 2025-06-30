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

    public void DeleteSaveData()
    {
        if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
        gameData = new GameData();
        SaveData();
        Debug.Log("[SaveManager] Save reseteado");
    }
}
