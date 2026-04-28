using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public class DailyReward
{
    public PowerUpType powerUpType;
    public int quantity;
    public Sprite icon;
    public string displayName;
    public string description;
}

[Serializable]
public class DailyRewardSaveData
{
    public bool[] claimedDays = new bool[7];
    public string lastClaimDate;
    public string lastClaimTimestampUtc;
    public string lastAutoShowDate;
    public string lastAutoShowTimestampUtc;
    public int currentWeekDay;
    public int consecutiveDays;

    public DailyRewardSaveData()
    {
        claimedDays = new bool[7];
        lastClaimDate = "";
        lastClaimTimestampUtc = "";
        lastAutoShowDate = "";
        lastAutoShowTimestampUtc = "";
        currentWeekDay = 0;
        consecutiveDays = 0;
    }
}

public class DailyRewardSystem : MonoBehaviourSingleton<DailyRewardSystem>
{
    [Header("SETTINGS")]
    public DailyReward[] weeklyRewards = new DailyReward[7];
    private int WeekLength => GameConfigManager.Config.dailyRewardWeekLength;

    private DailyRewardSaveData rewardData;

    private bool _hasDoubledToday = false;

    void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded)
            BootstrapFromSave();
    }

    private void OnEnable()
    {
        SaveManager.OnDataLoaded += HandleDataLoaded;
    }

    private void OnDisable()
    {
        SaveManager.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded(GameData _)
    {
        BootstrapFromSave();
    }

    private void BootstrapFromSave()
    {
        LoadRewardData();
        SyncRewardState();
        CheckDoubleRewardStatus();

        GameEvents.RaiseRewardAvailabilityChanged(CanClaimToday());
    }

    void LoadRewardData()
    {
        string jsonData = SaveManager.Instance.GetDailyRewardData();
        if (string.IsNullOrEmpty(jsonData))
        {
            rewardData = new DailyRewardSaveData();
        }
        else
        {
            try
            {
                rewardData = JsonUtility.FromJson<DailyRewardSaveData>(jsonData);
            }
            catch (Exception e)
            {
                rewardData = new DailyRewardSaveData();
            }
        }

        EnsureSaveDataShape();
        MigrateLegacyRewardTimestamps();
    }

    void SaveRewardData(bool updateLastRewardTimestamp = true)
    {
        string jsonData = JsonUtility.ToJson(rewardData);

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.SaveDailyRewardData(jsonData, updateLastRewardTimestamp);
        }
        else
        {
            SaveManager.Instance.SaveDailyRewardData(jsonData, updateLastRewardTimestamp);
        }
    }

    void CheckDailyReward()
    {
        bool wasAvailable = IsRewardAvailable();
        bool stateChanged = SyncRewardState();
        bool isAvailableNow = IsRewardAvailable();

        if (stateChanged || wasAvailable != isAvailableNow)
            GameEvents.RaiseRewardAvailabilityChanged(isAvailableNow);
    }

    private void CheckDoubleRewardStatus()
    {
        if (IsRewardAvailable())
            _hasDoubledToday = false;
    }

    public void DoubleTodaysReward()
    {
        if (_hasDoubledToday)
        {
            Debug.LogWarning("Ya se duplicó la recompensa de hoy");
            return;
        }

        if (rewardData.claimedDays[rewardData.currentWeekDay])
        {
            Debug.LogWarning("No se puede duplicar una recompensa ya reclamada");
            return;
        }

        var todayReward = weeklyRewards[rewardData.currentWeekDay];

        var doubledReward = new DailyReward
        {
            powerUpType = todayReward.powerUpType,
            quantity = todayReward.quantity * 2,
            icon = todayReward.icon,
            displayName = todayReward.displayName + " x2",
            description = "Recompensa duplicada por anuncio"
        };

        AddPowerUpToInventoryViaAutoSave(doubledReward);

        rewardData.claimedDays[rewardData.currentWeekDay] = true;
        SetLastClaimTimestamp(DateTime.UtcNow);

        _hasDoubledToday = true;

        SaveRewardData();

        GameEvents.RaiseRewardClaimed(doubledReward);
        GameEvents.RaiseRewardAvailabilityChanged(false);
        GameEvents.RaiseRewardDoubled();
    }

    public bool CanDoubleToday()
    {
        return !_hasDoubledToday &&
               !rewardData.claimedDays[rewardData.currentWeekDay] &&
               CanClaimToday();
    }

    public bool HasDoubledToday()
    {
        return _hasDoubledToday;
    }

    public bool ShouldAutoShowToday()
    {
        SyncRewardState();

        if (!CanClaimToday())
            return false;

        DateTime lastClaimUtc = GetLastClaimTimestampSafe();
        DateTime lastAutoShowUtc = GetLastAutoShowTimestampSafe();

        if (lastClaimUtc == DateTime.MinValue)
            return lastAutoShowUtc == DateTime.MinValue;

        return lastAutoShowUtc < GetNextRewardAvailabilityUtc();
    }

    public void MarkAutoShowShownToday()
    {
        if (rewardData == null)
            return;

        DateTime nowUtc = DateTime.UtcNow;
        rewardData.lastAutoShowTimestampUtc = nowUtc.ToString("o");
        rewardData.lastAutoShowDate = nowUtc.ToString("yyyy-MM-dd");
        SaveRewardData(updateLastRewardTimestamp: false);
    }

    void AdvanceDay()
    {
        rewardData.currentWeekDay = (rewardData.currentWeekDay + 1) % WeekLength;
        rewardData.consecutiveDays++;

        if (rewardData.currentWeekDay == 0)
        {
            rewardData.claimedDays = new bool[WeekLength];
        }

        //DEPRECATED
        //OnConsecutiveDaysUpdated?.Invoke(rewardData.consecutiveDays);

        GameEvents.RaiseConsecutiveDaysUpdated(rewardData.consecutiveDays);
    }

    void ResetWeeklyProgress()
    {
        int previousDays = rewardData.consecutiveDays;
        rewardData.currentWeekDay = 0;
        rewardData.consecutiveDays = 0;
        rewardData.claimedDays = new bool[WeekLength];

        // DEPRECATED
        //OnConsecutiveDaysUpdated?.Invoke(rewardData.consecutiveDays);

        GameEvents.RaiseConsecutiveDaysUpdated(rewardData.consecutiveDays);
    }

    public bool ClaimReward()
    {
        SyncRewardState();

        if (!CanClaimToday()) return false;

        if (rewardData.claimedDays[rewardData.currentWeekDay])
        {
            return false;
        }

        rewardData.claimedDays[rewardData.currentWeekDay] = true;
        SetLastClaimTimestamp(DateTime.UtcNow);

        var claimed = weeklyRewards[rewardData.currentWeekDay];

        AddPowerUpToInventoryViaAutoSave(claimed);

        SaveRewardData();

        GameEvents.RaiseRewardClaimed(claimed);
        GameEvents.RaiseRewardAvailabilityChanged(false);

        return true;
    }

    void AddPowerUpToInventoryDirect(DailyReward reward)
    {
        var gameData = SaveManager.Instance.GetGameData();
        var inventory = gameData.powerUpInventory;
        var item = inventory.Find(i => i.type == reward.powerUpType);

        if (item != null)
        {
            item.quantity += reward.quantity;
            item.lastUpdated = DateTime.UtcNow;
        }
        else
        {
            inventory.Add(new PowerUpInventoryItem(reward.powerUpType, reward.quantity));
        }

        SaveManager.Instance.SaveData();
    }

    void AddPowerUpToInventoryViaAutoSave(DailyReward reward)
    {
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpObtained(reward.powerUpType, reward.quantity);
        }
        else
        {
            AddPowerUpToInventoryDirect(reward);
        }
    }

    public bool CanClaimToday()
    {
        SyncRewardState();
        return IsRewardAvailable();
    }

    public DailyReward GetTodayReward()
    {
        SyncRewardState();
        return weeklyRewards[rewardData.currentWeekDay];
    }

    public int GetConsecutiveDays()
    {
        return rewardData.consecutiveDays;
    }

    public bool[] GetWeeklyProgress()
    {
        SyncRewardState();
        return rewardData.claimedDays;
    }

    public int GetCurrentWeekDay()
    {
        SyncRewardState();
        return rewardData.currentWeekDay;
    }

    public string GetTimeUntilNextReward()
    {
        if (CanClaimToday())
            return "DISPONIBLE AHORA";

        TimeSpan timeUntilNext = GetNextRewardAvailabilityUtc() - DateTime.UtcNow;

        if (timeUntilNext.TotalSeconds <= 0)
            return "DISPONIBLE AHORA";

        return DailyAvailabilityTimeUtility.FormatCountdown(timeUntilNext);
    }

    public DateTime GetNextRewardAvailabilityUtc()
    {
        DateTime lastClaimUtc = GetLastClaimTimestampSafe();
        if (lastClaimUtc == DateTime.MinValue)
            return DateTime.UtcNow;

        return lastClaimUtc + DailyAvailabilityTimeUtility.CooldownInterval;
    }

    private bool SyncRewardState()
    {
        if (rewardData == null)
            return false;

        EnsureSaveDataShape();

        DateTime lastClaimUtc = GetLastClaimTimestampSafe();
        if (lastClaimUtc == DateTime.MinValue)
            return false;

        if (!rewardData.claimedDays[rewardData.currentWeekDay])
            return false;

        TimeSpan elapsed = DateTime.UtcNow - lastClaimUtc;
        if (elapsed < DailyAvailabilityTimeUtility.CooldownInterval)
            return false;

        if (elapsed >= DailyAvailabilityTimeUtility.MissedWindowThreshold)
        {
            ResetWeeklyProgress();
            SaveRewardData(updateLastRewardTimestamp: false);
            return true;
        }

        AdvanceDay();
        SaveRewardData(updateLastRewardTimestamp: false);
        return true;
    }

    private bool IsRewardAvailable()
    {
        DateTime lastClaimUtc = GetLastClaimTimestampSafe();
        if (lastClaimUtc == DateTime.MinValue)
            return true;

        return DateTime.UtcNow >= lastClaimUtc + DailyAvailabilityTimeUtility.CooldownInterval;
    }

    private DateTime GetLastClaimTimestampSafe()
    {
        if (DailyAvailabilityTimeUtility.TryParseIsoUtc(rewardData?.lastClaimTimestampUtc, out var exactUtc))
            return exactUtc;

        var gd = SaveManager.Instance.GetGameData();
        if (DailyAvailabilityTimeUtility.TryParseIsoUtc(gd.lastRewardTimestamp, out var fallbackUtc))
            return fallbackUtc;

        if (DailyAvailabilityTimeUtility.TryParseUtcDate(rewardData?.lastClaimDate, out var legacyUtc))
            return legacyUtc;

        return DateTime.MinValue;
    }

    private DateTime GetLastAutoShowTimestampSafe()
    {
        if (DailyAvailabilityTimeUtility.TryParseIsoUtc(rewardData?.lastAutoShowTimestampUtc, out var exactUtc))
            return exactUtc;

        if (DailyAvailabilityTimeUtility.TryParseUtcDate(rewardData?.lastAutoShowDate, out var legacyUtc))
            return legacyUtc;

        return DateTime.MinValue;
    }

    private void SetLastClaimTimestamp(DateTime claimUtc)
    {
        DateTime normalizedUtc = DailyAvailabilityTimeUtility.NormalizeUtc(claimUtc);
        rewardData.lastClaimTimestampUtc = normalizedUtc.ToString("o");
        rewardData.lastClaimDate = normalizedUtc.ToString("yyyy-MM-dd");
    }

    private void EnsureSaveDataShape()
    {
        if (rewardData.claimedDays == null || rewardData.claimedDays.Length != WeekLength)
            rewardData.claimedDays = new bool[WeekLength];

        rewardData.currentWeekDay = Mathf.Clamp(rewardData.currentWeekDay, 0, Mathf.Max(0, WeekLength - 1));
    }

    private void MigrateLegacyRewardTimestamps()
    {
        DateTime lastClaimUtc = GetLastClaimTimestampSafe();
        if (lastClaimUtc > DateTime.MinValue)
        {
            if (string.IsNullOrEmpty(rewardData.lastClaimTimestampUtc))
                rewardData.lastClaimTimestampUtc = lastClaimUtc.ToString("o");

            if (string.IsNullOrEmpty(rewardData.lastClaimDate))
                rewardData.lastClaimDate = lastClaimUtc.ToString("yyyy-MM-dd");
        }

        DateTime lastAutoShowUtc = GetLastAutoShowTimestampSafe();
        if (lastAutoShowUtc > DateTime.MinValue)
        {
            if (string.IsNullOrEmpty(rewardData.lastAutoShowTimestampUtc))
                rewardData.lastAutoShowTimestampUtc = lastAutoShowUtc.ToString("o");

            if (string.IsNullOrEmpty(rewardData.lastAutoShowDate))
                rewardData.lastAutoShowDate = lastAutoShowUtc.ToString("yyyy-MM-dd");
        }
    }

    private void ValidateWeeklyRewardsArray()
    {
        if (weeklyRewards.Length != WeekLength)
        {
            Debug.LogWarning($"weeklyRewards debe tener {WeekLength} elementos");
        }
    }
}
