using System;
using UnityEngine;

public static class GameEvents
{
    #region LIFE SYSTEM EVENTS

    public static event Action<int> OnLivesChanged;
    public static event Action<TimeSpan> OnLifeRegenTimeChanged;
    public static event Action OnLifeGainedFromReward;
    public static event Action<string> OnNicknameChanged;

    public static void RaiseLivesChanged(int newLives)
    {
        OnLivesChanged?.Invoke(newLives);
    }

    public static void RaiseLifeRegenTimeChanged(TimeSpan timeToNext)
    {
        OnLifeRegenTimeChanged?.Invoke(timeToNext);
    }

    public static void RaiseLifeGainedFromReward()
    {
        OnLifeGainedFromReward?.Invoke();
    }

    #endregion

    #region LEVEL SYSTEM EVENTS

    public static event Action OnLevelStarted;
    public static event Action<LevelStats> OnLevelCompleted;
    public static event Action<LevelFailedContext> OnLevelFailed;
    public static event Action<float> OnLevelTimeChanged;
    public static event Action OnLevelTimeExpired;

    public static void RaiseLevelStarted()
    {
        OnLevelStarted?.Invoke();
    }

    public static void RaiseLevelCompleted(LevelStats stats)
    {
        OnLevelCompleted?.Invoke(stats);
    }

    public static void RaiseLevelFailed(LevelFailedContext context)
    {
        OnLevelFailed?.Invoke(context);
    }

    public static void RaiseLevelTimeChanged(float currentTime)
    {
        OnLevelTimeChanged?.Invoke(currentTime);
    }

    public static void RaiseLevelTimeExpired()
    {
        OnLevelTimeExpired?.Invoke();
    }

    #endregion

    #region COMBO EVENTS

    public static event Action<int, Vector3> OnComboUpdated;
    public static event Action OnComboReset;
    public static event Action<float> OnLevelTimeBonus;

    public static void RaiseComboUpdated(int comboLevel, Vector3 position)
    {
        OnComboUpdated?.Invoke(comboLevel, position);
    }

    public static void RaiseComboReset()
    {
        OnComboReset?.Invoke();
    }

    public static void RaiseLevelTimeBonus(float bonusSeconds)
    {
        OnLevelTimeBonus?.Invoke(bonusSeconds);
    }

    #endregion

    #region POWER-UP EVENTS

    public static event Action<PowerUpType, int> OnPowerUpActivated;

    public static event Action<PowerUpType> OnPowerUpExpired;

    public static event Action<PowerUpType, int> OnPowerUpUsesUpdated;

    public static event Action<PowerUpType, int> OnPowerUpUseConsumed;

    public static event Action<string> OnPowerUpUsesTextChanged;

    public static event Action OnLevelEndedConsumePowerUps;

    public static event Action<PowerUpType> OnPowerUpPurchased;

    public static void RaisePowerUpActivated(PowerUpType type, int uses)
    {
        OnPowerUpActivated?.Invoke(type, uses);
    }

    public static void RaisePowerUpExpired(PowerUpType type)
    {
        OnPowerUpExpired?.Invoke(type);
    }

    public static void RaisePowerUpUsesUpdated(PowerUpType type, int usesRemaining)
    {
        OnPowerUpUsesUpdated?.Invoke(type, usesRemaining);

        string usesText = $"{usesRemaining} uses";
        OnPowerUpUsesTextChanged?.Invoke(usesText);
    }

    public static void RaisePowerUpUseConsumed(PowerUpType type, int usesRemaining)
    {
        OnPowerUpUseConsumed?.Invoke(type, usesRemaining);
    }

    public static void RaiseLevelEndedConsumePowerUps()
    {
        OnLevelEndedConsumePowerUps?.Invoke();
    }

    public static void RaisePowerUpPurchased(PowerUpType type) => OnPowerUpPurchased?.Invoke(type);

    #endregion

    #region ENEMY EVENTS

    public static event Action<LevelStats> OnAllEnemiesDefeated;
    public static event Action<int> OnEnemyDefeated;

    public static void RaiseAllEnemiesDefeated(LevelStats stats)
    {
        OnAllEnemiesDefeated?.Invoke(stats);
    }

    public static void RaiseEnemyDefeated(int enemyCount)
    {
        OnEnemyDefeated?.Invoke(enemyCount);
    }

    // Evento canónico: emitido una única vez por enemigo muerto, desde Enemy.RegisterKill()
    public static event Action<Vector3> OnEnemyKilled;
    public static void RaiseEnemyKilled(Vector3 position)
    {
        OnEnemyKilled?.Invoke(position);
    }

    public static event Action<int> OnBL4ZTExplosionKills;
    public static void RaiseBL4ZTExplosionKills(int killCount)
    {
        OnBL4ZTExplosionKills?.Invoke(killCount);
    }

    public static event Action OnBreakablePlatformBroken;
    public static void RaiseBreakablePlatformBroken()
    {
        OnBreakablePlatformBroken?.Invoke();
    }

    #endregion

    #region PLAYER EVENTS

    public static event Action OnDashStarted;
    public static void RaiseDashStarted() => OnDashStarted?.Invoke();

    public static event Action OnDashEnded;
    public static void RaiseDashEnded() => OnDashEnded?.Invoke();

    public static event Action OnParrySuccessful;
    public static void RaiseParrySuccessful() => OnParrySuccessful?.Invoke();

    #endregion

    #region PROGRESSION EVENTS

    public static event Action OnProgressionUpdated;
    public static event Action<int, int> OnStarsUpdated;

    public static void RaiseProgressionUpdated()
    {
        OnProgressionUpdated?.Invoke();
    }

    public static void RaiseStarsUpdated(int levelId, int stars)
    {
        OnStarsUpdated?.Invoke(levelId, stars);
    }

    #endregion

    #region SAVE SYSTEM EVENTS

    public static event Action<GameData> OnDataLoaded;
    public static event Action OnDataSaved;

    public static void RaiseDataLoaded(GameData data)
    {
        OnDataLoaded?.Invoke(data);
    }

    public static void RaiseDataSaved()
    {
        OnDataSaved?.Invoke();
    }

    #endregion

    #region DAILY REWARDS EVENTS

    public static event Action<DailyReward> OnRewardClaimed;
    public static event Action<bool> OnRewardAvailabilityChanged;
    public static event Action<int> OnConsecutiveDaysUpdated;
    public static event Action OnRewardDoubled;
    public static event Action OnAdsRemoved;

    public static void RaiseRewardClaimed(DailyReward reward)
    {
        OnRewardClaimed?.Invoke(reward);
    }

    public static void RaiseRewardAvailabilityChanged(bool isAvailable)
    {
        OnRewardAvailabilityChanged?.Invoke(isAvailable);
    }

    public static void RaiseConsecutiveDaysUpdated(int days)
    {
        OnConsecutiveDaysUpdated?.Invoke(days);
    }

    public static void RaiseRewardDoubled()
    {
        OnRewardDoubled?.Invoke();
    }

    public static void RaiseAdsRemoved()
    {
        OnAdsRemoved?.Invoke();
    }

    #endregion

    #region CURRENCY EVENTS

    public static event Action<int> OnCoinsChanged;
    public static event Action<int, RectTransform> OnCoinPackPurchaseFeedbackRequested;

    public static void RaiseCoinsChanged(int newTotal)
    {
        OnCoinsChanged?.Invoke(newTotal);
    }

    public static void RaiseCoinPackPurchaseFeedbackRequested(int coinAmount, RectTransform sourceTransform)
    {
        OnCoinPackPurchaseFeedbackRequested?.Invoke(coinAmount, sourceTransform);
    }

    #endregion

    #region REWARD EVENTS

    public static event Action<StoreProductDefinition> OnRewardGranted;

    public static void RaiseRewardGranted(StoreProductDefinition product)
        => OnRewardGranted?.Invoke(product);

    #endregion

    #region DAILY WHEEL EVENTS

    public static event Action<bool> OnWheelAvailabilityChanged;
    public static event Action<WheelReward> OnWheelSpun;

    public static void RaiseWheelAvailabilityChanged(bool isAvailable)
    {
        OnWheelAvailabilityChanged?.Invoke(isAvailable);
    }

    public static void RaiseWheelSpun(WheelReward reward)
    {
        OnWheelSpun?.Invoke(reward);
    }

    #endregion

    #region CLEANING SUPPLIES

    public static void ClearAllLifeEvents()
    {
        OnLivesChanged = null;
        OnLifeRegenTimeChanged = null;
        OnLifeGainedFromReward = null;
    }

    public static void ClearAllLevelEvents()
    {
        OnLevelStarted = null;
        OnLevelCompleted = null;
        OnLevelFailed = null;
        OnLevelTimeChanged = null;
        OnLevelTimeExpired = null;
    }

    public static void ClearAllComboEvents()
    {
        OnComboUpdated = null;
        OnComboReset = null;
        OnLevelTimeBonus = null;
    }

    public static void ClearAllPowerUpEvents()
    {
        OnPowerUpActivated = null;
        OnPowerUpExpired = null;
        OnPowerUpPurchased = null;
    }

    public static void ClearAllEnemyEvents()
    {
        OnEnemyKilled = null;
        OnAllEnemiesDefeated = null;
        OnEnemyDefeated = null;
    }

    public static void ClearAllPlayerEvents()
    {
        OnDashStarted = null;
        OnDashEnded = null;
        OnParrySuccessful = null;
    }

    public static void ClearAllProgressionEvents()
    {
        OnProgressionUpdated = null;
        OnStarsUpdated = null;
    }

    public static void ClearAllSaveEvents()
    {
        OnDataLoaded = null;
        OnDataSaved = null;
    }

    public static void ClearAllRewardEvents()
    {
        OnRewardClaimed = null;
        OnRewardAvailabilityChanged = null;
        OnConsecutiveDaysUpdated = null;
        OnRewardDoubled = null;
        OnAdsRemoved = null;
        OnRewardGranted = null;
    }

    public static void ClearAllCurrencyEvents()
    {
        OnCoinsChanged = null;
        OnCoinPackPurchaseFeedbackRequested = null;
    }

    public static void ClearAllEvents()
    {
        ClearAllLifeEvents();
        ClearAllLevelEvents();
        ClearAllComboEvents();
        ClearAllPowerUpEvents();
        ClearAllEnemyEvents();
        ClearAllPlayerEvents();
        ClearAllProgressionEvents();
        ClearAllSaveEvents();
        ClearAllRewardEvents();
        ClearAllCurrencyEvents();
    }

    #endregion
}
