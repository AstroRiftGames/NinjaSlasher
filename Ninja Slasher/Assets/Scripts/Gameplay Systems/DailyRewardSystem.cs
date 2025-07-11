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
        SaveManager.Instance.SaveDailyRewardData(jsonData);
    }

    void CheckDailyReward()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        bool wasAvailable = CanClaimToday();

        if (string.IsNullOrEmpty(rewardData.lastClaimDate))
        {
            // Primer dia - no hacer nada
        }
        else
        {
            DateTime lastClaim = DateTime.Parse(rewardData.lastClaimDate);
            DateTime currentDate = DateTime.Now.Date;

            int daysDifference = (int)(currentDate - lastClaim.Date).TotalDays;

            if (daysDifference == 0)
            {
                // Ya se reclamo hoy - no hacer nada
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
        rewardData.currentWeekDay = 0;
        rewardData.consecutiveDays = 0;
        rewardData.claimedDays = new bool[7];

        OnConsecutiveDaysUpdated?.Invoke(rewardData.consecutiveDays);
    }

    public bool ClaimReward()
    {
        if (!CanClaimToday())
        {
            Debug.Log("No se puede reclamar la recompensa hoy");
            return false;
        }

        if (rewardData.claimedDays[rewardData.currentWeekDay])
        {
            Debug.Log("Recompensa ya reclamada");
            return false;
        }

        rewardData.claimedDays[rewardData.currentWeekDay] = true;
        rewardData.lastClaimDate = DateTime.Now.ToString("yyyy-MM-dd");

        DailyReward claimedReward = weeklyRewards[rewardData.currentWeekDay];

        AddPowerUpToInventory(claimedReward);

        SaveRewardData();

        OnRewardClaimed?.Invoke(claimedReward);
        OnRewardAvailabilityChanged?.Invoke(false);

        Debug.Log($"Recompensa reclamada {claimedReward.displayName} x{claimedReward.quantity}");
        return true;
    }

    void AddPowerUpToInventory(DailyReward reward)
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
            return "Disponible ahora";

        DateTime lastClaim = DateTime.Parse(rewardData.lastClaimDate);
        DateTime nextAvailable = lastClaim.AddDays(1);
        TimeSpan timeUntilNext = nextAvailable - DateTime.Now;

        if (timeUntilNext.TotalSeconds <= 0)
            return "Disponible ahora";

        return $"{timeUntilNext.Hours:D2}:{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
    }

    // METODOS PARA TESTING
    [ContextMenu("Reset Reward Data")]
    public void ResetRewardData()
    {
        SaveManager.Instance.SaveDailyRewardData("");
        LoadRewardData();
        CheckDailyReward();
    }

    [ContextMenu("Simular Día Siguiente")]
    public void SimulateNextDay()
    {
        DateTime yesterday = DateTime.Now.AddDays(-1);
        rewardData.lastClaimDate = yesterday.ToString("yyyy-MM-dd");
        SaveRewardData();
        CheckDailyReward();
    }
}