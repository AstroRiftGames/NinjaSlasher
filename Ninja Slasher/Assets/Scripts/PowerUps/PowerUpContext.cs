using System.Collections.Generic;
using System.Linq;

public class PowerUpContext
{
    public bool ExtraTimeActive;
    public float ExtraTimePercent;
    public float ExtraTimeRemaining;

    public bool DashTurboActive;
    public float DashCooldownMultiplier = 1f;
    public float DashTurboRemaining;

    public bool ParryPerfectActive;
    public float ParryBonusWindow = 0f;
    public float ParryPerfectRemaining;

    public bool ComboMasterActive;
    public float ComboBonusPercent;
    public float ComboMasterRemaining;

    public bool SecondChanceActive;
    public float SecondChanceRemaining;

    public bool TrajectoryGuideActive;
    public float TrajectoryGuideRemaining;

    public bool EnhancedParryActive;
    public int EnhancedParryBounces = 3;
    public float EnhancedParryVelocityRetention = 0.9f;
    public float EnhancedParryRemaining;

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

    public float GetLowestRemainingTime()
    {
        var times = new List<float>();

        if (ExtraTimeActive) times.Add(ExtraTimeRemaining);
        if (DashTurboActive) times.Add(DashTurboRemaining);
        if (ParryPerfectActive) times.Add(ParryPerfectRemaining);
        if (ComboMasterActive) times.Add(ComboMasterRemaining);
        if (SecondChanceActive) times.Add(SecondChanceRemaining);
        if (TrajectoryGuideActive) times.Add(TrajectoryGuideRemaining);
        if (EnhancedParryActive) times.Add(EnhancedParryRemaining);

        return times.Count > 0 ? times.Min() : 0f;
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