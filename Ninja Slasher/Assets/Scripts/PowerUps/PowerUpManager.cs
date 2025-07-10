using System;
using System.Collections.Generic;
using UnityEngine;

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
    public List<PowerUpBase> activePowerUps = new List<PowerUpBase>();
    public PowerUpContext context = new PowerUpContext();

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
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            var (pu, timeLeft) = _timers[i];
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0)
            {
                pu.Deactivate(context);
                _timers.RemoveAt(i);
            }
            else
            {
                _timers[i] = (pu, timeLeft);
            }
        }

        if (useExtraTime != _wasExtraTime)
        {
            if (useExtraTime)
                ActivatePowerUp(powerUpExtraTime);
            else
            {
                powerUpExtraTime.Deactivate(context);
                activePowerUps.Remove(powerUpExtraTime);
            }
            _wasExtraTime = useExtraTime;
        }

        if (useDashTurbo != _wasDashTurbo)
        {
            if (useDashTurbo)
                ActivatePowerUp(powerUpDashTurbo);
            else
            {
                powerUpDashTurbo.Deactivate(context);
                activePowerUps.Remove(powerUpDashTurbo);
            }
            _wasDashTurbo = useDashTurbo;
        }

        if (useParryPerfect != _wasParryPerfect)
        {
            if (useParryPerfect)
                ActivatePowerUp(powerUpParryPerfect);
            else
            {
                powerUpParryPerfect.Deactivate(context);
                activePowerUps.Remove(powerUpParryPerfect);
            }
            _wasParryPerfect = useParryPerfect;
        }

        if (useComboMaster != _wasComboMaster)
        {
            if (useComboMaster)
                ActivatePowerUp(powerUpComboMaster);
            else
            {
                powerUpComboMaster.Deactivate(context);
                activePowerUps.Remove(powerUpComboMaster);
            }
            _wasComboMaster = useComboMaster;
        }

        if (useSecondChance != _wasSecondChance)
        {
            if (useSecondChance)
                ActivatePowerUp(powerUpSecondChance);
            else
            {
                powerUpSecondChance.Deactivate(context);
                activePowerUps.Remove(powerUpSecondChance);
            }
            _wasSecondChance = useSecondChance;
        }

#if UNITY_EDITOR
        if (Application.isPlaying)
            UpdateDebugInfo();
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Activar Todos los PowerUps")]
    public void ActivateAll()
    {
        foreach (var pu in activePowerUps)
        {
            pu.Activate(context);
        }
    }

    [ContextMenu("Desactivar Todos los PowerUps")]
    public void DeactivateAll()
    {
        foreach (var pu in activePowerUps)
        {
            pu.Deactivate(context);
        }
    }
#endif

    public void ActivatePowerUp(PowerUpBase powerUp)
    {
        if (!activePowerUps.Contains(powerUp))
            activePowerUps.Add(powerUp);
        powerUp.Activate(context);
        _timers.Add((powerUp, powerUp.duration));
    }

    void LoadActivePowerUpsFromGameData()
    {
        var gameData = SaveManager.Instance.GetGameData();
        var currentTime = DateTime.Now;

        foreach (var powerUpData in gameData.activePowerUps)
        {
            var timeElapsed = (currentTime - powerUpData.activationTime).TotalSeconds;
            if (timeElapsed < powerUpData.duration)
            {
                switch (powerUpData.type)
                {
                    case PowerUpType.ExtraTime:
                        useExtraTime = true;
                        break;
                    case PowerUpType.DashTurbo:
                        useDashTurbo = true;
                        break;
                    case PowerUpType.ParryPerfect:
                        useParryPerfect = true;
                        break;
                    case PowerUpType.ComboMaster:
                        useComboMaster = true;
                        break;
                    case PowerUpType.SecondChance:
                        useSecondChance = true;
                        break;
                }
            }
        }

        gameData.activePowerUps.RemoveAll(pu =>
            (currentTime - pu.activationTime).TotalSeconds >= pu.duration);
        SaveManager.Instance.SaveData();
    }

    void UpdateDebugInfo()
    {
        availablePowerUps.Clear();
        var gameData = SaveManager.Instance.GetGameData();
        var currentTime = DateTime.Now;

        var powerUpCounts = new Dictionary<PowerUpType, System.Collections.Generic.List<PowerUpData>>();

        foreach (var powerUp in gameData.activePowerUps)
        {
            var timeElapsed = (currentTime - powerUp.activationTime).TotalSeconds;
            if (timeElapsed < powerUp.duration)
            {
                if (!powerUpCounts.ContainsKey(powerUp.type))
                    powerUpCounts[powerUp.type] = new System.Collections.Generic.List<PowerUpData>();

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
}
