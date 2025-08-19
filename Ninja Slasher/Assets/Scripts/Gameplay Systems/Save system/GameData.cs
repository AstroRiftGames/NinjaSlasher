using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class GameData : ISerializationCallbackReceiver
{
    // Progresión de Niveles
    public int highestUnlockedLevel = 1;
    public int highestUnlockedArea = 1;
    public int currentArea = 1;

    // === Diccionarios (uso en runtime) ===
    [NonSerialized] public Dictionary<int, int> levelStars = new Dictionary<int, int>();
    [NonSerialized] public Dictionary<int, List<int>> levelObjectives = new Dictionary<int, List<int>>();
    [NonSerialized] public Dictionary<int, LevelProgressData> levelProgressData = new Dictionary<int, LevelProgressData>();

    // === Backing fields serializables por JsonUtility ===
    [SerializeField] private List<int> levelStars_keys = new List<int>();
    [SerializeField] private List<int> levelStars_values = new List<int>();

    [SerializeField] private List<int> levelObjectives_keys = new List<int>();
    [SerializeField] private List<SerializableIntList> levelObjectives_values = new List<SerializableIntList>();

    [SerializeField] private List<int> levelProgressData_keys = new List<int>();
    [SerializeField] private List<LevelProgressData> levelProgressData_values = new List<LevelProgressData>();

    // Áreas desbloqueadas
    public List<int> unlockedAreas = new List<int> { 1 };

    public int totalStars = 0;

    // Sistema de vidas
    public int currentLives = 3;
    public string lastLifeRegenTime = ""; // ISO 8601 (UTC)
    public bool canRegenLives = true;

    // Power-ups
    public List<PowerUpData> activePowerUps = new List<PowerUpData>();
    public List<PowerUpInventoryItem> powerUpInventory = new List<PowerUpInventoryItem>();

    // Recompensas Diarias
    public string dailyRewardData = "";
    public List<int> dailyRewardOrder = new List<int>();
    public string lastRewardTimestamp = ""; // ISO 8601 (UTC)

    // Configuraciones
    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    // Estadísticas y Métricas
    public int totalGamesPlayed = 0;
    public int totalEnemiesKilled = 0;
    public int bestCombo = 0;
    public float totalPlayTime = 0f;

    public string lastPlayDateIso = DateTime.UtcNow.ToString("o");
    [NonSerialized] public DateTime lastPlayDate = DateTime.MinValue;

    // --- Métodos de progreso ---
    public void UpdateLevelProgress(int levelId, ObjectiveEvaluationResult result, LevelStats stats)
    {
        if (!levelProgressData.ContainsKey(levelId))
        {
            levelProgressData[levelId] = new LevelProgressData(levelId);
        }

        var progress = levelProgressData[levelId];
        progress.lastPlayedDateIso = DateTime.UtcNow.ToString("o");

        if (result.primaryCompleted && !progress.isCompleted)
        {
            progress.isCompleted = true;
            progress.firstCompletedDateIso = DateTime.UtcNow.ToString("o");
        }

        if (result.starsEarned > progress.maxStarsEarned)
            progress.maxStarsEarned = result.starsEarned;

        if (stats.timeTaken < progress.bestTimeSeconds)
            progress.bestTimeSeconds = stats.timeTaken;

        if (stats.movesUsed < progress.bestMoves)
            progress.bestMoves = stats.movesUsed;

        if (stats.parryKillDone)
            progress.parryKillAchieved = true;

        foreach (var objective in result.completedObjectives)
        {
            string objectiveId = objective.name;
            if (!progress.completedObjectiveIds.Contains(objectiveId))
                progress.completedObjectiveIds.Add(objectiveId);
        }
    }

    public bool IsObjectiveCompleted(int levelId, string objectiveId)
    {
        return levelProgressData.ContainsKey(levelId) &&
               levelProgressData[levelId].completedObjectiveIds.Contains(objectiveId);
    }

    public LevelProgressData GetLevelProgress(int levelId)
    {
        if (!levelProgressData.ContainsKey(levelId))
            levelProgressData[levelId] = new LevelProgressData(levelId);
        return levelProgressData[levelId];
    }

    // --- Callbacks para JsonUtility ---
    public void OnBeforeSerialize()
    {
        // Dedupe áreas
        unlockedAreas = new List<int>(new HashSet<int>(unlockedAreas));

        // Diccionario levelStars -> listas
        levelStars_keys.Clear(); levelStars_values.Clear();
        foreach (var kv in levelStars)
        {
            levelStars_keys.Add(kv.Key);
            levelStars_values.Add(kv.Value);
        }

        // Diccionario levelObjectives -> listas
        levelObjectives_keys.Clear(); levelObjectives_values.Clear();
        foreach (var kv in levelObjectives)
        {
            levelObjectives_keys.Add(kv.Key);
            levelObjectives_values.Add(new SerializableIntList { items = kv.Value ?? new List<int>() });
        }

        // Diccionario levelProgressData -> listas
        levelProgressData_keys.Clear(); levelProgressData_values.Clear();
        foreach (var kv in levelProgressData)
        {
            levelProgressData_keys.Add(kv.Key);
            levelProgressData_values.Add(kv.Value ?? new LevelProgressData(kv.Key));
        }

        // Sincroniza lastPlayDate -> ISO si está seteado
        if (lastPlayDate != DateTime.MinValue)
            lastPlayDateIso = lastPlayDate.ToUniversalTime().ToString("o");
    }

    public void OnAfterDeserialize()
    {
        // listas -> diccionario levelStars
        levelStars = new Dictionary<int, int>();
        for (int i = 0; i < Math.Min(levelStars_keys.Count, levelStars_values.Count); i++)
            levelStars[levelStars_keys[i]] = levelStars_values[i];

        // listas -> diccionario levelObjectives
        levelObjectives = new Dictionary<int, List<int>>();
        for (int i = 0; i < Math.Min(levelObjectives_keys.Count, levelObjectives_values.Count); i++)
            levelObjectives[levelObjectives_keys[i]] = levelObjectives_values[i]?.items ?? new List<int>();

        // listas -> diccionario levelProgressData
        levelProgressData = new Dictionary<int, LevelProgressData>();
        for (int i = 0; i < Math.Min(levelProgressData_keys.Count, levelProgressData_values.Count); i++)
        {
            var key = levelProgressData_keys[i];
            var val = levelProgressData_values[i] ?? new LevelProgressData(key);
            levelProgressData[key] = val;
        }

        // ISO -> DateTime (UTC)
        if (!string.IsNullOrEmpty(lastPlayDateIso) &&
            DateTime.TryParse(lastPlayDateIso, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            lastPlayDate = dt.ToUniversalTime();
        else
            lastPlayDate = DateTime.MinValue;
    }
}

[Serializable]
public class SerializableIntList
{
    public List<int> items = new List<int>();
}

[Serializable]
public class PowerUpData : ISerializationCallbackReceiver
{
    public PowerUpType type;

    // Nuevo (persistencia)
    public string activationTimeIso = ""; // UTC ISO 8601

    // Legacy (compat con tu código actual)
    [NonSerialized] public DateTime activationTime;

    public float duration;

    public PowerUpData() { }

    public PowerUpData(PowerUpType powerUpType, DateTime activation, float dur)
    {
        type = powerUpType;
        activationTime = activation.ToUniversalTime();
        activationTimeIso = activationTime.ToString("o");
        duration = dur;
    }

    public bool IsActive()
    {
        var start = activationTime != DateTime.MinValue
            ? activationTime
            : ParseIsoOrMin(activationTimeIso);
        return (DateTime.UtcNow - start).TotalSeconds < duration;
    }

    public float GetRemainingTime()
    {
        var start = activationTime != DateTime.MinValue
            ? activationTime
            : ParseIsoOrMin(activationTimeIso);
        float elapsed = (float)(DateTime.UtcNow - start).TotalSeconds;
        return Mathf.Max(0f, duration - elapsed);
    }

    public void OnBeforeSerialize()
    {
        // Sincroniza DateTime -> ISO
        if (activationTime != DateTime.MinValue)
            activationTimeIso = activationTime.ToUniversalTime().ToString("o");
        else if (!string.IsNullOrEmpty(activationTimeIso))
            activationTime = ParseIsoOrMin(activationTimeIso);
    }

    public void OnAfterDeserialize()
    {
        // Sincroniza ISO -> DateTime
        activationTime = ParseIsoOrMin(activationTimeIso);
    }

    private static DateTime ParseIsoOrMin(string s)
    {
        if (DateTime.TryParse(
                s, null,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var dt))
            return dt.ToUniversalTime();
        return DateTime.MinValue;
    }
}

[Serializable]
public class PowerUpInventoryItem : ISerializationCallbackReceiver
{
    public PowerUpType type;
    public int quantity;

    // Nuevo (persistencia)
    public string lastUpdatedIso = ""; // UTC ISO 8601

    // Legacy (compat con tu código actual)
    [NonSerialized] public DateTime lastUpdated;

    public PowerUpInventoryItem() { }

    public PowerUpInventoryItem(PowerUpType powerUpType, int qty)
    {
        type = powerUpType;
        quantity = qty;
        lastUpdated = DateTime.UtcNow;
        lastUpdatedIso = lastUpdated.ToString("o");
    }

    public void OnBeforeSerialize()
    {
        if (lastUpdated != DateTime.MinValue)
            lastUpdatedIso = lastUpdated.ToUniversalTime().ToString("o");
        else if (!string.IsNullOrEmpty(lastUpdatedIso))
            lastUpdated = ParseIsoOrMin(lastUpdatedIso);
    }

    public void OnAfterDeserialize()
    {
        lastUpdated = ParseIsoOrMin(lastUpdatedIso);
    }

    private static DateTime ParseIsoOrMin(string s)
    {
        if (DateTime.TryParse(
                s, null,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var dt))
            return dt.ToUniversalTime();
        return DateTime.MinValue;
    }
}

[Serializable]
public class LevelProgressData
{
    public int levelId;
    public bool isCompleted;
    public int maxStarsEarned;
    public string firstCompletedDateIso = ""; // UTC
    public string lastPlayedDateIso = "";     // UTC
    public List<string> completedObjectiveIds = new List<string>();
    public float bestTimeSeconds;
    public int bestMoves;
    public bool parryKillAchieved;

    public LevelProgressData() { }

    public LevelProgressData(int id)
    {
        levelId = id;
        isCompleted = false;
        maxStarsEarned = 0;
        firstCompletedDateIso = "";
        lastPlayedDateIso = DateTime.UtcNow.ToString("o");
        bestTimeSeconds = float.MaxValue;
        bestMoves = int.MaxValue;
        parryKillAchieved = false;
    }
}

[Serializable]
public class ObjectiveCompletionData
{
    public string objectiveId;
    public string objectiveName;
    public string completedDateIso; // UTC
    public bool isPermanentlyCompleted;

    public ObjectiveCompletionData() { }

    public ObjectiveCompletionData(string id, string name)
    {
        objectiveId = id;
        objectiveName = name;
        completedDateIso = DateTime.UtcNow.ToString("o");
        isPermanentlyCompleted = true;
    }
}
