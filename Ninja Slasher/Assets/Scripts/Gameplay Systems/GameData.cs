using System;
using System.Collections.Generic;

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
    public List<PowerUpInventoryItem> powerUpInventory = new List<PowerUpInventoryItem>();
    public string dailyRewardData = "";
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

[Serializable]
public class PowerUpInventoryItem
{
    public PowerUpType type;
    public int quantity;
    public DateTime lastUpdated;

    public PowerUpInventoryItem(PowerUpType powerUpType, int qty)
    {
        type = powerUpType;
        quantity = qty;
        lastUpdated = DateTime.Now;
    }
}
