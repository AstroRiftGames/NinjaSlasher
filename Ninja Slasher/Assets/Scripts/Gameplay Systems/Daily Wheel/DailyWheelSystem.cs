using System;
using UnityEngine;

public class DailyWheelSystem : MonoBehaviourSingleton<DailyWheelSystem>
{
    public static event Action OnBootstrapped;

    [Header("DEBUG")]
    [SerializeField] private bool debugInfiniteSpins = false;

    [Header("WHEEL CONFIGURATION")]
    [SerializeField] private WheelRewardSet _rewardSet;

    private WheelData wheelData = new WheelData();
    private bool _bootstrapSignalEmitted;
    private SaveBootstrapSync _saveBootstrapSync;

    public WheelReward[] WheelRewards => _rewardSet?.rewards;
    public bool IsBootstrapped { get; private set; }

    public override void Awake()
    {
        base.Awake();
        _saveBootstrapSync = new SaveBootstrapSync("DailyWheelSystem", () => IsBootstrapped, BootstrapFromSave);
    }

    private void OnEnable()
    {
        _saveBootstrapSync?.Enable();
    }

    private void OnDisable()
    {
        _saveBootstrapSync?.Disable();
    }

    protected override void OnDestroy()
    {
        _saveBootstrapSync?.Dispose();

        if (Instance == this)
            OnBootstrapped = null;

        base.OnDestroy();
    }

    public bool SpinWheel(out WheelReward reward)
    {
        reward = null;

        if (!CanSpinToday())
        {
            return false;
        }

        bool consumePendingFreeSpin = ShouldConsumePendingFreeSpin();
        reward = GetCalculatedReward();

        if (reward == null)
        {
            return false;
        }

        ApplyReward(reward);

        if (!debugInfiniteSpins)
        {
            UpdateSpinProgress(consumePendingFreeSpin);
            SaveWheelData();
        }

        GameEvents.RaiseWheelSpun(reward);

        CheckWheelAvailability();
        GameEvents.RaiseConsecutiveDaysUpdated(wheelData.consecutiveSpins);

        return true;
    }

    public bool CanSpinToday()
    {
        if (debugInfiniteSpins) return true;

        if (HasPendingFreeSpins())
            return true;

        return HasDailySpinAvailable();
    }

    public int GetAvailableSpinCount()
    {
        if (debugInfiniteSpins)
            return 1;

        return (HasDailySpinAvailable() ? 1 : 0) + wheelData.pendingFreeSpins;
    }

    public int GetPendingFreeSpins()
    {
        if (debugInfiniteSpins)
            return 0;

        return Mathf.Max(0, wheelData.pendingFreeSpins);
    }

    public DailyAvailabilitySnapshot GetAvailabilitySnapshot()
    {
        bool canSpinToday = CanSpinToday();
        int availableSpinCount = GetAvailableSpinCount();
        DateTime nextAvailabilityUtc = canSpinToday
            ? DateTime.UtcNow
            : GetNextDailySpinAvailabilityUtc();

        return new DailyAvailabilitySnapshot(canSpinToday, nextAvailabilityUtc, availableSpinCount);
    }

    public bool ShouldAutoShowToday()
    {
        if (!CanSpinToday())
            return false;

        return GetLastAutoShowDateSafe() < DateTime.UtcNow.Date;
    }

    public void MarkAutoShowShownToday()
    {
        if (debugInfiniteSpins)
            return;

        DateTime currentDate = DateTime.UtcNow.Date;
        if (GetLastAutoShowDateSafe() == currentDate)
            return;

        wheelData.lastAutoShowDate = currentDate.ToString("yyyy-MM-dd");
        wheelData.lastAutoShowTimestampUtc = DateTime.UtcNow.ToString("o");
        SaveWheelData();
    }

    public void GrantFreeSpins(int quantity)
    {
        if (debugInfiniteSpins)
            return;

        AddFreeSpins(quantity);
        SaveWheelData();
        CheckWheelAvailability();
    }

    private bool HasDailySpinAvailable()
    {
        DateTime lastSpinUtc = GetLastSpinTimestampSafe();
        if (lastSpinUtc == DateTime.MinValue)
            return true;

        return DateTime.UtcNow >= lastSpinUtc + DailyAvailabilityTimeUtility.CooldownInterval;
    }

    private bool HasPendingFreeSpins()
    {
        return wheelData.pendingFreeSpins > 0;
    }

    private bool ShouldConsumePendingFreeSpin()
    {
        return !HasDailySpinAvailable() && HasPendingFreeSpins();
    }

    public DateTime GetNextDailySpinAvailabilityUtc()
    {
        DateTime lastSpinUtc = GetLastSpinTimestampSafe();
        if (lastSpinUtc == DateTime.MinValue)
            return DateTime.UtcNow;

        return lastSpinUtc + DailyAvailabilityTimeUtility.CooldownInterval;
    }

    private WheelReward GetCalculatedReward()
    {
        var rewards = WheelRewards;
        if (rewards == null || rewards.Length == 0)
        {
            Debug.LogError("[DailyWheel] No rewards defined in the RewardSet!");
            return null;
        }

        float totalWeight = 0;
        foreach (var r in rewards) totalWeight += r.weight;

        if (totalWeight <= 0)
        {
            Debug.LogWarning("[DailyWheel] Total weight is 0. Returning a random reward with equal probability.");
            return rewards[UnityEngine.Random.Range(0, rewards.Length)];
        }

        float randomValue = UnityEngine.Random.value * totalWeight;
        float currentWeight = 0f;

        foreach (var reward in rewards)
        {
            currentWeight += reward.weight;
            if (randomValue <= currentWeight)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[DailyWheel] Rolled {randomValue}/{totalWeight}. Selected: {reward.displayName}");
#endif
                return reward;
            }
        }

        return rewards[rewards.Length - 1];
    }

    private void UpdateSpinProgress(bool consumePendingFreeSpin)
    {
        if (consumePendingFreeSpin)
        {
            wheelData.pendingFreeSpins = Mathf.Max(0, wheelData.pendingFreeSpins - 1);
            wheelData.totalSpins++;
            return;
        }

        DateTime nowUtc = DateTime.UtcNow;
        DateTime lastSpinUtc = GetLastSpinTimestampSafe();

        if (lastSpinUtc != DateTime.MinValue && nowUtc - lastSpinUtc < DailyAvailabilityTimeUtility.MissedWindowThreshold)
            wheelData.consecutiveSpins++;
        else
            wheelData.consecutiveSpins = 1;

        wheelData.lastSpinTimestampUtc = nowUtc.ToString("o");
        wheelData.lastSpinDate = nowUtc.ToString("yyyy-MM-dd");
        wheelData.totalSpins++;
    }

    private void CheckWheelAvailability()
    {
        bool isAvailable = CanSpinToday();
        GameEvents.RaiseWheelAvailabilityChanged(isAvailable);
    }

    private void BootstrapFromSave()
    {
        LoadWheelData();
        IsBootstrapped = true;

        if (!_bootstrapSignalEmitted)
        {
            _bootstrapSignalEmitted = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[DailyWheelSystem] Bootstrap -> Completed");
#endif
            OnBootstrapped?.Invoke();
        }

        CheckWheelAvailability();
    }

    private void LoadWheelData()
    {
        if (SaveManager.Instance == null) return;
        var saved = SaveManager.Instance.GetGameData().dailyWheelData;
        wheelData.lastSpinDate      = saved.lastSpinDateIso;
        wheelData.lastSpinTimestampUtc = saved.lastSpinTimestampUtc;
        wheelData.consecutiveSpins  = saved.consecutiveSpins;
        wheelData.totalSpins        = saved.totalSpins;
        wheelData.pendingFreeSpins  = saved.pendingFreeSpins;
        wheelData.lastAutoShowDate  = saved.lastAutoShowDateIso;
        wheelData.lastAutoShowTimestampUtc = saved.lastAutoShowTimestampUtc;

        MigrateLegacyTimestamps();
    }

    private void SaveWheelData()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Modify(d =>
        {
            d.dailyWheelData.lastSpinDateIso  = wheelData.lastSpinDate;
            d.dailyWheelData.lastSpinTimestampUtc = wheelData.lastSpinTimestampUtc;
            d.dailyWheelData.consecutiveSpins = wheelData.consecutiveSpins;
            d.dailyWheelData.totalSpins       = wheelData.totalSpins;
            d.dailyWheelData.pendingFreeSpins = wheelData.pendingFreeSpins;
            d.dailyWheelData.lastAutoShowDateIso = wheelData.lastAutoShowDate;
            d.dailyWheelData.lastAutoShowTimestampUtc = wheelData.lastAutoShowTimestampUtc;
        });
    }

    private DateTime GetLastSpinTimestampSafe()
    {
        if (DailyAvailabilityTimeUtility.TryParseIsoUtc(wheelData.lastSpinTimestampUtc, out DateTime exactUtc))
            return exactUtc;

        if (DailyAvailabilityTimeUtility.TryParseIsoUtc(wheelData.lastSpinDate, out DateTime legacyIsoUtc))
            return legacyIsoUtc;

        if (DailyAvailabilityTimeUtility.TryParseUtcDate(wheelData.lastSpinDate, out DateTime legacyDateUtc))
            return legacyDateUtc;

        return DateTime.MinValue;
    }

    private DateTime GetLastAutoShowDateSafe()
    {
        if (string.IsNullOrEmpty(wheelData.lastAutoShowDate)) return DateTime.MinValue;
        return DateTime.TryParse(wheelData.lastAutoShowDate, out DateTime result) ? result.Date : DateTime.MinValue;
    }

    private void MigrateLegacyTimestamps()
    {
        DateTime lastSpinUtc = GetLastSpinTimestampSafe();
        if (lastSpinUtc > DateTime.MinValue)
        {
            if (string.IsNullOrEmpty(wheelData.lastSpinTimestampUtc))
                wheelData.lastSpinTimestampUtc = lastSpinUtc.ToString("o");

            if (string.IsNullOrEmpty(wheelData.lastSpinDate))
                wheelData.lastSpinDate = lastSpinUtc.ToString("yyyy-MM-dd");
        }

        if (string.IsNullOrEmpty(wheelData.lastAutoShowTimestampUtc) && GetLastAutoShowDateSafe() > DateTime.MinValue)
        {
            wheelData.lastAutoShowTimestampUtc = GetLastAutoShowDateSafe().ToUniversalTime().ToString("o");
        }
    }


    private void ApplyReward(WheelReward reward)
    {
        if (reward == null)
            return;

        switch (reward.rewardType)
        {
            case WheelRewardType.FreeSpin:
                AddFreeSpins(reward.quantity);
                break;

            case WheelRewardType.PowerUp:
            default:
                AddPowerUpToInventory(reward);
                break;
        }
    }

    private void AddFreeSpins(int quantity)
    {
        int spinsToAdd = Mathf.Max(0, quantity);
        if (spinsToAdd <= 0)
            return;

        wheelData.pendingFreeSpins += spinsToAdd;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[DailyWheel] Granted {spinsToAdd} free spin(s). Pending={wheelData.pendingFreeSpins}");
#endif
    }

    private void AddPowerUpToInventory(WheelReward reward)
    {
        if (reward.quantity <= 0)
            return;

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpObtained(reward.powerUpType, reward.quantity);
        }
        else if (SaveManager.Instance != null)
        {
            var gameData = SaveManager.Instance.GetGameData();
            var item = gameData.powerUpInventory.Find(i => i.type == reward.powerUpType);
            if (item != null) item.quantity += reward.quantity;
            else gameData.powerUpInventory.Add(new PowerUpInventoryItem(reward.powerUpType, reward.quantity));
            SaveManager.Instance.SaveData();
        }
    }
}

[Serializable]
public class WheelData
{
    public string lastSpinDate;
    public string lastSpinTimestampUtc;
    public string lastAutoShowDate;
    public string lastAutoShowTimestampUtc;
    public int consecutiveSpins;
    public int totalSpins;
    public int pendingFreeSpins;
}
