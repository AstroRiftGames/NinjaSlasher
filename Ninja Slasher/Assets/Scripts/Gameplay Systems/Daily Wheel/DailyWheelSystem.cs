using System;
using System.Linq;
using UnityEngine;

public class DailyWheelSystem : MonoBehaviourSingleton<DailyWheelSystem>
{
    [Header("WHEEL CONFIGURATION")]
    [SerializeField] private WheelReward[] wheelRewards;

    private WheelData wheelData;
    //private const string WHEEL_DATA_KEY = "DailyWheelData";

    public static event Action<WheelReward> OnRewardSpun;
    public static event Action<bool> OnWheelAvailabilityChanged;
    public static event Action<int> OnConsecutiveSpinsUpdated;

    public WheelReward[] WheelRewards => wheelRewards;

    public override void Awake()
    {
        base.Awake();
        //LoadWheelData();
    }

    //private void Start()
    //{
    //    CheckWheelAvailability();
    //}

    //void LoadWheelData()
    //{
    //    string json = PlayerPrefs.GetString(WHEEL_DATA_KEY, string.Empty);

    //    if (!string.IsNullOrEmpty(json))
    //    {
    //        try
    //        {
    //            wheelData = JsonUtility.FromJson<WheelData>(json);
    //        }
    //        catch
    //        {
    //            wheelData = new WheelData();
    //        }
    //    }
    //    else
    //    {
    //        wheelData = new WheelData();
    //    }
    //}

    //void SaveWheelData()
    //{
    //    string json = JsonUtility.ToJson(wheelData);
    //    PlayerPrefs.SetString(WHEEL_DATA_KEY, json);
    //    PlayerPrefs.Save();
    //}

    //public bool CanSpinToday()
    //{
    //    DateTime lastSpin = GetLastSpinDateSafe();
    //    DateTime currentDate = DateTime.Now.Date;

    //    return lastSpin < currentDate;
    //}

    //DateTime GetLastSpinDateSafe()
    //{
    //    if (string.IsNullOrEmpty(wheelData.lastSpinDate))
    //        return DateTime.MinValue;

    //    if (DateTime.TryParse(wheelData.lastSpinDate, out DateTime result))
    //        return result.Date;

    //    return DateTime.MinValue;
    //}

    //void CheckWheelAvailability()
    //{
    //    bool isAvailableNow = CanSpinToday();
    //    OnWheelAvailabilityChanged?.Invoke(isAvailableNow);
    //}

    public WheelReward GetRandomReward()
    {
        if (wheelRewards == null || wheelRewards.Length == 0)
        {
            Debug.LogError("No hay recompensas configuradas en la ruleta");
            return null;
        }

        float totalWeight = wheelRewards.Sum(r => r.weight);
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var reward in wheelRewards)
        {
            currentWeight += reward.weight;
            if (randomValue <= currentWeight)
            {
                return reward;
            }
        }

        return wheelRewards[wheelRewards.Length - 1];
    }

    public bool SpinWheel(out WheelReward reward)
    {
        reward = null;

        //if (!CanSpinToday())
        //{
        //    Debug.LogWarning("La ruleta ya fue girada hoy");
        //    return false;
        //}

        reward = GetRandomReward();

        if (reward == null)
        {
            Debug.LogError("No se pudo obtener una recompensa de la ruleta");
            return false;
        }

        AddPowerUpToInventory(reward);

        //DateTime lastSpin = GetLastSpinDateSafe();
        //DateTime currentDate = DateTime.Now.Date;

        //if (lastSpin == currentDate.AddDays(-1))
        //{
        //    wheelData.consecutiveSpins++;
        //}
        //else if (lastSpin < currentDate.AddDays(-1))
        //{
        //    wheelData.consecutiveSpins = 1;
        //}

        //wheelData.lastSpinDate = DateTime.Now.ToString("yyyy-MM-dd");
        //wheelData.totalSpins++;

        //SaveWheelData();

        AudioManager.Instance?.PlaySFX(SFXClip.UI_Claim);

        OnRewardSpun?.Invoke(reward);
        OnWheelAvailabilityChanged?.Invoke(false);
        OnConsecutiveSpinsUpdated?.Invoke(wheelData.consecutiveSpins);

        return true;
    }

    void AddPowerUpToInventory(WheelReward reward)
    {
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpObtained(reward.powerUpType, reward.quantity);
        }
        else
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
    }

    public int GetConsecutiveSpins()
    {
        return wheelData.consecutiveSpins;
    }

    public int GetTotalSpins()
    {
        return wheelData.totalSpins;
    }

    //public DateTime GetLastSpinDate()
    //{
    //    return GetLastSpinDateSafe();
    //}

    //public TimeSpan GetTimeUntilNextSpin()
    //{
    //    if (CanSpinToday())
    //        return TimeSpan.Zero;

    //    DateTime lastSpin = GetLastSpinDateSafe();
    //    DateTime nextAvailable = lastSpin.AddDays(1).Date;

    //    return nextAvailable - DateTime.Now;
    //}
}