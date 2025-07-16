
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    // Progresión de Niveles
    public int highestUnlockedLevel = 1;
    public int currentArea = 1;
    public Dictionary<int, int> levelStars = new Dictionary<int, int>();
    public Dictionary<int, List<int>> levelObjectives = new Dictionary<int, List<int>>(); // objetivos por nivel
    public List<int> unlockedAreas = new List<int> { 1 }; // areas desbloqueadas
    public int totalStars = 0;

    // Sistema de vidas
    public int currentLives;
    public string lastLifeRegenTime = "";
    public bool canRegenLives = true;

    // Sistema de Power-ups
    public List<PowerUpData> activePowerUps = new List<PowerUpData>();
    public List<PowerUpInventoryItem> powerUpInventory = new List<PowerUpInventoryItem>();

    // Sistema de Recompensas Diarias
    public string dailyRewardData = "";
    public List<int> dailyRewardOrder = new List<int>(); // recompensas diarias orden (1-7)
    public string lastRewardTimestamp = ""; // timestamp ultima recompensa

    // Configuraciones
    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    // Estadísticas y Metricas
    public int totalGamesPlayed = 0;
    public int totalEnemiesKilled = 0;
    public int bestCombo = 0;
    public float totalPlayTime = 0f;
    public DateTime lastPlayDate = DateTime.Now;

    public GameData()
    {
        unlockedAreas.Add(1); // area 1 desbloqueada por defecto
    }
}

[Serializable]
public class PowerUpData
{
    public PowerUpType type;
    public DateTime activationTime;
    public float duration;

    public PowerUpData() { }

    public PowerUpData(PowerUpType powerUpType, DateTime activation, float dur)
    {
        type = powerUpType;
        activationTime = activation;
        duration = dur;
    }

    // para verificar si el power up aun esta activo
    public bool IsActive()
    {
        return (DateTime.Now - activationTime).TotalSeconds < duration;
    }

    // para obtener tiempo restante en segundos
    public float GetRemainingTime()
    {
        float elapsed = (float)(DateTime.Now - activationTime).TotalSeconds;
        return Mathf.Max(0f, duration - elapsed);
    }
}

[Serializable]
public class PowerUpInventoryItem
{
    public PowerUpType type;
    public int quantity;
    public DateTime lastUpdated;

    public PowerUpInventoryItem() { }

    public PowerUpInventoryItem(PowerUpType powerUpType, int qty)
    {
        type = powerUpType;
        quantity = qty;
        lastUpdated = DateTime.Now;
    }
}
