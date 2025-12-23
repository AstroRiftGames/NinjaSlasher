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
    public int currentWeekDay;
    public int consecutiveDays;

    public DailyRewardSaveData()
    {
        claimedDays = new bool[7];
        lastClaimDate = "";
        currentWeekDay = 0;
        consecutiveDays = 0;
    }
}

public class DailyRewardSystem : MonoBehaviourSingleton<DailyRewardSystem>
{
    [Header("SETTINGS")]
    public DailyReward[] weeklyRewards = new DailyReward[7];

    private DailyRewardSaveData rewardData;

    public static event Action<DailyReward> OnRewardClaimed;
    public static event Action<int> OnConsecutiveDaysUpdated;
    public static event Action<bool> OnRewardAvailabilityChanged;
    public static event Action OnRewardDoubled;

    private bool _hasDoubledToday = false;


    public override void Awake()
    {
        base.Awake();
    }

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
        CheckDailyReward();
        CheckDoubleRewardStatus();
        OnRewardAvailabilityChanged?.Invoke(CanClaimToday());
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
                //Debug.LogError("Error cargando datos de recompensas diarias: " + e.Message);
                rewardData = new DailyRewardSaveData();
            }
        }

        if (string.IsNullOrEmpty(rewardData.lastClaimDate))
        {
            var last = GetLastClaimDateSafe();
            if (last > DateTime.MinValue)
                rewardData.lastClaimDate = last.ToString("yyyy-MM-dd");
        }
    }

    void SaveRewardData()
    {
        string jsonData = JsonUtility.ToJson(rewardData);

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnDailyRewardClaimed(jsonData);
        }
        else
        {
            SaveManager.Instance.SaveDailyRewardData(jsonData);
            //Debug.LogWarning("[DailyRewardSystem] AutoSaveManager no encontrado");
        }
    }

    void CheckDailyReward()
    {
        bool wasAvailable = CanClaimToday();
        var last = GetLastClaimDateSafe();
        var currentDate = DateTime.Now.Date;

        if (last == DateTime.MinValue.Date)
        {
            Debug.Log("[DailyRewardSystem] Primer dia de recompensas");
        }
        else
        {
            int daysDifference = (int)(currentDate - last).TotalDays;

            if (daysDifference == 0)
            {
                Debug.Log("[DailyRewardSystem] Recompensa ya reclamada hoy");
            }
            else if (daysDifference == 1)
            {
                AdvanceDay();
            }
            else if (daysDifference > 1)
            {
                ResetWeeklyProgress();
            }
        }

        bool isAvailableNow = CanClaimToday();
        if (wasAvailable != isAvailableNow)
            OnRewardAvailabilityChanged?.Invoke(isAvailableNow);
    }

    private void CheckDoubleRewardStatus()
    {
        var last = GetLastClaimDateSafe();
        var currentDate = DateTime.Now.Date;

        if (last != currentDate)
        {
            _hasDoubledToday = false;
        }
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
        rewardData.lastClaimDate = DateTime.Now.ToString("yyyy-MM-dd");

        _hasDoubledToday = true;

        SaveRewardData();

        //Debug.Log($"Recompensa diaria duplicada: {doubledReward.powerUpType} x{doubledReward.quantity}");

        OnRewardClaimed?.Invoke(doubledReward);
        OnRewardAvailabilityChanged?.Invoke(false);
        OnRewardDoubled?.Invoke();
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

    void AdvanceDay()
    {
        rewardData.currentWeekDay = (rewardData.currentWeekDay + 1) % 7;
        rewardData.consecutiveDays++;

        if (rewardData.currentWeekDay == 0)
        {
            rewardData.claimedDays = new bool[7];
        }

        OnConsecutiveDaysUpdated?.Invoke(rewardData.consecutiveDays);
    }

    void ResetWeeklyProgress()
    {
        int previousDays = rewardData.consecutiveDays;
        rewardData.currentWeekDay = 0;
        rewardData.consecutiveDays = 0;
        rewardData.claimedDays = new bool[7];

        OnConsecutiveDaysUpdated?.Invoke(rewardData.consecutiveDays);
    }

    public bool ClaimReward()
    {
        if (!CanClaimToday()) return false;

        if (rewardData.claimedDays[rewardData.currentWeekDay])
        {
            return false;
        }
        AudioManager.Instance.PlaySFX(SFXClip.UI_Claim);

        rewardData.claimedDays[rewardData.currentWeekDay] = true;
        rewardData.lastClaimDate = DateTime.Now.ToString("yyyy-MM-dd");

        var claimed = weeklyRewards[rewardData.currentWeekDay];

        AddPowerUpToInventoryViaAutoSave(claimed);

        SaveRewardData();

        OnRewardClaimed?.Invoke(claimed);
        OnRewardAvailabilityChanged?.Invoke(false);
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
            item.lastUpdated = DateTime.Now;
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
        var last = GetLastClaimDateSafe();
        if (last == DateTime.MinValue.Date) return true;

        return DateTime.Now.Date > last;
    }

    public DailyReward GetTodayReward()
    {
        return weeklyRewards[rewardData.currentWeekDay];
    }

    public int GetConsecutiveDays()
    {
        return rewardData.consecutiveDays;
    }

    public bool[] GetWeeklyProgress()
    {
        return rewardData.claimedDays;
    }

    public int GetCurrentWeekDay()
    {
        return rewardData.currentWeekDay;
    }

    public string GetTimeUntilNextReward()
    {
        var last = GetLastClaimDateSafe();
        if (last == DateTime.MinValue.Date)
            return "AVAILABLE NOW";

        DateTime nextAvailable = last.AddDays(1);
        TimeSpan timeUntilNext = nextAvailable - DateTime.Now;

        if (timeUntilNext.TotalSeconds <= 0)
            return "AVAILABLE NOW";

        return $"{timeUntilNext.Hours:D2}:{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
    }

    private DateTime GetLastClaimDateSafe()
    {
        if (!string.IsNullOrEmpty(rewardData?.lastClaimDate) && TryParseYMD(rewardData.lastClaimDate, out var ymd))
            return ymd.Date;

        var gd = SaveManager.Instance.GetGameData();
        if (!string.IsNullOrEmpty(gd.lastRewardTimestamp) && TryParseISO(gd.lastRewardTimestamp, out var iso))
            return iso.Date;

        return DateTime.MinValue.Date;
    }

    private static bool TryParseYMD(string ymd, out DateTime date)
    {
        return DateTime.TryParseExact(ymd, "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None, out date);
    }

    private static bool TryParseISO(string iso, out DateTime date)
    {
        return DateTime.TryParse(iso, null,
            DateTimeStyles.RoundtripKind, out date);
    }

}