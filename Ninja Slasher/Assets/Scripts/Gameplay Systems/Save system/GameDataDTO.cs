using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameDataDTO
{
    public int highestUnlockedLevel;
    public int highestUnlockedArea;
    public int currentArea;
    public List<IntIntKV> levelStars = new();
    public List<IntListKV> levelObjectives = new();
    public List<IntLevelProgressKV> levelProgressData = new();
    public List<int> unlockedAreas = new();
    public int totalStars;

    public int currentLives;
    public string lastLifeRegenTime;
    public bool canRegenLives;

    public int consecutiveLevelWins;
    public int lastCompletedLevel;

    public List<PowerUpData> activePowerUps = new();
    public List<PowerUpInventoryItem> powerUpInventory = new();

    public string dailyRewardData;
    public List<int> dailyRewardOrder = new();
    public string lastRewardTimestamp;

    public float musicVolume;
    public float sfxVolume;

    public int totalGamesPlayed;
    public int totalEnemiesKilled;
    public int bestCombo;
    public float totalPlayTime;
    public string lastPlayDate; // ISO 8601
}

[Serializable] public struct IntIntKV { public int key; public int value; }
[Serializable] public struct IntListKV { public int key; public List<int> value; }
[Serializable] public struct IntLevelProgressKV { public int key; public LevelProgressData value; }

public static class GameDataMapper
{
    public static GameDataDTO ToDto(GameData d)
    {
        var dto = new GameDataDTO
        {
            highestUnlockedLevel = d.highestUnlockedLevel,
            highestUnlockedArea = d.highestUnlockedArea,
            currentArea = d.currentArea,
            unlockedAreas = new List<int>(d.unlockedAreas),
            totalStars = d.totalStars,

            currentLives = d.currentLives,
            lastLifeRegenTime = d.lastLifeRegenTime,
            canRegenLives = d.canRegenLives,

            activePowerUps = new List<PowerUpData>(d.activePowerUps),
            powerUpInventory = new List<PowerUpInventoryItem>(d.powerUpInventory),

            dailyRewardData = d.dailyRewardData,
            dailyRewardOrder = new List<int>(d.dailyRewardOrder),
            lastRewardTimestamp = d.lastRewardTimestamp,

            musicVolume = d.musicVolume,
            sfxVolume = d.sfxVolume,

            totalGamesPlayed = d.totalGamesPlayed,
            totalEnemiesKilled = d.totalEnemiesKilled,
            bestCombo = d.bestCombo,
            totalPlayTime = d.totalPlayTime,
            lastPlayDate = d.lastPlayDate.ToString("o"),

            consecutiveLevelWins = d.consecutiveLevelWins,
            lastCompletedLevel = d.lastCompletedLevel,
        };

        if (d.levelStars != null)
            foreach (var kv in d.levelStars)
                dto.levelStars.Add(new IntIntKV { key = kv.Key, value = kv.Value });

        if (d.levelObjectives != null)
            foreach (var kv in d.levelObjectives)
                dto.levelObjectives.Add(new IntListKV { key = kv.Key, value = kv.Value });

        if (d.levelProgressData != null)
            foreach (var kv in d.levelProgressData)
                dto.levelProgressData.Add(new IntLevelProgressKV { key = kv.Key, value = kv.Value });

        return dto;
    }

    public static GameData FromDto(GameDataDTO dto)
    {
        var d = new GameData
        {
            highestUnlockedLevel = dto.highestUnlockedLevel,
            highestUnlockedArea = dto.highestUnlockedArea,
            currentArea = dto.currentArea,
            unlockedAreas = dto.unlockedAreas ?? new List<int> { 1 },
            totalStars = dto.totalStars,

            currentLives = dto.currentLives,
            lastLifeRegenTime = dto.lastLifeRegenTime ?? "",
            canRegenLives = dto.canRegenLives,

            activePowerUps = dto.activePowerUps ?? new List<PowerUpData>(),
            powerUpInventory = dto.powerUpInventory ?? new List<PowerUpInventoryItem>(),

            dailyRewardData = dto.dailyRewardData ?? "",
            dailyRewardOrder = dto.dailyRewardOrder ?? new List<int>(),
            lastRewardTimestamp = dto.lastRewardTimestamp ?? "",

            musicVolume = dto.musicVolume,
            sfxVolume = dto.sfxVolume,

            totalGamesPlayed = dto.totalGamesPlayed,
            totalEnemiesKilled = dto.totalEnemiesKilled,
            bestCombo = dto.bestCombo,
            totalPlayTime = dto.totalPlayTime,

            consecutiveLevelWins = dto.consecutiveLevelWins,
            lastCompletedLevel = dto.lastCompletedLevel,
        };

        if (DateTime.TryParse(dto.lastPlayDate, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            d.lastPlayDate = parsed;

        d.levelStars = new Dictionary<int, int>();
        if (dto.levelStars != null)
            foreach (var kv in dto.levelStars) d.levelStars[kv.key] = kv.value;

        d.levelObjectives = new Dictionary<int, List<int>>();
        if (dto.levelObjectives != null)
            foreach (var kv in dto.levelObjectives) d.levelObjectives[kv.key] = kv.value ?? new List<int>();

        d.levelProgressData = new Dictionary<int, LevelProgressData>();
        if (dto.levelProgressData != null)
            foreach (var kv in dto.levelProgressData) d.levelProgressData[kv.key] = kv.value ?? new LevelProgressData(kv.key);

        return d;
    }
}
