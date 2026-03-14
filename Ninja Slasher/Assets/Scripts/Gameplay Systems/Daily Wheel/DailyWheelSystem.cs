using System;
using System.Linq;
using UnityEngine;

public class DailyWheelSystem : MonoBehaviourSingleton<DailyWheelSystem>
{
    [Header("DEBUG")]
    [SerializeField] private bool debugInfiniteSpins = false;

    [Header("WHEEL CONFIGURATION")]
    [SerializeField] private WheelReward[] wheelRewards;

    private WheelData wheelData = new WheelData();

    public static event Action<WheelReward> OnRewardSpun;
    public static event Action<bool> OnWheelAvailabilityChanged;
    public static event Action<int> OnConsecutiveSpinsUpdated;

    public WheelReward[] WheelRewards => wheelRewards;

    public override void Awake()
    {
        base.Awake();
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
        LoadWheelData();
        CheckWheelAvailability();
    }

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded)
        {
            LoadWheelData();
            CheckWheelAvailability();
        }
    }

    public bool SpinWheel(out WheelReward reward)
    {
        reward = null;

        if (!CanSpinToday())
        {
            return false;
        }

        reward = GetCalculatedReward();

        if (reward == null)
        {
            return false;
        }

        AddPowerUpToInventory(reward);

        if (!debugInfiniteSpins)
        {
            UpdateSpinProgress();
            SaveWheelData();
        }

        OnRewardSpun?.Invoke(reward);

        CheckWheelAvailability();
        OnConsecutiveSpinsUpdated?.Invoke(wheelData.consecutiveSpins);

        return true;
    }

    public bool CanSpinToday()
    {
        if (debugInfiniteSpins) return true;

        DateTime lastSpin = GetLastSpinDateSafe();
        DateTime currentDate = DateTime.UtcNow.Date;

        return lastSpin < currentDate;
    }

    private WheelReward GetCalculatedReward()
    {
        if (wheelRewards == null || wheelRewards.Length == 0) return null;

        float totalWeight = wheelRewards.Sum(r => r.weight);
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var reward in wheelRewards)
        {
            currentWeight += reward.weight;
            if (randomValue <= currentWeight)
                return reward;
        }
        return wheelRewards.Last();
    }

    private void UpdateSpinProgress()
    {
        DateTime lastSpin = GetLastSpinDateSafe();
        DateTime currentDate = DateTime.UtcNow.Date;

        if (lastSpin == currentDate.AddDays(-1))
            wheelData.consecutiveSpins++;
        else
            wheelData.consecutiveSpins = 1;

        wheelData.lastSpinDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        wheelData.totalSpins++;
    }

    private void CheckWheelAvailability()
    {
        bool isAvailable = CanSpinToday();
        OnWheelAvailabilityChanged?.Invoke(isAvailable);
    }

    private void LoadWheelData()
    {
        if (SaveManager.Instance == null) return;
        var saved = SaveManager.Instance.GetGameData().dailyWheelData;
        wheelData.lastSpinDate      = saved.lastSpinDateIso;
        wheelData.consecutiveSpins  = saved.consecutiveSpins;
        wheelData.totalSpins        = saved.totalSpins;
    }

    private void SaveWheelData()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Modify(d =>
        {
            d.dailyWheelData.lastSpinDateIso  = wheelData.lastSpinDate;
            d.dailyWheelData.consecutiveSpins = wheelData.consecutiveSpins;
            d.dailyWheelData.totalSpins       = wheelData.totalSpins;
        });
    }

    private DateTime GetLastSpinDateSafe()
    {
        if (string.IsNullOrEmpty(wheelData.lastSpinDate)) return DateTime.MinValue;
        return DateTime.TryParse(wheelData.lastSpinDate, out DateTime result) ? result.Date : DateTime.MinValue;
    }

    private void AddPowerUpToInventory(WheelReward reward)
    {
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
    public int consecutiveSpins;
    public int totalSpins;
}