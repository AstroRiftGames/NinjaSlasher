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

    public bool TrajectoryGuideActive;
    public int TrajectoryGuideUsesRemaining;

    public bool EnhancedParryActive;
    public int EnhancedParryBounces = 3;
    public float EnhancedParryVelocityRetention = 0.9f;
    public int EnhancedParryUsesRemaining;

    public bool HawkVisionActive;
    public int HawkVisionUsesRemaining;

    public bool AnyPowerUpActive()
    {
        return ExtraTimeActive ||
               DashTurboActive ||
               ParryPerfectActive ||
               ComboMasterActive ||
               SecondChanceActive ||
               TrajectoryGuideActive ||
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
        if (TrajectoryGuideActive) uses.Add(TrajectoryGuideUsesRemaining);
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
        if (TrajectoryGuideActive) count++;
        if (EnhancedParryActive) count++;
        return count;
    }

    public string GetActivePowerUpsNames()
    {
        var names = new List<string>();

        if (ExtraTimeActive) names.Add("Extra Time");
        if (DashTurboActive) names.Add("Dash Turbo");
        if (ParryPerfectActive) names.Add("Parry Perfect");
        if (ComboMasterActive) names.Add("Combo Master");
        if (SecondChanceActive) names.Add("Second Chance");
        if (TrajectoryGuideActive) names.Add("Trajectory Guide");
        if (EnhancedParryActive) names.Add("Enhanced Parry");

        return string.Join(", ", names);
    }
}