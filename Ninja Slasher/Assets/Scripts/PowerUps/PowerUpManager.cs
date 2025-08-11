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
    SecondChance
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
    public Image _puIconActive;
    public List<PowerUpBase> activePowerUps = new List<PowerUpBase>();
    public PowerUpContext context = new PowerUpContext();
    [HideInInspector] public float _puTimeLeft;

    [Header("PowerUp References")]
    public PowerUpExtraTime powerUpExtraTime;
    public PowerUpDashTurbo powerUpDashTurbo;
    public PowerUpParryPerfect powerUpParryPerfect;
    public PowerUpComboMaster powerUpComboMaster;
    public PowerUpSecondChance powerUpSecondChance;

    public bool useExtraTime;
    public bool useDashTurbo;
    public bool useParryPerfect;
    public bool useComboMaster;
    public bool useSecondChance;

    private bool _wasExtraTime, _wasDashTurbo, _wasParryPerfect, _wasComboMaster, _wasSecondChance;

    private List<(PowerUpBase, float)> _timers = new List<(PowerUpBase, float)>();

    [Header("DEBUG - Power-ups Disponibles")]
    [SerializeField] private List<PowerUpDebugInfo> availablePowerUps = new List<PowerUpDebugInfo>();

    public static event Action<PowerUpType> OnPowerUpActivated;
    public static event Action<PowerUpType> OnPowerUpDeactivated;
    public static event Action<PowerUpType, float> OnPowerUpTimeUpdated;

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
        HandleTestingToggles();

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
            _puTimeLeft = timeLeft;

            if (timeLeft <= 0)
            {
                DeactivatePowerUpInternal(pu);
                _timers.RemoveAt(i);
            }
            else
            {
                _timers[i] = (pu, timeLeft);

                if (Mathf.FloorToInt(timeLeft) != Mathf.FloorToInt(timeLeft + Time.deltaTime))
                {
                    OnPowerUpTimeUpdated?.Invoke(GetPowerUpType(pu), timeLeft);
                }
            }
        }
    }

    void HandleTestingToggles()
    {
        if (useExtraTime != _wasExtraTime)
        {
            if (useExtraTime)
                ActivatePowerUpFromInventory(PowerUpType.ExtraTime);
            else
                DeactivatePowerUpByType(PowerUpType.ExtraTime);
            _wasExtraTime = useExtraTime;
        }

        if (useDashTurbo != _wasDashTurbo)
        {
            if (useDashTurbo)
                ActivatePowerUpFromInventory(PowerUpType.DashTurbo);
            else
                DeactivatePowerUpByType(PowerUpType.DashTurbo);
            _wasDashTurbo = useDashTurbo;
        }

        if (useParryPerfect != _wasParryPerfect)
        {
            if (useParryPerfect)
                ActivatePowerUpFromInventory(PowerUpType.ParryPerfect);
            else
                DeactivatePowerUpByType(PowerUpType.ParryPerfect);
            _wasParryPerfect = useParryPerfect;
        }

        if (useComboMaster != _wasComboMaster)
        {
            if (useComboMaster)
                ActivatePowerUpFromInventory(PowerUpType.ComboMaster);
            else
                DeactivatePowerUpByType(PowerUpType.ComboMaster);
            _wasComboMaster = useComboMaster;
        }

        if (useSecondChance != _wasSecondChance)
        {
            if (useSecondChance)
                ActivatePowerUpFromInventory(PowerUpType.SecondChance);
            else
                DeactivatePowerUpByType(PowerUpType.SecondChance);
            _wasSecondChance = useSecondChance;
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
    }

    public bool ActivatePowerUpFromInventory(PowerUpType powerUpType, float duration = 1800f)
    {
        var gameData = SaveManager.Instance.GetGameData();
        var inventoryItem = gameData.powerUpInventory.Find(item => item.type == powerUpType);

        if (inventoryItem == null || inventoryItem.quantity <= 0)
        {
            Debug.LogWarning($"[PowerUpManager] No hay {powerUpType} disponibles en el inventario");
            return false;
        }

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpActivated(powerUpType, duration);
        }
        else
        {
            ActivatePowerUpDirect(powerUpType, duration);
            Debug.LogWarning("[PowerUpManager] AutoSaveManager no encontrado, activando directamente");
        }

        PowerUpBase powerUpToActivate = GetPowerUpReference(powerUpType);
        if (powerUpToActivate != null)
        {
            ActivatePowerUpInternal(powerUpToActivate, duration);
        }

        Debug.Log($"[PowerUpManager] Power-up {powerUpType} activado por {duration} segundos");
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

                    SyncTestingToggle(powerUpData.type, true);
                }

                Debug.Log($"[PowerUpManager] Cargado {powerUpData.type} con {timeRemaining:F0} segundos restantes");
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
        return PowerUpType.ExtraTime; // Default
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

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpDeactivated(type);
        }
        else
        {
            SaveManager.Instance.DeactivatePowerUp(type);
            Debug.LogWarning("[PowerUpManager] AutoSaveManager no encontrado, desactivando directamente en SaveManager");
        }

        SyncTestingToggle(type, false);

        OnPowerUpDeactivated?.Invoke(type);
        _puIconActive.enabled = false;
        Debug.Log($"[PowerUpManager] {type} desactivado (tiempo expirado)");
    }

    void ActivatePowerUpInternal(PowerUpBase powerUp, float duration = 1800f)
    {
        if (!activePowerUps.Contains(powerUp))
            activePowerUps.Add(powerUp);

        powerUp.Activate(context);

        _timers.RemoveAll(timer => timer.Item1 == powerUp);
        _timers.Add((powerUp, duration));

        PowerUpType type = GetPowerUpType(powerUp);
        OnPowerUpActivated?.Invoke(type);

        Debug.Log($"[PowerUpManager] {type} activado internamente");
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

    void SyncTestingToggle(PowerUpType type, bool active)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime: useExtraTime = _wasExtraTime = active; break;
            case PowerUpType.DashTurbo: useDashTurbo = _wasDashTurbo = active; break;
            case PowerUpType.ParryPerfect: useParryPerfect = _wasParryPerfect = active; break;
            case PowerUpType.ComboMaster: useComboMaster = _wasComboMaster = active; break;
            case PowerUpType.SecondChance: useSecondChance = _wasSecondChance = active; break;
        }
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

    public void DebugPrintPowerUpInventory()
    {
        var inventory = SaveManager.Instance.GetGameData().powerUpInventory;
        foreach (var item in inventory)
        {
            Debug.Log($"PowerUp: {item.type} | Cantidad: {item.quantity} | Última vez: {item.lastUpdated}");
        }
    }
}
