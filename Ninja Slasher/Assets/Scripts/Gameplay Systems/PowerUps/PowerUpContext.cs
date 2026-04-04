using System.Collections.Generic;
using System.Linq;

public class PowerUpContext
{
    public bool ExtraTimeActive;
    public float ExtraTimePercent;
    public int ExtraTimeUsesRemaining;

    public bool DashTurboActive;
    public float DashCooldownMultiplier = 1f;
    public int DashTurboUsesRemaining;

    public bool ParryPerfectActive;
    public float ParryBonusWindow = 0f;
    public int ParryPerfectUsesRemaining;

    public bool ComboMasterActive;
    public float ComboBonusPercent;
    public int ComboMasterUsesRemaining;

    public bool SecondChanceActive;
    public int SecondChanceUsesRemaining;

    public bool HawkVisionActive;
    public int HawkVisionUsesRemaining;

    public bool EnhancedParryActive;
    public int EnhancedParryBounces = 3;
    public float EnhancedParryVelocityRetention = 0.9f;
    public int EnhancedParryUsesRemaining;

    public bool AnyPowerUpActive()
    {
        return ExtraTimeActive ||
               DashTurboActive ||
               ParryPerfectActive ||
               ComboMasterActive ||
               SecondChanceActive ||
               HawkVisionActive ||
               EnhancedParryActive;
    }

    public int GetLowestRemainingUses()
    {
        var uses = new List<int>();

        if (ExtraTimeActive) uses.Add(ExtraTimeUsesRemaining);
        if (DashTurboActive) uses.Add(DashTurboUsesRemaining);
        if (ParryPerfectActive) uses.Add(ParryPerfectUsesRemaining);
        if (ComboMasterActive) uses.Add(ComboMasterUsesRemaining);
        if (SecondChanceActive) uses.Add(SecondChanceUsesRemaining);
        if (HawkVisionActive) uses.Add(HawkVisionUsesRemaining);
        if (EnhancedParryActive) uses.Add(EnhancedParryUsesRemaining);

        return uses.Count > 0 ? uses.Min() : 0;
    }

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
        if (HawkVisionActive) names.Add("Vision de halcon");
        if (EnhancedParryActive) names.Add("Parry potenciado");

        return string.Join(", ", names);
    }
}
