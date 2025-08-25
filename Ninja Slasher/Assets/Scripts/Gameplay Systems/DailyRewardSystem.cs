using System;
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

    public override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        LoadRewardData();
        CheckDailyReward();
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
                Debug.LogError("Error cargando datos de recompensas diarias: " + e.Message);
                rewardData = new DailyRewardSaveData();
            }
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
            Debug.LogWarning("[DailyRewardSystem] AutoSaveManager no encontrado");
        }
    }

    void CheckDailyReward()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        bool wasAvailable = CanClaimToday();

        if (string.IsNullOrEmpty(rewardData.lastClaimDate))
        {
            Debug.Log("[DailyRewardSystem] Primer día del sistema de recompensas");
        }
        else
        {
            DateTime lastClaim = DateTime.Parse(rewardData.lastClaimDate);
            DateTime currentDate = DateTime.Now.Date;

            int daysDifference = (int)(currentDate - lastClaim.Date).TotalDays;

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
        {
            OnRewardAvailabilityChanged?.Invoke(isAvailableNow);
        }
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
        if (!CanClaimToday())
        {
            return false;
        }

        if (rewardData.claimedDays[rewardData.currentWeekDay])
        {
            return false;
        }
        AudioManager.Instance.PlaySFX(SFXClip.UI_Claim);
        rewardData.claimedDays[rewardData.currentWeekDay] = true;
        rewardData.lastClaimDate = DateTime.Now.ToString("yyyy-MM-dd");

        DailyReward claimedReward = weeklyRewards[rewardData.currentWeekDay];

        AddPowerUpToInventoryViaAutoSave(claimedReward);

        SaveRewardData();

        OnRewardClaimed?.Invoke(claimedReward);
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
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        return rewardData.lastClaimDate != today;
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
        if (string.IsNullOrEmpty(rewardData.lastClaimDate))
            return "AVAILABLE NOW";

        DateTime lastClaim = DateTime.Parse(rewardData.lastClaimDate);
        DateTime nextAvailable = lastClaim.AddDays(1);
        TimeSpan timeUntilNext = nextAvailable - DateTime.Now;

        if (timeUntilNext.TotalSeconds <= 0)
            return "AVAILABLE NOW";

        return $"{timeUntilNext.Hours:D2}:{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
    }
}