using UnityEngine;

[CreateAssetMenu(fileName = "PlatformsBroken", menuName = "Game/Objectives/Platforms Broken")]
public class PlatformsBrokenObjective : ObjectiveData
{
    [Header("PLATFORMS BROKEN")]
    [Tooltip("Numero de plataformas rompibles que deben destruirse")]
    public int requiredCount = 1;

    public override bool IsCompleted(LevelStats stats, LevelContext context)
    {
        return stats.platformsBroken >= requiredCount;
    }

    public override bool CanBeEvaluated(LevelStats stats, LevelContext context)
    {
        return stats.totalBreakablePlatforms > 0;
    }

    public override float GetProgress(LevelStats stats, LevelContext context)
    {
        if (requiredCount <= 0) return 1f;
        return Mathf.Clamp01((float)stats.platformsBroken / requiredCount);
    }
}
