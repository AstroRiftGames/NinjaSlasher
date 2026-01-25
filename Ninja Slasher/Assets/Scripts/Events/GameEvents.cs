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
    public static event Action<string> OnLevelFailed;
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

    public static void RaiseLevelFailed(string reason)
    {
        OnLevelFailed?.Invoke(reason);
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
    }

    public static void ClearAllEnemyEvents()
    {
        OnAllEnemiesDefeated = null;
        OnEnemyDefeated = null;
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
    }

    public static void ClearAllEvents()
    {
        ClearAllLifeEvents();
        ClearAllLevelEvents();
        ClearAllComboEvents();
        ClearAllPowerUpEvents();
        ClearAllEnemyEvents();
        ClearAllProgressionEvents();
        ClearAllSaveEvents();
        ClearAllRewardEvents();
    }

    #endregion
}