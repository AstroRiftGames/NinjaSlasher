using System.Collections.Generic;

public class PowerUpContext
{
    public bool ExtraTimeActive;
    public float ExtraTimePercent;

    public bool DashTurboActive;
    public float DashSpeedMultiplier = 1f;

    public bool ParryPerfectActive;
    public float ParryBonusWindow = 0f;

    public bool ComboMasterActive;
    public float ComboBonusPercent;

    public bool SecondChanceActive;

    public bool HawkVisionActive;
    public float HawkVisionInitialTimeScale = 1f;
    public float HawkVisionInitialSlowDuration = 0f;
    public float HawkVisionTrajectoryMaxDistance = 100f;

    public bool EnhancedParryActive;
    public int EnhancedParryBounces = 0;
    public float EnhancedParryVelocityRetention = 1f;

    public int GetActivePowerUpsCount()
    {
        int count = 0;
        if (ExtraTimeActive) count++;
        if (DashTurboActive) count++;
        if (ParryPerfectActive) count++;
        if (ComboMasterActive) count++;
        if (SecondChanceActive) count++;
        if (HawkVisionActive) count++;
        if (EnhancedParryActive) count++;
        return count;
    }

    public string GetActivePowerUpsNames()
    {
        var names = new List<string>();

        if (ExtraTimeActive) names.Add("Tiempo extra");
        if (DashTurboActive) names.Add("Turbo de dash");
        if (ParryPerfectActive) names.Add("Parry perfecto");
        if (ComboMasterActive) names.Add("Maestro del combo");
        if (SecondChanceActive) names.Add("Segunda oportunidad");
        if (HawkVisionActive) names.Add("Ojo de Halcón");
        if (EnhancedParryActive) names.Add("Parry potenciado");

        return string.Join(", ", names);
    }
}
