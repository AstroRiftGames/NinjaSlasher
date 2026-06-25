using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameDataDTO
{
    public int saveVersion = 2;

    public int highestUnlockedLevel;
    public int highestUnlockedArea;
    public int currentArea;
    public List<IntIntKV> levelStars = new();
    public List<IntListKV> levelObjectives = new();
    public List<IntLevelProgressKV> levelProgressData = new();
    public List<StringIntKV> tutorialStates = new();
    public List<StringIntKV> tutorialStepIndices = new();
    public List<int> unlockedAreas = new();
    public int totalStars;

    public int currentLives;
    public string lastLifeRegenTime;
    public bool canRegenLives;
    public string lastKnownLocalUtc;
    public string lastTrustedUtc;
    public string lastTimeValidationUtc;
    public bool trustedTimeAvailable;
    public bool suspiciousTimeDetected;
    public ActiveLevelAttemptData activeLevelAttempt = new ActiveLevelAttemptData();

    public int consecutiveLevelWins;
    public int lastCompletedLevel;

    public List<PowerUpDataDTO> activePowerUps = new();
    public List<PowerUpInventoryItem> powerUpInventory = new();

    public string dailyRewardData;
    public List<int> dailyRewardOrder = new();
    public string lastRewardTimestamp;
    public bool adsRemoved;

    public float musicVolume;
    public float sfxVolume;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;

    public int totalGamesPlayed;
    public int totalEnemiesKilled;
    public int bestCombo;
    public float totalPlayTime;
    public string lastPlayDate; // ISO 8601

    /// <summary>Número de usos de Emergency Bundle en el día UTC actual.</summary>
    public int emergencyBundleUsesToday;

    /// <summary>Unix epoch (segundos UTC) de la última activación. 0 = nunca activado.</summary>
    public long emergencyBundleLastActivationUtc;

    /// <summary>Derrotas consecutivas acumuladas en niveles boss.</summary>
    public int consecutiveBossLosses;

    /// <summary>Derrotas consecutivas generales. Migrado desde PlayerPrefs.</summary>
    public int consecutiveLosses;

    /// <summary>Product ID de compra IAP pendiente de entrega de recompensa.</summary>
    public string pendingPurchaseProductId;

    /// <summary>Transaction ids de compras ya otorgadas.</summary>
    public List<string> grantedPurchaseTransactionIds = new();

    /// <summary>Unix epoch (segundos UTC) en que expiran las vidas ilimitadas. 0 = inactivo.</summary>
    public long unlimitedLivesStartUtc;

    /// <summary>Unix epoch (segundos UTC) en que expiran las vidas ilimitadas. 0 = inactivo.</summary>
    public long unlimitedLivesEndUtc;

    /// <summary>Monedas del jugador.</summary>
    public int coins;

    /// <summary>Marca que el evento firstOpen ya fue enviado una vez.</summary>
    public bool hasFirstOpenFired;

    /// <summary>Marca que la guia inicial de interfaz ya fue completada o saltada.</summary>
    public bool hasSeenFirstTimeWelcome;

    /// <summary>Fecha UTC yyyy-MM-dd en que el auto-show diario fue suprimido por el welcome.</summary>
    public string firstTimeWelcomeDailyStartupSuppressedDateUtc;

    /// <summary>Datos de daily wheel.</summary>
    public DailyWheelSaveData dailyWheelData = new DailyWheelSaveData();
}

[Serializable]
public class PowerUpDataDTO
{
    public PowerUpType type;
    public int usesRemaining;
    public string activationTimeIso;
}

[Serializable] public struct IntIntKV { public int key; public int value; }
[Serializable] public struct IntListKV { public int key; public List<int> value; }
[Serializable] public struct IntLevelProgressKV { public int key; public LevelProgressData value; }
[Serializable] public struct StringIntKV { public string key; public int value; }

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
            lastKnownLocalUtc = d.lastKnownLocalUtc,
            lastTrustedUtc = d.lastTrustedUtc,
            lastTimeValidationUtc = d.lastTimeValidationUtc,
            trustedTimeAvailable = d.trustedTimeAvailable,
            suspiciousTimeDetected = d.suspiciousTimeDetected,
            activeLevelAttempt = new ActiveLevelAttemptData
            {
                attemptId = d.activeLevelAttempt?.attemptId ?? "",
                levelId = d.activeLevelAttempt?.levelId ?? 0,
                isActive = d.activeLevelAttempt?.isActive ?? false,
                startedAtUtc = d.activeLevelAttempt?.startedAtUtc ?? "",
            },

            activePowerUps = d.activePowerUps.ConvertAll(p => new PowerUpDataDTO
            {
                type = p.type,
                usesRemaining = p.usesRemaining,
                activationTimeIso = p.activationTime.ToString("o"),
            }),
            powerUpInventory = new List<PowerUpInventoryItem>(d.powerUpInventory),

            dailyRewardData = d.dailyRewardData,
            dailyRewardOrder = new List<int>(d.dailyRewardOrder),
            lastRewardTimestamp = d.lastRewardTimestamp,
            adsRemoved = d.adsRemoved,

            musicVolume = d.musicVolume,
            sfxVolume = d.sfxVolume,
            musicEnabled = d.musicEnabled,
            sfxEnabled = d.sfxEnabled,

            totalGamesPlayed = d.totalGamesPlayed,
            totalEnemiesKilled = d.totalEnemiesKilled,
            bestCombo = d.bestCombo,
            totalPlayTime = d.totalPlayTime,
            lastPlayDate = d.lastPlayDate.ToString("o"),

            consecutiveLevelWins = d.consecutiveLevelWins,
            lastCompletedLevel = d.lastCompletedLevel,

            emergencyBundleUsesToday = d.emergencyBundleUsesToday,
            emergencyBundleLastActivationUtc = d.emergencyBundleLastActivationUtc,
            consecutiveBossLosses = d.consecutiveBossLosses,
            consecutiveLosses = d.consecutiveLosses,
            pendingPurchaseProductId = d.pendingPurchaseProductId,
            grantedPurchaseTransactionIds = new List<string>(d.grantedPurchaseTransactionIds ?? new List<string>()),
            unlimitedLivesStartUtc = d.unlimitedLivesStartUtc,
            unlimitedLivesEndUtc = d.unlimitedLivesEndUtc,
            coins = d.coins,
            hasFirstOpenFired = d.hasFirstOpenFired,
            hasSeenFirstTimeWelcome = d.hasSeenFirstTimeWelcome,
            firstTimeWelcomeDailyStartupSuppressedDateUtc = d.firstTimeWelcomeDailyStartupSuppressedDateUtc ?? "",
            dailyWheelData = new DailyWheelSaveData
            {
                lastSpinDateIso  = d.dailyWheelData?.lastSpinDateIso ?? "",
                lastSpinTimestampUtc = d.dailyWheelData?.lastSpinTimestampUtc ?? "",
                consecutiveSpins = d.dailyWheelData?.consecutiveSpins ?? 0,
                totalSpins       = d.dailyWheelData?.totalSpins ?? 0,
                pendingFreeSpins = d.dailyWheelData?.pendingFreeSpins ?? 0,
                lastAutoShowDateIso = d.dailyWheelData?.lastAutoShowDateIso ?? "",
                lastAutoShowTimestampUtc = d.dailyWheelData?.lastAutoShowTimestampUtc ?? "",
            },
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

        if (d.tutorialStates != null)
            foreach (var kv in d.tutorialStates)
                dto.tutorialStates.Add(new StringIntKV { key = kv.Key, value = kv.Value });

        if (d.tutorialStepIndices != null)
            foreach (var kv in d.tutorialStepIndices)
                dto.tutorialStepIndices.Add(new StringIntKV { key = kv.Key, value = kv.Value });

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
            lastKnownLocalUtc = dto.lastKnownLocalUtc ?? "",
            lastTrustedUtc = dto.lastTrustedUtc ?? "",
            lastTimeValidationUtc = dto.lastTimeValidationUtc ?? "",
            trustedTimeAvailable = dto.trustedTimeAvailable,
            suspiciousTimeDetected = dto.suspiciousTimeDetected,
            activeLevelAttempt = dto.activeLevelAttempt ?? new ActiveLevelAttemptData(),

            activePowerUps = dto.activePowerUps?.ConvertAll(p =>
            {
                var item = new PowerUpData(p.type, p.usesRemaining);
                if (DateTime.TryParse(p.activationTimeIso, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                    item.activationTime = dt;
                return item;
            }) ?? new List<PowerUpData>(),
            powerUpInventory = dto.powerUpInventory ?? new List<PowerUpInventoryItem>(),

            dailyRewardData = dto.dailyRewardData ?? "",
            dailyRewardOrder = dto.dailyRewardOrder ?? new List<int>(),
            lastRewardTimestamp = dto.lastRewardTimestamp ?? "",
            adsRemoved = dto.adsRemoved,

            musicVolume = dto.musicVolume,
            sfxVolume = dto.sfxVolume,
            musicEnabled = dto.musicEnabled,
            sfxEnabled = dto.sfxEnabled,

            totalGamesPlayed = dto.totalGamesPlayed,
            totalEnemiesKilled = dto.totalEnemiesKilled,
            bestCombo = dto.bestCombo,
            totalPlayTime = dto.totalPlayTime,

            consecutiveLevelWins = dto.consecutiveLevelWins,
            lastCompletedLevel = dto.lastCompletedLevel,

            emergencyBundleUsesToday = dto.emergencyBundleUsesToday,
            emergencyBundleLastActivationUtc = dto.emergencyBundleLastActivationUtc,
            consecutiveBossLosses = dto.consecutiveBossLosses,
            consecutiveLosses = dto.consecutiveLosses,
            pendingPurchaseProductId = dto.pendingPurchaseProductId ?? "",
            grantedPurchaseTransactionIds = SanitizeGrantedPurchaseTransactionIds(dto.grantedPurchaseTransactionIds),
            unlimitedLivesStartUtc = dto.unlimitedLivesStartUtc,
            unlimitedLivesEndUtc = dto.unlimitedLivesEndUtc,
            coins = dto.coins,
            hasFirstOpenFired = dto.hasFirstOpenFired,
            hasSeenFirstTimeWelcome = dto.hasSeenFirstTimeWelcome,
            firstTimeWelcomeDailyStartupSuppressedDateUtc = dto.firstTimeWelcomeDailyStartupSuppressedDateUtc ?? "",
            dailyWheelData = dto.dailyWheelData ?? new DailyWheelSaveData(),
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

        d.tutorialStates = new Dictionary<string, int>();
        if (dto.tutorialStates != null)
            foreach (var kv in dto.tutorialStates)
                if (!string.IsNullOrEmpty(kv.key))
                    d.tutorialStates[kv.key] = kv.value;

        d.tutorialStepIndices = new Dictionary<string, int>();
        if (dto.tutorialStepIndices != null)
            foreach (var kv in dto.tutorialStepIndices)
                if (!string.IsNullOrEmpty(kv.key))
                    d.tutorialStepIndices[kv.key] = kv.value;

        return d;
    }

    private static List<string> SanitizeGrantedPurchaseTransactionIds(List<string> purchaseKeys)
    {
        List<string> sanitizedKeys = new List<string>();
        if (purchaseKeys == null)
            return sanitizedKeys;

        for (int i = 0; i < purchaseKeys.Count; i++)
        {
            string purchaseKey = purchaseKeys[i];
            if (string.IsNullOrWhiteSpace(purchaseKey) || sanitizedKeys.Contains(purchaseKey))
                continue;

            sanitizedKeys.Add(purchaseKey);
        }

        return sanitizedKeys;
    }
}
