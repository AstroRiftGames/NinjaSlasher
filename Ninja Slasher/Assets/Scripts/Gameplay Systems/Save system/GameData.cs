using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    // Progresion de Niveles
    public int highestUnlockedLevel = 1;
    public int highestUnlockedArea = 1;
    public int currentArea = 1;
    public Dictionary<int, int> levelStars = new Dictionary<int, int>();
    public Dictionary<int, List<int>> levelObjectives = new Dictionary<int, List<int>>(); // objetivos por nivel
    public List<int> unlockedAreas = new List<int>(); // areas desbloqueadas
    public Dictionary<int, LevelProgressData> levelProgressData = new Dictionary<int, LevelProgressData>();
    public Dictionary<string, int> tutorialStates = new Dictionary<string, int>();
    public Dictionary<string, int> tutorialStepIndices = new Dictionary<string, int>();
    public int totalStars = 0;
    public int consecutiveLevelWins = 0;
    public int lastCompletedLevel = -1;

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
    public bool adsRemoved = false;

    // Sistema de Daily Wheel
    public DailyWheelSaveData dailyWheelData = new DailyWheelSaveData();

    // Configuraciones
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;

    // Estadisticas y Metricas
    public int totalGamesPlayed = 0;
    public int totalEnemiesKilled = 0;
    public int bestCombo = 0;
    public float totalPlayTime = 0f;
    public DateTime lastPlayDate = DateTime.Now;

    /// <summary>Número de veces que se activó un Emergency Bundle el día actual (UTC).</summary>
    public int emergencyBundleUsesToday = 0;

    /// <summary>
    /// Timestamp UTC (Unix epoch en segundos) de la última activación de Emergency Bundle.
    /// 0 indica que nunca se ha activado.
    /// Se almacena como long para evitar depender del parsing de strings de fecha.
    /// </summary>
    public long emergencyBundleLastActivationUtc = 0L;

    /// <summary>
    /// Derrotas consecutivas generales (normal + boss).
    /// Lo gestiona LifeManager; lo usa EmergencyBundleService via LevelFailedContext.
    /// </summary>
    public int consecutiveLosses = 0;

    /// <summary>
    /// Derrotas consecutivas especificamente en niveles boss.
    /// Contador independiente de currentConsecutiveLosses en LifeManager,
    /// que no distingue entre niveles normales y boss.
    /// </summary>
    public int consecutiveBossLosses = 0;

    /// <summary>
    /// Product ID de una compra IAP confirmada por la tienda pero cuyas recompensas
    /// aún no han sido entregadas. Permite recuperar recompensas tras un crash post-pago.
    /// Se limpia inmediatamente después de otorgar las recompensas.
    /// </summary>
    public string pendingPurchaseProductId = "";

    /// <summary>
    /// Unix epoch (segundos UTC) en que expiran las vidas ilimitadas.
    /// 0 = sin vidas ilimitadas activas.
    /// </summary>
    public long unlimitedLivesStartUtc = 0L;

    /// <summary>
    /// Unix epoch (segundos UTC) en que expiran las vidas ilimitadas.
    /// 0 = sin vidas ilimitadas activas.
    /// </summary>
    public long unlimitedLivesEndUtc = 0L;

    /// <summary>Monedas del jugador.</summary>
    public int coins = 0;

    /// <summary>
    /// Marca que el evento firstOpen ya fue enviado.
    /// Una vez true, no vuelve a dispararse aunque la app se reinstale sobre sí misma
    /// con datos de nube/backup. Valor false en saves existentes = primer open legítimo.
    /// </summary>
    public bool hasFirstOpenFired = false;

    public GameData()
    {
        unlockedAreas.Add(1); // area 1 desbloqueada
    }

    public void UpdateLevelProgress(int levelId, ObjectiveEvaluationResult result, LevelStats stats)
    {
        if (!levelProgressData.ContainsKey(levelId))
        {
            levelProgressData[levelId] = new LevelProgressData(levelId);
        }

        var progress = levelProgressData[levelId];
        progress.lastPlayedDate = DateTime.Now;

        if (result.primaryCompleted && !progress.isCompleted)
        {
            progress.isCompleted = true;
            progress.firstCompletedDate = DateTime.Now;
        }

        if (result.starsEarned > progress.maxStarsEarned)
        {
            progress.maxStarsEarned = result.starsEarned;
        }

        if (stats.timeTaken < progress.bestTimeSeconds)
        {
            progress.bestTimeSeconds = stats.timeTaken;
        }

        if (stats.movesUsed < progress.bestMoves)
        {
            progress.bestMoves = stats.movesUsed;
        }

        if (stats.parryKillDone)
        {
            progress.parryKillAchieved = true;
        }

        foreach (var objective in result.completedObjectives)
        {
            string objectiveId = objective.StableId;
            if (!progress.completedObjectiveIds.Contains(objectiveId))
            {
                progress.completedObjectiveIds.Add(objectiveId);
            }
        }
    }

    public bool IsObjectiveCompleted(int levelId, string objectiveId)
    {
        if (!levelProgressData.ContainsKey(levelId))
            return false;

        return levelProgressData[levelId].completedObjectiveIds.Contains(objectiveId);
    }

    public LevelProgressData GetLevelProgress(int levelId)
    {
        if (!levelProgressData.ContainsKey(levelId))
        {
            levelProgressData[levelId] = new LevelProgressData(levelId);
        }
        return levelProgressData[levelId];
    }

    public int GetTutorialState(string tutorialId)
    {
        if (string.IsNullOrEmpty(tutorialId) || tutorialStates == null)
            return 0;

        return tutorialStates.TryGetValue(tutorialId, out int state) ? state : 0;
    }

    public int GetTutorialStepIndex(string tutorialId)
    {
        if (string.IsNullOrEmpty(tutorialId) || tutorialStepIndices == null)
            return 0;

        return tutorialStepIndices.TryGetValue(tutorialId, out int stepIndex) ? stepIndex : 0;
    }

    public void SetTutorialProgress(string tutorialId, int state, int stepIndex)
    {
        if (string.IsNullOrEmpty(tutorialId))
            return;

        tutorialStates ??= new Dictionary<string, int>();
        tutorialStepIndices ??= new Dictionary<string, int>();

        tutorialStates[tutorialId] = state;
        tutorialStepIndices[tutorialId] = Math.Max(0, stepIndex);
    }
}

[Serializable]
public class PowerUpData
{
    public PowerUpType type;
    public DateTime activationTime;
    public int usesRemaining;

    public PowerUpData() { }

    public PowerUpData(PowerUpType type, int uses)
    {
        this.type = type;
        this.usesRemaining = uses;
        this.activationTime = DateTime.Now;
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

[Serializable]
public class LevelProgressData
{
    public int levelId;
    public bool isCompleted;
    public int maxStarsEarned;
    public DateTime firstCompletedDate;
    public DateTime lastPlayedDate;
    public List<string> completedObjectiveIds = new List<string>();
    public float bestTimeSeconds;
    public int bestMoves;
    public bool parryKillAchieved;

    // Total de intentos fallidos acumulados en este nivel.
    // Usado para enviar attemptNumber real en el evento levelFailed.
    // Valor 0 en saves existentes equivale a "sin historial previo".
    public int totalAttempts = 0;

    public LevelProgressData() { }

    public LevelProgressData(int id)
    {
        levelId = id;
        isCompleted = false;
        maxStarsEarned = 0;
        firstCompletedDate = DateTime.MinValue;
        lastPlayedDate = DateTime.Now;
        bestTimeSeconds = float.MaxValue;
        bestMoves = int.MaxValue;
        parryKillAchieved = false;
        totalAttempts = 0;
    }
}

[Serializable]
public class ObjectiveCompletionData
{
    public string objectiveId;
    public string objectiveName;
    public DateTime completedDate;
    public bool isPermanentlyCompleted;

    public ObjectiveCompletionData() { }

    public ObjectiveCompletionData(string id, string name)
    {
        objectiveId = id;
        objectiveName = name;
        completedDate = DateTime.Now;
        isPermanentlyCompleted = true;
    }
}

[Serializable]
public class DailyWheelSaveData
{
    public string lastSpinDateIso;
    public string lastSpinTimestampUtc;
    public int consecutiveSpins;
    public int totalSpins;
    public int pendingFreeSpins;
    public string lastAutoShowDateIso;
    public string lastAutoShowTimestampUtc;
}
