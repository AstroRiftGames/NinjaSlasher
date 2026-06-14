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
    public static event Action OnBootstrapped;
    private const int DefaultWeekLength = 7;

    [Header("SETTINGS")]
    public DailyReward[] weeklyRewards = new DailyReward[7];
    private int WeekLength
    {
        get
        {
            GameConfig config = GameConfigManager.GetConfig();
            if (config != null && config.dailyRewardWeekLength > 0)
                return config.dailyRewardWeekLength;

            if (weeklyRewards != null && weeklyRewards.Length > 0)
                return weeklyRewards.Length;

            return DefaultWeekLength;
        }
    }

    private DailyRewardSaveData rewardData;

    public bool IsBootstrapped { get; private set; }
    private bool _bootstrapSignalEmitted;
    private SaveBootstrapSync _saveBootstrapSync;

    public override void Awake()
    {
        base.Awake();
        _saveBootstrapSync = new SaveBootstrapSync("DailyRewardSystem", () => IsBootstrapped, BootstrapFromSave);
    }

    private void OnEnable()
    {
        _saveBootstrapSync?.Enable();
    }

    private void OnDisable()
    {
        _saveBootstrapSync?.Disable();
    }

    private void BootstrapFromSave()
    {
        LoadRewardData();
        SyncRewardState();
        IsBootstrapped = true;

        if (!_bootstrapSignalEmitted)
        {
            _bootstrapSignalEmitted = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[DailyRewardSystem] Bootstrap -> Completed");
#endif
            OnBootstrapped?.Invoke();
        }

        GameEvents.RaiseRewardAvailabilityChanged(CanClaimToday());
    }

    protected override void OnDestroy()
    {
        _saveBootstrapSync?.Dispose();

        if (Instance == this)
            OnBootstrapped = null;

        base.OnDestroy();
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
            catch (Exception)
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

        GameEvents.RaiseConsecutiveDaysUpdated(rewardData.consecutiveDays);
    }

    void ResetWeeklyProgress()
    {
        rewardData.currentWeekDay = 0;
        rewardData.consecutiveDays = 0;
        rewardData.claimedDays = new bool[WeekLength];

        GameEvents.RaiseConsecutiveDaysUpdated(rewardData.consecutiveDays);
    }

    public bool ClaimReward()
    {
        SyncRewardState();

        if (!CanClaimToday())
            return false;

        if (rewardData.claimedDays[rewardData.currentWeekDay])
            return false;

        rewardData.claimedDays[rewardData.currentWeekDay] = true;
        SetLastClaimTimestamp(DateTime.UtcNow);

        DailyReward claimed = weeklyRewards[rewardData.currentWeekDay];

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

    public DailyAvailabilitySnapshot GetAvailabilitySnapshot()
    {
        bool canClaimToday = CanClaimToday();
        DateTime nextAvailabilityUtc = canClaimToday
            ? DateTime.UtcNow
            : GetNextRewardAvailabilityUtc();

        return new DailyAvailabilitySnapshot(canClaimToday, nextAvailabilityUtc, canClaimToday ? 1 : 0);
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

public sealed class SaveBootstrapSync : IDisposable
{
    private readonly string _systemName;
    private readonly Func<bool> _isAlreadyBootstrapped;
    private readonly Action _bootstrapFromSave;
    private readonly Action<GameData> _dataLoadedHandler;
    private bool _isSubscribed;

    public SaveBootstrapSync(string systemName, Func<bool> isAlreadyBootstrapped, Action bootstrapFromSave)
    {
        _systemName = systemName;
        _isAlreadyBootstrapped = isAlreadyBootstrapped;
        _bootstrapFromSave = bootstrapFromSave;
        _dataLoadedHandler = _ => TryBootstrapFromCurrentSave("SaveManager.OnDataLoaded");
    }

    public void Enable()
    {
        if (!_isSubscribed)
        {
            SaveManager.OnDataLoaded += _dataLoadedHandler;
            _isSubscribed = true;
        }

        TryBootstrapFromCurrentSave("OnEnable");
    }

    public void Disable()
    {
        if (!_isSubscribed)
            return;

        SaveManager.OnDataLoaded -= _dataLoadedHandler;
        _isSubscribed = false;
    }

    public bool TryBootstrapFromCurrentSave(string reason)
    {
        if (SaveManager.Instance == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[{_systemName}] Bootstrap -> Waiting | Reason={reason} | saveManagerMissing=true");
#endif
            return false;
        }

        if (!SaveManager.Instance.IsDataLoaded)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[{_systemName}] Bootstrap -> Waiting | Reason={reason} | dataLoaded=false");
#endif
            return false;
        }

        _bootstrapFromSave?.Invoke();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[{_systemName}] Bootstrap -> Synced | Reason={reason} | alreadyBootstrapped={_isAlreadyBootstrapped()}");
#endif
        return true;
    }

    public void Dispose()
    {
        Disable();
    }
}

public readonly struct DailyAvailabilitySnapshot
{
    public bool IsAvailable { get; }
    public DateTime NextAvailabilityUtc { get; }
    public int AvailableCount { get; }

    public DailyAvailabilitySnapshot(bool isAvailable, DateTime nextAvailabilityUtc, int availableCount)
    {
        IsAvailable = isAvailable;
        NextAvailabilityUtc = nextAvailabilityUtc;
        AvailableCount = Math.Max(0, availableCount);
    }
}
