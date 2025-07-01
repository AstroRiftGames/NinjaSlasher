using System;
using System.Collections.Generic;
using UnityEngine;

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

    private void Start()
    {
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
}
