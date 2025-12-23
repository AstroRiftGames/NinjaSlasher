using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PowerUpType
{
    ExtraTime,
    DashTurbo,
    ParryPerfect,
    ComboMaster,
    SecondChance,
    HawkVision,
    EnhancedParry
}

[Serializable]
public class PowerUpDebugInfo
{
    public PowerUpType type;
    public int quantity;
    public string timeRemaining;
}

public class PowerUpManager : MonoBehaviourSingleton<PowerUpManager>
{
    [SerializeField] private Image _puIconActive;
    public List<PowerUpBase> activePowerUps = new List<PowerUpBase>();
    public PowerUpContext context = new PowerUpContext();
    [HideInInspector] public float puTimeLeft;

    [Header("REFERENCES")]
    public PowerUpExtraTime powerUpExtraTime;
    public PowerUpDashTurbo powerUpDashTurbo;
    public PowerUpParryPerfect powerUpParryPerfect;
    public PowerUpComboMaster powerUpComboMaster;
    public PowerUpSecondChance powerUpSecondChance;
    public PowerUpHawkVision powerUpTrajectoryGuide;
    public PowerUpEnhancedParry powerUpEnhancedParry;

    private List<(PowerUpBase, float)> _timers = new List<(PowerUpBase, float)>();

    [Header("DEBUG")]
    [SerializeField] private List<PowerUpDebugInfo> availablePowerUps = new List<PowerUpDebugInfo>();

    public static event Action<PowerUpType> OnPowerUpActivated;
    public static event Action<PowerUpType> OnPowerUpDeactivated;
    public static event Action<PowerUpType, float> OnPowerUpTimeUpdated;

    public static event Action<string> OnPowerUpRemainingTextChanged;

    void Start()
    {
        LoadActivePowerUpsFromGameData();

        foreach (var pu in activePowerUps)
        {
            pu.Activate(context);
            _timers.Add((pu, pu.duration));
        }
    }

    private void Update()
    {
        UpdatePowerUpTimers();

#if UNITY_EDITOR
        if (Application.isPlaying)
            UpdateDebugInfo();
#endif
    }

    void UpdatePowerUpTimers()
    {
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            var (pu, timeLeft) = _timers[i];
            timeLeft -= Time.deltaTime;
            puTimeLeft = timeLeft;

            PowerUpType type = GetPowerUpType(pu);

            if (timeLeft <= 0)
            {
                UpdateContextRemainingTime(type, 0f);

                OnPowerUpRemainingTextChanged?.Invoke("");

                DeactivatePowerUpInternal(pu);
                _timers.RemoveAt(i);
            }
            else
            {
                _timers[i] = (pu, timeLeft);

                UpdateContextRemainingTime(type, timeLeft);

                if (Mathf.FloorToInt(timeLeft) != Mathf.FloorToInt(timeLeft + Time.deltaTime))
                {
                    OnPowerUpTimeUpdated?.Invoke(type, timeLeft);
                }

                OnPowerUpRemainingTextChanged?.Invoke(FormatRemainingTime(timeLeft));
            }
        }
    }

    private string FormatRemainingTime(float time)
    {
        if (time <= 0f) return "";

        int totalSeconds = Mathf.CeilToInt(time);

        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        return $"{minutes:D2}:{seconds:D2}";
    }

    private void UpdateContextActiveState(PowerUpType type, bool isActive)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime:
                context.ExtraTimeActive = isActive;
                break;
            case PowerUpType.DashTurbo:
                context.DashTurboActive = isActive;
                break;
            case PowerUpType.ParryPerfect:
                context.ParryPerfectActive = isActive;
                break;
            case PowerUpType.ComboMaster:
                context.ComboMasterActive = isActive;
                break;
            case PowerUpType.SecondChance:
                context.SecondChanceActive = isActive;
                break;
            case PowerUpType.HawkVision:
                context.TrajectoryGuideActive = isActive;
                break;
            case PowerUpType.EnhancedParry:
                context.EnhancedParryActive = isActive;
                break;
        }
    }

    private void UpdateContextRemainingTime(PowerUpType type, float timeLeft)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime:
                context.ExtraTimeRemaining = timeLeft;
                break;
            case PowerUpType.DashTurbo:
                context.DashTurboRemaining = timeLeft;
                break;
            case PowerUpType.ParryPerfect:
                context.ParryPerfectRemaining = timeLeft;
                break;
            case PowerUpType.ComboMaster:
                context.ComboMasterRemaining = timeLeft;
                break;
            case PowerUpType.SecondChance:
                context.SecondChanceRemaining = timeLeft;
                break;
            case PowerUpType.HawkVision:
                context.TrajectoryGuideRemaining = timeLeft;
                break;
            case PowerUpType.EnhancedParry:
                context.EnhancedParryRemaining = timeLeft;
                break;
        }
    }

    public void ActivatePowerUp(PowerUpBase powerUp)
    {
        if (!activePowerUps.Contains(powerUp))
            activePowerUps.Add(powerUp);
        powerUp.Activate(context);
        _timers.Add((powerUp, powerUp.duration));
        _puIconActive.enabled = true;
        _puIconActive.sprite = powerUp.icon;

        OnPowerUpRemainingTextChanged?.Invoke(FormatRemainingTime(powerUp.duration));
    }

    public bool ActivatePowerUpFromInventory(PowerUpType powerUpType, float duration = 1800f)
    {
        var gameData = SaveManager.Instance.GetGameData();
        var inventoryItem = gameData.powerUpInventory.Find(item => item.type == powerUpType);

        if (inventoryItem == null || inventoryItem.quantity <= 0)
        {
            return false;
        }

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpActivated(powerUpType, duration);
        }
        else
        {
            ActivatePowerUpDirect(powerUpType, duration);
        }

        PowerUpBase powerUpToActivate = GetPowerUpReference(powerUpType);
        if (powerUpToActivate != null)
        {
            ActivatePowerUpInternal(powerUpToActivate, duration);
        }

        return true;
    }

    void LoadActivePowerUpsFromGameData()
    {
        var gameData = SaveManager.Instance.GetGameData();
        var currentTime = DateTime.Now;

        foreach (var powerUpData in gameData.activePowerUps)
        {
            var timeElapsed = (currentTime - powerUpData.activationTime).TotalSeconds;
            var timeRemaining = powerUpData.duration - timeElapsed;

            if (timeRemaining > 0)
            {
                PowerUpBase powerUpRef = GetPowerUpReference(powerUpData.type);
                if (powerUpRef != null)
                {
                    ActivatePowerUpInternal(powerUpRef, (float)timeRemaining);
                }
            }
        }

        if (AutoSaveManager.Instance != null)
        {
            SaveManager.Instance.UpdateActivePowerUps();
        }
    }

    PowerUpBase GetPowerUpReference(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime: return powerUpExtraTime;
            case PowerUpType.DashTurbo: return powerUpDashTurbo;
            case PowerUpType.ParryPerfect: return powerUpParryPerfect;
            case PowerUpType.ComboMaster: return powerUpComboMaster;
            case PowerUpType.SecondChance: return powerUpSecondChance;
            case PowerUpType.HawkVision: return powerUpTrajectoryGuide;
            case PowerUpType.EnhancedParry: return powerUpEnhancedParry;

            default: return null;
        }
    }

    PowerUpType GetPowerUpType(PowerUpBase powerUp)
    {
        if (powerUp == powerUpExtraTime) return PowerUpType.ExtraTime;
        if (powerUp == powerUpDashTurbo) return PowerUpType.DashTurbo;
        if (powerUp == powerUpParryPerfect) return PowerUpType.ParryPerfect;
        if (powerUp == powerUpComboMaster) return PowerUpType.ComboMaster;
        if (powerUp == powerUpSecondChance) return PowerUpType.SecondChance;
        if (powerUp == powerUpTrajectoryGuide) return PowerUpType.HawkVision;
        if (powerUp == powerUpEnhancedParry) return PowerUpType.EnhancedParry;

        return PowerUpType.ExtraTime;
    }

    public void DeactivatePowerUpByType(PowerUpType powerUpType)
    {
        PowerUpBase powerUpToDeactivate = GetPowerUpReference(powerUpType);
        if (powerUpToDeactivate != null && activePowerUps.Contains(powerUpToDeactivate))
        {
            DeactivatePowerUpInternal(powerUpToDeactivate);
            _timers.RemoveAll(timer => timer.Item1 == powerUpToDeactivate);
        }
    }

    void DeactivatePowerUpInternal(PowerUpBase powerUp)
    {
        powerUp.Deactivate(context);
        activePowerUps.Remove(powerUp);

        PowerUpType type = GetPowerUpType(powerUp);

        UpdateContextActiveState(type, false);

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpDeactivated(type);
        }
        else
        {
            SaveManager.Instance.DeactivatePowerUp(type);
        }

        OnPowerUpDeactivated?.Invoke(type);

        OnPowerUpRemainingTextChanged?.Invoke("");

        if (activePowerUps.Count == 0 && _puIconActive != null)
        {
            _puIconActive.enabled = false;
        }
    }

    void ActivatePowerUpInternal(PowerUpBase powerUp, float duration = 1800f)
    {
        if (!activePowerUps.Contains(powerUp))
            activePowerUps.Add(powerUp);

        powerUp.Activate(context);

        int timerIndex = _timers.FindIndex(timer => timer.Item1 == powerUp);
        float totalDuration;

        if (timerIndex != -1)
        {
            var (pu, timeLeft) = _timers[timerIndex];
            totalDuration = timeLeft + duration;
            _timers[timerIndex] = (pu, totalDuration);
        }
        else
        {
            totalDuration = duration;
            _timers.Add((powerUp, duration));
        }

        PowerUpType type = GetPowerUpType(powerUp);

        UpdateContextActiveState(type, true);
        UpdateContextRemainingTime(type, totalDuration);
        Debug.Log($"ActivatePowerUpInternal: Type={type}, Duration={totalDuration}, ContextActive={context.ExtraTimeActive}, ContextRemaining={context.ExtraTimeRemaining}");
        OnPowerUpActivated?.Invoke(type);

        if (_puIconActive != null)
        {
            _puIconActive.enabled = true;
            _puIconActive.sprite = powerUp.icon;
        }

        OnPowerUpRemainingTextChanged?.Invoke(FormatRemainingTime(totalDuration));
    }

    void ActivatePowerUpDirect(PowerUpType powerUpType, float duration)
    {
        SaveManager.Instance.RemovePowerUpFromInventory(powerUpType, 1);

        SaveManager.Instance.ActivatePowerUp(powerUpType, duration);
    }

    public bool IsPowerUpActive(PowerUpType type)
    {
        PowerUpBase powerUpRef = GetPowerUpReference(type);
        return powerUpRef != null && activePowerUps.Contains(powerUpRef);
    }

    public float GetRemainingTime(PowerUpType type)
    {
        PowerUpBase powerUpRef = GetPowerUpReference(type);
        if (powerUpRef == null) return 0f;

        var timer = _timers.Find(t => t.Item1 == powerUpRef);
        return timer.Item1 != null ? timer.Item2 : 0f;
    }

    public int GetInventoryCount(PowerUpType type)
    {
        var gameData = SaveManager.Instance.GetGameData();
        var inventoryItem = gameData.powerUpInventory.Find(item => item.type == type);
        return inventoryItem?.quantity ?? 0;
    }

    void UpdateDebugInfo()
    {
        availablePowerUps.Clear();
        var gameData = SaveManager.Instance.GetGameData();
        var currentTime = DateTime.Now;

        var powerUpCounts = new Dictionary<PowerUpType, List<PowerUpData>>();

        foreach (var powerUp in gameData.activePowerUps)
        {
            var timeElapsed = (currentTime - powerUp.activationTime).TotalSeconds;
            if (timeElapsed < powerUp.duration)
            {
                if (!powerUpCounts.ContainsKey(powerUp.type))
                    powerUpCounts[powerUp.type] = new List<PowerUpData>();

                powerUpCounts[powerUp.type].Add(powerUp);
            }
        }

        foreach (var kvp in powerUpCounts)
        {
            var nextToExpire = kvp.Value[0];
            foreach (var pu in kvp.Value)
            {
                if (pu.activationTime < nextToExpire.activationTime)
                    nextToExpire = pu;
            }

            var timeElapsed = (currentTime - nextToExpire.activationTime).TotalSeconds;
            var timeRemaining = nextToExpire.duration - timeElapsed;

            var hours = (int)(timeRemaining / 3600);
            var minutes = (int)((timeRemaining % 3600) / 60);

            availablePowerUps.Add(new PowerUpDebugInfo
            {
                type = kvp.Key,
                quantity = kvp.Value.Count,
                timeRemaining = $"{hours:D2}:{minutes:D2}"
            });
        }
    }

    #region DEBUG_CONTEXT_MENU

    [ContextMenu("Add 5 Units of All Power-Ups")]
    private void AddAllPowerUps()
    {
        AddExtraTime();
        AddDashTurbo();
        AddParryPerfect();
        AddComboMaster();
        AddSecondChance();
        AddHawkVision();
        AddRicochetParry();
    }

    [ContextMenu("Add 5x Enhanced Parry")]
    private void AddRicochetParry()
    {
        AddPowerUpToInventory(PowerUpType.EnhancedParry, 5);
    }

    [ContextMenu("Add 5x Extra Time")]
    private void AddExtraTime()
    {
        AddPowerUpToInventory(PowerUpType.ExtraTime, 5);
    }

    [ContextMenu("Add 5x Dash Turbo")]
    private void AddDashTurbo()
    {
        AddPowerUpToInventory(PowerUpType.DashTurbo, 5);
    }

    [ContextMenu("Add 5x Parry Perfect")]
    private void AddParryPerfect()
    {
        AddPowerUpToInventory(PowerUpType.ParryPerfect, 5);
    }

    [ContextMenu("Add 5x Combo Master")]
    private void AddComboMaster()
    {
        AddPowerUpToInventory(PowerUpType.ComboMaster, 5);
    }

    [ContextMenu("Add 5x Second Chance")]
    private void AddSecondChance()
    {
        AddPowerUpToInventory(PowerUpType.SecondChance, 5);
    }

    [ContextMenu("Add 5x Hawk Vision")]
    private void AddHawkVision()
    {
        AddPowerUpToInventory(PowerUpType.HawkVision, 5);
    }

    [ContextMenu("Clear All Power-Ups")]
    private void ClearAllPowerUps()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        var gameData = SaveManager.Instance.GetGameData();
        gameData.powerUpInventory.Clear();
        SaveManager.Instance.SaveData();
    }

    private void AddPowerUpToInventory(PowerUpType powerUpType, int quantity)
    {
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpObtained(powerUpType, quantity);
        }
        else if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AddPowerUpToInventory(powerUpType, quantity);
        }
    }

    #endregion
}
