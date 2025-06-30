using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    // Progresión
    public int highestUnlockedLevel = 1;
    public int currentArea = 1;
    public Dictionary<int, int> levelStars = new Dictionary<int, int>();
    public int totalStars = 0;

    // Sistema de vidas
    public int currentLives;
    public string lastLifeRegenTime = "";
    public bool canRegenLives = true;

    // Otros datos de juego
    public List<PowerUpData> activePowerUps = new List<PowerUpData>();
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool tutorialCompleted = false;
    public int totalGamesPlayed = 0;
    public int totalEnemiesKilled = 0;
    public int bestCombo = 0;
    public float totalPlayTime = 0f;
    public DateTime lastPlayDate = DateTime.Now;

    public GameData() { }
}

[Serializable]
public class PowerUpData
{
    public PowerUpType type;
    public DateTime activationTime;
    public float duration;
}

public enum PowerUpType
{
    ExtraTime,
    DashTurbo,
    ParryPerfect,
    ComboMaster,
    SecondChance
}
