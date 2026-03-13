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
public class PowerUpInfo
{
    public PowerUpType type;
    public int quantity;
    public int usesRemaining;
}

public class PowerUpManager : MonoBehaviourSingleton<PowerUpManager>
{
    [SerializeField] private Image _puIconActive;
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

    private List<(PowerUpBase powerUp, int usesRemaining)> _activeUsages = new List<(PowerUpBase, int)>();

    [Header("DEBUG")]
    [SerializeField] private List<PowerUpInfo> availablePowerUps = new List<PowerUpInfo>();

    void Start()
    {
        LoadActivePowerUpsFromGameData();
    }

    void OnEnable()
    {
        GameEvents.OnLevelEndedConsumePowerUps += OnLevelEndedConsumePowerUps;
    }

    void OnDisable()
    {
        GameEvents.OnLevelEndedConsumePowerUps -= OnLevelEndedConsumePowerUps;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            UpdateDebugInfo();
#endif
    }

    public void ConsumePowerUpUse(PowerUpType powerUpType)
    {
        PowerUpBase powerUpRef = GetPowerUpReference(powerUpType);
        if (powerUpRef == null || !activePowerUps.Contains(powerUpRef))
        {
            return;
        }

        int index = _activeUsages.FindIndex(x => x.powerUp == powerUpRef);
        if (index == -1) return;

        var (pu, usesRemaining) = _activeUsages[index];
        usesRemaining--;

        UpdateContextRemainingUses(powerUpType, usesRemaining);

        pu.OnUseConsumed(context);

        GameEvents.RaisePowerUpUsesUpdated(powerUpType, usesRemaining);

        if (usesRemaining <= 0)
        {
            DeactivatePowerUpInternal(pu);
            _activeUsages.RemoveAt(index);
        }
        else
        {
            _activeUsages[index] = (pu, usesRemaining);
            SaveActivePowerUpUsage(powerUpType, usesRemaining);
        }
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

    private void UpdateContextRemainingUses(PowerUpType type, int usesRemaining)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime:
                context.ExtraTimeUsesRemaining = usesRemaining;
                break;
            case PowerUpType.DashTurbo:
                context.DashTurboUsesRemaining = usesRemaining;
                break;
            case PowerUpType.ParryPerfect:
                context.ParryPerfectUsesRemaining = usesRemaining;
                break;
            case PowerUpType.ComboMaster:
                context.ComboMasterUsesRemaining = usesRemaining;
                break;
            case PowerUpType.SecondChance:
                context.SecondChanceUsesRemaining = usesRemaining;
                break;
            case PowerUpType.HawkVision:
                context.HawkVisionUsesRemaining = usesRemaining;
                break;
            case PowerUpType.EnhancedParry:
                context.EnhancedParryUsesRemaining = usesRemaining;
                break;
        }
    }

    public void ActivatePowerUp(PowerUpBase powerUp)
    {
        if (!activePowerUps.Contains(powerUp))
            activePowerUps.Add(powerUp);

        powerUp.Activate(context);
        _activeUsages.Add((powerUp, powerUp.maxUses));
        _puIconActive.enabled = true;
        _puIconActive.sprite = powerUp.icon;

        PowerUpType type = GetPowerUpType(powerUp);
        UpdateContextRemainingUses(type, powerUp.maxUses);

        GameEvents.RaisePowerUpUsesUpdated(type, powerUp.maxUses);
    }

    public bool ActivatePowerUpFromInventory(PowerUpType powerUpType)
    {
        var gameData = SaveManager.Instance.GetGameData();
        var inventoryItem = gameData.powerUpInventory.Find(item => item.type == powerUpType);

        if (inventoryItem == null || inventoryItem.quantity <= 0)
        {
            return false;
        }

        PowerUpBase powerUpToActivate = GetPowerUpReference(powerUpType);
        if (powerUpToActivate != null)
        {
            ActivatePowerUpInternal(powerUpToActivate);
        }

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpActivated(powerUpType, powerUpToActivate.maxUses);
        }
        else
        {
            ActivatePowerUpDirect(powerUpType, powerUpToActivate.maxUses);
        }

        return true;
    }

    void LoadActivePowerUpsFromGameData()
    {
        var gameData = SaveManager.Instance.GetGameData();

        foreach (var powerUpData in gameData.activePowerUps)
        {
            if (powerUpData.usesRemaining > 0)
            {
                PowerUpBase powerUpRef = GetPowerUpReference(powerUpData.type);
                if (powerUpRef != null)
                {
                    ActivatePowerUpInternal(powerUpRef, powerUpData.usesRemaining);
                }
            }
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
        {
            DeactivatePowerUpInternal(powerUpToDeactivate);
            _activeUsages.RemoveAll(usage => usage.powerUp == powerUpToDeactivate);
        }
    }

    void DeactivatePowerUpInternal(PowerUpBase powerUp)
    {
        powerUp.Deactivate(context);
        activePowerUps.Remove(powerUp);

        PowerUpType type = GetPowerUpType(powerUp);
        UpdateContextActiveState(type, false);
        UpdateContextRemainingUses(type, 0);

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpDeactivated(type);
        }
        else
        {
            SaveManager.Instance.DeactivatePowerUp(type);
        }

        GameEvents.RaisePowerUpExpired(type);

        if (activePowerUps.Count == 0 && _puIconActive != null)
        {
            _puIconActive.enabled = false;
        }
    }

    void ActivatePowerUpInternal(PowerUpBase powerUp, int uses = -1)
    {
        int usesToSet = uses > 0 ? uses : powerUp.maxUses;

        if (!activePowerUps.Contains(powerUp))
            activePowerUps.Add(powerUp);

        powerUp.Activate(context);

        int usageIndex = _activeUsages.FindIndex(usage => usage.powerUp == powerUp);

        if (usageIndex != -1)
        {
            var (pu, currentUses) = _activeUsages[usageIndex];
            int totalUses = currentUses + usesToSet;
            _activeUsages[usageIndex] = (pu, totalUses);
            usesToSet = totalUses;
        }
        else
        {
            _activeUsages.Add((powerUp, usesToSet));
        }

        PowerUpType type = GetPowerUpType(powerUp);

        UpdateContextActiveState(type, true);
        UpdateContextRemainingUses(type, usesToSet);

        Debug.Log($"[PowerUpManager] Power-up {type} activado - Usos: {usesToSet}");

        GameEvents.RaisePowerUpActivated(type, usesToSet);

        if (_puIconActive != null)
        {
            _puIconActive.enabled = true;
            _puIconActive.sprite = powerUp.icon;
        }

        GameEvents.RaisePowerUpUsesUpdated(type, usesToSet);
    }

    void ActivatePowerUpDirect(PowerUpType powerUpType, int uses)
    {
        SaveManager.Instance.RemovePowerUpFromInventory(powerUpType, 1);
        SaveManager.Instance.ActivatePowerUp(powerUpType, uses);
    }

    void SaveActivePowerUpUsage(PowerUpType powerUpType, int usesRemaining)
    {
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnPowerUpUsesUpdated(powerUpType, usesRemaining);
        }
        else
        {
            SaveManager.Instance.UpdatePowerUpUses(powerUpType, usesRemaining);
        }
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

        var usage = _activeUsages.Find(u => u.powerUp == powerUpRef);
        return usage.powerUp != null ? usage.usesRemaining : 0;
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

        foreach (var powerUp in gameData.activePowerUps)
        {
            if (powerUp.usesRemaining > 0)
            {
                availablePowerUps.Add(new PowerUpInfo
                {
                    type = powerUp.type,
                    quantity = 1,
                    usesRemaining = powerUp.usesRemaining
                });
            }
        }
    }

    private void OnLevelEndedConsumePowerUps()
    {
        if (activePowerUps.Count == 0)
        {
            return;
        }

        ConsumeAllActivePowerUps();
    }

    public void ConsumeAllActivePowerUps()
    {
        if (activePowerUps.Count == 0)
        {
            return;
        }

        for (int i = activePowerUps.Count - 1; i >= 0; i--)
        {
            PowerUpType type = GetPowerUpType(activePowerUps[i]);
            ConsumePowerUpUse(type);
        }

        Debug.Log("[PowerUpManager] Consumo completado");
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