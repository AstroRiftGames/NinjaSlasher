using System;
using System.Collections.Generic;
using UnityEngine;

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
public class PowerUpInfo
{
    public PowerUpType type;
    public int quantity;
    public int usesRemaining;
}

[DefaultExecutionOrder(-100)]
public class PowerUpManager : MonoBehaviourSingleton<PowerUpManager>
{
    public List<PowerUpBase> activePowerUps = new List<PowerUpBase>();
    public PowerUpContext context = new PowerUpContext();

    [Header("REFERENCES")]
    public PowerUpExtraTime powerUpExtraTime;
    public PowerUpDashTurbo powerUpDashTurbo;
    public PowerUpParryPerfect powerUpParryPerfect;
    public PowerUpComboMaster powerUpComboMaster;
    public PowerUpSecondChance powerUpSecondChance;
    public PowerUpHawkVision powerUpTrajectoryGuide;
    public PowerUpEnhancedParry powerUpEnhancedParry;

    private bool _isInLevel;

    [Header("DEBUG")]
    [SerializeField] private List<PowerUpInfo> availablePowerUps = new List<PowerUpInfo>();

    public override void Awake()
    {
        base.Awake();
    }

    void OnEnable()
    {
        GameEvents.OnLevelEndedConsumePowerUps += OnLevelEndedConsumePowerUps;
        GameEvents.OnLevelStarted += OnLevelStarted;
        GameEvents.OnLevelSessionClosed += OnLevelSessionClosed;
    }

    void OnDisable()
    {
        GameEvents.OnLevelEndedConsumePowerUps -= OnLevelEndedConsumePowerUps;
        GameEvents.OnLevelStarted -= OnLevelStarted;
        GameEvents.OnLevelSessionClosed -= OnLevelSessionClosed;
    }

    private void OnLevelStarted()
    {
        _isInLevel = true;
    }

    private void OnLevelSessionClosed()
    {
        if (!_isInLevel)
            return;

        ClearRuntimePowerUpsForAttempt();
        _isInLevel = false;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            UpdateDebugInfo();
#endif
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
                context.HawkVisionActive = isActive;
                break;
            case PowerUpType.EnhancedParry:
                context.EnhancedParryActive = isActive;
                break;
        }
    }

    public void ActivatePowerUp(PowerUpBase powerUp)
    {
        if (!activePowerUps.Contains(powerUp))
            activePowerUps.Add(powerUp);

        powerUp.Activate(context);

        PowerUpType type = GetPowerUpType(powerUp);
        UpdateContextActiveState(type, true);

        GameEvents.RaisePowerUpActivated(type, 1);
    }

    public bool ActivatePowerUpFromInventory(PowerUpType powerUpType)
    {
        var gameData = SaveManager.Instance.GetGameData();
        var inventoryItem = gameData.powerUpInventory.Find(item => item.type == powerUpType);

        if (inventoryItem == null || inventoryItem.quantity <= 0)
            return false;

        PowerUpBase powerUpToActivate = GetPowerUpReference(powerUpType);
        if (powerUpToActivate != null)
        {
            if (activePowerUps.Contains(powerUpToActivate))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[PowerUpManager] {powerUpToActivate.name} ya está activo.");
#endif
                return false;
            }

            ActivatePowerUpInternal(powerUpToActivate);
        }

        SaveManager.Instance.RemovePowerUpFromInventory(powerUpType, 1);

        return true;
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

    public PowerUpType GetPowerUpType(PowerUpBase powerUp)
    {
        if (powerUp == powerUpExtraTime) return PowerUpType.ExtraTime;
        if (powerUp == powerUpDashTurbo) return PowerUpType.DashTurbo;
        if (powerUp == powerUpParryPerfect) return PowerUpType.ParryPerfect;
        if (powerUp == powerUpComboMaster) return PowerUpType.ComboMaster;
        if (powerUp == powerUpSecondChance) return PowerUpType.SecondChance;
        if (powerUp == powerUpTrajectoryGuide) return PowerUpType.HawkVision;
        if (powerUp == powerUpEnhancedParry) return PowerUpType.EnhancedParry;

        Debug.LogError($"[PowerUpManager] GetPowerUpType: power-up no registrado '{powerUp?.name}'. " +
                       "Verificar que esté asignado en el Inspector y que GetPowerUpType esté actualizado.");
        return PowerUpType.ExtraTime;
    }

    public void DeactivatePowerUpByType(PowerUpType powerUpType)
    {
        PowerUpBase powerUpToDeactivate = GetPowerUpReference(powerUpType);
        if (powerUpToDeactivate != null && activePowerUps.Contains(powerUpToDeactivate))
            DeactivatePowerUpInternal(powerUpToDeactivate);
    }

    void DeactivatePowerUpInternal(PowerUpBase powerUp)
    {
        powerUp.Deactivate(context);
        activePowerUps.Remove(powerUp);

        PowerUpType type = GetPowerUpType(powerUp);
        UpdateContextActiveState(type, false);

        GameEvents.RaisePowerUpExpired(type);
    }

    void ActivatePowerUpInternal(PowerUpBase powerUp)
    {
        if (!activePowerUps.Contains(powerUp))
            activePowerUps.Add(powerUp);

        powerUp.Activate(context);

        PowerUpType type = GetPowerUpType(powerUp);
        UpdateContextActiveState(type, true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PowerUpManager] Power-up {type} activado");
#endif

        GameEvents.RaisePowerUpActivated(type, 1);
    }

    public bool IsPowerUpActive(PowerUpType type)
    {
        PowerUpBase powerUpRef = GetPowerUpReference(type);
        return powerUpRef != null && activePowerUps.Contains(powerUpRef);
    }

    public int GetRemainingUses(PowerUpType type)
    {
        PowerUpBase powerUpRef = GetPowerUpReference(type);
        if (powerUpRef == null) return 0;
        return activePowerUps.Contains(powerUpRef) ? 1 : 0;
    }

    public bool TryGetAnyActivePowerUp(out PowerUpBase powerUp, out int remainingUses)
    {
        if (activePowerUps.Count > 0)
        {
            powerUp = activePowerUps[0];
            remainingUses = 1;
            return true;
        }

        powerUp = null;
        remainingUses = 0;
        return false;
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
        foreach (var powerUp in activePowerUps)
        {
            PowerUpType type = GetPowerUpType(powerUp);
            availablePowerUps.Add(new PowerUpInfo
            {
                type = type,
                quantity = 1,
                usesRemaining = 1
            });
        }
    }

    public string GetActivePowerUpsString()
    {
        if (activePowerUps.Count == 0) return "";
        var names = new List<string>(activePowerUps.Count);
        foreach (var pu in activePowerUps)
            names.Add(GetPowerUpType(pu).ToString());
        return string.Join(",", names);
    }

    public void ReloadFromSave()
    {
        activePowerUps.Clear();
        context = new PowerUpContext();
        RefreshActivePowerUpState();
    }

    public void RefreshActivePowerUpState()
    {
        foreach (var powerUp in activePowerUps)
        {
            PowerUpType type = GetPowerUpType(powerUp);
            GameEvents.RaisePowerUpUsesUpdated(type, 1);
        }
    }

    private void ClearRuntimePowerUpsForAttempt()
    {
        for (int i = activePowerUps.Count - 1; i >= 0; i--)
        {
            PowerUpBase powerUp = activePowerUps[i];
            powerUp.Deactivate(context);
            activePowerUps.RemoveAt(i);

            PowerUpType type = GetPowerUpType(powerUp);
            UpdateContextActiveState(type, false);
            GameEvents.RaisePowerUpExpired(type);
        }
    }

    private void OnLevelEndedConsumePowerUps()
    {
        ClearRuntimePowerUpsForAttempt();
        _isInLevel = false;
    }

    #region DEBUG

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
        if (SaveManager.Instance == null) return;

        var gameData = SaveManager.Instance.GetGameData();
        gameData.powerUpInventory.Clear();
        SaveManager.Instance.SaveData();
    }

    private void AddPowerUpToInventory(PowerUpType powerUpType, int quantity)
    {
        if (AutoSaveManager.Instance != null)
            AutoSaveManager.Instance.OnPowerUpObtained(powerUpType, quantity);
        else if (SaveManager.Instance != null)
            SaveManager.Instance.AddPowerUpToInventory(powerUpType, quantity);
    }

    #endregion
}
