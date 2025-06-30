using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class GameData
{
    // Progresión del jugador
    public int highestUnlockedLevel = 1;
    public int currentArea = 1;
    public Dictionary<int, int> levelStars = new Dictionary<int, int>(); // LevelID -> Stars
    public int totalStars = 0;

    // Sistema de vidas
    public int currentLives = 5;
    public DateTime lastLifeRegenTime;
    public bool canRegenLives = true;

    // Power-ups activos
    public List<PowerUpData> activePowerUps = new List<PowerUpData>();

    // Configuraciones
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool tutorialCompleted = false;

    // Estadísticas
    public int totalGamesPlayed = 0;
    public int totalEnemiesKilled = 0;
    public int bestCombo = 0;

    // Tiempo de juego
    public float totalPlayTime = 0f;
    public DateTime lastPlayDate;

    public GameData()
    {
        lastLifeRegenTime = DateTime.Now;
        lastPlayDate = DateTime.Now;
        levelStars = new Dictionary<int, int>();
        activePowerUps = new List<PowerUpData>();
    }
}

[System.Serializable]
public class PowerUpData
{
    public PowerUpType type;
    public DateTime activationTime;
    public float duration; // en segundos

    public bool IsExpired()
    {
        return DateTime.Now > activationTime.AddSeconds(duration);
    }

    public float GetRemainingTime()
    {
        double elapsed = (DateTime.Now - activationTime).TotalSeconds;
        return Mathf.Max(0f, duration - (float)elapsed);
    }
}

public enum PowerUpType
{
    ExtraTime,      // Tiempo Extra Global
    DashTurbo,      // Dash Turbo
    ParryPerfect,   // Parry Perfecto
    ComboMaster,    // Combo Master
    SecondChance    // Segunda Oportunidad
}